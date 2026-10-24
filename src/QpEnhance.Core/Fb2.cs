namespace QpEnhance.Core;

/// <summary>
/// Index of each weight in the Meta/FB "face tracking 2" (XR_FB_face_tracking2)
/// 70-blendshape array. This is exactly the array Virtual Desktop publishes in its
/// <c>VirtualDesktop.BodyState</c> shared memory and that Steam Link sends over OSC
/// (the <c>/sl/xrfb/facew/*</c> addresses), so it is available on a stock,
/// un-rooted Quest Pro.
///
/// The ordering is the canonical <c>XrFaceExpression2FB</c> enum order. Only the
/// values we actually consume are commented in detail; the rest are listed so the
/// enum can double as documentation.
/// </summary>
public enum Fb2
{
    BrowLowererL = 0,
    BrowLowererR = 1,
    CheekPuffL = 2,
    CheekPuffR = 3,
    CheekRaiserL = 4,
    CheekRaiserR = 5,
    CheekSuckL = 6,
    CheekSuckR = 7,
    ChinRaiserB = 8,
    ChinRaiserT = 9,
    DimplerL = 10,
    DimplerR = 11,
    EyesClosedL = 12,
    EyesClosedR = 13,
    EyesLookDownL = 14,
    EyesLookDownR = 15,
    EyesLookLeftL = 16,
    EyesLookLeftR = 17,
    EyesLookRightL = 18,
    EyesLookRightR = 19,
    EyesLookUpL = 20,
    EyesLookUpR = 21,
    InnerBrowRaiserL = 22,
    InnerBrowRaiserR = 23,
    JawDrop = 24,
    JawSidewaysLeft = 25,
    JawSidewaysRight = 26,
    JawThrust = 27,
    LidTightenerL = 28,
    LidTightenerR = 29,
    LipCornerDepressorL = 30,
    LipCornerDepressorR = 31,
    LipCornerPullerL = 32,
    LipCornerPullerR = 33,
    LipFunnelerLB = 34,
    LipFunnelerLT = 35,
    LipFunnelerRB = 36,
    LipFunnelerRT = 37,
    LipPressorL = 38,
    LipPressorR = 39,
    LipPuckerL = 40,
    LipPuckerR = 41,
    LipStretcherL = 42,
    LipStretcherR = 43,
    LipSuckLB = 44,
    LipSuckLT = 45,
    LipSuckRB = 46,
    LipSuckRT = 47,
    LipTightenerL = 48,
    LipTightenerR = 49,
    LipsToward = 50,
    LowerLipDepressorL = 51,
    LowerLipDepressorR = 52,
    MouthLeft = 53,
    MouthRight = 54,
    NoseWrinklerL = 55,
    NoseWrinklerR = 56,
    OuterBrowRaiserL = 57,
    OuterBrowRaiserR = 58,
    UpperLidRaiserL = 59,
    UpperLidRaiserR = 60,
    UpperLipRaiserL = 61,
    UpperLipRaiserR = 62,
    TongueTipInterdental = 63,
    TongueTipAlveolar = 64,
    TongueFrontDorsalPalate = 65,
    TongueMidDorsalPalate = 66,
    TongueBackDorsalPalate = 67,
    TongueOut = 68,
    TongueRetreat = 69,
}

public static class Fb2Constants
{
    /// <summary>Number of weights in a full FB2 frame.</summary>
    public const int Count = 70;
}
