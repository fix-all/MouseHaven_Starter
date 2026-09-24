using MouseHaven.Core;

var tests = new (string Name, Action Run)[]
{
    ("视图切换保留同一动作进度", ViewSwitchKeepsAction),
    ("家园到桌面再回家只有一个所有权", OneOwnerRoundTrip),
    ("回家目标随入口移动且重复受惊无效", ReturnFollowsEntry),
    ("暂停恢复不补播积压时间", PauseDoesNotReplay),
    ("固定随机种子行为可重现", FixedSeedIsRepeatable),
    ("外出演示仅从微型家园启动", DemoGate),
    ("损坏或越界设置恢复默认", InvalidSettingsRecover),
    ("静止啃咬后受惊不跳过动作", StartleAfterLongStaticNibble),
    ("菜地交互点与可见作物状态", GardenWorkAtFixedPlace),
    ("家园与桌面左右朝向", FacingFollowsMovement),
    ("同一模拟文件夹在啃咬前后保持位置", FolderContinuity)
};
var failures = 0;
foreach (var (name, run) in tests)
{
    try { run(); Console.WriteLine($"PASS {name}"); }
    catch (Exception ex) { failures++; Console.WriteLine($"FAIL {name}: {ex.Message}"); }
}
Console.WriteLine($"{tests.Length - failures}/{tests.Length} passed");
return failures == 0 ? 0 : 1;

static void ViewSwitchKeepsAction()
{
    var clock = new FakeClock();
    var world = new World(clock);
    Step(world, clock, 0.35);
    var action = world.State.Action;
    var progress = world.State.ActionSeconds;
    var position = world.State.HomeX;
    world.SetMode(HomeMode.Expanded);
    Check(world.State.Owner == CharacterOwner.Home, "切换后所有权改变");
    Check(world.State.Action == action && world.State.ActionSeconds == progress && world.State.HomeX == position,
        "切换时动作或位置被重置");
    world.SetMode(HomeMode.Micro);
    Check(world.State.ActionSeconds == progress, "收起时动作进度被重置");
}

static void OneOwnerRoundTrip()
{
    var (world, clock) = NewDemo();
    Until(world, clock, () => world.State.Action == MouseAction.Nibble);
    Check(world.State.Owner == CharacterOwner.Desktop, "未移交给桌面");
    Check(world.State.Mode == HomeMode.Micro, "外出自动改变家园尺寸");
    world.SetMode(HomeMode.Expanded);
    Check(world.State.Owner == CharacterOwner.Desktop, "展开时复制或召回角色");
    world.SetMode(HomeMode.Micro);
    Check(world.Startle(), "点击桌面角色未触发受惊");
    Until(world, clock, () => world.State.Owner == CharacterOwner.Home);
    Check(!world.State.DemoActive && world.State.Action == MouseAction.Idle, "返回后演示未结束");
}

static void ReturnFollowsEntry()
{
    var (world, clock) = NewDemo();
    Until(world, clock, () => world.State.Action == MouseAction.Nibble);
    Check(world.Startle(), "受惊首次触发失败");
    Check(!world.Startle(), "受惊重复触发重置了路径");
    Until(world, clock, () => world.State.Action == MouseAction.ReturnHome);
    world.SetHomeEntry(new Point2(50, 160));
    Until(world, clock, () => world.State.Action == MouseAction.Hide);
    Check(world.State.DesktopPosition.DistanceTo(new Point2(50, 160)) < 0.001,
        "返回目标没有跟随新入口");
    Check(!world.Startle(), "隐藏期间接受了重复受惊");
}

static void PauseDoesNotReplay()
{
    var clock = new FakeClock();
    var world = new World(clock);
    Step(world, clock, 0.2);
    var elapsed = world.State.ActionSeconds;
    world.SetPaused(true);
    Step(world, clock, 30);
    Check(world.State.ActionSeconds == elapsed, "暂停时动作仍推进");
    world.SetPaused(false);
    Step(world, clock, 0.033);
    Check(world.State.ActionSeconds - elapsed < 0.05, "恢复补播了积压时间");
}

static void FixedSeedIsRepeatable()
{
    var aClock = new FakeClock(); var bClock = new FakeClock();
    var a = new World(aClock, 42); var b = new World(bClock, 42);
    for (int i = 0; i < 900; i++)
    {
        Step(a, aClock, 0.033); Step(b, bClock, 0.033);
        Check(a.State.Action == b.State.Action && a.State.HomeX == b.State.HomeX &&
              a.State.ActionSeconds == b.State.ActionSeconds, "相同种子产生不同状态");
    }
}

static void DemoGate()
{
    var clock = new FakeClock();
    var world = new World(clock);
    world.SetMode(HomeMode.Expanded);
    Check(!world.TryStartDemo(), "展开模式启动了演示");
    world.SetMode(HomeMode.Micro);
    Check(world.TryStartDemo(), "微型家园不能启动演示");
    Check(!world.TryStartDemo(), "转场中再次启动演示");
}

