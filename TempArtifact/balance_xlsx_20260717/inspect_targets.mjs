import { FileBlob, SpreadsheetFile } from "file:///C:/Users/noxis/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/@oai/artifact-tool/dist/artifact_tool.mjs";

const root = "D:/GameDev/ProjectDG";
const workbook = await SpreadsheetFile.importXlsx(
  await FileBlob.load(`${root}/docs/DefenseGame_Balance_Skill_Summary.xlsx`),
);

for (const [sheetName, range] of [
  ["요약", "A30:D45"],
  ["영웅 스킬", "A20:M32"],
  ["미정_확인", "A1:F20"],
  ["HeroAugments_V1", "A1:L30"],
  ["실측_5판", "A1:U15"],
  ["변경이력", "A1:F30"],
  ["Latest_2026-07-17", "A1:F25"],
  ["Validation_2026-07-17", "A1:F20"],
]) {
  const result = await workbook.inspect({
    kind: "table",
    range: `${sheetName}!${range}`,
    include: "values,formulas",
    tableMaxRows: 40,
    tableMaxCols: 24,
    tableMaxCellChars: 240,
    maxChars: 30000,
  });
  console.log(`\n=== ${sheetName}!${range} ===`);
  console.log(result.ndjson);
}


const hero32Matches = await workbook.inspect({
  kind: "match",
  searchTerm: "hero_32",
  options: { useRegex: false, maxResults: 30 },
  summary: "hero_32 workbook matches",
  maxChars: 12000,
});
console.log("\n=== hero_32 matches ===");
console.log(hero32Matches.ndjson);
