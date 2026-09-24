using System.Text.Json;
using MouseHaven.Core;

namespace MouseHaven.Windows;

internal static class SettingsStore
{
    private static readonly string DirectoryPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MouseHaven");
    private static readonly string SettingsPath = Path.Combine(DirectoryPath, "settings.json");
    internal static string DiagnosticsPath => Path.Combine(DirectoryPath, "diagnostics.log");

    internal static (HavenSettings Settings, string? Warning) Load(Rectangle work)
    {
        var fallback = Default(work);
        if (!File.Exists(SettingsPath)) return (fallback, null);
        try
        {
            if (!SettingsPolicy.TryParse(File.ReadAllText(SettingsPath), Area(work), out var settings))
                return (fallback, "设置文件无效或位置已离开主屏幕，已恢复默认位置。");
            return (settings, null);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return (fallback, "无法读取设置，已恢复默认位置：" + ex.Message);
        }
    }

    internal static HavenSettings Default(Rectangle work) => SettingsPolicy.Default(Area(work));
    private static WindowArea Area(Rectangle work) =>
        new(work.Left, work.Top, work.Right, work.Bottom);

    internal static void Save(HavenSettings settings)
    {
        Directory.CreateDirectory(DirectoryPath);
        var temp = SettingsPath + ".tmp";
        File.WriteAllText(temp, JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true }));
        File.Move(temp, SettingsPath, true);
    }

    internal static void AppendDiagnostics(string line)
    {
        Directory.CreateDirectory(DirectoryPath);
        File.AppendAllText(DiagnosticsPath, line + Environment.NewLine);
    }
}
