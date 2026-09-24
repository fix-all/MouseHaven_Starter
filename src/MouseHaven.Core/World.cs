namespace MouseHaven.Core;

public enum HomeMode { Micro, Expanded }
public enum CharacterOwner { Home, Desktop }
public enum MouseAction { Idle, Walk, Work, ExitHome, Roam, Nibble, Startled, ReturnHome, Hide }

public readonly record struct Point2(double X, double Y)
{
    public double DistanceTo(Point2 other) => Math.Sqrt(Math.Pow(other.X - X, 2) + Math.Pow(other.Y - Y, 2));
    public Point2 MoveToward(Point2 target, double distance)
    {
        var remaining = DistanceTo(target);
        if (remaining <= distance || remaining < 0.001) return target;
        var ratio = distance / remaining;
        return new Point2(X + (target.X - X) * ratio, Y + (target.Y - Y) * ratio);
    }
}

public interface IClock { TimeSpan Now { get; } }

public sealed class StopwatchClock : IClock
{
    private readonly System.Diagnostics.Stopwatch _watch = System.Diagnostics.Stopwatch.StartNew();
    public TimeSpan Now => _watch.Elapsed;
}

// All character ownership, movement and action timing live here. Windows only render this state.
public sealed class WorldState
{
    public HomeMode Mode { get; internal set; } = HomeMode.Micro;
    public CharacterOwner Owner { get; internal set; } = CharacterOwner.Home;
    public MouseAction Action { get; internal set; } = MouseAction.Idle;
    public double HomeX { get; internal set; } = 160;
    public Point2 DesktopPosition { get; internal set; }
    public Point2 HomeEntry { get; internal set; }
    public Point2 FolderTarget { get; internal set; }
    public string PlaceId { get; internal set; } = "home";
    public string SegmentId { get; internal set; } = "idle";
    public double SegmentProgress { get; internal set; }
    public double ActionSeconds { get; internal set; }
    public bool Paused { get; internal set; }
    public bool DemoActive { get; internal set; }
}

public sealed class World
{
    public const double HomeEntranceX = 60;
    private readonly IClock _clock;
    private readonly Random _random;
    private TimeSpan _last;
    private double _remaining = 1.0;
    private double _homeStart;
    private double _homeTarget;
    private Point2 _desktopStart;

    public WorldState State { get; } = new();

    public World(IClock clock, int seed = 20260924)
    {
        _clock = clock;
        _random = new Random(seed);
        _last = clock.Now;
    }

    public void SetMode(HomeMode mode) => State.Mode = mode;
    public void SetHomeEntry(Point2 entry) => State.HomeEntry = entry;
    public void SetFolderTarget(Point2 target) => State.FolderTarget = target;

    public bool TryStartDemo()
    {
        if (State.Paused || State.Owner != CharacterOwner.Home || State.Mode != HomeMode.Micro ||
            State.Action is MouseAction.ExitHome or MouseAction.Hide) return false;
        State.DemoActive = true;
        BeginHomeTravel(MouseAction.ExitHome, HomeEntranceX);
        return true;
    }

    public bool Startle()
    {
        if (State.Paused || State.Owner != CharacterOwner.Desktop ||
            State.Action is MouseAction.Startled or MouseAction.ReturnHome or MouseAction.Hide) return false;
        SetAction(MouseAction.Startled, "startled");
        _remaining = 0.22;
        return true;
    }

    public void SetPaused(bool paused)
    {
        if (State.Paused == paused) return;
        State.Paused = paused;
        _last = _clock.Now; // paused wall time is never replayed
    }

