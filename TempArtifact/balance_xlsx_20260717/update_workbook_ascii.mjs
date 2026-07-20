import fs from "node:fs/promises";
import { FileBlob, SpreadsheetFile } from "file:///C:/Users/noxis/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/@oai/artifact-tool/dist/artifact_tool.mjs";
const root = "D:/GameDev/ProjectDG";
const outputDir = root + "/outputs/balance_xlsx_20260717";
const previewDir = root + "/TempXlsxPreviewAfter";
await fs.mkdir(outputDir, { recursive: true });
await fs.mkdir(previewDir, { recursive: true });
const workbook = await SpreadsheetFile.importXlsx(await FileBlob.load(root + "/docs/DefenseGame_Balance_Skill_Summary.xlsx"));
const playtest = JSON.parse(await fs.readFile(root + "/BatchPlaytestResults/DefenseGame_Playtest5_Human3.json", "utf8"));
const ws = (name) => workbook.worksheets.getItem(name);
const copyRow = (name, source, destination) => ws(name).getRange(destination).copyFrom(ws(name).getRange(source), "all");

for (const row of [39, 40, 41, 42]) copyRow("\uc694\uc57d", "A38:D38", "A" + row + ":D" + row);
ws("\uc694\uc57d").getRange("A38:D42").values = [
  ["2026-07-17 \uc548\uc815\ud654", "R3+8R \uc18c\ud615 \uc0c1\uc810 \ub2e8\uc77c\ud654, \ubaa8\ubc14\uc77c Safe Area/\uac00\ub3c5\uc131, VFX \ud638\ud658\u00b7\ud22c\uc0ac\uccb4 \ud480\ub9c1, \ucd08\ubc18 \uacbd\uc81c \uc911\ucca9 \ucd95\uc18c", "RuntimeSceneBootstrap.cs / RunShopSystem.cs / RuntimeEffectUtility.cs / Projectile.cs", "C# 0/0, \ucd5c\uc2e0 Unity \uc2a4\ubaa8\ud06c PASS, \uc778\uac04\ud615 5\ud310 R10 4/5"],
  ["2026-07-17 \ub3d9\uc2dc\uc0ac\ub9dd \ud310\uc815", "\uac19\uc740 \uc804\ud22c \ud504\ub808\uc784\uc5d0 \ub77c\uc6b4\ub4dc\uc758 \ub9c8\uc9c0\ub9c9 \uc801\uacfc \uc720\uc800 HP\uac00 \ud568\uaed8 0\uc774 \ub418\uba74 HP 1\uc744 \ub0a8\uae30\uace0 \uc720\uc800 \uc2b9\ub9ac\ub85c \ucc98\ub9ac.", "DefenseGameController.cs / DefenseGamePlayModeSmoke.cs", "\ub9c8\uc9c0\ub9c9 \uc801 \uc0ac\ub9dd \ud655\uc815\uc774 \uc5c6\ub294 \ub2e8\uc21c \ub204\uc218 \ud328\ubc30\ub294 \uc2b9\ub9ac\ub85c \uc624\uc778\ud558\uc9c0 \uc54a\uc74c"],
  ["2026-07-17 hero_32 \ud655\uc815", "\uc57c\uc131\uc758 \ucd94\uc801\ud0c4: 220% \ud53c\ud574, 35% \ub454\ud654 4\ucd08, \uacf5\uaca9\ub825 30% \ub3c5 \ud53c\ud574\ub97c \ucd08\ub2f9 4\ud68c, \uc790\uc2e0 \uacf5\uaca9\uc18d\ub3c4 +25% 5\ucd08.", "CharacterCombatTuningConfig.cs / AugmentManager.cs / CharacterDatabase.cs", "\uc804\uc6a9 \uc99d\uac15 3\uc885\uacfc \uc601\uc6c5 \uc2a4\ud0ac \uc2dc\ud2b8\uae4c\uc9c0 \ub3d9\uae30\ud654"],
  ["2026-07-17 R10 \ubbf8\uc138 \uc0c1\ud5a5", "10\ub77c\uc6b4\ub4dc \uace8\ub818 \ubcf4\uc2a4 \uccb4\ub825 \ubc30\uc728 2.75\u21922.94(+6.9%), \uacf5\uaca9 \ubc30\uc728 1.78\u21921.84(+3.4%).", "MonsterDatabase.cs", "5\ud310 \uacb0\uacfc 4/5=80%; 5\ud310 \ub2e8\uc704\ub294 20% \uac04\uaca9\uc774\ub77c \ubaa9\ud45c 65~75%\uc758 \ucd5c\uadfc\uc811 \uc0c1\ub2e8"],
  ["2026-07-17 \uc2b9\ub9ac \uacb0\uacfc UI", "\uacb0\uacfc \uc694\uc57d\u00b7\uba54\ud0c0\u00b7\ub2e4\uc74c \ud589\ub3d9 \uae00\uc790 \ud06c\uae30\uc640 \ubc84\ud2bc \ub192\uc774\ub97c \ud0a4\uc6b0\uace0 \uc678\uacfd\uc120\uc744 \ubcf4\uac15.", "MetaFlowUI.cs", "\ubaa8\ubc14\uc77c\uc5d0\uc11c \uc810\uc218\u00b7\ubcf4\uc0c1\u00b7\uacc4\uc18d\ud558\uae30 \uc6b0\uc120\uc21c\uc704\uac00 \ud55c\ub208\uc5d0 \ubcf4\uc774\ub3c4\ub85d \uc815\ub9ac"]
];
ws("\uc694\uc57d").getRange("A38:D42").format.wrapText = true;
ws("\uc694\uc57d").getRange("A38:D42").format.rowHeight = 48;

