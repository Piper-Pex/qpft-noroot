namespace QpEnhance.Core;

/// <summary>Processed state for one eye. Gaze X+ = user's right, Y+ = up.</summary>
public readonly record struct EyeState(float GazeX, float GazeY, float Openness, float Squint, bool Valid);

/// <summary>Result of one pipeline pass: both eyes plus the adjusted face weights.</summary>
public readonly record struct EnhanceResult(EyeState Left, EyeState Right);

/// <summary>
/// The enhancement pipeline. Takes a raw 70-weight FB2 frame from the stock headset,
/// applies the un-rooted improvements (gaze smoothing, openness calibration, cheek
/// separation, brow sensitivity) and produces:
///   - calibrated per-eye <see cref="EyeState"/> for gaze/openness/squint, and
///   - an adjusted copy of the 70 face weights for the module to map 1:1.
///
/// This type owns the stateful filters/calibrators, so create one instance per
/// tracking session and call <see cref="Process"/> once per frame. It has no
/// dependency on VRCFaceTracking or the headset, which keeps it fully unit-testable.
/// </summary>
public sealed class EnhancePipeline
{
    private readonly GazeProcessor _gaze = new();
    private readonly OpennessCalibrator _openL;
    private readonly OpennessCalibrator _openR;
    private EnhanceConfig _cfg;

    public EnhancePipeline(EnhanceConfig? config = null)
    {
        _cfg = config ?? new EnhanceConfig();
        _openL = new OpennessCalibrator(_cfg.OpenClosedPoint, _cfg.OpenOpenPoint, _cfg.OpennessGamma, _cfg.OpennessAuto);
        _openR = new OpennessCalibrator(_cfg.OpenClosedPoint, _cfg.OpenOpenPoint, _cfg.OpennessGamma, _cfg.OpennessAuto);
        ApplyConfig();
    }

    public EnhanceConfig Config => _cfg;

    /// <summary>Swap in new settings at runtime (e.g. from a config UI or file watcher).</summary>
    public void UpdateConfig(EnhanceConfig config)
    {
        _cfg = config;
        _openL.SetRange(_cfg.OpenClosedPoint, _cfg.OpenOpenPoint);
        _openL.SetGamma(_cfg.OpennessGamma);
        _openR.SetRange(_cfg.OpenClosedPoint, _cfg.OpenOpenPoint);
        _openR.SetGamma(_cfg.OpennessGamma);
        ApplyConfig();
    }

    private void ApplyConfig() =>
        _gaze.Configure(_cfg.GazeSmoothing, _cfg.GazeGain, _cfg.GazeMinCutoff, _cfg.GazeBeta, _cfg.GazeDCutoff);

    public void Reset() => _gaze.Reset();

    /// <param name="raw">Incoming 70-weight FB2 frame.</param>
    /// <param name="outShapes">
    /// Buffer of at least 70 floats that receives the adjusted face weights. Eye-look
    /// and eye-closed indices are left as received; drive the eyes from the returned
    /// <see cref="EyeState"/> instead.
    /// </param>
    /// <param name="eyeValid">Whether the headset currently has a valid eye signal.</param>
    /// <param name="faceValid">Whether the headset currently has a valid face signal.</param>
    /// <param name="tSeconds">Monotonic timestamp in seconds (for the smoothing filters).</param>
    /// <param name="gaze">
    /// Pre-derived raw gaze (e.g. from the eye-orientation quaternion). When null the
    /// gaze is reconstructed from the EYES_LOOK_* blendshapes in <paramref name="raw"/>.
    /// </param>
    public EnhanceResult Process(
        ReadOnlySpan<float> raw,
        Span<float> outShapes,
        bool eyeValid,
        bool faceValid,
        double tSeconds,
        GazeSample? gaze = null)
    {
        if (raw.Length < Fb2Constants.Count)
            throw new ArgumentException($"Expected {Fb2Constants.Count} weights, got {raw.Length}.", nameof(raw));
        if (outShapes.Length < Fb2Constants.Count)
            throw new ArgumentException($"Output needs {Fb2Constants.Count} slots, got {outShapes.Length}.", nameof(outShapes));

        // Start from a verbatim copy; processors overwrite only the slots they own.
        raw[..Fb2Constants.Count].CopyTo(outShapes);

        if (faceValid)
        {
            if (_cfg.CheekSeparation)
            {
                var (pl, pr) = CheekProcessor.Separate(At(raw, Fb2.CheekPuffL), At(raw, Fb2.CheekPuffR), _cfg.CheekStrong);
                outShapes[(int)Fb2.CheekPuffL] = pl;
                outShapes[(int)Fb2.CheekPuffR] = pr;

                var (sl, sr) = CheekProcessor.Separate(At(raw, Fb2.CheekSuckL), At(raw, Fb2.CheekSuckR), _cfg.CheekStrong);
                outShapes[(int)Fb2.CheekSuckL] = sl;
                outShapes[(int)Fb2.CheekSuckR] = sr;
            }

            if (_cfg.BrowSensitivity != 1.0f)
            {
                Scale(outShapes, Fb2.InnerBrowRaiserL, _cfg.BrowSensitivity);
                Scale(outShapes, Fb2.InnerBrowRaiserR, _cfg.BrowSensitivity);
                Scale(outShapes, Fb2.OuterBrowRaiserL, _cfg.BrowSensitivity);
                Scale(outShapes, Fb2.OuterBrowRaiserR, _cfg.BrowSensitivity);
                Scale(outShapes, Fb2.BrowLowererL, _cfg.BrowSensitivity);
                Scale(outShapes, Fb2.BrowLowererR, _cfg.BrowSensitivity);
            }
        }

        GazeSample rawGaze = gaze ?? GazeMath.FromLookShapes(raw);
        GazeSample g = _gaze.Process(rawGaze, tSeconds);

        float rawOpenL = RawOpenness(At(raw, Fb2.EyesClosedL), At(raw, Fb2.LidTightenerL));
        float rawOpenR = RawOpenness(At(raw, Fb2.EyesClosedR), At(raw, Fb2.LidTightenerR));
        float opennessL = _cfg.OpennessCalibration ? _openL.Apply(rawOpenL) : rawOpenL;
        float opennessR = _cfg.OpennessCalibration ? _openR.Apply(rawOpenR) : rawOpenR;

        float squintL = Math.Clamp(At(raw, Fb2.LidTightenerL) * _cfg.SquintGain, 0f, 1f);
        float squintR = Math.Clamp(At(raw, Fb2.LidTightenerR) * _cfg.SquintGain, 0f, 1f);

        var left = new EyeState(g.Lx, g.Ly, opennessL, squintL, eyeValid);
        var right = new EyeState(g.Rx, g.Ry, opennessR, squintR, eyeValid);
        return new EnhanceResult(left, right);
    }

    // Openness before calibration. Lid-tightener amplifies closure only once the eye is
    // already closing, so a hard squint reads as more closed while a smile (which can
    // tighten the lids) does not force the eye shut. Matches Virtual Desktop's formula.
    private static float RawOpenness(float eyesClosed, float lidTightener)
        => 1f - Math.Clamp(eyesClosed + eyesClosed * lidTightener, 0f, 1f);

    private static float At(ReadOnlySpan<float> w, Fb2 index)
    {
        float v = w[(int)index];
        return float.IsFinite(v) ? v : 0f;
    }

    private static void Scale(Span<float> w, Fb2 index, float gain) =>
        w[(int)index] = EyebrowProcessor.Apply(w[(int)index], gain);
}
