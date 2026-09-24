# P0 鼠鼠动作预览 v2

使用内置 `image_gen` 生成。参考图为用户提供的图 6（在家角色）与图 1（外出背包角色）。本图仅供视觉评估，尚未切成固定帧、接入程序或验证 40×40 观察口可读性。

## 初次生成提示词

```text
Use case: stylized-concept. Asset type: P0 2D side-view sprite sheet preview for a tiny Windows desktop mouse pet, intended to remain readable in a 40x40 logical-pixel home viewport. Use reference image 1 only for the same mouse's face/body/colors and HOME outfit, and reference image 2 only for the same mouse's green backpack in OUTDOOR outfit. Generate ONE true transparent PNG with exactly EIGHT independent sprites in an orderly invisible 4-column by 2-row grid, generous transparent gutters, no touching or overlap. Every mouse faces RIGHT in strict side view, same character model, same head/ear/body/tail proportions, same apparent body height, feet aligned to a consistent baseline per row. Warm pastoral 16-bit retro pixel art, limited palette, crisp square pixel clusters and hard edges, simplified shapes that survive reduction to about 26 pixels character height; recognizable large pink ears, gray-brown fur, cream belly/muzzle, dark eye, green scarf. TOP ROW: home outfit without backpack: (1) idle standing, (2) walk step A, (3) walk step B with alternate feet, (4) gardening work reaching toward one tiny sprout. BOTTOM ROW: outdoor outfit with the SAME mouse and a small green backpack: (1) walking, (2) visually nibbling one tiny self-drawn yellow pretend folder icon, (3) startled with raised paws, (4) returning home in a clear running pose. Make poses semantically distinct while keeping consistent frame dimensions and foot pivots. This is a practical sprite preview, not a poster or character concept board. No labels, no text, no typography, no UI frames, no window chrome, no checkerboard, no black background, no scenery, no 2.5D or isometric view, no floor shadows, no decorative sparkles, no gradients, no painterly smoothing, no fuzzy alpha halo, no stray colored pixels. Background outside each sprite must be completely transparent.
```

## 局部修正 1

```text
Edit this exact transparent 4-column by 2-row mouse sprite sheet. Preserve the canvas dimensions, transparent background, layout, pose positions, character identity, colors, pixel style, and ALL other sprites. Make ONE localized correction only: the THIRD sprite in the TOP row is a HOME walk frame and must have NO backpack, NO backpack strap, and NO yellow button. Replace that green backpack shape on its back with only the same small loose green scarf tail seen on the first two sprites in the top row. Keep its walking legs and all other details unchanged. Every bottom-row character retains its backpack. No text, no grid lines, no background.
```

## 局部修正 2

```text
Edit this exact 4-column by 2-row transparent pixel mouse sprite sheet. Preserve the exact 1774x887 canvas, transparency, positions, identity, all eight mouse poses, all outfits, colors and pixel style. Make ONE localized correction only to the SECOND sprite in the BOTTOM row: replace the yellow star-shaped object held in its paws with a small ordinary rectangular manila-yellow file folder icon, clearly identifiable by a small tab at its top edge and a tiny visual bite notch on one corner. It is a harmless imaginary drawn prop. Keep the mouse, backpack, paws, face and every other cell unchanged. No text, no scene, no grid lines, no background.
```
