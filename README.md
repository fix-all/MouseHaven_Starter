# MouseHaven · 横版 2D 视觉闭环试用版

这是在 P0 小窗口与单一世界状态上完成的一段横版生活与外出演出：小屋→菜地干活→胡萝卜出现→回小屋；托盘触发桌面外出、靠近同一个模拟文件夹啃咬、受惊跑回当前入口。托盘可切换 1×/2× 展开镜头和几何调试画法。最大化 2.5D 属于后续正式需求，本程序没有该按钮。真实桌面验收和性能结论见 [2D 视觉报告](docs/2D_VISUAL_REPORT.md)，历史 P0 记录仍在 [P0 报告](docs/P0_REPORT.md)。

## 环境与构建

目标系统为 Windows 11 x64，项目使用 .NET 10 Windows Desktop。实际验证的 SDK 为 10.0.401。项目不依赖第三方 NuGet 包。当前工作目录下 `.tools/dotnet/dotnet.exe` 是本地临时工具链，不属于源码交付；有匹配 SDK 的电脑可直接使用 `dotnet`。以下命令在仓库根目录的 PowerShell 中执行：

```powershell
$Dotnet = if (Test-Path '.\.tools\dotnet\dotnet.exe') { '.\.tools\dotnet\dotnet.exe' } else { 'dotnet' }
& $Dotnet --info
& $Dotnet build .\MouseHaven.slnx -c Release
& $Dotnet run --project .\tests\MouseHaven.Core.Tests\MouseHaven.Core.Tests.csproj -c Release
& $Dotnet run --project .\tests\MouseHaven.Windows.Tests\MouseHaven.Windows.Tests.csproj -c Release
& $Dotnet run --project .\src\MouseHaven.Windows\MouseHaven.Windows.csproj -c Release
```

如电脑没有 .NET 10 SDK，可使用微软官方 `dotnet-install.ps1` 将 10.0.401 放进项目内 `.tools/dotnet`。这是项目本地工具链，不需要管理员权限，也不修改系统 PATH：

```powershell
New-Item -ItemType Directory -Force .tools | Out-Null
Invoke-WebRequest 'https://dot.net/v1/dotnet-install.ps1' -OutFile '.tools/dotnet-install.ps1'
& '.\.tools\dotnet-install.ps1' -Version 10.0.401 -InstallDir '.\.tools\dotnet' -NoPath
```

## 发布与运行

公开下载的 [v0.1.0-p0 预发布版](https://github.com/fix-all/MouseHaven_Starter/releases/tag/v0.1.0-p0) 是旧几何占位原型。下列命令生成当前本地 2D 视觉试用版：

```powershell
& $Dotnet publish .\src\MouseHaven.Windows\MouseHaven.Windows.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o .\publish\MouseHaven-2d-visual-win-x64
& '.\publish\MouseHaven-2d-visual-win-x64\MouseHaven.Windows.exe'
```

该目录下的 `MouseHaven.Windows.exe` 为自包含 Release 程序。构建和离屏渲染不能证明真实桌面的透明命中、焦点、DPI 或性能已通过。

## 操作

- 启动后，默认右下角附近出现 40×40 逻辑像素的家园观察口。单击展开为 480×270 的 16:9 横版画面；单击右上角减号收起。按住鼠标左键拖动家园可改位置。
- 托盘菜单可暂停/恢复、显示/隐藏家园、切换 32/40/48/64 微型尺寸、设置展开镜头 1×/2×、重置位置、启动“演示外出”和退出。家园右键也有菜单。
- 默认显示侧视像素小屋、菜地、花圃与多帧鼠鼠。托盘“侧视像素场景”可切换到旧几何调试画法，不重置动作与位置。外出的鼠鼠只使用约 48×44 逻辑像素的小窗口。
- 在微型、鼠鼠在家且未暂停时，先显示桌面，再从托盘启动外出演示。鼠鼠走出家园，到程序自绘的模拟文件夹旁；单击外出的鼠鼠后她跑回当前家园入口。家园不会自行展开。
- “诊断记录”开关每 5 秒将模式、角色归属、动作、绘制次数、进程 CPU 累计时间、工作集与私有字节写入 `%LOCALAPPDATA%\MouseHaven\diagnostics.log`。位置和尺寸存于同一目录的 `settings.json`；损坏设置会恢复默认并提示。

本程序不会读取真实桌面目录，不移动或修改用户文件与系统图标。文件夹、碎屑和啃咬均为应用窗口内的视觉占位。默认只支持单显示器首测；多显示器、全屏覆盖与 100%/150%/200% DPI 的真实交互尚待验收。

## 目录

- `src/MouseHaven.Core`：单一世界状态、时钟、行为与设置验证。
- `src/MouseHaven.Windows`：小尺寸 Win32 分层窗口、缓存的场景与角色图集、镜头、托盘和本地设置。
- `tests/MouseHaven.Core.Tests`、`tests/MouseHaven.Windows.Tests`：确定性状态、资源边界、动作帧、根节点和文件夹连续性测试。
- `assets/README.md`：侧视资源清单与来源。
- `docs/2D_VISUAL_TASK.md`、`docs/2D_VISUAL_ACCEPTANCE.md`：当前阶段范围与验收。
- `docs/PRODUCT.md`、`docs/P0_TASK.md`、`docs/ACCEPTANCE.md`：产品与历史 P0 规格。
