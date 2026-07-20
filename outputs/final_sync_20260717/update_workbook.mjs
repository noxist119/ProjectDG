import fs from "node:fs/promises";
import path from "node:path";
import { FileBlob, SpreadsheetFile } from "@oai/artifact-tool";

const workDir = path.resolve("outputs/final_sync_20260717");
const sourcePath = path.resolve("docs/DefenseGame_Balance_Skill_Summary.xlsx");
const input = await FileBlob.load(sourcePath);
const workbook = await SpreadsheetFile.importXlsx(input);

const mode = process.argv[2] || "inspect";
if (mode === "targets") {
  const targets = [
    ["Latest_2026-07-11", "A1:F41"],
    ["Validation_2026-07-11", "A1:F14"],
    ["초반튜닝_최신", "A1:D16"],
    ["실측_5판", "A1:U13"],
    ["운명카드_18", "A1:I22"],
  ];
  for (const [sheetId, range] of targets) {
    const values = await workbook.inspect({
      kind: "table",
      sheetId,
      range,
      include: "values,formulas",
      maxChars: 10000,
      tableMaxRows: 50,
      tableMaxCols: 24,
      tableMaxCellChars: 140,
    });
    const styles = await workbook.inspect({
      kind: "computedStyle",
      sheetId,
      range: range.split(":")[0] + ":" + range.split(":")[0],
      maxChars: 2500,
    });
    console.log(values.ndjson);
    console.log(styles.ndjson);
  }
  process.exit(0);
}

