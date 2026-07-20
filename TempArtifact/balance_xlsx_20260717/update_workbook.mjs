import fs from "node:fs/promises";
import { FileBlob, SpreadsheetFile } from "file:///C:/Users/noxis/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/@oai/artifact-tool/dist/artifact_tool.mjs";

const root = "D:/GameDev/ProjectDG";
const sourcePath = root + "/docs/DefenseGame_Balance_Skill_Summary.xlsx";
const outputDir = root + "/outputs/balance_xlsx_20260717";
const previewDir = root + "/TempXlsxPreviewAfter";
const outputPath = outputDir + "/DefenseGame_Balance_Skill_Summary.xlsx";
const playtestPath = root + "/BatchPlaytestResults/DefenseGame_Playtest5_Human3.json";

await fs.mkdir(outputDir, { recursive: true });
await fs.mkdir(previewDir, { recursive: true });

const workbook = await SpreadsheetFile.importXlsx(await FileBlob.load(sourcePath));
const playtest = JSON.parse(await fs.readFile(playtestPath, "utf8"));

function sheet(name) {
  return workbook.worksheets.getItem(name);
}

function setRowValues(sheetName, address, values) {
  sheet(sheetName).getRange(address).values = [values];
}

function copyRow(sheetName, sourceAddress, destinationAddress) {
  const targetSheet = sheet(sheetName);
  targetSheet.getRange(destinationAddress).copyFrom(targetSheet.getRange(sourceAddress), "all");
}

const summary = sheet("요약");
for (const row of [39, 40, 41, 42]) {
  copyRow("요약", "A38:D38", "A" + row + ":D" + row);
}
summary.getRange("A38:D38").values = [[
  "2026-07-17 안정화",
  "R3+8R 소형 상점 단일화, 모바일 Safe Area/가독성, VFX 호환·투사체 풀링, 초반 경제 중첩 축소",
  "RuntimeSceneBootstrap.cs / RunShopSystem.cs / RuntimeEffectUtility.cs / Projectile.cs",
  "C# 0/0, 최신 Unity 스모크 PASS, 인간형 5판 R10 4/5"
]];
summary.getRange("A39:D42").values = [
  [
    "2026-07-17 동시사망 판정",
    "같은 전투 프레임에 라운드의 마지막 적과 유저 HP가 함께 0이 되면 HP 1을 남기고 유저 승리로 처리.",
    "DefenseGameController.cs / DefenseGamePlayModeSmoke.cs",
    "마지막 적 사망 확정이 없는 단순 누수 패배는 승리로 오인하지 않음"
  ],
  [
    "2026-07-17 hero_32 확정",
    "야성의 추적탄: 220% 피해, 35% 둔화 4초, 공격력 30% 독 피해를 초당 4회, 자신 공격속도 +25% 5초.",
    "CharacterCombatTuningConfig.cs / AugmentManager.cs / CharacterDatabase.cs",
    "전용 증강 3종과 영웅 스킬 시트까지 동기화"
  ],
  [
    "2026-07-17 R10 미세 상향",
    "10라운드 골렘 보스 체력 배율 2.75→2.94(+6.9%), 공격 배율 1.78→1.84(+3.4%).",
    "MonsterDatabase.cs",
    "5판 결과 4/5=80%; 5판 단위는 20% 간격이라 목표 65~75%의 최근접 상단"
  ],
  [
    "2026-07-17 승리 결과 UI",
    "결과 요약·메타·다음 행동 글자 크기와 버튼 높이를 키우고 외곽선을 보강.",
    "MetaFlowUI.cs",
    "모바일에서 점수·보상·계속하기 우선순위가 한눈에 보이도록 정리"
  ]
];
summary.getRange("A38:D42").format.wrapText = true;
summary.getRange("A38:D42").format.rowHeight = 48;

