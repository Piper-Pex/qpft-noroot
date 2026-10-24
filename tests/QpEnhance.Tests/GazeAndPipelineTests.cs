using QpEnhance.Core;

namespace QpEnhance.Tests;

public static class GazeAndPipelineTests
{
    private static float[] Frame()
    {
        var w = new float[Fb2Constants.Count];
        return w;
    }

    public static void Run(TestRunner t)
    {
        t.Section("GazeProcessor + EnhancePipeline");

        var cfg = new EnhanceConfig();
        var pipe = new EnhancePipeline(cfg);
        var outShapes = new float[Fb2Constants.Count];

        // Looking right with the left eye -> positive X; looking up -> positive Y.
        var w = Frame();
        w[(int)Fb2.EyesLookRightL] = 0.8f;
        w[(int)Fb2.EyesLookUpL] = 0.5f;
        w[(int)Fb2.EyesLookLeftR] = 0.8f; // right eye looks to the user's left -> negative X
        var r1 = pipe.Process(w, outShapes, eyeValid: true, faceValid: true, tSeconds: 0.0);
        t.Greater("left eye gaze X positive", r1.Left.GazeX, 0.0);
        t.Greater("left eye gaze Y positive", r1.Left.GazeY, 0.0);
        t.Less("right eye gaze X negative", r1.Right.GazeX, 0.0);
        t.True("validity propagates", r1.Left.Valid && r1.Right.Valid);

        // Openness calibration: fully open -> 1, fully closed -> 0.
        var open = Frame();
        open[(int)Fb2.EyesClosedL] = 0.0f;
        var ro = pipe.Process(open, outShapes, true, true, 0.1);
        t.Near("eyes open -> openness 1", ro.Left.Openness, 1.0, 1e-3);

        var closed = Frame();
        closed[(int)Fb2.EyesClosedL] = 0.85f; // openness 0.15 == configured closed point
        var rc = pipe.Process(closed, outShapes, true, true, 0.2);
        t.Near("eyes closed -> openness 0", rc.Left.Openness, 0.0, 1e-3);

        // Cheek separation is reflected in the output weights.
        var cheek = Frame();
        cheek[(int)Fb2.CheekPuffL] = 0.8f;
        cheek[(int)Fb2.CheekPuffR] = 0.15f;
        pipe.Process(cheek, outShapes, true, true, 0.3);
        t.Greater("puff keeps active cheek", outShapes[(int)Fb2.CheekPuffL], 0.5);
        t.Less("puff suppresses leaking cheek", outShapes[(int)Fb2.CheekPuffR], 0.3);

        // Brow sensitivity (default 1.4) amplifies a subtle brow raise.
        var brow = Frame();
        brow[(int)Fb2.InnerBrowRaiserL] = 0.5f;
        pipe.Process(brow, outShapes, true, true, 0.4);
        t.Greater("brow amplified", outShapes[(int)Fb2.InnerBrowRaiserL], 0.5);

        // Unrelated shapes pass through untouched.
        var jaw = Frame();
        jaw[(int)Fb2.JawDrop] = 0.42f;
        pipe.Process(jaw, outShapes, true, true, 0.5);
        t.Near("jaw passes through", outShapes[(int)Fb2.JawDrop], 0.42, 1e-5);

        // A short gaze frame is rejected rather than silently misread.
        bool threw = false;
        try
        {
            pipe.Process(new float[10], outShapes, true, true, 0.6);
        }
        catch (ArgumentException)
        {
            threw = true;
        }
        t.True("short frame rejected", threw);
    }
}