ws("\uc601\uc6c5 \uc2a4\ud0ac").getRange("D23:M23").values = [[
  "\uc57c\uc131\uc758 \ucd94\uc801\ud0c4",
  "\uacf5\uaca9\ub825 220% \ud53c\ud574 + 4\ucd08\uac04 \uc774\ub3d9\uc18d\ub3c4 35% \uac10\uc18c + \ucd08\ub2f9 \uacf5\uaca9\ub825 30% \ub3c5 \ud53c\ud574 4\ucd08 + \uc790\uc2e0 \uacf5\uaca9\uc18d\ub3c4 +25% 5\ucd08",
  "220%, 35%\u00b74\ucd08, 30%\u00d74, 25%\u00b75\ucd08",
  "\ud53c\ud574\ub7c9",
  "DamageSlow",
  "SkillFire \ub610\ub294 SkillHit",
  "\ud22c\uc0ac\uccb4\u00b7\ub454\ud654\u00b7\ub3c5 \ud45c\uc2dc / \uc790\uc2e0 \uacf5\uaca9\uc18d\ub3c4 \ubc84\ud504 \uc544\uc774\ucf58",
  "36.0 (\uacf5\uaca9\ub825 100%, \uc6d0\uac70\ub9ac 6m)",
  "79.2 \uc9c1\uc811 \ud53c\ud574 + 43.2 \ub3c5 \ud53c\ud574 = \ucd5c\ub300 122.4",
  "\ud53c\ud574\ub7c9 \uc131\uc7a5 / \ub454\ud654\u00b7\ub3c5\u00b7\uacf5\uc18d \uc218\uce58\ub294 \uace0\uc815"
]];
ws("\uc601\uc6c5 \uc2a4\ud0ac").getRange("A23:M23").format.wrapText = true;
ws("\uc601\uc6c5 \uc2a4\ud0ac").getRange("A23:M23").format.rowHeight = 58;
ws("HeroAugments_V1").getRange("A59:F61").values = [
  ["hero_32", "hero32_predator_rhythm_n", "Normal", "\uc77c\ubc18", "\ud3ec\uc2dd \ub9ac\ub4ec", "\uc2a4\ud0ac \uc0ac\uc6a9 \uc2dc 5\ucd08 \ub3d9\uc548 \uacf5\uaca9\ub825 +10%, \uacf5\uaca9\uc18d\ub3c4 +20%\ub97c \uc5bb\uc2b5\ub2c8\ub2e4."],
  ["hero_32", "hero32_pack_hunt_r", "Rare", "\ub808\uc5b4", "\ubb34\ub9ac \uc0ac\ub0e5\ud0c4", "\uc2a4\ud0ac \uc0ac\uc6a9 \uc2dc \ubb34\uc791\uc704 \uc801\uc5d0\uac8c \uacf5\uaca9\ub825 60% \ucd94\uac00 \uc0ac\uaca9 2\ubc1c\uc744 \ubc1c\uc0ac\ud569\ub2c8\ub2e4. \uac19\uc740 \uc801\uc744 \ub2e4\uc2dc \ub9de\ud790 \uc218 \uc788\uc2b5\ub2c8\ub2e4."],
  ["hero_32", "hero32_alpha_hunt_m", "Mythic", "\uc2e0\ud654\uae09", "\uc54c\ud30c\uc758 \ud3ec\ud6a8", "\uc2a4\ud0ac \uc0ac\uc6a9 \uc2dc \ubb34\uc791\uc704 \uc801\uc5d0\uac8c \uacf5\uaca9\ub825 70% \ucd94\uac00 \uc0ac\uaca9 4\ubc1c\uc744 \ubc1c\uc0ac\ud558\uace0 6\ucd08\uac04 \uacf5\uaca9\ub825 +16%, \uacf5\uaca9\uc18d\ub3c4 +28%\ub97c \uc5bb\uc2b5\ub2c8\ub2e4."]
];
ws("HeroAugments_V1").getRange("A59:F61").format.wrapText = true;
ws("HeroAugments_V1").getRange("A59:F61").format.rowHeight = 44;