setRowValues("영웅 스킬", "D23:M23", [
  "야성의 추적탄",
  "공격력 220% 피해 + 4초간 이동속도 35% 감소 + 초당 공격력 30% 독 피해 4초 + 자신 공격속도 +25% 5초",
  "220%, 35%·4초, 30%×4, 25%·5초",
  "피해량",
  "DamageSlow",
  "SkillFire 또는 SkillHit",
  "투사체·둔화·독 표시 / 자신 공격속도 버프 아이콘",
  "36.0 (공격력 100%, 원거리 6m)",
  "79.2 직접 피해 + 43.2 독 피해 = 최대 122.4",
  "피해량 성장 / 둔화·독·공속 수치는 고정"
]);
sheet("영웅 스킬").getRange("D23:M23").format.wrapText = true;
sheet("영웅 스킬").getRange("A23:M23").format.rowHeight = 58;

sheet("HeroAugments_V1").getRange("A59:F61").values = [
  ["hero_32", "hero32_predator_rhythm_n", "Normal", "일반", "포식 리듬", "스킬 사용 시 5초 동안 공격력 +10%, 공격속도 +20%를 얻습니다."],
  ["hero_32", "hero32_pack_hunt_r", "Rare", "레어", "무리 사냥탄", "스킬 사용 시 무작위 적에게 공격력 60% 추가 사격 2발을 발사합니다. 같은 적을 다시 맞힐 수 있습니다."],
  ["hero_32", "hero32_alpha_hunt_m", "Mythic", "신화급", "알파의 포효", "스킬 사용 시 무작위 적에게 공격력 70% 추가 사격 4발을 발사하고 6초간 공격력 +16%, 공격속도 +28%를 얻습니다."]
];
sheet("HeroAugments_V1").getRange("A59:F61").format.wrapText = true;
sheet("HeroAugments_V1").getRange("A59:F61").format.rowHeight = 44;

const strategySummary = {
  "summon-heavy": [playtest.summonHeavyRuns, playtest.summonHeavyR10Clears],
  "balanced": [playtest.balancedRuns, playtest.balancedR10Clears],
  "shop-save": [playtest.shopSaveRuns, playtest.shopSaveR10Clears]
};
sheet("실측_5판").getRange("A1:U1").values = [["인간 3전략 5판 실측 · 2026-07-17", ...Array(20).fill(null)]];
sheet("실측_5판").getRange("A2:U2").values = [[
  "40배 안전속도·고정 시드. 동시사망 승리·hero_32 확정·R10 미세 상향·결과 UI 개선 코드 기준. 5판 단위에서 목표 65~75%에 가장 가까운 결과는 80%다.",
  ...Array(20).fill(null)
]];
sheet("실측_5판").getRange("A5:M5").values = [[
  playtest.runs,
  playtest.targetRuns,
  playtest.r10Clears,
  playtest.r10SuccessRate,
  strategySummary["summon-heavy"][0] + "판 / " + strategySummary["summon-heavy"][1] + "승",
  strategySummary["balanced"][0] + "판 / " + strategySummary["balanced"][1] + "승",
  strategySummary["shop-save"][0] + "판 / " + strategySummary["shop-save"][1] + "승",
  playtest.avgFirstRarePlusRound,
  playtest.avgFirstMergeRound,
  playtest.fateUses,
  playtest.shopPurchases,
  playtest.shopGoldSpent,
  "R10 4/5=80%. 성공 4판 중 최저 종료 HP 2, 실패 1판은 보스 HP 0%와 생명력 0이 함께 기록되어 동시사망 승리 규칙의 핵심 회귀 대상."
]];
const detailRows = playtest.results.map((run) => [
  run.index,
  run.strategy,
  run.reachedRound,
  run.clearedR10,
  run.summons,
  run.merges,
  run.shopPurchases,
  run.shopGoldSpent,
  run.fateUses,
  run.fateCardTitle,
  run.fateCardDebt,
  run.fateActivationRound,
  run.r3ShopSeen,
  run.r6ShopSeen,
  run.firstRarePlusRound,
  run.firstMergeRound,
  run.endGold,
  run.endLife,
  run.r10BossHealthRemaining01,
  run.timeout,
  run.notes
]);
sheet("실측_5판").getRange("A9:U13").values = detailRows;
sheet("실측_5판").getRange("D5:D5").format.numberFormat = "0.0%";
sheet("실측_5판").getRange("S9:S13").format.numberFormat = "0.0%";
sheet("실측_5판").getRange("A2:U2").format.wrapText = true;
sheet("실측_5판").getRange("A5:M5").format.wrapText = true;
sheet("실측_5판").getRange("A5:M5").format.rowHeight = 64;
sheet("실측_5판").getRange("A9:U13").format.wrapText = true;
sheet("실측_5판").getRange("A9:U13").format.rowHeight = 44;

