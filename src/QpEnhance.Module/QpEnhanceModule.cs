using System.Diagnostics;
using Microsoft.Extensions.Logging;
using QpEnhance.Core;
using VRCFaceTracking;

namespace QpEnhance.Module;

/// <summary>
/// VRCFaceTracking module that enhances the stock, un-rooted Quest Pro face/eye signal
/// coming from Virtual Desktop. It replaces the default Virtual Desktop module: it reads
/// the same shared memory, runs the <see cref="EnhancePipeline"/> (gaze smoothing,
/// openness calibration, cheek separation, brow sensitivity) and writes the result into
/// VRCFaceTracking's unified data. No root, no firmware changes, no camera access.
/// </summary>
public sealed class QpEnhanceModule : ExtTrackingModule
{
    private const string ConfigFileName = "QpEnhance.config.json";

    private readonly VirtualDesktopFaceSource _source = new();
    private readonly float[] _shapes = new float[Fb2Constants.Count];
    private readonly Stopwatch _clock = Stopwatch.StartNew();

    private EnhancePipeline _pipeline = new();
    private string _configPath = ConfigFileName;
    private DateTime _configStamp = DateTime.MinValue;
    private long _nextConfigCheckMs;

    // Which capabilities the host handed us. We only write what we own so we never
    // stomp another module that is providing eye or expression data.
    private bool _ownEye;
    private bool _ownExpression;

    public override (bool SupportsEye, bool SupportsExpression) Supported => (true, true);

    public override (bool eyeSuccess, bool expressionSuccess) Initialize(bool eyeAvailable, bool expressionAvailable)
    {
        ModuleInformation.Name = "QpEnhance (Quest Pro, no-root)";
        ModuleInformation.StaticImages = new List<Stream>();

        LoadConfig(initial: true);

        if (!_source.TryConnect())
        {
            Log(l => l.LogInformation(
                "Virtual Desktop face state not found yet. Start VD streaming with face/eye tracking enabled; " +
                "QpEnhance will attach automatically."));
        }
        else
        {
            Log(l => l.LogInformation("QpEnhance attached to Virtual Desktop face state."));
        }

        // We can supply both eye and expression from the VD signal; claim whatever the
        // host still needs a provider for, and remember what we own.
        _ownEye = eyeAvailable;
        _ownExpression = expressionAvailable;
        return (_ownEye, _ownExpression);
    }

    public override void Update()
    {
        MaybeReloadConfig();

        VdFrame? frame = _source.Read();
        if (frame is null)
        {
            // VD not streaming (yet). Idle without spinning; Read() will reconnect.
            Thread.Sleep(50);
            return;
        }

        double t = (double)_clock.ElapsedTicks / Stopwatch.Frequency;

        GazeSample? gaze = frame.AnyEyeValid ? frame.Gaze : null;
        EnhanceResult res = _pipeline.Process(
            frame.Weights,
            _shapes,
            eyeValid: frame.AnyEyeValid,
            faceValid: frame.LowerFaceValid || frame.EyeExpressionValid,
            tSeconds: t,
            gaze: gaze);

        if (_ownEye)
            WriteEyes(res);
        if (_ownExpression)
            Fb2UnifiedMap.Apply(_shapes, UnifiedTracking.Data);

        // Pace to roughly the headset's face-tracking rate (~90 Hz).
        Thread.Sleep(10);
    }

    private static void WriteEyes(EnhanceResult res)
    {
        var eye = UnifiedTracking.Data.Eye;

        eye.Left.Gaze.x = res.Left.GazeX;
        eye.Left.Gaze.y = res.Left.GazeY;
        eye.Left.Openness = res.Left.Openness;

        eye.Right.Gaze.x = res.Right.GazeX;
        eye.Right.Gaze.y = res.Right.GazeY;
        eye.Right.Openness = res.Right.Openness;

        // No real pupil data without root; publish a neutral 5 mm with a plausible
        // range so VRCFaceTracking's normalised dilation parameter sits mid-scale.
        eye.Left.PupilDiameter_MM = 5f;
        eye.Right.PupilDiameter_MM = 5f;
        eye._minDilation = 2f;
        eye._maxDilation = 8f;
    }

    public override void Teardown()
    {
        _source.Dispose();
    }

    // ---- Config ----

    private void LoadConfig(bool initial)
    {
        try
        {
            string? dir = Path.GetDirectoryName(typeof(QpEnhanceModule).Assembly.Location);
            if (string.IsNullOrEmpty(dir))
                dir = AppContext.BaseDirectory;
            _configPath = Path.Combine(dir, ConfigFileName);

            if (!File.Exists(_configPath))
            {
                // Write defaults on first run so the file is there to edit.
                new EnhanceConfig().Save(_configPath);
            }

            var cfg = EnhanceConfig.Load(_configPath);
            _pipeline.UpdateConfig(cfg);
            _configStamp = SafeStamp(_configPath);

            if (!initial)
                Log(l => l.LogInformation("QpEnhance reloaded {Config}.", ConfigFileName));
        }
        catch (Exception ex)
        {
            Log(l => l.LogWarning(ex, "QpEnhance could not load {Config}; using current settings.", ConfigFileName));
        }
    }

    private void Log(Action<ILogger> write)
    {
        if (Logger is not null)
            write(Logger);
    }

    private void MaybeReloadConfig()
    {
        long nowMs = _clock.ElapsedMilliseconds;
        if (nowMs < _nextConfigCheckMs)
            return;
        _nextConfigCheckMs = nowMs + 2000;

        DateTime stamp = SafeStamp(_configPath);
        if (stamp != _configStamp && stamp != DateTime.MinValue)
            LoadConfig(initial: false);
    }

    private static DateTime SafeStamp(string path)
    {
        try
        {
            return File.Exists(path) ? File.GetLastWriteTimeUtc(path) : DateTime.MinValue;
        }
        catch
        {
            return DateTime.MinValue;
        }
    }
}
