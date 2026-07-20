import fs from "node:fs/promises";
import { FileBlob, SpreadsheetFile } from "file:///C:/Users/noxis/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/@oai/artifact-tool/dist/artifact_tool.mjs";

const root = "D:/GameDev/ProjectDG";
const inputPath = root + "/docs/DefenseGame_Balance_Skill_Summary.xlsx";
const outputDir = root + "/outputs/final_device_20260717";
const previewDir = root + "/TempXlsxPreviewDeviceFinal";
const outputPath = outputDir + "/DefenseGame_Balance_Skill_Summary.xlsx";

await fs.mkdir(outputDir, { recursive: true });
await fs.mkdir(previewDir, { recursive: true });

const workbook = await SpreadsheetFile.importXlsx(await FileBlob.load(inputPath));

const latest = workbook.worksheets.getItem("Latest_2026-07-17");
latest.getRange("A22:F22").values = [[
  "\uae30\uae30",
  "Galaxy Z Flip 4",
  "\ucd5c\uc885 APK \uc124\uce58\u00b7\ucf5c\ub4dc \uc2a4\ud0c0\ud2b8\u00b7SurfaceFlinger \uc2e4\uce21 \uc644\ub8cc",
  "Builds/Android/ProjectDG.apk",
  "\ud1b5\uacfc",
  "59.85fps / p95 17.0ms / 20ms \ucd08\uacfc 0 / Thermal 0",
]];
latest.getRange("A22:F22").format.wrapText = true;
latest.getRange("A22:F22").format.rowHeight = 58;

const validation = workbook.worksheets.getItem("Validation_2026-07-17");
validation.getRange("A10:F10").values = [[
  "\ubaa8\ubc14\uc77c \uc131\ub2a5",
  "\ucd5c\uc885 APK \uc124\uce58 PASS / 59.85fps",
  "AndroidBuild_MetaRoot_20260717.log / SurfaceFlinger",
  "\uc815\uc0c1",
  "p95 17.0ms, >20ms 0, PSS 671.6MB, Thermal 0",
  "Galaxy Z Flip 4",
]];
validation.getRange("A10:F10").format.wrapText = true;
validation.getRange("A10:F10").format.rowHeight = 62;

for (const sheetName of ["Latest_2026-07-17", "Validation_2026-07-17"]) {
  const preview = await workbook.render({
    sheetName,
    autoCrop: "all",
    scale: 0.8,
    format: "png",
  });
  await fs.writeFile(
    previewDir + "/" + sheetName + ".png",
    new Uint8Array(await preview.arrayBuffer()),
  );
}

const latestCheck = await workbook.inspect({
  kind: "table",
  range: "Latest_2026-07-17!A20:F22",
  include: "values,formulas",
  tableMaxRows: 5,
  tableMaxCols: 6,
  tableMaxCellChars: 240,
  maxChars: 12000,
});
const validationCheck = await workbook.inspect({
  kind: "table",
  range: "Validation_2026-07-17!A8:F11",
  include: "values,formulas",
  tableMaxRows: 6,
  tableMaxCols: 6,
  tableMaxCellChars: 240,
  maxChars: 12000,
});
const formulaErrors = await workbook.inspect({
  kind: "match",
  searchTerm: "#REF!|#DIV/0!|#VALUE!|#NAME\\?|#N/A",
  options: { useRegex: true, maxResults: 300 },
  maxChars: 30000,
});

const output = await SpreadsheetFile.exportXlsx(workbook);
await output.save(outputPath);

const report = {
  outputPath,
  latest: latestCheck.ndjson,
  validation: validationCheck.ndjson,
  formulaErrors: formulaErrors.ndjson,
};
await fs.writeFile(
  outputDir + "/device_sync_verification.json",
  JSON.stringify(report, null, 2),
  "utf8",
);
console.log(JSON.stringify(report, null, 2));