for (const row of [17, 18, 19, 20, 21]) {
  copyRow("변경이력", "A16:F16", "A" + row + ":F" + row);
}
sheet("변경이력").getRange("A17:F21").values = [
  ["2026-07-17 KST", "동시사망 승리", "마지막 적 사망과 유저 HP 0이 같은 전투 프레임에 확정되면 HP 1 승리. 정적 정책 검사와 Play Mode 스모크 추가.", null, null, null],
  ["2026-07-17 KST", "hero_32 확정", "야성의 추적탄 220%·35% 둔화 4초·30%/초 독 4초·공속 +25% 5초, 전용 증강 3종 동기화.", null, null, null],
  ["2026-07-17 KST", "R10 튜닝", "골렘 보스 체력 배율 +6.9%, 공격 배율 +3.4%. 최신 인간형 3전략 5판 R10 4/5.", null, null, null],
  ["2026-07-17 KST", "승리 결과 UI", "결과 요약·메타·다음 행동 텍스트 확대, 외곽선과 계속하기 버튼 크기 보강.", null, null, null],
  ["2026-07-17 KST", "엑셀 동기화", "@oai/artifact-tool로 29개 시트 보존 편집, 전체 렌더와 수식 오류 검사를 수행.", null, null, null]
];
sheet("변경이력").getRange("A17:F21").format.wrapText = true;
sheet("변경이력").getRange("A17:F21").format.rowHeight = 44;

for (const row of [18, 20, 22]) {
  copyRow("Latest_2026-07-17", "A16:F16", "A" + row + ":F" + row);
}
for (const row of [19, 21]) {
  copyRow("Latest_2026-07-17", "A17:F17", "A" + row + ":F" + row);
}
sheet("Latest_2026-07-17").getRange("A16:F22").values = [
  ["검증", "Unity 스모크", "세로 UI·hero55~57·hero32·동시사망 정책 PASS", "DefenseGame_PlayModeSmoke.json", "통과", "runtimeErrors 0"],
  ["검증", "인간형 3전략 5판", "R10 4/5 = 80%", "DefenseGame_Playtest5_Human3.json", "목표 근접", "5판 단위 목표 65~75%의 최근접 상단값"],
  ["판정", "동시사망", "마지막 적과 동시사망이면 HP 1 유저 승리", "DefenseGameController.cs", "적용", "마지막 적 사망 확정이 없는 누수는 패배"],
  ["영웅", "hero_32", "야성의 추적탄: 220%·둔화 35% 4초·독 30%×4·공속 +25% 5초", "CharacterCombatTuningConfig.cs / AugmentManager.cs", "적용", "전용 증강 3종 포함"],
  ["밸런스", "R10 골렘", "체력 배율 2.94 / 공격 배율 1.84", "MonsterDatabase.cs", "적용", "직전 대비 체력 +6.9%, 공격 +3.4%"],
  ["UI", "승리 결과", "요약 28pt·메타 24pt·계속하기 28pt와 외곽선", "MetaFlowUI.cs", "적용", "모바일 결과 확인과 다음 행동 가독성 보강"],
  ["기기", "Galaxy Z Flip 4", "새 APK 빌드·설치 후 장시간 성능 결과를 최종 검증 시트에 기록", "Builds/Android/ProjectDG.apk", "검증 예정", "60fps 목표"]
];
sheet("Latest_2026-07-17").getRange("A16:F22").format.wrapText = true;
sheet("Latest_2026-07-17").getRange("A16:F22").format.rowHeight = 46;