static void InvalidSettingsRecover()
{
    var area = new WindowArea(0, 0, 1920, 1080);
    Check(!SettingsPolicy.TryParse("{broken", area, out var corrupt), "损坏 JSON 被接受");
    Check(corrupt == SettingsPolicy.Default(area), "损坏 JSON 未恢复默认");
    Check(!SettingsPolicy.TryParse("{\"X\":9999,\"Y\":30,\"MicroSize\":40,\"Diagnostics\":false}",
        area, out var offscreen), "屏幕外位置被接受");
    Check(offscreen == SettingsPolicy.Default(area), "越界设置未恢复默认");
    Check(!SettingsPolicy.TryParse("{\"X\":2147483647,\"Y\":30,\"MicroSize\":40,\"Diagnostics\":false}",
        area, out _), "整数溢出绕过位置校验");
    Check(SettingsPolicy.TryParse("{\"X\":100,\"Y\":100,\"MicroSize\":48,\"Diagnostics\":true}",
        area, out var valid) && valid.MicroSize == 48 && valid.Diagnostics, "合法设置被拒绝");
}

static void StartleAfterLongStaticNibble()
{
    var (world, clock) = NewDemo();
    Until(world, clock, () => world.State.Action == MouseAction.Nibble);
    clock.Elapsed += TimeSpan.FromMinutes(3); // a suspended process must not skip the startled pose
    Check(world.Startle(), "长时间静止后未能受惊");
    Step(world, clock, 0.033);
    Check(world.State.Action == MouseAction.Startled, "受惊动作被积压时间直接跳过");
}

static void GardenWorkAtFixedPlace()
{
    var clock = new FakeClock();
    var world = new World(clock);
    Until(world, clock, () => world.State.Action == MouseAction.Work);
    Check(world.State.HomeX == World.GardenInteractionX, "工作发生在菜地以外");
    Check(world.State.Garden == GardenStage.Bare, "工作前作物已变化");
    Until(world, clock, () => world.State.Garden == GardenStage.Carrots);
    Check(world.State.Action == MouseAction.Walk && world.State.Facing == Facing.Left,
        "工作完成后未带着可见作物变化回小屋");
    Until(world, clock, () => world.State.Action == MouseAction.Idle);
    Check(world.State.HomeX == World.HomeEntranceX, "没有回到小屋入口");
}

static void FacingFollowsMovement()
{
    var clock = new FakeClock();
    var world = new World(clock);
    Until(world, clock, () => world.State.Action == MouseAction.Walk);
    Check(world.State.Facing == Facing.Right, "向右去菜地时朝向错误");
    Until(world, clock, () => world.State.Action == MouseAction.Work);
    Until(world, clock, () => world.State.Action == MouseAction.Walk);
    Check(world.State.Facing == Facing.Left, "向左回小屋时朝向错误");

    var outsideClock = new FakeClock();
    var outside = new World(outsideClock);
    outside.SetHomeEntry(new Point2(300, 100));
    outside.SetFolderTarget(new Point2(100, 100));
    Check(outside.TryStartDemo(), "向左外出演示不能启动");
    Until(outside, outsideClock, () => outside.State.Owner == CharacterOwner.Desktop);
    Check(outside.State.Facing == Facing.Left, "桌面目标在左侧却朝右");
    Until(outside, outsideClock, () => outside.State.Action == MouseAction.Nibble);
    Check(outside.Startle() && outside.State.Facing == Facing.Right, "受惊回家未转向右侧入口");
}

static void FolderContinuity()
{
    var (world, clock) = NewDemo();
    var original = world.State.FolderPosition;
    Until(world, clock, () => world.State.Action == MouseAction.Nibble);
    Check(world.State.FolderPosition == original && !world.State.FolderBitten,
        "啃咬开始前文件夹被替换");
    for (var i = 0; i < 25; i++) Step(world, clock, 0.033);
    Check(world.State.FolderBitten && world.State.FolderPosition == original,
        "啃咬后文件夹位置改变或未出现咬痕");
    Check(world.Startle(), "啃咬后不能受惊");
    world.SetHomeEntry(new Point2(50, 160));
    Until(world, clock, () => world.State.Action == MouseAction.ReturnHome);
    Check(world.State.FolderPosition == original && world.State.FolderBitten,
        "回程中模拟文件夹消失或重置");
}

static (World, FakeClock) NewDemo()
{
    var clock = new FakeClock();
    var world = new World(clock);
    world.SetHomeEntry(new Point2(100, 100));
    world.SetFolderPosition(new Point2(220, 130));
    world.SetFolderTarget(new Point2(220, 130));
    Check(world.TryStartDemo(), "演示不能启动");
    return (world, clock);
}

static void Until(World world, FakeClock clock, Func<bool> condition)
{
    for (int i = 0; i < 1000 && !condition(); i++) Step(world, clock, 0.033);
    Check(condition(), "状态未在超时内到达");
}

static void Step(World world, FakeClock clock, double seconds)
{
    clock.Elapsed += TimeSpan.FromSeconds(seconds);
    world.Advance();
}

static void Check(bool condition, string message)
{
    if (!condition) throw new Exception(message);
}

sealed class FakeClock : IClock
{
    public TimeSpan Elapsed { get; set; }
    public TimeSpan Now => Elapsed;
}
