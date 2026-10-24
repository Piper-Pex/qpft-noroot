using System.Diagnostics;
using System.Globalization;
using QpEnhance.Core;

namespace QpEnhance.ConfigUI;

/// <summary>
/// Minimal tuning window for QpEnhance. Sliders / checkboxes write straight into the
/// module's <c>QpEnhance.config.json</c>; the running module notices the file change and
/// hot-reloads within ~2 seconds, so edits here are effectively live.
///
/// This app is standalone — it has no dependency on VRCFaceTracking. It just edits the
/// JSON that the module watches, reusing <see cref="EnhanceConfig"/> so the on-disk format
/// is byte-for-byte what the module expects.
/// </summary>
public sealed class MainForm : Form
{
    // Must match QpEnhanceModule's module.json ModuleId (the CustomLibs sub-folder name).
    private const string ModuleId = "b2e1c7a4-6f3d-4a90-9c2e-7d1f0a5b4e33";
    private const string ConfigFileName = "QpEnhance.config.json";

    private readonly EnhanceConfig _cfg;
    private readonly string _path;
    private readonly System.Windows.Forms.Timer _saveTimer = new() { Interval = 300 };

    private FlowLayoutPanel _groups = null!;
    private Label _status = null!;
    private bool _loading;

    public MainForm()
    {
        _path = ResolveConfigPath();
        _cfg = EnhanceConfig.Load(_path);
        _saveTimer.Tick += (_, _) => { _saveTimer.Stop(); SaveNow(); };
        BuildChrome();
        _loading = true;
        BuildGroups();
        _loading = false;
    }

    private static string ResolveConfigPath()
    {
        string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(appData, "VRCFaceTracking", "CustomLibs", ModuleId, ConfigFileName);
    }

    // ---- Layout ----

