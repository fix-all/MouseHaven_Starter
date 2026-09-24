using MouseHaven.Core;
using MouseHaven.Windows;

var tests = new (string Name, Action Run)[]
{
    ("所有精灵源矩形和缩小帧均不触边", AtlasFramesStayInsideBounds),
    ("走路干活奔跑啃咬使用不同真实帧", AnimationUsesDistinctFrames),
    ("左向帧是右向帧的镜像", FacingFramesMirror),
    ("微型和两档展开共享交接根节点", HandoffRootMatches),
    ("左右啃咬嘴部对准同一文件夹边缘", NibbleMouthTouchesFolder),
    ("同一文件夹啃咬前后尺寸与位置不变", FolderRendersInPlace)
};
var failed = 0;
foreach (var (name, test) in tests)
{
    try { test(); Console.WriteLine($"PASS {name}"); }
    catch (Exception ex) { failed++; Console.WriteLine($"FAIL {name}: {ex.Message}"); }
}
Console.WriteLine($"{tests.Length - failed}/{tests.Length} passed");
return failed == 0 ? 0 : 1;

static void AtlasFramesStayInsideBounds()
{
    using var frames = new SpriteFrames(); // constructor also checks every source edge and cached border
    Check(SpriteFrames.Specs.Length == Enum.GetValues<SpriteFrameId>().Length, "帧元数据不完整");
    var startled = SpriteFrames.Specs.Single(s => s.Id == SpriteFrameId.OutdoorStartled);
    Check(startled.SourceRect.Right == 1290, "受惊帧仍切入原图跨界尾尖");
    foreach (var spec in SpriteFrames.Specs)
    {
        using var bitmap = (Bitmap)frames.GetCachedFrame(spec.Id, Facing.Right).Clone();
        var visible = VisibleBounds(bitmap);
        Check(visible != Rectangle.Empty, $"空精灵帧：{spec.Id}");
        if (spec.Id is not (SpriteFrameId.OutdoorRunA or SpriteFrameId.OutdoorRunB or SpriteFrameId.OutdoorStartled))
            Check(Math.Abs(visible.Bottom - SpriteFrames.RootInCanvas.Y) <= 2,
                $"脚底锚点偏移：{spec.Id} bottom={visible.Bottom}");
    }
}

static void AnimationUsesDistinctFrames()
{
    Check(SpriteFrames.HomeFrame(MouseAction.Walk, 0) != SpriteFrames.HomeFrame(MouseAction.Walk, 0.17), "家中走路未换帧");
    Check(SpriteFrames.HomeFrame(MouseAction.Work, 0) != SpriteFrames.HomeFrame(MouseAction.Work, 0.31), "干活未换帧");
    Check(SpriteFrames.DesktopFrame(MouseAction.Roam, 0) != SpriteFrames.DesktopFrame(MouseAction.Roam, 0.17), "桌面走路未换帧");
    Check(SpriteFrames.DesktopFrame(MouseAction.ReturnHome, 0) != SpriteFrames.DesktopFrame(MouseAction.ReturnHome, 0.12), "跑回家未换帧");
    Check(SpriteFrames.DesktopFrame(MouseAction.Nibble, 0) != SpriteFrames.DesktopFrame(MouseAction.Nibble, 0.26), "啃咬未换帧");
    using var frames = new SpriteFrames();
    foreach (var (a, b, label) in new[]
    {
        (SpriteFrameId.HomeWalkA, SpriteFrameId.HomeWalkB, "家中走路"),
        (SpriteFrameId.HomeWorkA, SpriteFrameId.HomeWorkB, "干活"),
        (SpriteFrameId.OutdoorWalkA, SpriteFrameId.OutdoorWalkB, "桌面走路"),
        (SpriteFrameId.OutdoorRunA, SpriteFrameId.OutdoorRunB, "回家奔跑"),
        (SpriteFrameId.OutdoorNibbleA, SpriteFrameId.OutdoorNibbleB, "啃咬")
    })
        Check(PixelDifference(frames.GetCachedFrame(a, Facing.Right),
            frames.GetCachedFrame(b, Facing.Right)) > 40, $"{label}两帧画面没有真实差异");
    Check(SpriteFrames.Specs.Where(s => s.Id.ToString().StartsWith("Home")).All(s => !s.Backpack), "家中帧带背包");
    Check(SpriteFrames.Specs.Where(s => s.Id.ToString().StartsWith("Outdoor")).All(s => s.Backpack), "外出帧缺背包");
}

static void FacingFramesMirror()
{
    using var frames = new SpriteFrames();
    var right = frames.GetCachedFrame(SpriteFrameId.OutdoorWalkA, Facing.Right);
    var left = frames.GetCachedFrame(SpriteFrameId.OutdoorWalkA, Facing.Left);
    for (var y = 0; y < right.Height; y++)
    for (var x = 0; x < right.Width; x++)
        Check(right.GetPixel(x, y).ToArgb() == left.GetPixel(right.Width - 1 - x, y).ToArgb(), "左向图像不是精确镜像");
}

