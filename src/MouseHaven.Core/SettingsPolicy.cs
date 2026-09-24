using System.Text.Json;

namespace MouseHaven.Core;

public sealed record HavenSettings(int X, int Y, int MicroSize, bool Diagnostics);
public readonly record struct WindowArea(int Left, int Top, int Right, int Bottom);

public static class SettingsPolicy
{
    public static HavenSettings Default(WindowArea area) =>
        new(Math.Max(area.Left, area.Right - 90), Math.Max(area.Top, area.Bottom - 100), 40, false);

    public static bool TryParse(string json, WindowArea area, out HavenSettings settings)
    {
        settings = Default(area);
        try
        {
            var parsed = JsonSerializer.Deserialize<HavenSettings>(json);
            if (parsed is null || parsed.MicroSize is not (32 or 40 or 48 or 64) ||
                parsed.X < area.Left || parsed.Y < area.Top ||
                (long)parsed.X + parsed.MicroSize > area.Right ||
                (long)parsed.Y + parsed.MicroSize > area.Bottom)
                return false;
            settings = parsed;
            return true;
        }
        catch (JsonException) { return false; }
    }
}