ws("\uc2e4\uce21_5\ud310").getRange("A1").values = [["\uc778\uac04 3\uc804\ub7b5 5\ud310 \uc2e4\uce21 \u00b7 2026-07-17"]];
ws("\uc2e4\uce21_5\ud310").getRange("A2").values = [["40\ubc30 \uc548\uc804\uc18d\ub3c4\u00b7\uace0\uc815 \uc2dc\ub4dc. \ub3d9\uc2dc\uc0ac\ub9dd \uc2b9\ub9ac\u00b7hero_32 \ud655\uc815\u00b7R10 \ubbf8\uc138 \uc0c1\ud5a5\u00b7\uacb0\uacfc UI \uac1c\uc120 \ucf54\ub4dc \uae30\uc900. 5\ud310 \ub2e8\uc704\uc5d0\uc11c \ubaa9\ud45c 65~75%\uc5d0 \uac00\uc7a5 \uac00\uae4c\uc6b4 \uacb0\uacfc\ub294 80%\ub2e4."]];
ws("\uc2e4\uce21_5\ud310").getRange("A5:M5").values = [[
  playtest.runs, playtest.targetRuns, playtest.r10Clears, playtest.r10SuccessRate,
  playtest.summonHeavyRuns + "\ud310 / " + playtest.summonHeavyR10Clears + "\uc2b9",
  playtest.balancedRuns + "\ud310 / " + playtest.balancedR10Clears + "\uc2b9",
  playtest.shopSaveRuns + "\ud310 / " + playtest.shopSaveR10Clears + "\uc2b9",
  playtest.avgFirstRarePlusRound, playtest.avgFirstMergeRound, playtest.fateUses,
  playtest.shopPurchases, playtest.shopGoldSpent,
  "R10 4/5=80%. \uc131\uacf5 4\ud310 \uc911 \ucd5c\uc800 \uc885\ub8cc HP 2, \uc2e4\ud328 1\ud310\uc740 \ubcf4\uc2a4 HP 0%\uc640 \uc0dd\uba85\ub825 0\uc774 \ud568\uaed8 \uae30\ub85d\ub418\uc5b4 \ub3d9\uc2dc\uc0ac\ub9dd \uc2b9\ub9ac \uaddc\uce59\uc758 \ud575\uc2ec \ud68c\uadc0 \ub300\uc0c1."
]];
ws("\uc2e4\uce21_5\ud310").getRange("A9:U13").values = playtest.results.map((run) => [
  run.index, run.strategy, run.reachedRound, run.clearedR10, run.summons, run.merges,
  run.shopPurchases, run.shopGoldSpent, run.fateUses, run.fateCardTitle, run.fateCardDebt,
  run.fateActivationRound, run.r3ShopSeen, run.r6ShopSeen, run.firstRarePlusRound,
  run.firstMergeRound, run.endGold, run.endLife, run.r10BossHealthRemaining01, run.timeout, run.notes
]);
ws("\uc2e4\uce21_5\ud310").getRange("D5").format.numberFormat = "0.0%";
ws("\uc2e4\uce21_5\ud310").getRange("S9:S13").format.numberFormat = "0.0%";
ws("\uc2e4\uce21_5\ud310").getRange("A2:U2").format.wrapText = true;
ws("\uc2e4\uce21_5\ud310").getRange("A5:M5").format.wrapText = true;
ws("\uc2e4\uce21_5\ud310").getRange("A5:M5").format.rowHeight = 64;
ws("\uc2e4\uce21_5\ud310").getRange("A9:U13").format.wrapText = true;
ws("\uc2e4\uce21_5\ud310").getRange("A9:U13").format.rowHeight = 44;

