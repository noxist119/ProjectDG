import fs from "node:fs/promises";
import { FileBlob, SpreadsheetFile } from "file:///C:/Users/noxis/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/@oai/artifact-tool/dist/artifact_tool.mjs";
const root = "D:/GameDev/ProjectDG";
const path = root + "/outputs/balance_xlsx_20260717/DefenseGame_Balance_Skill_Summary.xlsx";
const workbook = await SpreadsheetFile.importXlsx(await FileBlob.load(path));
workbook.worksheets.getItem("\u0056\u0061\u006c\u0069\u0064\u0061\u0074\u0069\u006f\u006e\u005f\u0032\u0030\u0032\u0036\u002d\u0030\u0037\u002d\u0031\u0037").getRange("A10:F12").format.rowHeight = 62;
workbook.worksheets.getItem("\u004c\u0061\u0074\u0065\u0073\u0074\u005f\u0032\u0030\u0032\u0036\u002d\u0030\u0037\u002d\u0031\u0037").getRange("A18:F22").format.rowHeight = 52;
const errors = await workbook.inspect({ kind: "match", searchTerm: "#REF!|#DIV/0!|#VALUE!|#NAME\\?|#N/A", options: { useRegex: true, maxResults: 300 }, maxChars: 30000 });
for (let index = 0; index < workbook.worksheets.items.length; index += 1) {
  const current = workbook.worksheets.getItemAt(index);
  const preview = await workbook.render({ sheetName: current.name, autoCrop: "all", scale: 0.8, format: "png" });
  const safe = String(index + 1).padStart(2, "0") + "_" + current.name.replace(/[\\/:*?"<>|]/g, "_") + ".png";
  await fs.writeFile(root + "/TempXlsxPreviewAfter/" + safe, new Uint8Array(await preview.arrayBuffer()));
}
const output = await SpreadsheetFile.exportXlsx(workbook);
await output.save(path);
console.log(JSON.stringify({ path, sheets: workbook.worksheets.items.length, formulaErrors: errors.ndjson }));
