using System.Text.Json;
using System.Text.Json.Serialization;

namespace QpEnhance.Core;

/// <summary>
/// User-tunable settings for the enhancement pipeline. Plain data; serialises to JSON
/// so the module (and a future config UI) can persist it next to the VRCFaceTracking
/// module. All of this operates purely on the stock, un-rooted signal.
/// </summary>
public sealed class EnhanceConfig
{
    // ---- Gaze ----
    public bool GazeSmoothing { get; set; } = true;
    /// <summary>1€ filter minimum cutoff (Hz). Lower = smoother at rest, more lag.</summary>
    public double GazeMinCutoff { get; set; } = 1.2;
    /// <summary>1€ filter speed coefficient. Higher = snappier on fast eye movements.</summary>
    public double GazeBeta { get; set; } = 0.25;
    public double GazeDCutoff { get; set; } = 1.0;
    /// <summary>Multiplier applied to raw gaze so the eyes reach the corners of their range.</summary>
    public float GazeGain { get; set; } = 1.15f;

    // ---- Openness / squint ----
    public bool OpennessCalibration { get; set; } = true;
    /// <summary>Openness (1 - EYES_CLOSED) value that should read as fully closed.</summary>
    public float OpenClosedPoint { get; set; } = 0.15f;
    /// <summary>Openness value that should read as fully open.</summary>
    public float OpenOpenPoint { get; set; } = 0.90f;
    /// <summary>Curve on the calibrated openness. &gt;1 keeps eyes wider; &lt;1 closes sooner.</summary>
    public float OpennessGamma { get; set; } = 1.0f;
    /// <summary>Let the openness range widen itself from observed extremes.</summary>
    public bool OpennessAuto { get; set; } = false;
    /// <summary>Gain on lid-tightener driven squint.</summary>
    public float SquintGain { get; set; } = 1.0f;

    // ---- Cheeks ----
    public bool CheekSeparation { get; set; } = true;
    public bool CheekStrong { get; set; } = false;

    // ---- Eyebrows ----
    public float BrowSensitivity { get; set; } = 1.4f;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
    };

    public string ToJson() => JsonSerializer.Serialize(this, JsonOptions);

    public static EnhanceConfig FromJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return new EnhanceConfig();
        return JsonSerializer.Deserialize<EnhanceConfig>(json) ?? new EnhanceConfig();
    }

    /// <summary>Load from disk, returning defaults if the file is missing or unreadable.</summary>
    public static EnhanceConfig Load(string path)
    {
        try
        {
            return File.Exists(path) ? FromJson(File.ReadAllText(path)) : new EnhanceConfig();
        }
        catch
        {
            return new EnhanceConfig();
        }
    }

    public void Save(string path)
    {
        string? dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);
        File.WriteAllText(path, ToJson());
    }
}