for (const row of [17, 18, 19, 20, 21]) copyRow("\ubcc0\uacbd\uc774\ub825", "A16:F16", "A" + row + ":F" + row);
ws("\ubcc0\uacbd\uc774\ub825").getRange("A17:F21").values = [
  ["2026-07-17 KST", "\ub3d9\uc2dc\uc0ac\ub9dd \uc2b9\ub9ac", "\ub9c8\uc9c0\ub9c9 \uc801 \uc0ac\ub9dd\uacfc \uc720\uc800 HP 0\uc774 \uac19\uc740 \uc804\ud22c \ud504\ub808\uc784\uc5d0 \ud655\uc815\ub418\uba74 HP 1 \uc2b9\ub9ac. \uc815\uc801 \uc815\ucc45 \uac80\uc0ac\uc640 Play Mode \uc2a4\ubaa8\ud06c \ucd94\uac00.", null, null, null],
  ["2026-07-17 KST", "hero_32 \ud655\uc815", "\uc57c\uc131\uc758 \ucd94\uc801\ud0c4 220%\u00b735% \ub454\ud654 4\ucd08\u00b730%/\ucd08 \ub3c5 4\ucd08\u00b7\uacf5\uc18d +25% 5\ucd08, \uc804\uc6a9 \uc99d\uac15 3\uc885 \ub3d9\uae30\ud654.", null, null, null],
  ["2026-07-17 KST", "R10 \ud29c\ub2dd", "\uace8\ub818 \ubcf4\uc2a4 \uccb4\ub825 \ubc30\uc728 +6.9%, \uacf5\uaca9 \ubc30\uc728 +3.4%. \ucd5c\uc2e0 \uc778\uac04\ud615 3\uc804\ub7b5 5\ud310 R10 4/5.", null, null, null],
  ["2026-07-17 KST", "\uc2b9\ub9ac \uacb0\uacfc UI", "\uacb0\uacfc \uc694\uc57d\u00b7\uba54\ud0c0\u00b7\ub2e4\uc74c \ud589\ub3d9 \ud14d\uc2a4\ud2b8 \ud655\ub300, \uc678\uacfd\uc120\uacfc \uacc4\uc18d\ud558\uae30 \ubc84\ud2bc \ud06c\uae30 \ubcf4\uac15.", null, null, null],
  ["2026-07-17 KST", "\uc5d1\uc140 \ub3d9\uae30\ud654", "@oai/artifact-tool\ub85c 29\uac1c \uc2dc\ud2b8 \ubcf4\uc874 \ud3b8\uc9d1, \uc804\uccb4 \ub80c\ub354\uc640 \uc218\uc2dd \uc624\ub958 \uac80\uc0ac\ub97c \uc218\ud589.", null, null, null]
];
ws("\ubcc0\uacbd\uc774\ub825").getRange("A17:F21").format.wrapText = true;
ws("\ubcc0\uacbd\uc774\ub825").getRange("A17:F21").format.rowHeight = 44;

