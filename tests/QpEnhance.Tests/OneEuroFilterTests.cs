using QpEnhance.Core;

namespace QpEnhance.Tests;

public static class OneEuroFilterTests
{
    public static void Run(TestRunner t)
    {
        t.Section("OneEuroFilter");

        // First sample passes through unchanged.
        var f = new OneEuroFilter(1.0, 0.0, 1.0);
        t.Near("first sample passthrough", f.Filter(0.5, 0.0), 0.5);

        // A constant signal converges to that constant.
        var fConst = new OneEuroFilter(1.0, 0.1, 1.0);
        double y = 0;
        double tt = 0;
        for (int i = 0; i < 200; i++)
        {
            tt += 1.0 / 72.0;
            y = fConst.Filter(1.0, tt);
        }
        t.Near("constant converges", y, 1.0, 1e-2);

        // Noise is attenuated: filtered variance < raw variance around a fixed mean.
        var fNoise = new OneEuroFilter(0.8, 0.0, 1.0);
        var rng = new Random(1234);
        double rawSumSq = 0, filtSumSq = 0;
        double time = 0;
        int n = 0;
        for (int i = 0; i < 400; i++)
        {
            time += 1.0 / 72.0;
            double noise = (rng.NextDouble() - 0.5) * 0.4; // +/-0.2 around 0
            double raw = 0.5 + noise;
            double filt = fNoise.Filter(raw, time);
            if (i > 50) // let it settle
            {
                rawSumSq += (raw - 0.5) * (raw - 0.5);
                filtSumSq += (filt - 0.5) * (filt - 0.5);
                n++;
            }
        }
        t.Less("noise attenuated", filtSumSq / n, rawSumSq / n);

        // Non-finite input does not corrupt state.
        var fNan = new OneEuroFilter();
        fNan.Filter(0.3, 0.0);
        double afterNan = fNan.Filter(double.NaN, 0.1);
        t.True("NaN handled", double.IsFinite(afterNan));
    }
}
