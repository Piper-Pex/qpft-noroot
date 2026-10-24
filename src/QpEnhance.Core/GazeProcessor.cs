namespace QpEnhance.Core;

/// <summary>
/// Applies gain and 1€ smoothing to a raw per-eye gaze sample. The derivation of the
/// raw sample (from an eye quaternion or from blendshapes) lives in <see cref="GazeMath"/>;
/// this type only conditions the signal, holding one filter per axis.
///
/// Output convention is preserved from the input: X+ = user's right, Y+ = up.
/// </summary>
public sealed class GazeProcessor
{
    private readonly OneEuroFilter _lx = new();
    private readonly OneEuroFilter _ly = new();
    private readonly OneEuroFilter _rx = new();
    private readonly OneEuroFilter _ry = new();

    private bool _smoothing = true;
    private float _gain = 1.0f;

    public void Configure(bool smoothing, float gain, double minCutoff, double beta, double dCutoff)
    {
        _smoothing = smoothing;
        _gain = gain > 0 ? gain : 1.0f;
        _lx.SetParameters(minCutoff, beta, dCutoff);
        _ly.SetParameters(minCutoff, beta, dCutoff);
        _rx.SetParameters(minCutoff, beta, dCutoff);
        _ry.SetParameters(minCutoff, beta, dCutoff);
    }

    public void Reset()
    {
        _lx.Reset();
        _ly.Reset();
        _rx.Reset();
        _ry.Reset();
    }

    /// <param name="sample">Raw per-eye gaze.</param>
    /// <param name="t">Timestamp in seconds.</param>
    public GazeSample Process(GazeSample sample, double t)
    {
        float lx = Condition(_lx, sample.Lx, t);
        float ly = Condition(_ly, sample.Ly, t);
        float rx = Condition(_rx, sample.Rx, t);
        float ry = Condition(_ry, sample.Ry, t);
        return new GazeSample(lx, ly, rx, ry);
    }

    private float Condition(OneEuroFilter filter, float raw, double t)
    {
        if (!float.IsFinite(raw))
            raw = 0f;
        float gained = Math.Clamp(raw * _gain, -1f, 1f);
        return _smoothing ? (float)filter.Filter(gained, t) : gained;
    }
}
