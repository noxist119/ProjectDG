# CODEX STATUS

- Latest completed pass: Pass 2U — Opening HUD Guidance.
- Change: The old generic opening banner/tutorial text no longer appears. Entering the battlefield through the Lobby Battle button now starts one non-blocking, boss-warning-style center panel per run: `방어선 돌파 주의!` for 1.5 seconds, `몬스터가 아래 끝에 도달하면 / HP가 감소합니다` for the next 1.5 seconds, and `수호자를 소환해 / 막아내세요!` for the final 1.5 seconds. The same panel is centered, then fades out over 0.22 seconds and deactivates. Boss warnings restore their normal layout and remain isolated from the opening sequence.
- Input: The opening panel CanvasGroup remains non-interactable and does not block raycasts; Battle and Summon remain usable while it is displayed.
- Verification: `dotnet build Assembly-CSharp.csproj --no-restore` PASS (0 warnings, 0 errors). Unity PlayMode Smoke PASS with `runtimeErrors: 0`. The actual LobbyBattleButton UI path started the opening guidance, and the Pass 2U lifecycle regression verified all three stages, centered copy, fade/hide, and non-blocking input.
- Next task: Start the next user-requested ProjectDG pass.
