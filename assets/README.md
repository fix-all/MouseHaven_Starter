# 横版 2D 素材清单

所有 PNG 均带透明通道，在程序启动时作为程序集资源加载并缩到固定逻辑尺寸；绘制循环不重新读盘。它们是这一轮 2D 视觉试用资源，尚未通过真实桌面小尺寸与 DPI 验收，不应描述为最终美术。生成提示词见 `GENERATED_PROMPTS.md`，旧动作预览的生成过程见 `previews/mouse-p0-v2-prompt.md`。

| 资源 | 来源与用途 |
| --- | --- |
| `previews/mouse-p0-v2-preview.png` | 以用户提供的无背包/背包鼠鼠图为参考生成；只取家中待机、两帧走路和受惊帧。旧图集中跨界的冲刺尾巴帧不使用。 |
| `characters/mouse-home-work.png` | 同角色无背包两帧干活姿势，不带土或作物；干活对象由场景层绘制。 |
| `characters/mouse-outdoor-motion.png` | 同角色背包状态的两帧走、两帧跑、两帧空手啃咬姿势。 |
| `scene/house-side.png`、`garden-bare.png`、`garden-carrots.png`、`flower-patch.png` | 参考用户提供的田园像素图生成的独立侧视资源。菜地前后用两个同位素材表现可见变化。 |
| `props/desktop-icons-source.png` | 用户提供的图 3；程序只裁出普通模拟文件夹，啃咬效果在同一窗口中加透明咬痕与碎屑。 |

每帧的 sourceRect、统一脚底根节点、背包状态和啃咬嘴部锚点记录在 `src/MouseHaven.Windows/SpriteFrames.cs`。场景裁切与实际逻辑尺寸记录在 `RasterAssets.cs`。几何房子/菜地/花仅保留在托盘可切换的调试画法。没有真实文件或系统图标素材，也没有商业游戏素材。
