namespace QpEnhance.Core;

/// <summary>
/// Scales eyebrow weights by a user sensitivity. Quest Pro brow movement is subtle
/// and many avatars barely show it; a gain (typically 1.0–2.0) makes raised/lowered
/// brows read clearly without the user having to exaggerate the expression.
/// </summary>
public static class EyebrowProcessor
{
    /// <summary>Apply sensitivity to a single brow weight, clamped to 0..1.</summary>
    public static float Apply(float raw, float sensitivity)
    {
        if (!float.IsFinite(raw))
            return 0f;
        if (!float.IsFinite(sensitivity) || sensitivity < 0f)
            sensitivity = 1.0f;
        return Math.Clamp(raw * sensitivity, 0f, 1f);
    }
}
