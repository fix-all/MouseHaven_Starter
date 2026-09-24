using System.Diagnostics;
using MouseHaven.Core;

namespace MouseHaven.Windows;

internal sealed class HavenContext : ApplicationContext
{
    private readonly World _world = new(new StopwatchClock());
    private readonly ISceneRenderer _artwork = new Artwork();
    private readonly LayeredSurface _home = new();
    private readonly LayeredSurface _mouse = new();
    private readonly LayeredSurface _folder = new(clickThrough: true);
    private readonly NotifyIcon _tray = new();
    private ContextMenuStrip? _menu;
    private readonly System.Windows.Forms.Timer _frame = new();
    private readonly System.Windows.Forms.Timer _diagnosticsTimer = new() { Interval = 5000 };
    private readonly Rectangle _work;
    private HavenSettings _settings;
    private bool _homeVisible = true;
    private bool _dragging;
    private bool _mouseDown;
    private bool _closing;
    private Point _dragCursor;
    private Point _dragHome;
    private ToolStripMenuItem _pauseMenu = null!;
    private ToolStripMenuItem _showMenu = null!;
    private ToolStripMenuItem _diagnosticsMenu = null!;
    private ToolStripMenuItem _sizesMenu = null!;
    private readonly string? _loadWarning;

    internal HavenContext()
    {
        _work = Screen.PrimaryScreen?.WorkingArea ?? new Rectangle(0, 0, 1280, 720);
        (_settings, _loadWarning) = SettingsStore.Load(_work);
        _home.MouseDown += HomeMouseDown;
        _home.MouseMove += HomeMouseMove;
        _home.MouseUp += HomeMouseUp;
        _home.FormClosed += (_, _) => CloseApp();
        _home.DpiChanged += (_, _) => RefreshView();
        _mouse.MouseClick += (_, e) =>
        {
            if (e.Button == MouseButtons.Left && _world.Startle()) RefreshView();
        };
        _mouse.DpiChanged += (_, _) => RefreshView();
        _folder.DpiChanged += (_, _) => RefreshView();

        _home.Location = new Point(_settings.X, _settings.Y);
        _home.SetLogicalSize(_settings.MicroSize, _settings.MicroSize);
        _mouse.SetLogicalSize(32, 32);
        _folder.SetLogicalSize(48, 48);
        _home.Show();

        BuildTray();
        _frame.Tick += (_, _) => Tick();
        _diagnosticsTimer.Tick += (_, _) => WriteDiagnostics();
        _diagnosticsTimer.Enabled = _settings.Diagnostics;
        RefreshView();
        if (_loadWarning is not null)
            _tray.ShowBalloonTip(5000, "MouseHaven 设置", _loadWarning, ToolTipIcon.Warning);
    }

    private void BuildTray()
    {
        _pauseMenu = new ToolStripMenuItem("暂停", null, (_, _) => TogglePause());
        _showMenu = new ToolStripMenuItem("隐藏家园", null, (_, _) => ToggleHomeVisibility());
        _diagnosticsMenu = new ToolStripMenuItem("诊断记录", null, (_, _) => ToggleDiagnostics())
        { Checked = _settings.Diagnostics };
        var sizes = new ToolStripMenuItem("微型尺寸");
        _sizesMenu = sizes;
        foreach (var size in new[] { 32, 40, 48, 64 })
        {
            var selected = size;
            sizes.DropDownItems.Add(new ToolStripMenuItem($"{size} × {size}", null,
                (_, _) => ChangeMicroSize(selected)) { Checked = _settings.MicroSize == size, Tag = size });
        }
        var menu = new ContextMenuStrip();
        _menu = menu;
        menu.Items.AddRange([
            _pauseMenu,
            _showMenu,
            new ToolStripMenuItem("展开 / 收起家园", null, (_, _) => ToggleMode()),
            sizes,
            new ToolStripMenuItem("演示外出", null, (_, _) => StartDemo()),
            new ToolStripMenuItem("重置位置", null, (_, _) => ResetPosition()),
            _diagnosticsMenu,
            new ToolStripSeparator(),
            new ToolStripMenuItem("退出", null, (_, _) => CloseApp())
        ]);
        _home.ContextMenuStrip = menu;
        _mouse.ContextMenuStrip = menu;
        _tray.Icon = SystemIcons.Application;
        _tray.Text = "MouseHaven P0 演示";
        _tray.ContextMenuStrip = menu;
        _tray.Visible = true;
        _tray.DoubleClick += (_, _) => ToggleHomeVisibility();
    }

