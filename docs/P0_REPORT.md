# MouseHaven P0 实施与验证记录

记录日期：2026-09-24。P0 源码和 Release 程序已生成；**P0 尚未通过真实桌面放行条件**。本报告把代码、自动化测试、隔离环境窗口冒烟检查和用户桌面验收分开。

## 实施范围

- 一个 `WorldState` 和一个行为调度器；显示模式、角色所有权、家园坐标、桌面坐标、动作计时分别保存。切换横版视图不会建立第二只鼠鼠。
- 家园为 32/40/48/64 微型或 480×270 展开的小分层窗口；桌面鼠鼠和模拟文件夹各用紧包自身的小窗口。家园内部镜头随鼠鼠位置移动，窗口锚点保持不变。
- 原创几何绘制鼠鼠、房子、菜地、花和模拟文件夹。外出时角色所有权由家园转到桌面；点击桌面鼠鼠后返回当前入口。啃咬不关联真实文件。
- 托盘提供暂停、显示、尺寸、重置、演示、诊断和退出。未启用整屏透明表面；静止家园不持续重绘，隐藏在家时停止帧定时器。
- 本地设置只写 `%LOCALAPPDATA%\MouseHaven`。没有桌面目录枚举、网络服务、2.5D 或账户功能。

## 已执行命令与结果

环境：Windows 11 专业版 64 位，版本 10.0.26200；项目本地 .NET SDK 10.0.401，Windows Desktop Runtime 10.0.12。SDK 放在 `.tools/dotnet`，未安装系统组件或更改系统配置。

| 检查 | 实际执行 | 结果 |
| --- | --- | --- |
| 构建 | `.tools\dotnet\dotnet.exe build MouseHaven.slnx -c Release` | 通过，最终顺序执行为 0 警告、0 错误 |
| 核心测试 | `.tools\dotnet\dotnet.exe run --project tests\MouseHaven.Core.Tests\MouseHaven.Core.Tests.csproj -c Release` | 7/7 通过 |
| 发布 | `.tools\dotnet\dotnet.exe publish src\MouseHaven.Windows\MouseHaven.Windows.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o publish\MouseHaven-win-x64` | 通过，生成 `MouseHaven.Windows.exe` |
| 隔离环境冒烟 | 用本地 `dotnet` 启动 Release DLL；枚举该进程窗口，并以 Win32 消息模拟家园点击与关闭 | 启动有 40×40 窗口；模拟点击后为 480×270；关闭后进程退出码 0 |
| 发布版冒烟 | 启动自包含 `MouseHaven.Windows.exe`，枚举窗口并发送关闭消息 | 进程保持运行且有 40×40 窗口；关闭消息后进程结束 |

核心测试具体覆盖：模式切换保持动作进度、Home → Desktop → Home 的单一所有权、返回目标更新和重复点击防重、暂停恢复不补播、固定随机种子、演示启动门槛，以及损坏/越界设置恢复默认。

## Windows 桌面验收

`ACCEPTANCE.md` 的 A01–A15 **均为未测试**。隔离桌面的窗口枚举和消息注入不能证明真实画面是否可见、鼠标是否命中、透明区域是否穿透、窗口是否抢焦点，也不能代替 100 次切换或多 DPI 测试。当前环境抓屏得到黑色画面，无法可靠目视确认像素效果；没有把它当作产品截图。

需要在用户真实 Windows 桌面运行 Release 程序，逐项填写 A01–A15。优先检查微型角色可辨认度、透明角落/鼠鼠点击、窗口焦点、外出交接与回程、拖动入口、100 次展开收起和退出清理。单显示器先测；多显示器支持尚未验证。

## 性能

`ACCEPTANCE.md` 的六类 Release 性能场景均**未测试**。没有取得可代表真实桌面合成负担的 60 秒/5 分钟样本，也没有泄漏、CPU、工作集或 GDI 增长的放行结论。诊断记录开关已经实现，可用于后续采样；当前没有编造性能数据。

## 放行判断

源码、确定性测试与自包含 Release 程序已交付。真实 Windows UI 测试、DPI 验证和性能基线仍缺失，因此按 `ACCEPTANCE.md` 的放行条件，不能标记“P0 已完成验收”或“可发布成品”。本轮到 P0 原型为止，不进入 2.5D 下一轮。
