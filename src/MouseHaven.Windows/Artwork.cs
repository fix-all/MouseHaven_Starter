using System.Drawing.Drawing2D;
using MouseHaven.Core;

namespace MouseHaven.Windows;

// Original procedural placeholder art. Everything is drawn inside our own windows.
internal sealed class Artwork : ISceneRenderer
{
    private static readonly Color Sky = Color.FromArgb(255, 192, 231, 224);
    private static readonly Color Grass = Color.FromArgb(255, 107, 174, 106);
    private static readonly Color Soil = Color.FromArgb(255, 115, 78, 57);
    private static readonly Color Fur = Color.FromArgb(255, 180, 137, 110);
    private static readonly Color DarkFur = Color.FromArgb(255, 121, 81, 69);
    private static readonly Color Pink = Color.FromArgb(255, 230, 151, 156);

    private static double CameraX(WorldState state, int width)
    {
        var focus = state.Owner == CharacterOwner.Home ? state.HomeX : World.HomeEntranceX;
        return state.Mode == HomeMode.Micro ? focus - width * 0.4 :
            Math.Clamp(focus - width / 2.0, 0, 800 - width);
    }

    private static double CameraY(WorldState state) => state.Mode == HomeMode.Micro ? 183 : 0;

    public Point2 EntryInHome(WorldState state, int width) =>
        new(World.HomeEntranceX - CameraX(state, width), 204 - CameraY(state));

    public void Home(Graphics g, WorldState state, int width, int height)
    {
        using var border = Rounded(0, 0, width, height, state.Mode == HomeMode.Micro ? 7 : 14);
        g.SetClip(border);
        using (var sky = new SolidBrush(Sky)) g.FillRectangle(sky, 0, 0, width, height);
        var cx = (float)CameraX(state, width);
        var cy = (float)CameraY(state);
        var saved = g.Save();
        g.TranslateTransform(-cx, -cy);

        using (var distant = new SolidBrush(Color.FromArgb(255, 146, 203, 177)))
        {
            g.FillEllipse(distant, -30, 163, 215, 100);
            g.FillEllipse(distant, 190, 159, 240, 100);
            g.FillEllipse(distant, 460, 170, 245, 90);
        }
        using (var ground = new SolidBrush(Grass)) g.FillRectangle(ground, -50, 211, 900, 100);
        using (var earth = new SolidBrush(Soil)) g.FillRectangle(earth, -50, 224, 900, 50);
        using (var edge = new Pen(Color.FromArgb(255, 67, 128, 77), 3)) g.DrawLine(edge, -50, 211, 850, 211);

        DrawHouse(g);
        DrawPlot(g);
        DrawFlower(g, 355, 210);
        DrawFlower(g, 393, 210);
        DrawFlower(g, 570, 210);

        if (state.Owner == CharacterOwner.Home)
            Mouse(g, (float)state.HomeX, 214, state.Action, state.ActionSeconds);
        g.Restore(saved);

        using (var outline = new Pen(Color.FromArgb(235, 73, 109, 86), state.Mode == HomeMode.Micro ? 1.5f : 2.5f))
            g.DrawPath(outline, border);
        if (state.Mode == HomeMode.Expanded) DrawCollapse(g, width);
        g.ResetClip();
    }

    public void DesktopMouse(Graphics g, WorldState state)
    {
        Mouse(g, 16, 27, state.Action, state.ActionSeconds);
    }

    public void Folder(Graphics g, WorldState state)
    {
        using var shadow = new SolidBrush(Color.FromArgb(70, 75, 59, 40));
        g.FillEllipse(shadow, 6, 34, 37, 8);
        using var tab = new SolidBrush(Color.FromArgb(255, 247, 194, 93));
        using var front = new SolidBrush(Color.FromArgb(255, 235, 161, 66));
        using var pen = new Pen(Color.FromArgb(255, 143, 96, 47), 1.4f);
        g.FillRectangle(tab, 8, 10, 20, 9);
        g.FillRectangle(front, 5, 16, 38, 21);
        g.DrawRectangle(pen, 5, 16, 38, 21);
        if (state.Action == MouseAction.Nibble)
        {
            using var bite = new SolidBrush(Color.Transparent);
            g.CompositingMode = CompositingMode.SourceCopy;
            g.FillEllipse(bite, 3, 22, 11, 11);
            g.CompositingMode = CompositingMode.SourceOver;
            using var crumb = new SolidBrush(Color.FromArgb(255, 246, 197, 114));
            g.FillRectangle(crumb, 4, 38, 3, 2);
            g.FillRectangle(crumb, 12, 40, 2, 2);
        }
    }

