# 横版 2D 视觉闭环生成提示词

使用内置 imagegen。`scene/*` 以用户提供的田园像素物件图（先前图 5）为风格参考；`characters/mouse-outdoor-motion.png` 以用户提供的背包鼠鼠图（图 1）和本地 `previews/mouse-p0-v2-preview.png` 为角色参考；`characters/mouse-home-work.png` 以同一预览图为角色参考。参考图用于身份、配色与质感，不作为程序外部文件读取。以下是生成时的有效要求；生成结果仍需逐帧校准。

## 小屋 `scene/house-side.png`

```text
Create one original small mouse cottage for a horizontal 2D side-scrolling game. Match the reference's warm terracotta roof, cream plaster, brown wood, ivy, daisies and 16-bit pastoral pixel palette. Flat side-scroller elevation with an arched wooden door at ground level and one small window; no visible side wall, isometric camera or top-down ground plane. Readable at about 120x95 logical pixels. Crisp square pixel clusters, limited palette, transparent PNG around one isolated cottage. No text, UI, scenery, black background or commercial game elements.
```

## 空菜地 `scene/garden-bare.png`

```text
Create one short wide flat side-view bare garden bed, a horizontal strip of dark tilled soil with a thin grassy edge, in the reference's warm retro pixel style. Readable at about 95x22 logical pixels. Genuine transparent PNG, no crops, flowers, fence, house, character, text, poster or isometric/top-down perspective.
```

## 胡萝卜菜地 `scene/garden-carrots.png`

```text
Edit the referenced bare bed into its grown state. Preserve the bed's long horizontal silhouette, soil, edge, colors, position and transparent background. Add four small carrot plants with green tops and tiny orange shoulders, readable when the bed is about 95 pixels wide. Strict eye-level side view, no extra objects, scenery, text or perspective change.
```

## 花圃 `scene/flower-patch.png`

```text
Create one isolated short wide flat side-view flower cluster: two small white daisies, one soft pink blossom, a few leaves and a narrow grass strip. Match the reference's warm pastoral 16-bit pixel palette and dark olive outline. Readable at about 70x36 logical pixels. Genuine transparent PNG; no top-down/isometric bed, text, house, fence, scenery or black background.
```

## 外出动作 `characters/mouse-outdoor-motion.png`

```text
Create one transparent six-pose source sheet for the same gray-brown mouse with pink ears, cream belly, green scarf and small green backpack. Strict right-facing side view, same proportions, baseline and pixel palette. Invisible 3x2 grid with generous transparent gutters. Top: two genuinely different walk contact steps and one airborne run step. Bottom: a different run landing step, empty-pawed crouched nibble with mouth open, empty-pawed crouched nibble with mouth closed. No folder or attached object; the game draws a separate fixed folder. Crisp pixel clusters, no text, effects, scenery, grid lines, black or checkerboard background.
```

## 在家干活 `characters/mouse-home-work.png`

```text
Create two genuinely different right-facing side-view garden-work frames for the same mouse without backpack. Frame A reaches one paw toward empty ground with closed mouth; frame B crouches lower and reaches the other paw with mouth slightly open. Keep body proportions and foot/root point consistent. No soil, sprout, vegetable, tool, flower, folder or other attached prop. Transparent PNG, crisp square pixel clusters, no text, scenery or background.
```
