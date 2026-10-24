namespace QpEnhance.Core;

/// <summary>
/// Separates left/right cheek puff (and suck) into clean one-sided or two-sided poses.
///
/// Problem this solves: on Quest Pro a genuine one-sided cheek puff still raises the
/// opposite cheek's raw weight a little, so avatars "leak" the puff onto both cheeks.
/// A real two-sided puff is close to symmetric, while a one-sided puff keeps a small
/// but consistent lead on the active side. We use the asymmetry between the two raw
/// weights to decide how much shared (bilateral) weight to keep versus how much to
/// route to a single side.
/// </summary>
public static class CheekProcessor
{
    /// <param name="left">Raw left weight (0..1).</param>
    /// <param name="right">Raw right weight (0..1).</param>
    /// <param name="strong">Push uncertain poses harder toward a clean 0/1 result.</param>
    public static (float Left, float Right) Separate(float left, float right, bool strong = false)
    {
        left = Sanitize(left);
        right = Sanitize(right);

        float peak = MathF.Max(left, right);
        if (peak < 0.02f)
            return (0f, 0f);

        float asymmetry = MathF.Abs(left - right) / peak; // 0 = symmetric, 1 = fully one-sided

        // Shared component, faded out as the pose becomes more one-sided.
        float unilateralBlend = SmoothStep(0.12f, 0.32f, asymmetry);
        float bilateral = MathF.Min(left, right) * (1f - unilateralBlend);

        // Side-exclusive component: the lead one side has over the other, with a small
        // dead-zone so sensor noise near symmetry does not flicker between sides.
        float leftOnly = MathF.Max(0f, (left - right - 0.015f)) * (1f + unilateralBlend);
        float rightOnly = MathF.Max(0f, (right - left - 0.015f)) * (1f + unilateralBlend);

        float outL = Math.Clamp(MathF.Max(bilateral, leftOnly), 0f, 1f);
        float outR = Math.Clamp(MathF.Max(bilateral, rightOnly), 0f, 1f);

        if (strong)
        {
            // Increase contrast so near-tied poses resolve instead of sitting at 0.5/0.5.
            outL = Math.Clamp((outL - 0.03f) * 1.6f, 0f, 1f);
            outR = Math.Clamp((outR - 0.03f) * 1.6f, 0f, 1f);
        }

        return (outL, outR);
    }

    private static float Sanitize(float v) => float.IsFinite(v) ? Math.Clamp(v, 0f, 1f) : 0f;

    private static float SmoothStep(float low, float high, float value)
    {
        float t = Math.Clamp((value - low) / (high - low), 0f, 1f);
        return t * t * (3f - 2f * t);
    }
}