    public void Advance()
    {
        var now = _clock.Now;
        var elapsed = Math.Max(0, (now - _last).TotalSeconds);
        // Long wake gaps may finish an idle wait, but cannot teleport a moving mouse.
        var seconds = State.Action == MouseAction.Idle ? elapsed : Math.Min(elapsed, 0.25);
        _last = now;
        if (State.Paused || seconds <= 0) return;
        State.ActionSeconds += seconds;

        switch (State.Action)
        {
            case MouseAction.Idle:
                _remaining -= seconds;
                if (_remaining <= 0)
                    BeginHomeTravel(MouseAction.Walk, 105 + _random.NextDouble() * 520);
                break;
            case MouseAction.Walk:
            case MouseAction.ExitHome:
                State.HomeX = Move(State.HomeX, _homeTarget, 42 * seconds);
                UpdateHomeProgress();
                if (Math.Abs(State.HomeX - _homeTarget) < 0.001)
                {
                    if (State.Action == MouseAction.ExitHome)
                    {
                        State.Owner = CharacterOwner.Desktop;
                        State.PlaceId = "desktop";
                        State.DesktopPosition = State.HomeEntry;
                        _desktopStart = State.DesktopPosition;
                        SetAction(MouseAction.Roam, "entry-to-folder");
                    }
                    else
                    {
                        SetAction(MouseAction.Work, "work");
                        _remaining = 2.2;
                    }
                }
                break;
            case MouseAction.Work:
                _remaining -= seconds;
                if (_remaining <= 0) BeginIdle();
                break;
            case MouseAction.Roam:
                State.DesktopPosition = State.DesktopPosition.MoveToward(State.FolderTarget, 76 * seconds);
                UpdateDesktopProgress(_desktopStart, State.FolderTarget);
                if (State.DesktopPosition.DistanceTo(State.FolderTarget) < 0.001)
                    SetAction(MouseAction.Nibble, "nibble");
                break;
            case MouseAction.Nibble:
                break; // stays at the self-drawn prop until clicked
            case MouseAction.Startled:
                _remaining -= seconds;
                if (_remaining <= 0)
                {
                    _desktopStart = State.DesktopPosition;
                    SetAction(MouseAction.ReturnHome, "folder-to-entry");
                }
                break;
            case MouseAction.ReturnHome:
                State.DesktopPosition = State.DesktopPosition.MoveToward(State.HomeEntry, 105 * seconds);
                UpdateDesktopProgress(_desktopStart, State.HomeEntry);
                if (State.DesktopPosition.DistanceTo(State.HomeEntry) < 0.001)
                {
                    SetAction(MouseAction.Hide, "hide");
                    _remaining = 0.55;
                }
                break;
            case MouseAction.Hide:
                _remaining -= seconds;
                if (_remaining <= 0)
                {
                    State.Owner = CharacterOwner.Home;
                    State.PlaceId = "home";
                    State.HomeX = HomeEntranceX;
                    State.DemoActive = false;
                    BeginIdle();
                }
                break;
        }
    }

    public int NextWakeMilliseconds => State.Paused ? 0 : State.Action switch
    {
        MouseAction.Idle => Math.Clamp((int)Math.Ceiling(_remaining * 1000), 20, 1000),
        MouseAction.Work or MouseAction.Nibble => 120,
        MouseAction.Hide or MouseAction.Startled => 60,
        _ => 33
    };

    private void BeginIdle()
    {
        SetAction(MouseAction.Idle, "idle");
        _remaining = 0.8 + _random.NextDouble() * 1.3;
    }

    private void BeginHomeTravel(MouseAction action, double target)
    {
        _homeStart = State.HomeX;
        _homeTarget = target;
        SetAction(action, action == MouseAction.ExitHome ? "to-entrance" : "home-walk");
    }

    private void SetAction(MouseAction action, string segment)
    {
        State.Action = action;
        State.SegmentId = segment;
        State.SegmentProgress = 0;
        State.ActionSeconds = 0;
    }

    private void UpdateHomeProgress()
    {
        var total = Math.Abs(_homeTarget - _homeStart);
        State.SegmentProgress = total < 0.001 ? 1 : Math.Clamp(Math.Abs(State.HomeX - _homeStart) / total, 0, 1);
    }

    private void UpdateDesktopProgress(Point2 start, Point2 target)
    {
        var total = start.DistanceTo(target);
        State.SegmentProgress = total < 0.001 ? 1 : Math.Clamp(start.DistanceTo(State.DesktopPosition) / total, 0, 1);
    }

    private static double Move(double value, double target, double distance) =>
        Math.Abs(target - value) <= distance ? target : value + Math.Sign(target - value) * distance;
}