copyRow("Validation_2026-07-17", "A10:F10", "A11:F11");
copyRow("Validation_2026-07-17", "A9:F9", "A12:F12");
sheet("Validation_2026-07-17").getRange("A4:F12").values = [
  ["C# 빌드", "PASS / 경고 0 / 오류 0", "Assembly-CSharp.csproj --no-restore", "통과", "최신 어셈블리", "전체 어셈블리"],
  ["세로 UI + hero_55~57 + hero_32", "PASS / runtimeErrors 0", "DefenseGame_PlayModeSmoke.json", "정상", "Safe Area·HP10·VFX·hero32 시그니처", "모바일 HUD/초월 3종/신화"],
  ["보스/몬스터 애니메이션", "PASS / runtimeErrors 0", "DefenseGame_BossAnimationSmoke.json", "정상", "AttackHit·SkillHit·FireProjectile", "mob_01~09"],
  ["보스 상태이상 면역", "PASS / runtimeErrors 0", "DefenseGame_BossStatusImmunitySmoke.json", "정상", "제어·석화표현·도트 차단 / 직접피해 허용", "Boss"],
  ["인간형 3전략 5판", "R10 4/5 (80%)", "DefenseGame_Playtest5_Human3.json", "목표 근접", "5판은 20% 단위라 65~75%를 정확히 표현할 수 없음", "summon-heavy / balanced / shop-save"],
  ["소형 상점 일정", "R3 + 8R 단일 스케줄", "RunShopSystem.cs", "적용", "별도 정규 상점 분기 비활성", "R3·11·19·27·35"],
  ["모바일 성능", "60fps 목표 / 투사체 풀 48 / 원샷 VFX 72", "RuntimeEffectUtility.cs / Projectile.cs", "빌드 후 실측", "새 APK 장시간 프로파일 결과로 갱신", "Galaxy Z Flip 4"],
  ["동시사망 정책", "PASS", "DefenseGame_PlayModeSmoke.json", "정상", "마지막 적 사망 확정 시 HP 1 승리 / 단순 누수 패배", "정적 정책 회귀"],
  ["hero_32 시그니처", "PASS", "DefenseGame_PlayModeSmoke.json", "정상", "DamageSlow·220%·35%·4초", "스킬 정의 회귀"]
];
sheet("Validation_2026-07-17").getRange("A4:F12").format.wrapText = true;
sheet("Validation_2026-07-17").getRange("A4:F12").format.rowHeight = 46;

const keyInspection = await workbook.inspect({
  kind: "table",
  range: "영웅 스킬!A22:M24",
  include: "values,formulas",
  tableMaxRows: 10,
  tableMaxCols: 16,
  tableMaxCellChars: 300,
  maxChars: 12000
});
const playtestInspection = await workbook.inspect({
  kind: "table",
  range: "실측_5판!A1:U13",
  include: "values,formulas",
  tableMaxRows: 20,
  tableMaxCols: 24,
  tableMaxCellChars: 200,
  maxChars: 30000
});
const errorScan = await workbook.inspect({
  kind: "match",
  searchTerm: "#REF!|#DIV/0!|#VALUE!|#NAME\\?|#N/A",
  options: { useRegex: true, maxResults: 300 },
  summary: "final formula error scan",
  maxChars: 30000
});

const inspectionText = [
  "=== hero_32 ===",
  keyInspection.ndjson,
  "=== playtest ===",
  playtestInspection.ndjson,
  "=== formula errors ===",
  errorScan.ndjson
].join("\n");
await fs.writeFile(outputDir + "/inspection.txt", inspectionText, "utf8");

for (let index = 0; index < workbook.worksheets.items.length; index += 1) {
  const current = workbook.worksheets.getItemAt(index);
  const preview = await workbook.render({
    sheetName: current.name,
    autoCrop: "all",
    scale: 0.8,
    format: "png"
  });
  const safeName = String(index + 1).padStart(2, "0") + "_" + current.name.replace(/[\\/:*?"<>|]/g, "_") + ".png";
  await fs.writeFile(previewDir + "/" + safeName, new Uint8Array(await preview.arrayBuffer()));
}

const output = await SpreadsheetFile.exportXlsx(workbook);
await output.save(outputPath);
console.log(JSON.stringify({
  outputPath,
  sheets: workbook.worksheets.items.length,
  formulaErrors: errorScan.ndjson
}, null, 2));
