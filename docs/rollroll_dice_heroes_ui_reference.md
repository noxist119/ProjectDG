# RollRoll Dice Heroes UI Reference

Use this document as the visual direction for future DefenseGame UI work. The reference comes from the user's previous game, "롤롤 다이스히어로즈".

## Overall Mood

- Bright toy-like mobile game UI over a 3D board scene.
- Rounded, chunky, highly readable controls.
- Strong contrast between deep navy dim overlays and saturated reward/action colors.
- UI should feel playful, tactile, and collectible rather than flat or minimalist.

## Source Asset Folder

The original RollRoll-style UI assets are stored outside this Unity project at:

`D:\프로젝트리소스\!RandomChess\!RandomChess_Client\RandomChess\Assets\03.Resource\Image\UI`

Current scan:

- About 353 PNG files.
- About 15 MB total.
- Main folders: `Assets`, `InGame`, `OutGame`.
- Useful subfolders include `Assets/img_dice`, `Assets/img_icon`, `Assets/img_item`, `Assets/img_ranked_grade`, `OutGame/Common`, `OutGame/Icons`, `OutGame/LobbyCanvas`, `OutGame/OpenLootBox`, `OutGame/Roll`, and `OutGame/Collection`.

Import guidance:

- Use this folder as the source/reference.
- Do not blindly copy old `.meta` files from the previous Unity project unless preserving GUIDs is intentionally needed.
- Prefer copying selected `.png` files into `Assets/Art/Ui/RollRollReference` or feature-specific UI folders in this project, then let Unity generate new `.meta` files.
- Copy only the assets needed for the current UI pass to keep the project clean.

## Imported First Pass

Selected PNG assets have been copied into this project at:

`Assets/Resources/UI/RollRoll`

Preview sheet:

`docs/rollroll_selected_assets_preview.png`

Applied in code:

- `FloatingCombatUI` now loads `InGame/dice-1.png` through `InGame/dice-6.png` as compact unit grade/dice icons.
- `FloatingCombatUI` now uses `InGame/minimi-ui-gauge-panel.png`, `InGame/minimi-ui-gauge-own.png`, and `InGame/mana-gauge.png` for the compact overhead unit gauge.

## Imported Second Pass

Additional reference-looking PNG assets have been copied into:

`Assets/Resources/UI/RollRoll`

New folders:

- `Lobby`: lobby panels, preset buttons, top/bottom menu parts.
- `Collection`: inventory/card backgrounds and card progress tracks.
- `CharacterInfo`: character detail popup panels, title bars, progress fills.
- `Roll`: dice face icons and roll result label background.
- `DiceTower`: rank backgrounds, speech bubbles, tab assets, decorative clouds.
- `Minimi`: prerendered character portraits used by cards and detail panels.

Preview sheet:

`docs/rollroll_second_pass_assets_preview.png`

Applied in code:

- `CharacterCollectionUI` now uses RollRoll card/popup sprites for runtime-generated panels and buttons.
- `CharacterCollectionUI` now maps hero IDs to RollRoll `Minimi` portrait sprites, with text fallback if a portrait is unavailable.
- `MetaFlowUI` now uses RollRoll card/button/modal sprites for lobby, loadout, shop, matchmaking, and result overlays.
- `MetaFlowUI` now applies RollRoll preset button sprites and Minimi portraits to featured/loadout roster cards.

## Expanded Runtime Pool

The larger useful pool has also been copied into `Assets/Resources/UI/RollRoll` so it is easier to find from Unity and easier to load from runtime UI code.

Added groups:

- `DiceIcons`: 66 dice/skill icons.
- `GradeAndGoodsIcons`: grade badges, nametags, currency, trophy, and ticket icons.
- `Items`: reward box, ruby, gold, dice token, mode ticket.
- `RankedGrade`: bronze/silver/gold/diamond/master rank emblems.
- `InGame`: reroll/end-turn buttons, top-view gauges, boss HP parts, speech bubbles, dice faces.
- `Common`: shared popup, button, gauge, slider, toggle, tooltip, list, and reward tile parts.
- `OpenLootBox`, `GameModePopup`, `RaidDifficulty`: small modal/battle mode support assets.

Preview sheet:

`docs/rollroll_expanded_resources_preview.png`

## Color Direction

- Background overlays: deep navy / blue-black with high opacity, often around 70-90%.
- Primary action: coral red-orange for battle/start/close actions.
- Positive action: vivid green for confirm/reward/double reward.
- Secondary action: bright sky blue for lobby/back/neutral actions.
- Premium currency: magenta/purple gem.
- Coin/reward: warm gold/yellow.
- Panels: blue, violet, or slate-blue bodies with light rim/highlight strokes.

## Shape Language

- Large rounded rectangles and pill buttons.
- Thick outlines, inner shadows, and soft drop shadows.
- Card corners are very rounded.
- Important panels often have a layered look: bright header, darker content body, and bottom action buttons.
- Bottom navigation uses large icon tabs with selected tab in vivid blue.

## Typography

- Bold, rounded Korean text.
- White text with dark shadow/outline for readability.
- Large numbers and timers use extra-heavy weight.
- Avoid thin text in gameplay-critical UI.

## Lobby UI Notes

- Top row should use compact currency capsules with icon + value + green plus button.
- Player info uses a gold badge/rank block.
- Main battle button is oversized, centered, coral red-orange.
- Deck/cards row should use icon cards with level badges.
- Timed event icons can sit at left/right edges with small dark timer pills.
- Bottom navigation should be icon-first and chunky.

## Matchmaking / Modal Notes

- Use full-screen navy dim overlay.
- Keep modal content centered vertically.
- Use a large crossed-swords or combat icon above the timer.
- Timer should be bright mint/green and large.
- Close button should be coral red-orange.

## Result UI Notes

- Use full-screen darkened battlefield as backdrop.
- Victory uses large blue ribbon, gold title, confetti, and celebratory props.
- Rewards are centered as big icon cards.
- Primary buttons: lobby in blue, bonus/reward action in green.
- Include daily limit or reward cap panel in purple when needed.

## Inventory / Character UI Notes

- Inventory grid uses 3 columns of rounded white cards.
- Each card has a large character render, name, level badge, progress bar, and optional check mark.
- Details popup uses a bright grade-colored header, large character art, progress bar, tab strip, stat cards, and large bottom actions.
- Locked/unowned cards may be gray/desaturated, but still keep the same card silhouette.

## In-Game HUD Notes

- Keep battlefield mostly visible.
- Unit health bars should be compact and readable: small dice/grade icon plus short HP bar.
- Avoid wide name panels above units because adjacent units overlap.
- Important player/opponent hearts and deck cards can sit in compact dark rounded containers.
- Bottom combat controls should be large and thumb-friendly.

## Practical Rule For This Project

When making new UI:

1. Use a dark translucent blocker for full-screen modals.
2. Prefer rounded, thick, icon-led controls.
3. Use saturated action colors instead of muted flat colors.
4. Keep in-game unit UI compact enough for adjacent board slots.
5. Make text bold with outline/shadow before increasing panel size.
6. Preserve the toy-like 3D board as the visual center whenever possible.
