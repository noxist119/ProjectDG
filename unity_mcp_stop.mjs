const endpoint = "http://127.0.0.1:8080/mcp";
let sessionId = "";
let id = 1;
function parse(text) {
  if (!text) return null;
  const lines = text.split(/\r?\n/).filter((line) => line.startsWith("data:"));
  return JSON.parse(lines.length ? lines.at(-1).slice(5).trim() : text);
}
async function post(payload) {
  const headers = { Accept: "application/json, text/event-stream", "Content-Type": "application/json" };
  if (sessionId) headers["mcp-session-id"] = sessionId;
  const response = await fetch(endpoint, { method: "POST", headers, body: JSON.stringify(payload) });
  sessionId = response.headers.get("mcp-session-id") || sessionId;
  const body = await response.text();
  if (!response.ok) throw new Error(body);
  return parse(body);
}
await post({ jsonrpc: "2.0", id: id++, method: "initialize", params: { protocolVersion: "2025-03-26", capabilities: {}, clientInfo: { name: "codex-local-unity", version: "1.0" } } });
await post({ jsonrpc: "2.0", method: "notifications/initialized", params: {} });
const result = await post({ jsonrpc: "2.0", id: id++, method: "tools/call", params: { name: "manage_editor", arguments: { action: "stop" } } });
console.log(JSON.stringify(result, null, 2));
