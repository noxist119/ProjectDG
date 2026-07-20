import fs from "node:fs/promises";
const path = "D:/GameDev/ProjectDG/BatchPlaytestResults/DefenseGame_Playtest5_Human3.json";
const text = (await fs.readFile(path, "utf8")).replace(/^\uFEFF/, "");
await fs.writeFile(path, text, "utf8");
