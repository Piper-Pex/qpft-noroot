using QpEnhance.Core;

namespace QpEnhance.Tests;

public static class ConfigTests
{
    public static void Run(TestRunner t)
    {
        t.Section("EnhanceConfig");

        var def = new EnhanceConfig();
        t.True("default smoothing on", def.GazeSmoothing);
        t.Near("default gaze gain", def.GazeGain, 1.15, 1e-6);
        t.Near("default brow sensitivity", def.BrowSensitivity, 1.4, 1e-6);

        // JSON round-trip preserves a changed value.
        def.GazeBeta = 0.42;
        def.CheekStrong = true;
        var clone = EnhanceConfig.FromJson(def.ToJson());
        t.Near("round-trip beta", clone.GazeBeta, 0.42, 1e-9);
        t.True("round-trip cheek-strong", clone.CheekStrong);

        // Missing file -> defaults, no throw.
        var missing = EnhanceConfig.Load(Path.Combine(Path.GetTempPath(), "qpenhance_does_not_exist_" + Guid.NewGuid() + ".json"));
        t.True("missing file -> defaults", missing.GazeSmoothing);

        // Save then load round-trips through disk.
        var path = Path.Combine(Path.GetTempPath(), "qpenhance_cfg_" + Guid.NewGuid() + ".json");
        try
        {
            def.Save(path);
            var loaded = EnhanceConfig.Load(path);
            t.Near("disk round-trip beta", loaded.GazeBeta, 0.42, 1e-9);
        }
        finally
        {
            try { File.Delete(path); } catch { /* best effort */ }
        }
    }
}
