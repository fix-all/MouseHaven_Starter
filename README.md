# MouseHaven P0 · Windows 桌面交互原型

这是可构建的 P0 原型源码，使用原创程序绘制占位图。当前实现范围为微型与展开横版家园、单一鼠鼠状态、托盘外出演示和受惊回家。最大化 2.5D 属于下一轮，本程序没有该按钮。此原型未通过真实用户桌面的完整验收，详见 [P0 报告](docs/P0_REPORT.md)。

## 环境与构建

目标系统为 Windows 11 x64，项目使用 .NET 10 Windows Desktop。实际验证的 SDK 为 10.0.401。项目不依赖第三方 NuGet 包。当前工作目录下 `.tools/dotnet/dotnet.exe` 是本地临时工具链，不属于源码交付；有匹配 SDK 的电脑可直接使用 `dotnet`。以下命令在仓库根目录的 PowerShell 中执行：

```powershell
$Dotnet = if (Test-Path '.\.tools\dotnet\dotnet.exe') { '.\.tools\dotnet\dotnet.exe' } else { 'dotnet' }
& $Dotnet --info
& $Dotnet build .\MouseHaven.slnx -c Release
& $Dotnet run --project .\tests\MouseHaven.Core.Tests\MouseHaven.Core.Tests.csproj -c Release
& $Dotnet run --project .\src\MouseHaven.Windows\MouseHaven.Windows.csproj -c Release
```

如电脑没有 .NET 10 SDK，可使用微软官方 `dotnet-install.ps1` 将 10.0.401 放进项目内 `.tools/dotnet`。这是项目本地工具链，不需要管理员权限，也不修改系统 PATH：

```powershell
New-Item -ItemType Directory -Force .tools | Out-Null
Invoke-WebRequest 'https://dot.net/v1/dotnet-install.ps1' -OutFile '.tools/dotnet-install.ps1'
& '.\.tools\dotnet-install.ps1' -Version 10.0.401 -InstallDir '.\.tools\dotnet' -NoPath
```

## 发布与运行

```powershell
& $Dotnet publish .\src\MouseHaven.Windows\MouseHaven.Windows.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o .\publish\MouseHaven-win-x64
& '.\publish\MouseHaven-win-x64\MouseHaven.Windows.exe'
```

`publish/MouseHaven-win-x64/MouseHaven.Windows.exe` 是自包含的 Release 程序。发布成功仅表示已生成程序，不代表透明命中、焦点、DPI、视觉或性能已在真实桌面通过。

## 操作

- 启动后，默认右下角附近出现 40×40 逻辑像素的家园观察口。单击展开为 480×270 的 16:9 横版画面；单击右上角减号收起。按住鼠标左键拖动家园可改位置。
- 托盘菜单可暂停/恢复、显示/隐藏家园、切换 32/40/48/64 微型尺寸、重置位置、启动“演示外出”和退出。家园右键也有菜单。
- 在微型、鼠鼠在家且未暂停时，先显示桌面，再从托盘启动外出演示。鼠鼠走出家园，到程序自绘的模拟文件夹旁；单击外出的鼠鼠后她跑回当前家园入口。家园不会自行展开。
- “诊断记录”开关每 5 秒将模式、角色归属、动作、绘制次数、进程 CPU 累计时间、工作集与私有字节写入 `%LOCALAPPDATA%\MouseHaven\diagnostics.log`。位置和尺寸存于同一目录的 `settings.json`；损坏设置会恢复默认并提示。

本程序不会读取真实桌面目录，不移动或修改用户文件与系统图标。文件夹、碎屑和啃咬均为应用窗口内的视觉占位。默认只支持单显示器首测；多显示器、全屏覆盖与 100%/150%/200% DPI 的真实交互尚待验收。

## 目录

- `src/MouseHaven.Core`：单一世界状态、时钟、行为与设置验证。
- `src/MouseHaven.Windows`：小尺寸 Win32 分层窗口、程序绘制、托盘和本地设置。
- `tests/MouseHaven.Core.Tests`：无需外部测试包的确定性核心测试。
- `assets/README.md`：占位美术来源与替换边界。
- `docs/PRODUCT.md`、`docs/P0_TASK.md`、`docs/ACCEPTANCE.md`：原始规格与验收项。