    private void BuildChrome()
    {
        SuspendLayout();
        Text = "QpEnhance 配置";
        ClientSize = new Size(540, 784);
        MinimumSize = new Size(556, 440);
        StartPosition = FormStartPosition.CenterScreen;
        AutoScaleMode = AutoScaleMode.Dpi;
        Font = new Font("Microsoft YaHei UI", 9f);

        // Root = scrollable groups on top, a fixed footer below. A TableLayoutPanel avoids
        // all the Dock z-order pitfalls.
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2 };
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        _groups = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoScroll = true,
            Padding = new Padding(8),
        };
        root.Controls.Add(_groups, 0, 0);

        var footer = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoSize = true,
            Padding = new Padding(10, 4, 10, 10),
        };

        var buttons = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, AutoSize = true, Margin = new Padding(0) };
        var openBtn = new Button { Text = "打开所在文件夹", AutoSize = true };
        openBtn.Click += (_, _) => OpenFolder();
        var defBtn = new Button { Text = "恢复默认", AutoSize = true, Margin = new Padding(8, 3, 3, 3) };
        defBtn.Click += (_, _) => RestoreDefaults();
        buttons.Controls.Add(openBtn);
        buttons.Controls.Add(defBtn);

        var pathLbl = new Label
        {
            Text = _path,
            AutoSize = true,
            ForeColor = Color.Gray,
            MaximumSize = new Size(484, 0),
            Margin = new Padding(3, 6, 3, 0),
        };
        _status = new Label { AutoSize = true, Margin = new Padding(3, 4, 3, 0) };

        footer.Controls.Add(buttons);
        footer.Controls.Add(pathLbl);
        footer.Controls.Add(_status);
        root.Controls.Add(footer, 0, 1);

        Controls.Add(root);
        ShowInstalledStatus();
        ResumeLayout();
    }

    private void BuildGroups()
    {
        _groups.Controls.Clear();

        var gaze = AddGroup("视线 Gaze");
        AddCheck(gaze, "启用视线平滑 / 防抖", () => _cfg.GazeSmoothing, v => _cfg.GazeSmoothing = v);
        AddSlider(gaze, "防抖 (越低越稳)", 0.3, 3.0, () => _cfg.GazeMinCutoff, v => _cfg.GazeMinCutoff = v);
        AddSlider(gaze, "扫视跟手", 0.0, 1.5, () => _cfg.GazeBeta, v => _cfg.GazeBeta = v);
        AddSlider(gaze, "幅度增益", 1.0, 1.6, () => _cfg.GazeGain, v => _cfg.GazeGain = (float)v);

        var open = AddGroup("睁闭眼 Openness");
        AddCheck(open, "启用睁闭眼校准", () => _cfg.OpennessCalibration, v => _cfg.OpennessCalibration = v);
        AddSlider(open, "完全闭眼点", 0.0, 0.6, () => _cfg.OpenClosedPoint, v => _cfg.OpenClosedPoint = (float)v);
        AddSlider(open, "完全睁眼点", 0.5, 1.0, () => _cfg.OpenOpenPoint, v => _cfg.OpenOpenPoint = (float)v);
        AddSlider(open, "曲线 Gamma (>1 更睁)", 0.5, 2.0, () => _cfg.OpennessGamma, v => _cfg.OpennessGamma = (float)v);
        AddCheck(open, "自动适应睁闭范围 (开头眨几次校准)", () => _cfg.OpennessAuto, v => _cfg.OpennessAuto = v);
        AddSlider(open, "眯眼灵敏度", 0.5, 2.0, () => _cfg.SquintGain, v => _cfg.SquintGain = (float)v);

        var cheek = AddGroup("脸颊 Cheek");
        AddCheck(cheek, "鼓腮/吸腮左右分离", () => _cfg.CheekSeparation, v => _cfg.CheekSeparation = v);
        AddCheck(cheek, "强分离 (左右分不干净时勾选)", () => _cfg.CheekStrong, v => _cfg.CheekStrong = v);

        var brow = AddGroup("眉毛 Brow");
        AddSlider(brow, "眉毛灵敏度", 1.0, 2.5, () => _cfg.BrowSensitivity, v => _cfg.BrowSensitivity = (float)v);
    }

    private TableLayoutPanel AddGroup(string title)
    {
        var box = new GroupBox
        {
            Text = title,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Margin = new Padding(4, 4, 4, 8),
            Padding = new Padding(10, 4, 10, 10),
        };
        // The table is POSITIONED, not docked. A Dock=Top child inside an AutoSize
        // GroupBox collapses the group's width to zero (the caption then wraps one
        // character per line). An auto-sized child with fixed column widths instead
        // gives the group a definite size to grow around.
        var table = new TableLayoutPanel
        {
            Location = new Point(10, 22),
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 3,
        };
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150f));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 250f));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 54f));
        box.Controls.Add(table);
        _groups.Controls.Add(box);
        return table;
    }

    private void AddSlider(TableLayoutPanel table, string name, double min, double max,
                           Func<double> get, Action<double> set)
    {
        int row = table.RowCount++;
        table.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var label = new Label { Text = name, AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(3, 9, 3, 3) };
        var bar = new TrackBar
        {
            Minimum = 0,
            Maximum = 1000,
            TickStyle = TickStyle.None,
            Dock = DockStyle.Fill,
            AutoSize = false,
            Height = 32,
        };
        var value = new Label { AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(3, 9, 3, 3) };

        double current = get();
        bar.Value = ToTick(current, min, max);
        value.Text = Format(current);

        bar.ValueChanged += (_, _) =>
        {
            double v = FromTick(bar.Value, min, max);
            value.Text = Format(v);
            if (_loading) return;
            set(v);
            QueueSave();
        };

        table.Controls.Add(label, 0, row);
        table.Controls.Add(bar, 1, row);
        table.Controls.Add(value, 2, row);
    }

    private void AddCheck(TableLayoutPanel table, string name, Func<bool> get, Action<bool> set)
    {
        int row = table.RowCount++;
        table.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var check = new CheckBox { Text = name, AutoSize = true, Checked = get(), Margin = new Padding(3, 7, 3, 3) };
        check.CheckedChanged += (_, _) =>
        {
            if (_loading) return;
            set(check.Checked);
            QueueSave();
        };

        table.Controls.Add(check, 0, row);
        table.SetColumnSpan(check, 3);
    }

    private static string Format(double v) => v.ToString("0.00", CultureInfo.InvariantCulture);
    private static int ToTick(double v, double min, double max)
        => Math.Clamp((int)Math.Round((v - min) / (max - min) * 1000.0), 0, 1000);
    private static double FromTick(int tick, double min, double max)
        => min + (max - min) * tick / 1000.0;

    // ---- Save / actions ----

    private void QueueSave()
    {
        _saveTimer.Stop();
        _saveTimer.Start();
    }

    private void SaveNow()
    {
        try
        {
            _cfg.Save(_path);
            _status.ForeColor = Color.ForestGreen;
            _status.Text = $"✓ 已保存 {DateTime.Now:HH:mm:ss} — 模块约 2 秒后自动生效";
        }
        catch (Exception ex)
        {
            _status.ForeColor = Color.Firebrick;
            _status.Text = "保存失败：" + ex.Message;
        }
    }

    private void ShowInstalledStatus()
    {
        string dir = Path.GetDirectoryName(_path)!;
        bool installed = File.Exists(Path.Combine(dir, "QpEnhance.Module.dll"));
        _status.ForeColor = installed ? Color.Gray : Color.DarkOrange;
        _status.Text = installed
            ? "已检测到已安装的 QpEnhance 模块 ✓"
            : "⚠ 此位置未检测到模块 DLL（确认模块已安装；保存仍会写入此路径）";
    }

    private void RestoreDefaults()
    {
        var d = new EnhanceConfig();
        _cfg.GazeSmoothing = d.GazeSmoothing;
        _cfg.GazeMinCutoff = d.GazeMinCutoff;
        _cfg.GazeBeta = d.GazeBeta;
        _cfg.GazeDCutoff = d.GazeDCutoff;
        _cfg.GazeGain = d.GazeGain;
        _cfg.OpennessCalibration = d.OpennessCalibration;
        _cfg.OpenClosedPoint = d.OpenClosedPoint;
        _cfg.OpenOpenPoint = d.OpenOpenPoint;
        _cfg.OpennessGamma = d.OpennessGamma;
        _cfg.OpennessAuto = d.OpennessAuto;
        _cfg.SquintGain = d.SquintGain;
        _cfg.CheekSeparation = d.CheekSeparation;
        _cfg.CheekStrong = d.CheekStrong;
        _cfg.BrowSensitivity = d.BrowSensitivity;

        _loading = true;
        BuildGroups();
        _loading = false;
        SaveNow();
    }

    private void OpenFolder()
    {
        try
        {
            string dir = Path.GetDirectoryName(_path)!;
            Directory.CreateDirectory(dir);
            Process.Start(new ProcessStartInfo { FileName = dir, UseShellExecute = true });
        }
        catch (Exception ex)
        {
            _status.ForeColor = Color.Firebrick;
            _status.Text = "无法打开文件夹：" + ex.Message;
        }
    }
}
