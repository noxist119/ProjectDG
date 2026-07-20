import fs from "node:fs/promises";
import { FileBlob, SpreadsheetFile } from "@oai/artifact-tool";

const workbookPath = "D:/GameDev/ProjectDG/docs/DefenseGame_Balance_Skill_Summary.xlsx";
const input = await FileBlob.load(workbookPath);
const workbook = await SpreadsheetFile.importXlsx(input);
const result = await workbook.inspect({
  kind: "sheet,table",
  include: "id,name",
  maxChars: 12000,
  tableMaxRows: 8,
  tableMaxCols: 12,
});
console.log(result.ndjson);
