using QpEnhance.Core;

namespace QpEnhance.Tests;

public static class GazeMathTests
{
    public static void Run(TestRunner t)
    {
        t.Section("GazeMath");

        // Identity orientation looks straight ahead.
        var (ix, iy) = GazeMath.FromEyeQuaternion(0f, 0f, 0f, 1f);
        t.Near("identity X", ix, 0.0);
        t.Near("identity Y", iy, 0.0);

        // Degenerate (zero) quaternion is safe.
        var (zx, zy) = GazeMath.FromEyeQuaternion(0f, 0f, 0f, 0f);
        t.Near("zero-quat X", zx, 0.0);
        t.Near("zero-quat Y", zy, 0.0);

        const float theta = 0.3f;
        float s = MathF.Sin(theta / 2f);
        float c = MathF.Cos(theta / 2f);

        // Pure yaw (rotation about Y) moves the horizontal axis only.
        var (yx, yy) = GazeMath.FromEyeQuaternion(0f, s, 0f, c);
        t.Greater("yaw moves X", Math.Abs(yx), 0.2);
        t.Near("yaw keeps Y flat", yy, 0.0, 1e-3);

        // Pure pitch (rotation about X) moves the vertical axis only.
        var (px, py) = GazeMath.FromEyeQuaternion(s, 0f, 0f, c);
        t.Near("pitch keeps X flat", px, 0.0, 1e-3);
        t.Greater("pitch moves Y", Math.Abs(py), 0.2);

        // Blendshape fallback: looking right/up gives positive left-eye axes.
        var w = new float[Fb2Constants.Count];
        w[(int)Fb2.EyesLookRightL] = 0.7f;
        w[(int)Fb2.EyesLookUpL] = 0.4f;
        var ls = GazeMath.FromLookShapes(w);
        t.Greater("look-shape X positive", ls.Lx, 0.0);
        t.Greater("look-shape Y positive", ls.Ly, 0.0);
    }
}
