using QpEnhance.Core;

namespace QpEnhance.Tests;

public static class CheekProcessorTests
{
    public static void Run(TestRunner t)
    {
        t.Section("CheekProcessor");

        // Zero in, zero out.
        var (zl, zr) = CheekProcessor.Separate(0f, 0f);
        t.Near("zero left", zl, 0.0);
        t.Near("zero right", zr, 0.0);

        // Symmetric puff keeps both cheeks near the shared value.
        var (bl, br) = CheekProcessor.Separate(0.6f, 0.6f);
        t.Greater("symmetric keeps left", bl, 0.4);
        t.Greater("symmetric keeps right", br, 0.4);
        t.Near("symmetric stays balanced", Math.Abs(bl - br), 0.0, 1e-3);

        // Strongly one-sided puff suppresses the opposite (leaking) cheek.
        var (ol, or) = CheekProcessor.Separate(0.8f, 0.15f);
        t.Greater("one-sided keeps active side", ol, 0.5);
        t.Less("one-sided suppresses other side", or, ol * 0.5);

        // Strong mode pushes a near-tie toward a cleaner result than balanced mode.
        var (nl, _) = CheekProcessor.Separate(0.5f, 0.42f, strong: false);
        var (sl, _) = CheekProcessor.Separate(0.5f, 0.42f, strong: true);
        t.Greater("strong raises the leading side", sl, nl);

        // Output never leaves 0..1.
        var (cl, cr) = CheekProcessor.Separate(5f, -3f);
        t.True("left clamped", cl is >= 0f and <= 1f);
        t.True("right clamped", cr is >= 0f and <= 1f);
    }
}