    private void HomeMouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left) return;
        _mouseDown = true;
        _dragging = false;
        _dragCursor = Cursor.Position;
        _dragHome = _home.Location;
        _home.Capture = true;
    }

    private void HomeMouseMove(object? sender, MouseEventArgs e)
    {
        if (!_mouseDown) return;
        var cursor = Cursor.Position;
        var dx = cursor.X - _dragCursor.X;
        var dy = cursor.Y - _dragCursor.Y;
        if (Math.Abs(dx) + Math.Abs(dy) > 5) _dragging = true;
        if (!_dragging) return;
        _home.Location = Clamp(new Point(_dragHome.X + dx, _dragHome.Y + dy), _home.Size);
        RefreshView();
    }

    private void HomeMouseUp(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left || !_mouseDown) return;
        _mouseDown = false;
        _home.Capture = false;
        if (_dragging)
        {
            _settings = _settings with { X = _home.Left, Y = _home.Top };
            SaveSettings();
            _dragging = false;
            return;
        }
        if (_world.State.Mode == HomeMode.Micro) ToggleMode();
        else if (e.X / _home.DpiScale >= _home.LogicalWidth - 35 && e.Y / _home.DpiScale <= 36) ToggleMode();
    }

    private void Tick()
    {
        UpdateTargets();
        var beforeAction = _world.State.Action;
        var beforeOwner = _world.State.Owner;
        var beforeHomeX = _world.State.HomeX;
        _world.Advance();
        var s = _world.State;
        var homeDirty = beforeOwner != s.Owner || beforeAction != s.Action ||
            Math.Abs(beforeHomeX - s.HomeX) > 0.001 ||
            (s.Owner == CharacterOwner.Home && s.Action == MouseAction.Work);
        RefreshView(homeDirty, beforeAction != s.Action);
    }

    private void UpdateTargets()
    {
        var s = _world.State;
        var local = _artwork.EntryInHome(s, _home.LogicalWidth);
        var entry = new Point2(_home.Left + local.X * _home.DpiScale, _home.Top + local.Y * _home.DpiScale);
        _world.SetHomeEntry(entry);
        var direction = entry.X + 235 < _work.Right ? 1 : -1;
        var target = new Point2(Math.Clamp(entry.X + direction * 185, _work.Left + 25, _work.Right - 25),
            Math.Clamp(entry.Y + 62, _work.Top + 25, _work.Bottom - 25));
        if (!s.DemoActive) _world.SetFolderTarget(target);
    }

    private void RefreshView(bool homeDirty = true, bool folderDirty = true)
    {
        if (_closing) return;
        UpdateTargets();
        var s = _world.State;
        if (_homeVisible)
        {
            var newlyShown = !_home.Visible;
            if (!_home.Visible) _home.Show();
            if (homeDirty || newlyShown)
                _home.Render(g => _artwork.Home(g, s, _home.LogicalWidth, _home.LogicalHeight));
        }
        else if (_home.Visible) _home.Hide();

        var showDesktop = s.Owner == CharacterOwner.Desktop && s.Action != MouseAction.Hide;
        if (showDesktop)
        {
            if (!_mouse.Visible) _mouse.Show();
            var p = s.DesktopPosition;
            _mouse.Location = new Point((int)Math.Round(p.X - 16 * _mouse.DpiScale),
                (int)Math.Round(p.Y - 16 * _mouse.DpiScale));
            _mouse.Render(g => _artwork.DesktopMouse(g, s));
        }
        else if (_mouse.Visible) _mouse.Hide();

        if (s.DemoActive && s.Owner == CharacterOwner.Desktop && s.Action != MouseAction.Hide)
        {
            var newlyShown = !_folder.Visible;
            if (!_folder.Visible) _folder.Show();
            var p = s.FolderTarget;
            _folder.Location = new Point((int)Math.Round(p.X - 24 * _folder.DpiScale),
                (int)Math.Round(p.Y - 24 * _folder.DpiScale));
            if (folderDirty || newlyShown) _folder.Render(g => _artwork.Folder(g, s));
        }
        else if (_folder.Visible) _folder.Hide();

        var delay = !_homeVisible && s.Owner == CharacterOwner.Home ? 0 : _world.NextWakeMilliseconds;
        if (delay == 0) _frame.Stop();
        else
        {
            _frame.Interval = delay;
            _frame.Start();
        }
    }

    private void ToggleMode()
    {
        var next = _world.State.Mode == HomeMode.Micro ? HomeMode.Expanded : HomeMode.Micro;
        _world.SetMode(next);
        _home.SetLogicalSize(next == HomeMode.Micro ? _settings.MicroSize : 480,
            next == HomeMode.Micro ? _settings.MicroSize : 270);
        _home.Location = Clamp(new Point(_settings.X, _settings.Y), _home.Size);
        RefreshView();
    }

    private void StartDemo()
    {
        if (!_world.TryStartDemo())
            _tray.ShowBalloonTip(3000, "无法开始演示", "请先收起为微型家园，并确认鼠鼠在家且未暂停。", ToolTipIcon.Info);
        else
        {
            _tray.ShowBalloonTip(3000, "外出演示", "请先显示桌面；点击外出的鼠鼠，她会回家。", ToolTipIcon.Info);
            RefreshView();
        }
    }

    private void TogglePause()
    {
        _world.SetPaused(!_world.State.Paused);
        _pauseMenu.Text = _world.State.Paused ? "恢复" : "暂停";
        RefreshView();
    }

    private void ToggleHomeVisibility()
    {
        _homeVisible = !_homeVisible;
        _showMenu.Text = _homeVisible ? "隐藏家园" : "显示家园";
        RefreshView();
    }

    private void ChangeMicroSize(int size)
    {
        _settings = _settings with { MicroSize = size };
        foreach (ToolStripMenuItem item in _sizesMenu.DropDownItems)
            item.Checked = (int)item.Tag! == size;
        if (_world.State.Mode == HomeMode.Micro)
        {
            _home.SetLogicalSize(size, size);
            _home.Location = Clamp(new Point(_settings.X, _settings.Y), _home.Size);
        }
        SaveSettings();
        RefreshView();
    }

    private void ResetPosition()
    {
        var defaults = SettingsStore.Default(_work);
        _settings = _settings with { X = defaults.X, Y = defaults.Y };
        _home.Location = Clamp(new Point(_settings.X, _settings.Y), _home.Size);
        SaveSettings();
        RefreshView();
    }

    private void ToggleDiagnostics()
    {
        _settings = _settings with { Diagnostics = !_settings.Diagnostics };
        _diagnosticsMenu.Checked = _settings.Diagnostics;
        _diagnosticsTimer.Enabled = _settings.Diagnostics;
        SaveSettings();
        if (_settings.Diagnostics) WriteDiagnostics();
    }

    private void WriteDiagnostics()
    {
        if (!_settings.Diagnostics) return;
        using var process = Process.GetCurrentProcess();
        var s = _world.State;
        var line = $"{DateTimeOffset.Now:O} mode={s.Mode} owner={s.Owner} action={s.Action} " +
            $"homeDraws={_home.DrawCount} mouseDraws={_mouse.DrawCount} folderDraws={_folder.DrawCount} " +
            $"cpuSeconds={process.TotalProcessorTime.TotalSeconds:F3} workingSetBytes={process.WorkingSet64} " +
            $"privateBytes={process.PrivateMemorySize64}";
        try { SettingsStore.AppendDiagnostics(line); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _diagnosticsTimer.Stop();
            _tray.ShowBalloonTip(5000, "诊断记录已停止", ex.Message, ToolTipIcon.Warning);
        }
    }

    private Point Clamp(Point desired, Size size) => new(
        Math.Clamp(desired.X, _work.Left, Math.Max(_work.Left, _work.Right - size.Width)),
        Math.Clamp(desired.Y, _work.Top, Math.Max(_work.Top, _work.Bottom - size.Height)));

    private void SaveSettings()
    {
        try { SettingsStore.Save(_settings); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _tray.ShowBalloonTip(5000, "无法保存设置", ex.Message, ToolTipIcon.Warning);
        }
    }

    private void CloseApp()
    {
        if (_closing) return;
        _closing = true;
        SaveSettings();
        _frame.Stop();
        _diagnosticsTimer.Stop();
        _tray.Visible = false;
        _tray.Dispose();
        if (!_home.IsDisposed && !_home.Disposing) _home.Close();
        if (!_mouse.IsDisposed) _mouse.Close();
        if (!_folder.IsDisposed) _folder.Close();
        _menu?.Dispose();
        _frame.Dispose();
        _diagnosticsTimer.Dispose();
        ExitThread();
    }
}
