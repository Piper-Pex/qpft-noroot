namespace QpEnhance.Core;

/// <summary>One raw, unsmoothed per-eye gaze sample. X+ = user's right, Y+ = up.</summary>
public readonly record struct GazeSample(float Lx, float Ly, float Rx, float Ry);

/// <summary>
/// Pure gaze-geometry helpers. Two independent ways to obtain a raw per-eye gaze from
/// the stock (un-rooted) Quest Pro signal:
///
///  * <see cref="FromEyeQuaternion"/> — the headset's real eye-orientation quaternion,
///    which Virtual Desktop publishes per eye. This is the higher-quality source and
///    the module uses it whenever the eye is valid.
///  * <see cref="FromLookShapes"/> — reconstructed from the EYES_LOOK_* blendshapes.
///    A fallback for when the eye pose is missing or when only blendshapes are available.
///
/// The quaternion-to-angle extraction matches the convention Virtual Desktop/VRCFT
/// expect, so the result drops straight into VRCFaceTracking's <c>Eye.*.Gaze</c>.
/// </summary>
public static class GazeMath
{
    /// <summary>Convert a (normalised or not) eye-orientation quaternion to a 2D gaze angle in radians.</summary>
    public static (float X, float Y) FromEyeQuaternion(float x, float y, float z, float w)
    {
        float length = MathF.Sqrt(x * x + y * y + z * z + w * w);
        if (length <= 1e-6f)
            return (0f, 0f);
        x /= length;
        y /= length;
        z /= length;
        w /= length;

        float horizontal = MathF.Asin(Math.Clamp(2f * (x * z - w * y), -1f, 1f));
        float vertical = MathF.Atan2(2f * (y * z + w * x), w * w - x * x - y * y + z * z);
        return (horizontal, vertical);
    }

    /// <summary>Reconstruct per-eye gaze from the four EYES_LOOK_* blendshapes per eye.</summary>
    public static GazeSample FromLookShapes(ReadOnlySpan<float> w)
    {
        return new GazeSample(
            Axis(w, Fb2.EyesLookRightL, Fb2.EyesLookLeftL),
            Axis(w, Fb2.EyesLookUpL, Fb2.EyesLookDownL),
            Axis(w, Fb2.EyesLookRightR, Fb2.EyesLookLeftR),
            Axis(w, Fb2.EyesLookUpR, Fb2.EyesLookDownR));
    }

    private static float Axis(ReadOnlySpan<float> w, Fb2 positive, Fb2 negative)
        => Get(w, positive) - Get(w, negative);

    private static float Get(ReadOnlySpan<float> w, Fb2 index)
    {
        int i = (int)index;
        if ((uint)i >= (uint)w.Length)
            return 0f;
        float v = w[i];
        return float.IsFinite(v) ? v : 0f;
    }
}