static void HandoffRootMatches()
{
    using var art = new Artwork();
    var clock = new FakeClock();
    var world = new World(clock);
    foreach (var mode in new[] { HomeMode.Micro, HomeMode.Expanded })
    foreach (var zoom in new[] { 1, 2 })
    foreach (var dpi in new[] { 1.0, 1.5, 2.0 })
    {
        world.SetMode(mode);
        art.ExpandedZoom = zoom;
        var local = art.EntryInHome(world.State, mode == HomeMode.Micro ? 40 : 480);
        var root = art.DesktopRoot;
        var absolute = new Point2(300 + local.X * dpi, 400 + local.Y * dpi);
        var surface = PlacementMath.ActorTopLeft(absolute, root, (float)dpi);
        Check(Math.Abs(surface.X + root.X * dpi - absolute.X) <= 0.5 &&
              Math.Abs(surface.Y + root.Y * dpi - absolute.Y) <= 0.5,
              $"交接根节点不一致：{mode} {zoom}x {dpi}DPI");
    }
}

static void NibbleMouthTouchesFolder()
{
    using var art = new Artwork();
    var center = new Point2(500, 300);
    foreach (var facing in new[] { Facing.Left, Facing.Right })
    foreach (var dpi in new[] { 1f, 1.5f, 2f })
    {
        var mouth = art.NibbleMouthOffset(facing);
        var root = PlacementMath.NibbleRoot(center, facing, mouth, dpi, dpi);
        var contactX = root.X + mouth.X * dpi;
        var contactY = root.Y + mouth.Y * dpi;
        var folderEdge = center.X + (facing == Facing.Right ? -19 : 19) * dpi;
        Check(Math.Abs(contactX - folderEdge) < 0.001 && Math.Abs(contactY - center.Y) < 0.001,
            $"嘴部未接触同一文件夹：{facing} {dpi}DPI");
    }
}

static void FolderRendersInPlace()
{
    using var art = new Artwork();
    var clock = new FakeClock();
    var world = new World(clock);
    world.SetHomeEntry(new Point2(100, 100));
    world.SetFolderPosition(new Point2(220, 130));
    world.SetFolderTarget(new Point2(190, 144));
    Check(world.TryStartDemo(), "演示启动失败");
    Until(world, clock, () => world.State.Action == MouseAction.Nibble);
    using var before = DrawFolder(art, world.State);
    for (var i = 0; i < 25; i++) Step(world, clock, 0.033);
    using var after = DrawFolder(art, world.State);
    Check(world.State.FolderPosition == new Point2(220, 130), "啃咬改变文件夹位置");
    var beforeBounds = VisibleBounds(before);
    var afterBounds = VisibleBounds(after);
    // Crumbs may extend below/left of the prop; the folder itself keeps its canvas and top/right edges.
    Check(before.Size == after.Size && beforeBounds.Top == afterBounds.Top &&
          beforeBounds.Right == afterBounds.Right,
        $"啃咬改变文件夹主体位置：{beforeBounds} -> {afterBounds}");
    Check(PixelDifference(before, after) > 0, "啃咬没有产生视觉咬痕");
}

static Bitmap DrawFolder(Artwork art, WorldState state)
{
    var bitmap = new Bitmap(48, 48);
    using var g = Graphics.FromImage(bitmap);
    g.Clear(Color.Transparent);
    art.Folder(g, state);
    return bitmap;
}

static Rectangle VisibleBounds(Bitmap bitmap)
{
    var minX = bitmap.Width; var minY = bitmap.Height; var maxX = -1; var maxY = -1;
    for (var y = 0; y < bitmap.Height; y++)
    for (var x = 0; x < bitmap.Width; x++)
        if (bitmap.GetPixel(x, y).A >= 20)
        {
            minX = Math.Min(minX, x); minY = Math.Min(minY, y);
            maxX = Math.Max(maxX, x); maxY = Math.Max(maxY, y);
        }
    return maxX < 0 ? Rectangle.Empty : Rectangle.FromLTRB(minX, minY, maxX + 1, maxY + 1);
}

static int PixelDifference(Bitmap a, Bitmap b)
{
    var count = 0;
    for (var y = 0; y < a.Height; y++)
    for (var x = 0; x < a.Width; x++)
        if (a.GetPixel(x, y).ToArgb() != b.GetPixel(x, y).ToArgb()) count++;
    return count;
}

static void Until(World world, FakeClock clock, Func<bool> condition)
{
    for (var i = 0; i < 1000 && !condition(); i++) Step(world, clock, 0.033);
    Check(condition(), "状态未在测试上限内到达");
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
