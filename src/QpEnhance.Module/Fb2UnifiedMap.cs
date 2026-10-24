using QpEnhance.Core;
using VRCFaceTracking.Core.Params.Data;
using VRCFaceTracking.Core.Params.Expressions;

namespace QpEnhance.Module;

/// <summary>
/// Maps the (already enhanced) FB2 70-weight frame onto VRCFaceTracking's
/// <see cref="UnifiedExpressions"/> shapes. Eye gaze/openness are written separately by
/// the module from the <see cref="EyeState"/>; this covers the eye-squint/wide shapes
/// and the whole brow/cheek/nose/jaw/mouth/tongue set, so the module is a drop-in
/// replacement for the stock Virtual Desktop module rather than an add-on.
/// </summary>
internal static class Fb2UnifiedMap
{
    public static void Apply(ReadOnlySpan<float> w, UnifiedTrackingData data)
    {
        var s = data.Shapes;

        // --- Eye socket (gaze + openness handled on UnifiedEye, not here) ---
        Set(s, UnifiedExpressions.EyeSquintLeft, w, Fb2.LidTightenerL);
        Set(s, UnifiedExpressions.EyeSquintRight, w, Fb2.LidTightenerR);
        Set(s, UnifiedExpressions.EyeWideLeft, w, Fb2.UpperLidRaiserL);
        Set(s, UnifiedExpressions.EyeWideRight, w, Fb2.UpperLidRaiserR);

        // --- Brows ---
        Set(s, UnifiedExpressions.BrowInnerUpLeft, w, Fb2.InnerBrowRaiserL);
        Set(s, UnifiedExpressions.BrowInnerUpRight, w, Fb2.InnerBrowRaiserR);
        Set(s, UnifiedExpressions.BrowOuterUpLeft, w, Fb2.OuterBrowRaiserL);
        Set(s, UnifiedExpressions.BrowOuterUpRight, w, Fb2.OuterBrowRaiserR);
        // FB's single brow-lowerer drives both the outer lowerer and the inner pinch.
        Set(s, UnifiedExpressions.BrowLowererLeft, w, Fb2.BrowLowererL);
        Set(s, UnifiedExpressions.BrowLowererRight, w, Fb2.BrowLowererR);
        Set(s, UnifiedExpressions.BrowPinchLeft, w, Fb2.BrowLowererL);
        Set(s, UnifiedExpressions.BrowPinchRight, w, Fb2.BrowLowererR);

        // --- Cheeks ---
        Set(s, UnifiedExpressions.CheekSquintLeft, w, Fb2.CheekRaiserL);
        Set(s, UnifiedExpressions.CheekSquintRight, w, Fb2.CheekRaiserR);
        Set(s, UnifiedExpressions.CheekPuffLeft, w, Fb2.CheekPuffL);
        Set(s, UnifiedExpressions.CheekPuffRight, w, Fb2.CheekPuffR);
        Set(s, UnifiedExpressions.CheekSuckLeft, w, Fb2.CheekSuckL);
        Set(s, UnifiedExpressions.CheekSuckRight, w, Fb2.CheekSuckR);

        // --- Nose ---
        Set(s, UnifiedExpressions.NoseSneerLeft, w, Fb2.NoseWrinklerL);
        Set(s, UnifiedExpressions.NoseSneerRight, w, Fb2.NoseWrinklerR);

        // --- Jaw ---
        Set(s, UnifiedExpressions.JawOpen, w, Fb2.JawDrop);
        Set(s, UnifiedExpressions.JawLeft, w, Fb2.JawSidewaysLeft);
        Set(s, UnifiedExpressions.JawRight, w, Fb2.JawSidewaysRight);
        Set(s, UnifiedExpressions.JawForward, w, Fb2.JawThrust);
        Set(s, UnifiedExpressions.MouthClosed, w, Fb2.LipsToward);

        // --- Mouth corners / smile / frown / dimple ---
        Set(s, UnifiedExpressions.MouthCornerPullLeft, w, Fb2.LipCornerPullerL);
        Set(s, UnifiedExpressions.MouthCornerPullRight, w, Fb2.LipCornerPullerR);
        Set(s, UnifiedExpressions.MouthFrownLeft, w, Fb2.LipCornerDepressorL);
        Set(s, UnifiedExpressions.MouthFrownRight, w, Fb2.LipCornerDepressorR);
        Set(s, UnifiedExpressions.MouthDimpleLeft, w, Fb2.DimplerL);
        Set(s, UnifiedExpressions.MouthDimpleRight, w, Fb2.DimplerR);

        // --- Lip raise / lower ---
        Set(s, UnifiedExpressions.MouthUpperUpLeft, w, Fb2.UpperLipRaiserL);
        Set(s, UnifiedExpressions.MouthUpperUpRight, w, Fb2.UpperLipRaiserR);
        Set(s, UnifiedExpressions.MouthLowerDownLeft, w, Fb2.LowerLipDepressorL);
        Set(s, UnifiedExpressions.MouthLowerDownRight, w, Fb2.LowerLipDepressorR);

        // --- Mouth horizontal (FB sends one weight per direction for the whole mouth) ---
        Set(s, UnifiedExpressions.MouthUpperLeft, w, Fb2.MouthLeft);
        Set(s, UnifiedExpressions.MouthLowerLeft, w, Fb2.MouthLeft);
        Set(s, UnifiedExpressions.MouthUpperRight, w, Fb2.MouthRight);
        Set(s, UnifiedExpressions.MouthLowerRight, w, Fb2.MouthRight);

        // --- Stretch / press / tighten ---
        Set(s, UnifiedExpressions.MouthStretchLeft, w, Fb2.LipStretcherL);
        Set(s, UnifiedExpressions.MouthStretchRight, w, Fb2.LipStretcherR);
        Set(s, UnifiedExpressions.MouthPressLeft, w, Fb2.LipPressorL);
        Set(s, UnifiedExpressions.MouthPressRight, w, Fb2.LipPressorR);
        Set(s, UnifiedExpressions.MouthTightenerLeft, w, Fb2.LipTightenerL);
        Set(s, UnifiedExpressions.MouthTightenerRight, w, Fb2.LipTightenerR);

        // --- Chin raiser ---
        Set(s, UnifiedExpressions.MouthRaiserUpper, w, Fb2.ChinRaiserT);
        Set(s, UnifiedExpressions.MouthRaiserLower, w, Fb2.ChinRaiserB);

        // --- Funnel (FB top/bottom per side -> Unified upper/lower per side) ---
        Set(s, UnifiedExpressions.LipFunnelUpperLeft, w, Fb2.LipFunnelerLT);
        Set(s, UnifiedExpressions.LipFunnelLowerLeft, w, Fb2.LipFunnelerLB);
        Set(s, UnifiedExpressions.LipFunnelUpperRight, w, Fb2.LipFunnelerRT);
        Set(s, UnifiedExpressions.LipFunnelLowerRight, w, Fb2.LipFunnelerRB);

        // --- Pucker (FB one per side -> Unified upper/lower per side) ---
        Set(s, UnifiedExpressions.LipPuckerUpperLeft, w, Fb2.LipPuckerL);
        Set(s, UnifiedExpressions.LipPuckerLowerLeft, w, Fb2.LipPuckerL);
        Set(s, UnifiedExpressions.LipPuckerUpperRight, w, Fb2.LipPuckerR);
        Set(s, UnifiedExpressions.LipPuckerLowerRight, w, Fb2.LipPuckerR);

        // --- Lip suck (FB top/bottom per side -> Unified upper/lower per side) ---
        Set(s, UnifiedExpressions.LipSuckUpperLeft, w, Fb2.LipSuckLT);
        Set(s, UnifiedExpressions.LipSuckLowerLeft, w, Fb2.LipSuckLB);
        Set(s, UnifiedExpressions.LipSuckUpperRight, w, Fb2.LipSuckRT);
        Set(s, UnifiedExpressions.LipSuckLowerRight, w, Fb2.LipSuckRB);

        // --- Tongue: only "out" is available without root; the rest stay at zero. ---
        Set(s, UnifiedExpressions.TongueOut, w, Fb2.TongueOut);
    }

    private static void Set(UnifiedExpressionShape[] shapes, UnifiedExpressions target, ReadOnlySpan<float> w, Fb2 source)
    {
        int i = (int)source;
        float v = (uint)i < (uint)w.Length ? w[i] : 0f;
        shapes[(int)target].Weight = float.IsFinite(v) ? v : 0f;
    }
}
