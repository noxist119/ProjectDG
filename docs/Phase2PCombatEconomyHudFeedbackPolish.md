# Pass 2P - Combat Economy HUD Feedback Polish

## Scope

Presentation and successful-purchase audio feedback only. Gameplay balance values are unchanged.

## Exact HUD coordinates

| Element | RectTransform target |
| --- | --- |
| BuildReadoutPanel | Keep X; anchored Y = `630` |
| GradeUpgradeBar | anchored X = `0`, Y = `470` |
| SummonGradeLuckUpgrade | anchored X = `-230`; existing Y and size retained |
| SummonGradeLuckInfoButton | anchored X = `-80`; existing Y and size retained |

## High-grade chance readability

- The Info tooltip title uses font size `23`.
- The tooltip body uses font size `20` with Best Fit disabled.
- The tooltip was expanded to `620 x 210`; title/body text rectangles were increased to prevent clipping.
- The high-grade chance label explicitly enables rich text.
- Only the changing values use `#FFD84A`: level, Epic+ probability, and GOLD cost.
- The MAX line keeps Epic+ `7` highlighted without changing any chance or cost values.

## Upgrade feedback

- A successful grade stat upgrade plays exactly one normal button request and one normal summon appearance request.
- A rejected grade upgrade plays neither success feedback request.
- A successful high-grade chance investment plays exactly one normal button request and no summon appearance request.
- Calls are inside the successful controller purchase paths, so insufficient GOLD, MAX, blocking choices, defeat, and other rejected requests remain silent.
- The existing immediate stat application to current and future units is unchanged.

## Validation

- `Assembly-CSharp`: 0 warnings, 0 errors.
- `Assembly-CSharp-Editor`: 0 errors; one existing `DefenseGameBatchPlaytest.RunResult.emptyGradeUpgradeAttemptCount` CS0649 warning remains.
- `git diff --check`: no whitespace errors (Git reports only local LF-to-CRLF normalization notices).
- Static smoke coverage now checks the exact coordinates, tooltip font/layout, rich-text numeric emphasis, combat availability, independent Fate placement, and successful/rejected audio request routing.
- Unity PlayMode Smoke: **not executed in this pass** because the ProjectDG Unity Editor was already open; no second batch Editor was launched against the active project.

## Gameplay balance change

**NONE**. Summon Grade Luck levels, costs, probability milestones, combat-time availability, retry reset semantics, and Grade Upgrade stats/costs are unchanged.