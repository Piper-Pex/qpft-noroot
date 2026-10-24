namespace QpEnhance.Core;

/// <summary>
/// Maps a raw per-eye "openness" value (1 - EYES_CLOSED) onto a calibrated 0..1 range.
///
/// Why this matters on Quest Pro: the stock signal rarely reaches a clean 0 (fully
/// closed) or 1 (fully open), so with a 1:1 mapping avatars look permanently
/// half-lidded and blinks never fully close. We remap the user's real
/// "closed point" and "open point" onto 0..1, with an optional gamma curve, and can
/// slowly widen the range automatically as more extreme samples are seen.
/// </summary>
public sealed class OpennessCalibrator
{
    private float _closedPoint;
    private float _openPoint;
    private float _gamma;

    private readonly bool _auto;
    // Auto-tracked extremes, seeded from the configured points.
    private float _obsMin;
    private float _obsMax;

    public OpennessCalibrator(float closedPoint, float openPoint, float gamma = 1.0f, bool auto = false)
    {
        SetRange(closedPoint, openPoint);
        _gamma = gamma > 0 ? gamma : 1.0f;
        _auto = auto;
        _obsMin = _closedPoint;
        _obsMax = _openPoint;
    }

    /// <param name="closedPoint">Openness value that should map to 0 (fully closed).</param>
    /// <param name="openPoint">Openness value that should map to 1 (fully open).</param>
    public void SetRange(float closedPoint, float openPoint)
    {
        // Keep closed strictly below open so the normaliser never divides by ~0.
        _closedPoint = Math.Clamp(closedPoint, 0f, 1f);
        _openPoint = Math.Clamp(openPoint, 0f, 1f);
        if (_openPoint - _closedPoint < 1e-3f)
        {
            _closedPoint = Math.Max(0f, _openPoint - 1e-3f);
        }
        _obsMin = _closedPoint;
        _obsMax = _openPoint;
    }

    public void SetGamma(float gamma) => _gamma = gamma > 0 ? gamma : 1.0f;

    /// <summary>Map a raw openness value to the calibrated 0..1 output.</summary>
    public float Apply(float openness)
    {
        if (!float.IsFinite(openness))
            return 0f;

        float lo = _closedPoint;
        float hi = _openPoint;

        if (_auto)
        {
            // Fast expansion toward new extremes, slow relaxation back inward so a
            // single noisy sample cannot permanently widen the range.
            const float expand = 0.5f;
            const float relax = 0.0015f;
            _obsMax = openness > _obsMax ? Lerp(_obsMax, openness, expand) : Lerp(_obsMax, openness, relax);
            _obsMin = openness < _obsMin ? Lerp(_obsMin, openness, expand) : Lerp(_obsMin, openness, relax);
            lo = _obsMin;
            hi = _obsMax;
            if (hi - lo < 1e-3f)
                return Math.Clamp(openness, 0f, 1f);
        }

        float t = Math.Clamp((openness - lo) / (hi - lo), 0f, 1f);
        return _gamma == 1.0f ? t : MathF.Pow(t, _gamma);
    }

    private static float Lerp(float a, float b, float k) => a + (b - a) * k;
}