    private static void DrawHouse(Graphics g)
    {
        using var wall = new SolidBrush(Color.FromArgb(255, 239, 217, 169));
        using var roof = new SolidBrush(Color.FromArgb(255, 137, 87, 77));
        using var door = new SolidBrush(Color.FromArgb(255, 94, 70, 65));
        g.FillRectangle(wall, 19, 151, 91, 62);
        g.FillPolygon(roof, [new Point(12, 153), new Point(65, 118), new Point(119, 153)]);
        g.FillRectangle(door, 48, 174, 25, 39);
        using var window = new SolidBrush(Color.FromArgb(255, 140, 202, 194));
        g.FillRectangle(window, 82, 170, 18, 17);
    }

    private static void DrawPlot(Graphics g)
    {
        using var soil = new SolidBrush(Color.FromArgb(255, 96, 67, 51));
        using var leaf = new SolidBrush(Color.FromArgb(255, 58, 141, 77));
        g.FillEllipse(soil, 230, 207, 98, 19);
        for (int i = 0; i < 4; i++)
        {
            var x = 245 + i * 20;
            g.FillEllipse(leaf, x, 199, 12, 7);
            g.FillEllipse(leaf, x + 5, 194, 11, 8);
        }
    }

    private static void DrawFlower(Graphics g, int x, int y)
    {
        using var stem = new Pen(Color.FromArgb(255, 50, 139, 71), 2);
        using var petal = new SolidBrush(Color.FromArgb(255, 246, 149, 172));
        using var center = new SolidBrush(Color.FromArgb(255, 246, 207, 85));
        g.DrawLine(stem, x, y, x, y - 17);
        g.FillEllipse(petal, x - 6, y - 24, 8, 8);
        g.FillEllipse(petal, x + 1, y - 24, 8, 8);
        g.FillEllipse(petal, x - 3, y - 29, 8, 8);
        g.FillEllipse(center, x - 1, y - 23, 5, 5);
    }

    private static void Mouse(Graphics g, float x, float footY, MouseAction action, double elapsed)
    {
        var stride = action is MouseAction.Walk or MouseAction.ExitHome or MouseAction.Roam or MouseAction.ReturnHome
            ? (float)Math.Sin(elapsed * 15) * 2.2f : 0;
        var paw = action is MouseAction.Work or MouseAction.Nibble ? (float)Math.Sin(elapsed * 12) * 2f : 0;
        using var tail = new Pen(Pink, 2.2f) { StartCap = LineCap.Round, EndCap = LineCap.Round };
        g.DrawBezier(tail, x + 7, footY - 10, x + 13, footY - 20, x + 21, footY - 9, x + 20, footY - 4);
        using var body = new SolidBrush(Fur);
        using var head = new SolidBrush(DarkFur);
        using var ear = new SolidBrush(Pink);
        using var eye = new SolidBrush(Color.FromArgb(255, 38, 40, 39));
        g.FillEllipse(body, x - 10, footY - 16, 19, 14);
        g.FillEllipse(head, x - 13, footY - 19, 13, 12);
        g.FillEllipse(body, x - 11, footY - 26, 8, 10);
        g.FillEllipse(body, x - 4, footY - 27, 8, 10);
        g.FillEllipse(ear, x - 9, footY - 24, 4, 6);
        g.FillEllipse(ear, x - 2, footY - 25, 4, 6);
        g.FillEllipse(eye, x - 10, footY - 16, 2.4f, 2.4f);
        g.FillEllipse(ear, x - 14, footY - 11, 3.5f, 3.5f);
        using var feet = new Pen(DarkFur, 2) { StartCap = LineCap.Round, EndCap = LineCap.Round };
        g.DrawLine(feet, x - 4, footY - 4, x - 5 + stride, footY);
        g.DrawLine(feet, x + 4, footY - 4, x + 5 - stride, footY);
        g.DrawLine(feet, x - 5, footY - 9, x - 9, footY - 6 + paw);
    }

    private static void DrawCollapse(Graphics g, int width)
    {
        using var fill = new SolidBrush(Color.FromArgb(235, 250, 245, 228));
        using var pen = new Pen(Color.FromArgb(255, 70, 104, 81), 2);
        g.FillEllipse(fill, width - 31, 8, 22, 22);
        g.DrawLine(pen, width - 25, 19, width - 15, 19);
    }

    private static GraphicsPath Rounded(float x, float y, float width, float height, float radius)
    {
        var path = new GraphicsPath();
        var d = radius * 2;
        path.AddArc(x, y, d, d, 180, 90);
        path.AddArc(x + width - d, y, d, d, 270, 90);
        path.AddArc(x + width - d, y + height - d, d, d, 0, 90);
        path.AddArc(x, y + height - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }
}
