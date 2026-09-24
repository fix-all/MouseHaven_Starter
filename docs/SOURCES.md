# 官方参考资料

核对日期：2026-09-24。下列支持技术机制；不支持销量、收入或特定资源占用保证。产品参数、阶段和预算是本项目建议。

## S0 · .NET 支持政策

用于核对 .NET 10 的 LTS 状态及稳定工具链，初始化时还应核对本机版本。

```text
https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core
```

## S1 · UpdateLayeredWindow

说明整窗口更新、参数、错误处理，以及尽量使用小分层窗口的性能建议。

```text
https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-updatelayeredwindow
```

## S2 · 分层窗口与透明命中

Window Features 的 Layered Windows 小节，说明 Alpha 为零区域与 WS_EX_TRANSPARENT 的不同命中行为。

```text
https://learn.microsoft.com/en-us/windows/win32/winmsg/window-features
```

## S3 · 不激活窗口

WS_EX_NOACTIVATE 的语义。实际点击、焦点、托盘菜单行为仍需实机验证。

```text
https://learn.microsoft.com/en-us/windows/win32/winmsg/extended-window-styles
```

## S4 · DPI Awareness

通过 manifest 设置 DPI 模式及 PerMonitorV2。不要混淆窗口逻辑尺寸与桌面物理坐标。

```text
https://learn.microsoft.com/en-us/windows/win32/hidpi/setting-the-default-dpi-awareness-for-a-process
```

## S5 · Codex 项目规则文件

官方说明 Codex 如何读取项目中的 AGENTS.md。

```text
https://developers.openai.com/codex/guides/agents-md
```

## S6 · Codex Windows 运行环境

官方说明原生 Windows 工作方式和沙箱隔离。编译工具、沙箱运行与真实桌面 UI 验收不要混为一谈。

```text
https://developers.openai.com/codex/windows
```