for (const row of [18, 20, 22]) copyRow("Latest_2026-07-17", "A16:F16", "A" + row + ":F" + row);
for (const row of [19, 21]) copyRow("Latest_2026-07-17", "A17:F17", "A" + row + ":F" + row);
ws("Latest_2026-07-17").getRange("A16:F22").values = [
  ["\uac80\uc99d", "Unity \uc2a4\ubaa8\ud06c", "\uc138\ub85c UI\u00b7hero55~57\u00b7hero32\u00b7\ub3d9\uc2dc\uc0ac\ub9dd \uc815\ucc45 PASS", "DefenseGame_PlayModeSmoke.json", "\ud1b5\uacfc", "runtimeErrors 0"],
  ["\uac80\uc99d", "\uc778\uac04\ud615 3\uc804\ub7b5 5\ud310", "R10 4/5 = 80%", "DefenseGame_Playtest5_Human3.json", "\ubaa9\ud45c \uadfc\uc811", "5\ud310 \ub2e8\uc704 \ubaa9\ud45c 65~75%\uc758 \ucd5c\uadfc\uc811 \uc0c1\ub2e8\uac12"],
  ["\ud310\uc815", "\ub3d9\uc2dc\uc0ac\ub9dd", "\ub9c8\uc9c0\ub9c9 \uc801\uacfc \ub3d9\uc2dc\uc0ac\ub9dd\uc774\uba74 HP 1 \uc720\uc800 \uc2b9\ub9ac", "DefenseGameController.cs", "\uc801\uc6a9", "\ub9c8\uc9c0\ub9c9 \uc801 \uc0ac\ub9dd \ud655\uc815\uc774 \uc5c6\ub294 \ub204\uc218\ub294 \ud328\ubc30"],
  ["\uc601\uc6c5", "hero_32", "\uc57c\uc131\uc758 \ucd94\uc801\ud0c4: 220%\u00b7\ub454\ud654 35% 4\ucd08\u00b7\ub3c5 30%\u00d74\u00b7\uacf5\uc18d +25% 5\ucd08", "CharacterCombatTuningConfig.cs / AugmentManager.cs", "\uc801\uc6a9", "\uc804\uc6a9 \uc99d\uac15 3\uc885 \ud3ec\ud568"],
  ["\ubc38\ub7f0\uc2a4", "R10 \uace8\ub818", "\uccb4\ub825 \ubc30\uc728 2.94 / \uacf5\uaca9 \ubc30\uc728 1.84", "MonsterDatabase.cs", "\uc801\uc6a9", "\uc9c1\uc804 \ub300\ube44 \uccb4\ub825 +6.9%, \uacf5\uaca9 +3.4%"],
  ["UI", "\uc2b9\ub9ac \uacb0\uacfc", "\uc694\uc57d 28pt\u00b7\uba54\ud0c0 24pt\u00b7\uacc4\uc18d\ud558\uae30 28pt\uc640 \uc678\uacfd\uc120", "MetaFlowUI.cs", "\uc801\uc6a9", "\ubaa8\ubc14\uc77c \uacb0\uacfc \ud655\uc778\uacfc \ub2e4\uc74c \ud589\ub3d9 \uac00\ub3c5\uc131 \ubcf4\uac15"],
  ["\uae30\uae30", "Galaxy Z Flip 4", "\uc0c8 APK \ube4c\ub4dc \uc644\ub8cc(77.5MB), \uc5f0\uacb0 \uc7ac\ud655\uc778 \ud6c4 \uc124\uce58\u00b7\uc131\ub2a5 \uc2e4\uce21", "Builds/Android/ProjectDG.apk", "\uc124\uce58 \ub300\uae30", "60fps \ubaa9\ud45c"]
];
ws("Latest_2026-07-17").getRange("A16:F22").format.wrapText = true;
ws("Latest_2026-07-17").getRange("A16:F22").format.rowHeight = 46;

