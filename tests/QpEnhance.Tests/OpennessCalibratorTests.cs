using QpEnhance.Core;

namespace QpEnhance.Tests;

public static class OpennessCalibratorTests
{
    public static void Run(TestRunner t)
    {
        t.Section("OpennessCalibrator");

        // Range [0.15 .. 0.90] maps endpoints to 0 and 1, midpoint to ~0.5.
        var c = new OpennessCalibrator(0.15f, 0.90f, 1.0f);
        t.Near("closed point -> 0", c.Apply(0.15f), 0.0);
        t.Near("open point -> 1", c.Apply(0.90f), 1.0);
        t.Near("midpoint -> 0.5", c.Apply(0.525f), 0.5, 1e-3);

        // Values outside the range clamp.
        t.Near("below closed clamps to 0", c.Apply(0.0f), 0.0);
        t.Near("above open clamps to 1", c.Apply(1.0f), 1.0);

        // Gamma > 1 keeps the eye wider (output >= linear for mid inputs).
        var g = new OpennessCalibrator(0.0f, 1.0f, 2.0f);
        t.Near("gamma squares the midpoint", g.Apply(0.5f), 0.25, 1e-3);

        // Degenerate range does not divide by zero.
        var d = new OpennessCalibrator(0.5f, 0.5f, 1.0f);
        t.True("degenerate range is finite", float.IsFinite(d.Apply(0.5f)));

        // Auto mode widens toward observed extremes so a full blink reaches ~0.
        var a = new OpennessCalibrator(0.4f, 0.6f, 1.0f, auto: true);
        for (int i = 0; i < 50; i++)
        {
            a.Apply(0.0f); // fully closed samples
            a.Apply(1.0f); // fully open samples
        }
        t.Less("auto drives closed low", a.Apply(0.0f), 0.1);
        t.Greater("auto drives open high", a.Apply(1.0f), 0.9);
    }
}
