namespace QpEnhance.Core;

/// <summary>
/// The "1€ filter" (Casiez, Roussel, Vogel, 2012): an adaptive low-pass filter that
/// trades latency against jitter based on signal speed. When the signal is still it
/// smooths hard (kills tracker jitter); when the signal moves fast it lets through
/// (keeps gaze responsive). This is the right tool for eye-gaze smoothing, which is
/// our main un-rooted improvement over the raw Quest Pro signal.
///
/// Timestamps are in seconds. The filter is stateful; use one instance per scalar
/// channel (e.g. one each for left-eye X, left-eye Y, ...).
/// </summary>
public sealed class OneEuroFilter
{
    private double _minCutoff;
    private double _beta;
    private double _dCutoff;

    private double _xPrev;
    private double _dxPrev;
    private double _tPrev;
    private bool _primed;

    /// <param name="minCutoff">Minimum cutoff frequency (Hz). Lower = smoother at rest. Must be &gt; 0.</param>
    /// <param name="beta">Speed coefficient. Higher = less lag when moving fast.</param>
    /// <param name="dCutoff">Cutoff for the derivative. 1.0 is a good default.</param>
    public OneEuroFilter(double minCutoff = 1.0, double beta = 0.0, double dCutoff = 1.0)
    {
        SetParameters(minCutoff, beta, dCutoff);
    }

    public void SetParameters(double minCutoff, double beta, double dCutoff)
    {
        _minCutoff = minCutoff > 0 ? minCutoff : 1e-3;
        _beta = beta >= 0 ? beta : 0;
        _dCutoff = dCutoff > 0 ? dCutoff : 1e-3;
    }

    /// <summary>Forget all history; the next sample is passed through unchanged.</summary>
    public void Reset()
    {
        _primed = false;
        _xPrev = 0;
        _dxPrev = 0;
        _tPrev = 0;
    }

    /// <summary>Feed one sample at time <paramref name="t"/> (seconds); returns the filtered value.</summary>
    public double Filter(double x, double t)
    {
        if (!double.IsFinite(x))
            return _primed ? _xPrev : 0;

        if (!_primed)
        {
            _primed = true;
            _xPrev = x;
            _dxPrev = 0;
            _tPrev = t;
            return x;
        }

        double dt = t - _tPrev;
        if (!(dt > 0))
            dt = 1.0 / 72.0; // fall back to a sane frame time if timestamps stall or go backwards
        _tPrev = t;

        // Filter the derivative, then use its magnitude to raise the cutoff.
        double dx = (x - _xPrev) / dt;
        double aD = Alpha(_dCutoff, dt);
        double dxHat = aD * dx + (1 - aD) * _dxPrev;

        double cutoff = _minCutoff + _beta * Math.Abs(dxHat);
        double aX = Alpha(cutoff, dt);
        double xHat = aX * x + (1 - aX) * _xPrev;

        _xPrev = xHat;
        _dxPrev = dxHat;
        return xHat;
    }

    private static double Alpha(double cutoff, double dt)
    {
        double tau = 1.0 / (2.0 * Math.PI * cutoff);
        return 1.0 / (1.0 + tau / dt);
    }
}