if (mode === "update") {
  const playtestText = await fs.readFile(path.resolve("BatchPlaytestResults/DefenseGame_Playtest5_Human3.json"), "utf8");
  const playtest = JSON.parse(playtestText.replace(/^\uFEFF/, ""));

  const early = workbook.worksheets.getItem("초반튜닝_최신");
  early.getRange("A2").values = [["2026-07-17 최신 코드 기준. 시작 3소환 보장, R3+8R 소형 상점 단일화, 초반 보급 중첩 축소, 40배 안전 실측 반영."]];
  early.getRange("C9:D10").values = [
    [4, "빈 슬롯 보급 실패 시 최소 보상. 기존 6G에서 축소"],
    [0, "무조건 R4 보급 제거. 소환·상점 선택의 기회비용 유지"],
  ];
  early.getRange("A14:D14").values = [[
    "별도 정규 전투상점",
    "RunShopSystem.enableRegularShop",
    "비활성",
    "R3부터 8라운드마다 열리는 소형 상점과 중복 제거",
  ]];
  early.getRange("C16:D16").values = [[40, "fixed 0.025 / maxDelta 0.33 / 고정 시드"]];

  const runSheet = workbook.worksheets.getItem("실측_5판");
  runSheet.getRange("A1").values = [["인간 3전략 5판 실측 · 2026-07-17"]];
  runSheet.getRange("A2").values = [["40배 안전속도·고정 시드. 최신 경제·상점·VFX·UI 코드 기준이며, 5판 단위에서 목표 65~75%에 가장 가까운 결과는 80%다."]];
  const summonHeavy = playtest.summonHeavyRuns + "판 / " + playtest.summonHeavyR10Clears + "승";
  const balanced = playtest.balancedRuns + "판 / " + playtest.balancedR10Clears + "승";
  const shopSave = playtest.shopSaveRuns + "판 / " + playtest.shopSaveR10Clears + "승";
  runSheet.getRange("A5:M5").values = [[
    playtest.runs,
    playtest.targetRuns,
    playtest.r10Clears,
    playtest.r10SuccessRate,
    summonHeavy,
    balanced,
    shopSave,
    playtest.avgFirstRarePlusRound,
    playtest.avgFirstMergeRound,
    playtest.fateUses,
    playtest.shopPurchases,
    playtest.shopGoldSpent,
    "R10 4/5=80%. 5판 단위 목표 근접 상단값; 성공 4판 보스 잔여 HP 0%, 실패 1판 R10 생명력 0",
  ]];
  const resultRows = playtest.results.map((result) => [
    result.index,
    result.strategy,
    result.reachedRound,
    result.clearedR10,
    result.summons,
    result.merges,
    result.shopPurchases,
    result.shopGoldSpent,
    result.fateUses,
    result.fateCardTitle,
    result.fateCardDebt,
    result.fateActivationRound,
    result.r3ShopSeen,
    result.r6ShopSeen,
    result.firstRarePlusRound,
    result.firstMergeRound,
    result.endGold,
    result.endLife,
    result.r10BossHealthRemaining01,
    result.timeout,
    result.notes,
  ]);
  runSheet.getRange("A9:U13").values = resultRows;
  runSheet.getRange("D5").format.numberFormat = "0.0%";
  runSheet.getRange("S9:S13").format.numberFormat = "0.0%";

  const fate = workbook.worksheets.getItem("운명카드_18");
  fate.getRange("A2").values = [["준비 단계에는 숨김. 전투 시작 후 버튼이 올라오며 클릭하면 0.1배 슬로우 중 3장을 제시한다. 각 카드에는 즉시 효과와 운명 빚 수치가 함께 표시되고, 1장 선택 후 UI는 내려가며 해당 런에는 다시 나타나지 않는다."]];
  const fateUpdates = [
    ["E5", "다음 라운드 적 수 +50%"],
    ["E8", "이번 포함 3라운드 소환비 +40%"],
    ["E9", "실패: HP -2, 현재 적 2초 기절"],
    ["E10", "다음 라운드 적 수 +50%"],
    ["E11", "다음 라운드 적 수 +50%"],
    ["E13", "이번 포함 2라운드 소환비 +30%"],
    ["E14", "HP 1, 다음 라운드 적 수 +50%"],
    ["E16", "다음 라운드 적 수 +50%"],
    ["E17", "다음 라운드 적 수 +50%"],
    ["E19", "이번 포함 2라운드 소환비 +25%"],
    ["E20", "다음 라운드 적 수 +50%"],
    ["E22", "다음 라운드 적 수 +50%"],
  ];
  for (const [cell, value] of fateUpdates) {
    fate.getRange(cell).values = [[value]];
  }

  const summary = workbook.worksheets.getItem("요약");
  summary.getRange("A2").values = [["작성일 2026-06-05 / 최신 업데이트 2026-07-17 / 코드·Unity 스모크·5판 실측 동기화"]];
  summary.getRange("A38:D38").copyFrom(summary.getRange("A37:D37"), "all");
  summary.getRange("A38:D38").values = [[
    "2026-07-17 안정화",
    "R3+8R 소형 상점 단일화, 모바일 Safe Area/가독성, VFX 호환·투사체 풀링, 초반 경제 중첩 축소",
    "RuntimeSceneBootstrap.cs / RunShopSystem.cs / RuntimeEffectUtility.cs / Projectile.cs",
    "C# 0/0, 세로·몬스터·보스면역 스모크 PASS, 인간형 5판 R10 4/5",
  ]];

  const latestName = "Latest_2026-07-17";
  const latest = workbook.worksheets.getOrAdd(latestName);
  latest.getRange("A1:F40").clear({ applyTo: "all" });
  latest.showGridLines = false;
  latest.getRange("A1:F1").merge();
  latest.getRange("A1").values = [["최신 수정 및 밸런스 동기화 (2026-07-17)"]];
  latest.getRange("A2:F2").merge();
  latest.getRange("A2").values = [["디바이스 안정화, 전투 선택 가독성, 상점 일정 충돌, 경제 중첩, Unity 실측을 한 번에 동기화한 기준 시트"]];
  latest.getRange("A4:F4").values = [["분류", "항목", "최신 설정", "코드 근거", "상태", "메모"]];
  const latestRows = [
    ["릴리스", "Build Settings", "DG.unity 표준 빌드 씬 등록", "ProjectSettings/EditorBuildSettings.asset", "적용", "커스텀 빌더 외 일반 Build에서도 씬 누락 방지"],
    ["상점", "등장 주기", "R3부터 8라운드마다 소형 상점 / 별도 정규 상점 비활성", "RunShopSystem.cs", "적용", "R11·19·27·35 중복 분기 제거"],
    ["UI", "Safe Area", "상점·증강·미션·레시피·컬렉션 모달을 SafeAreaRoot 아래 생성", "RuntimeSceneBootstrap.cs", "적용", "Z Flip 계열 세로 화면 모서리 보호"],
    ["UI", "모바일 안내문", "Android에서 PC 키보드 힌트 숨김", "RuntimeSceneBootstrap.cs", "적용", "개발용 Space/S/1-5 문구 제거"],
    ["UI", "운명카드 가독성", "카드 300x250 / 최소 18pt / 즉시 효과+운명 빚 표시", "RuntimeSceneBootstrap.cs / DefenseGameController.cs", "적용", "선택 결과와 미래 반동을 동시에 비교"],
    ["UI", "시너지 축약행", "제목 17pt / 설명 14pt", "RuntimeSceneBootstrap.cs", "적용", "모바일 최소 가독성 상향"],
    ["성능", "60fps 런타임", "vSync 0 / targetFrameRate 60", "RuntimeEffectUtility.cs", "적용", "미추적 런타임 파일 의존성 제거"],
    ["VFX", "공통 호환 처리", "모든 원샷 VFX URP 검사 / 파티클 시스템당 최대 96 / 동시 원샷 72", "RuntimeEffectUtility.cs", "적용", "마젠타·그림자·과도한 입자 방지"],
    ["성능", "투사체 풀링", "프리팹당 최대 48개 재사용", "Projectile.cs / DefenderUnit.cs", "적용", "다연사·스킬 연속 발사 GC 스파이크 완화"],
    ["경제", "라운드 수입", "시작 36G / 시작수입 1+floor(Rx0.40) / 클리어 5+floor(Rx0.75)", "DefenseGameController.cs", "적용", "첫 3소환은 유지하고 R10 누적 보급 축소"],
    ["경제", "초반 보급", "R3 대체 보급 4G / R4 무조건 보스 보급 0G", "DefenseGameController.cs", "적용", "클릭형 무조건 혜택 중첩 제거"],
    ["검증", "Unity 스모크", "세로 UI·hero55~57 / mob01~09 / 보스 상태이상 면역 모두 PASS", "BatchPlaytestResults", "통과", "runtimeErrors 0"],
    ["검증", "인간형 3전략 5판", "R10 4/5 = 80%", "DefenseGame_Playtest5_Human3.json", "통과", "5판 단위 목표 65~75%의 최근접 상단값"],
  ];
  latest.getRange("A5:F17").values = latestRows;

  const validationName = "Validation_2026-07-17";
  const validation = workbook.worksheets.getOrAdd(validationName);
  validation.getRange("A1:F30").clear({ applyTo: "all" });
  validation.showGridLines = false;
  validation.getRange("A1:F1").merge();
  validation.getRange("A1").values = [["Unity 최신 검증 스냅샷 (2026-07-17)"]];
  validation.getRange("A3:F3").values = [["검증 항목", "결과", "출처", "현재 상태", "비고", "범위"]];
  validation.getRange("A4:F10").values = [
    ["C# 빌드", "PASS / 경고 0 / 오류 0", "Assembly-CSharp.csproj --no-restore", "통과", "Unity 재임포트 후 재검증", "전체 어셈블리"],
    ["세로 UI + hero_55~57", "PASS / runtimeErrors 0", "DefenseGame_PlayModeSmoke.json", "정상", "Safe Area·HP10·VFX·프리팹", "모바일 HUD/초월 3종"],
    ["보스/몬스터 애니메이션", "PASS / runtimeErrors 0", "DefenseGame_BossAnimationSmoke.json", "정상", "AttackHit·SkillHit·FireProjectile", "mob_01~09"],
    ["보스 상태이상 면역", "PASS / runtimeErrors 0", "DefenseGame_BossStatusImmunitySmoke.json", "정상", "제어·석화표현·도트 차단 / 직접피해 허용", "Boss"],
    ["인간형 3전략 5판", "R10 4/5 (80%)", "DefenseGame_Playtest5_Human3.json", "목표 근접", "5판은 20% 단위라 65~75%를 정확히 표현할 수 없음", "summon-heavy / balanced / shop-save"],
    ["소형 상점 일정", "R3 + 8R 단일 스케줄", "RunShopSystem.cs", "적용", "별도 정규 상점 분기 비활성", "R3·11·19·27·35"],
    ["모바일 성능", "60fps 목표 / 투사체 풀 48 / 원샷 VFX 72", "RuntimeEffectUtility.cs / Projectile.cs", "적용", "실기기 장시간 프레임은 후속 프로파일링", "Galaxy Z Flip 4"],
  ];

  const styleSheet = (sheet, endRow) => {
    sheet.getRange("A1:F1").format = {
      fill: "#253B73",
      font: { bold: true, color: "#FFFFFF", fontSize: 16 },
      horizontalAlignment: "center",
      verticalAlignment: "center",
    };
    sheet.getRange("A1:F1").format.rowHeight = 28;
    const headerRow = sheet.name === latestName ? "A4:F4" : "A3:F3";
    sheet.getRange(headerRow).format = {
      fill: "#3E5D9A",
      font: { bold: true, color: "#FFFFFF", fontSize: 11 },
      horizontalAlignment: "center",
      verticalAlignment: "center",
      wrapText: true,
      borders: { preset: "all", style: "thin", color: "#C8D3E8" },
    };
    const dataStart = sheet.name === latestName ? 5 : 4;
    sheet.getRange("A" + dataStart + ":F" + endRow).format = {
      font: { color: "#1F2937", fontSize: 10 },
      verticalAlignment: "top",
      wrapText: true,
      borders: { preset: "all", style: "thin", color: "#D9E1F2" },
    };
    for (let row = dataStart; row <= endRow; row++) {
      if ((row - dataStart) % 2 === 1) {
        sheet.getRange("A" + row + ":F" + row).format.fill = "#F3F6FB";
      }
      sheet.getRange("A" + row + ":F" + row).format.rowHeight = 38;
    }
    sheet.getRange("A1:A" + endRow).format.columnWidth = 17;
    sheet.getRange("B1:B" + endRow).format.columnWidth = 23;
    sheet.getRange("C1:C" + endRow).format.columnWidth = 46;
    sheet.getRange("D1:D" + endRow).format.columnWidth = 38;
    sheet.getRange("E1:E" + endRow).format.columnWidth = 12;
    sheet.getRange("F1:F" + endRow).format.columnWidth = 48;
    sheet.freezePanes.freezeRows(sheet.name === latestName ? 4 : 3);
  };
  styleSheet(latest, 17);
  latest.getRange("A2:F2").format = {
    fill: "#E9EFF9",
    font: { italic: true, color: "#344563", fontSize: 10 },
    wrapText: true,
    horizontalAlignment: "left",
    verticalAlignment: "center",
  };
  latest.getRange("A2:F2").format.rowHeight = 28;
  styleSheet(validation, 10);
  validation.getRange("D4:D10").format.font = { bold: true, color: "#177245" };

  const formulaErrors = await workbook.inspect({
    kind: "match",
    searchTerm: "#REF!|#DIV/0!|#VALUE!|#NAME\\?|#N/A",
    options: { useRegex: true, maxResults: 300 },
    summary: "final formula error scan",
  });
  console.log(formulaErrors.ndjson);

  const finalInspect = await workbook.inspect({
    kind: "table",
    sheetId: latestName,
    range: "A1:F17",
    include: "values,formulas",
    maxChars: 10000,
    tableMaxRows: 20,
    tableMaxCols: 6,
  });
  console.log(finalInspect.ndjson);

  const afterSheets = await workbook.inspect({ kind: "sheet", include: "id,name", maxChars: 24000 });
  const afterRecords = afterSheets.ndjson
    .split(/\r?\n/)
    .filter(Boolean)
    .map((line) => JSON.parse(line))
    .filter((record) => record.kind === "sheet");
  const afterPreviewDir = path.join(workDir, "previews_after");
  await fs.mkdir(afterPreviewDir, { recursive: true });
  const renderChecks = [];
  for (const record of afterRecords) {
    const preview = await workbook.render({ sheetName: record.name, autoCrop: "all", scale: 1, format: "png" });
    const safeName = record.name.replace(/[\\/:*?"<>|]/g, "_");
    const previewPath = path.join(afterPreviewDir, safeName + ".png");
    await fs.writeFile(previewPath, new Uint8Array(await preview.arrayBuffer()));
    const stat = await fs.stat(previewPath);
    renderChecks.push({ sheet: record.name, bytes: stat.size });
  }

  const outputPath = path.join(workDir, "DefenseGame_Balance_Skill_Summary.xlsx");
  const output = await SpreadsheetFile.exportXlsx(workbook);
  await output.save(outputPath);
  console.log(JSON.stringify({ outputPath, formulaErrors: formulaErrors.ndjson, renderChecks }, null, 2));
  process.exit(0);
}


const sheetInspect = await workbook.inspect({
  kind: "sheet",
  include: "id,name",
  maxChars: 20000,
});
console.log(sheetInspect.ndjson);
await fs.writeFile(path.join(workDir, "before.inspect.ndjson"), sheetInspect.ndjson, "utf8");

const sheetRecords = sheetInspect.ndjson
  .split(/\r?\n/)
  .filter(Boolean)
  .map((line) => JSON.parse(line))
  .filter((record) => record.kind === "sheet");
const previewDir = path.join(workDir, "previews_before");
await fs.mkdir(previewDir, { recursive: true });
for (const record of sheetRecords) {
  const preview = await workbook.render({
    sheetName: record.name,
    autoCrop: "all",
    scale: 1,
    format: "png",
  });
  const safeName = record.name.replace(/[\\/:*?"<>|]/g, "_");
  await fs.writeFile(path.join(previewDir, safeName + ".png"), new Uint8Array(await preview.arrayBuffer()));
}
console.log(JSON.stringify({ renderedSheets: sheetRecords.map((record) => record.name) }, null, 2));

