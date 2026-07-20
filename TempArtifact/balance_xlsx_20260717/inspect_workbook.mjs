import fs from "node:fs/promises";
import { FileBlob, SpreadsheetFile } from "@oai/artifact-tool";

const root = "D:/GameDev/ProjectDG";
const workbookPath = `${root}/docs/DefenseGame_Balance_Skill_Summary.xlsx`;
const outputDir = `${root}/TempArtifact/balance_xlsx_20260717/before`;
await fs.mkdir(outputDir, { recursive: true });

const workbook = await SpreadsheetFile.importXlsx(await FileBlob.load(workbookPath));
const sheets = await workbook.inspect({
  kind: "sheet",
  include: "id,name",
  maxChars: 12000,
});
console.log(sheets.ndjson);

const overview = await workbook.inspect({
  kind: "workbook,sheet,table",
  maxChars: 16000,
  tableMaxRows: 6,
  tableMaxCols: 10,
  tableMaxCellChars: 100,
});
console.log(overview.ndjson);

const names = [];
for (let i = 0; i < workbook.worksheets.items.length; i += 1) {
  const sheet = workbook.worksheets.getItemAt(i);
  names.push(sheet.name);
  const preview = await workbook.render({
    sheetName: sheet.name,
    autoCrop: "all",
    scale: 0.8,
    format: "png",
  });
  const safeName = `${String(i + 1).padStart(2, "0")}_${sheet.name.replace(/[\\/:*?"<>|]/g, "_")}.png`;
  await fs.writeFile(`${outputDir}/${safeName}`, new Uint8Array(await preview.arrayBuffer()));
}
await fs.writeFile(`${outputDir}/sheet_names.json`, JSON.stringify(names, null, 2), "utf8");