copyRow("Validation_2026-07-17", "A10:F10", "A11:F11");
copyRow("Validation_2026-07-17", "A9:F9", "A12:F12");
ws("Validation_2026-07-17").getRange("A4:F12").values = [
  ["C# \ube4c\ub4dc", "PASS / \uacbd\uace0 0 / \uc624\ub958 0", "Assembly-CSharp.csproj --no-restore", "\ud1b5\uacfc", "\ucd5c\uc2e0 \uc5b4\uc148\ube14\ub9ac", "\uc804\uccb4 \uc5b4\uc148\ube14\ub9ac"],
  ["\uc138\ub85c UI + hero_55~57 + hero_32", "PASS / runtimeErrors 0", "DefenseGame_PlayModeSmoke.json", "\uc815\uc0c1", "Safe Area\u00b7HP10\u00b7VFX\u00b7hero32 \uc2dc\uadf8\ub2c8\ucc98", "\ubaa8\ubc14\uc77c HUD/\ucd08\uc6d4 3\uc885/\uc2e0\ud654"],
  ["\ubcf4\uc2a4/\ubaac\uc2a4\ud130 \uc560\ub2c8\uba54\uc774\uc158", "PASS / runtimeErrors 0", "DefenseGame_BossAnimationSmoke.json", "\uc815\uc0c1", "AttackHit\u00b7SkillHit\u00b7FireProjectile", "mob_01~09"],
  ["\ubcf4\uc2a4 \uc0c1\ud0dc\uc774\uc0c1 \uba74\uc5ed", "PASS / runtimeErrors 0", "DefenseGame_BossStatusImmunitySmoke.json", "\uc815\uc0c1", "\uc81c\uc5b4\u00b7\uc11d\ud654\ud45c\ud604\u00b7\ub3c4\ud2b8 \ucc28\ub2e8 / \uc9c1\uc811\ud53c\ud574 \ud5c8\uc6a9", "Boss"],
  ["\uc778\uac04\ud615 3\uc804\ub7b5 5\ud310", "R10 4/5 (80%)", "DefenseGame_Playtest5_Human3.json", "\ubaa9\ud45c \uadfc\uc811", "5\ud310\uc740 20% \ub2e8\uc704\ub77c 65~75%\ub97c \uc815\ud655\ud788 \ud45c\ud604\ud560 \uc218 \uc5c6\uc74c", "summon-heavy / balanced / shop-save"],
  ["\uc18c\ud615 \uc0c1\uc810 \uc77c\uc815", "R3 + 8R \ub2e8\uc77c \uc2a4\ucf00\uc904", "RunShopSystem.cs", "\uc801\uc6a9", "\ubcc4\ub3c4 \uc815\uaddc \uc0c1\uc810 \ubd84\uae30 \ube44\ud65c\uc131", "R3\u00b711\u00b719\u00b727\u00b735"],
  ["\ubaa8\ubc14\uc77c \uc131\ub2a5", "\uc0c8 APK \ube4c\ub4dc PASS / 77.5MB", "AndroidBuild_20260717.log", "\uc124\uce58 \ub300\uae30", "ADB \uc5f0\uacb0 \uc7ac\ud655\uc778 \ud6c4 \uc7a5\uc2dc\uac04 \ud504\ub808\uc784 \uac31\uc2e0", "Galaxy Z Flip 4"],
  ["\ub3d9\uc2dc\uc0ac\ub9dd \uc815\ucc45", "PASS", "DefenseGame_PlayModeSmoke.json", "\uc815\uc0c1", "\ub9c8\uc9c0\ub9c9 \uc801 \uc0ac\ub9dd \ud655\uc815 \uc2dc HP 1 \uc2b9\ub9ac / \ub2e8\uc21c \ub204\uc218 \ud328\ubc30", "\uc815\uc801 \uc815\ucc45 \ud68c\uadc0"],
  ["hero_32 \uc2dc\uadf8\ub2c8\ucc98", "PASS", "DefenseGame_PlayModeSmoke.json", "\uc815\uc0c1", "DamageSlow\u00b7220%\u00b735%\u00b74\ucd08", "\uc2a4\ud0ac \uc815\uc758 \ud68c\uadc0"]
];
ws("Validation_2026-07-17").getRange("A4:F12").format.wrapText = true;
ws("Validation_2026-07-17").getRange("A4:F12").format.rowHeight = 46;

const heroInspect = await workbook.inspect({ kind: "table", range: "\uc601\uc6c5 \uc2a4\ud0ac!A22:M24", include: "values,formulas", tableMaxRows: 10, tableMaxCols: 16, tableMaxCellChars: 300, maxChars: 12000 });
const testInspect = await workbook.inspect({ kind: "table", range: "\uc2e4\uce21_5\ud310!A1:U13", include: "values,formulas", tableMaxRows: 20, tableMaxCols: 24, tableMaxCellChars: 200, maxChars: 30000 });
const errorScan = await workbook.inspect({ kind: "match", searchTerm: "#REF!|#DIV/0!|#VALUE!|#NAME\\?|#N/A", options: { useRegex: true, maxResults: 300 }, summary: "final formula error scan", maxChars: 30000 });
await fs.writeFile(outputDir + "/inspection.txt", ["=== hero_32 ===", heroInspect.ndjson, "=== playtest ===", testInspect.ndjson, "=== formula errors ===", errorScan.ndjson].join("\n"), "utf8");
for (let index = 0; index < workbook.worksheets.items.length; index += 1) {
  const current = workbook.worksheets.getItemAt(index);
  const preview = await workbook.render({ sheetName: current.name, autoCrop: "all", scale: 0.8, format: "png" });
  const safeName = String(index + 1).padStart(2, "0") + "_" + current.name.replace(/[\\/:*?"<>|]/g, "_") + ".png";
  await fs.writeFile(previewDir + "/" + safeName, new Uint8Array(await preview.arrayBuffer()));
}
const output = await SpreadsheetFile.exportXlsx(workbook);
const outputPath = outputDir + "/DefenseGame_Balance_Skill_Summary.xlsx";
await output.save(outputPath);
console.log(JSON.stringify({ outputPath, sheets: workbook.worksheets.items.length, formulaErrors: errorScan.ndjson }, null, 2));
