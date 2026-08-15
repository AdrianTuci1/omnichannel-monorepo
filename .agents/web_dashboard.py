#!/usr/bin/env python3
"""
Dashboard web curat pentru monitorizarea live a swarm-ului.
Accesibil la http://127.0.0.1:8080
Arată: ierarhie, status workeri, filesystem (ce se construiește) și log-uri live.
"""
import json
import os
import subprocess
from datetime import datetime, timezone
from pathlib import Path
from http.server import HTTPServer, BaseHTTPRequestHandler

WORKSPACE = Path("/root/omnichannel-monorepo")
STATE_FILE = WORKSPACE / ".agents" / "state" / "watchdog.json"
WORKERS_FILE = WORKSPACE / ".agents" / "state" / "workers.json"
BUS_DIR = WORKSPACE / ".agents" / "bus"
LOGS_DIR = WORKSPACE / ".agents" / "logs"

EXCLUDE_DIRS = {".agents", ".git", "bin", "obj", "node_modules", "dist", "target",
                ".next", "build", "coverage", "wwwroot", ".gradle", ".idea"}
EXCLUDE_SUFFIXES = (".dll", ".pdb", ".cache", ".deps.json", ".runtimeconfig.json",
                    ".assets.cache", ".csproj.nuget.dgspec.json", ".nuget.g.props",
                    ".nuget.g.targets", ".FileListAbsolute.txt", ".AssemblyInfo.cs",
                    ".GlobalUsings.g.cs", ".editorconfig", ".lock")

HTML = r"""<!doctype html>
<html lang="ro">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
<title>Omnichannel Swarm Monitor</title>
<style>
* { box-sizing: border-box; }
html, body { height: 100%; margin: 0; }
body { font-family: ui-monospace, SFMono-Regular, Menlo, monospace; background:#0b0c15; color:#d1d5db; overflow:hidden; }
header { padding:12px 20px; border-bottom:1px solid #1f2937; display:flex; align-items:center; justify-content:space-between; background:#0f172a; }
header h1 { margin:0; color:#22d3ee; font-size:1.15rem; }
.stats { display:flex; gap:16px; font-size:0.82rem; color:#94a3b8; }
.stats b { color:#e2e8f0; }
#layout { display:grid; grid-template-columns:400px 1fr; height:calc(100vh - 54px); }
#panel { border-right:1px solid #1f2937; overflow:auto; background:#0b1220; }
#panel section { padding:14px 16px; border-bottom:1px solid #1f2937; }
#panel h3 { margin:0 0 8px; font-size:0.78rem; color:#64748b; text-transform:uppercase; letter-spacing:0.05em; }
.agent { padding:5px 10px; border-radius:6px; margin-bottom:4px; font-size:0.85rem; display:flex; align-items:center; gap:8px; }
.agent::before { content:"●"; font-size:0.65rem; color:#64748b; }
.agent.root { color:#f472b6; background:#1e1b2e; }
.agent.sub { color:#a78bfa; background:#181624; padding-left:20px; }
.agent.leaf { color:#34d399; background:#111f1b; padding-left:34px; }
.agent.active::before { color:#34d399; }
.agent.failed::before { color:#f87171; }
.wkr { display:flex; align-items:center; gap:8px; font-size:0.82rem; padding:3px 0; }
.wkr .dot { width:8px; height:8px; border-radius:50%; }
.wkr .dot.running { background:#fbbf24; }
.wkr .dot.done { background:#34d399; }
.wkr .dot.failed { background:#f87171; }
.wkr .dot.timeout { background:#f59e0b; }
.wkr .dot.error { background:#f87171; }
.wkr .nm { color:#e2e8f0; flex:1; }
.wkr .st { color:#94a3b8; }
#fs { white-space:pre; font-size:0.72rem; line-height:1.35; color:#7dd3fc; overflow:auto; max-height:45vh; }
#recent { font-size:0.72rem; line-height:1.4; }
#recent .r { display:flex; gap:6px; color:#94a3b8; }
#recent .r .p { color:#7dd3fc; flex:1; }
#recent .r .a { color:#64748b; }
#logs-col { display:flex; flex-direction:column; overflow:hidden; background:#080a12; }
#logs-header { padding:8px 16px; border-bottom:1px solid #1f2937; font-size:0.78rem; color:#64748b; display:flex; justify-content:space-between; align-items:center; }
#logs { flex:1; overflow:auto; padding:10px 16px; white-space:pre; font-size:0.8rem; line-height:1.45; color:#a3b3cc; }
#logs .line { padding:2px 0; }
#logs .ts { color:#64748b; }
#logs .agent { color:#22d3ee; font-weight:bold; }
#logs .lvl-stderr { color:#f87171; }
</style>
</head>
<body>
<header>
  <h1>⚕ Omnichannel Swarm Monitor</h1>
  <div class="stats">
    <span>Watchdog <b id="pid">-</b></span>
    <span>Contracts <b id="contracts">-</b></span>
    <span>RPC <b id="rpc">-</b></span>
    <span>UTC <b id="utc">-</b></span>
  </div>
</header>
<div id="layout">
  <div id="panel">
    <section><h3>Hierarchy</h3>
      <div class="agent root">Root Planner</div>
      <div class="agent sub" id="sub-domain">Domain Subplanner</div>
      <div class="agent sub" id="sub-clients">Clients Subplanner</div>
      <div class="agent sub" id="sub-integrations">Integrations Subplanner</div>
      <div class="agent sub" id="sub-infra">Infra/Data Subplanner</div>
      <div class="agent leaf" id="leaf-domain">Domain Worker</div>
      <div class="agent leaf" id="leaf-nextjs">Next.js Worker</div>
      <div class="agent leaf" id="leaf-android">Android Worker</div>
      <div class="agent leaf" id="leaf-pos">POS Worker</div>
      <div class="agent leaf" id="leaf-odoo">Odoo Worker</div>
      <div class="agent leaf" id="leaf-akeneo">Akeneo Worker</div>
      <div class="agent leaf" id="leaf-cdp">CDP Worker</div>
      <div class="agent leaf" id="leaf-recommender">Recommender Worker</div>
      <div class="agent leaf" id="leaf-dbt">dbt Worker</div>
      <div class="agent leaf" id="leaf-terraform">Terraform Worker</div>
      <div class="agent leaf" id="leaf-helm">Helm Worker</div>
    </section>
    <section><h3>Workers</h3><div id="workers">-</div></section>
    <section><h3>Filesystem</h3><div id="fs">Se încarcă...</div></section>
    <section><h3>Recent files</h3><div id="recent"></div></section>
  </div>
  <div id="logs-col">
    <div id="logs-header"><span>Live Logs</span><span id="log-count">0</span></div>
    <div id="logs">Se încarcă log-urile...</div>
  </div>
</div>
<script>
function escapeHtml(s){ return String(s).replace(/&/g,'&amp;').replace(/</g,'&lt;').replace(/>/g,'&gt;'); }
function renderLine(line){
  var colon = line.indexOf(':');
  var open = line.indexOf('[');
  var close = line.indexOf(']');
  var cls = (/STDERR|Error|error|failed|Timeout/).test(line) ? 'lvl-stderr' : '';
  if (colon === -1 || open === -1 || close === -1) {
    return '<div class="line ' + cls + '">' + escapeHtml(line) + '</div>';
  }
  var ts = line.slice(colon + 2, open - 1);
  var agent = line.slice(open + 1, close);
  var msg = line.slice(close + 2);
  return '<div class="line ' + cls + '"><span class="ts">' + escapeHtml(ts) + '</span> <span class="agent">' + escapeHtml(agent) + '</span> ' + escapeHtml(msg) + '</div>';
}
var HIER = {
  'sub-domain': ['domain'], 'sub-clients': ['clients'], 'sub-integrations': ['integrations','odoo','akeneo','cdp','recommender'],
  'sub-infra': ['infra','dbt','terraform','helm'],
  'leaf-domain': ['domain','m1'], 'leaf-nextjs': ['nextjs','web'], 'leaf-android': ['android'], 'leaf-pos': ['pos'],
  'leaf-odoo': ['odoo'], 'leaf-akeneo': ['akeneo'], 'leaf-cdp': ['cdp'], 'leaf-recommender': ['recommender'],
  'leaf-dbt': ['dbt'], 'leaf-terraform': ['terraform'], 'leaf-helm': ['helm']
};
function statusClass(st){ return st === 'running' ? 'running' : (st === 'done' ? 'done' : 'failed'); }
function updateHierarchy(workers){
  for (var id in HIER){
    var el = document.getElementById(id);
    if (!el) continue;
    var cls = '';
    var keys = HIER[id];
    for (var name in workers){
      var w = workers[name];
      var matched = keys.some(function(k){ return name.indexOf(k) !== -1; });
      if (matched){
        if (w.status === 'running' || w.status === 'done') cls = 'active';
        else if (w.status === 'failed' || w.status === 'error' || w.status === 'timeout') cls = 'failed';
      }
    }
    el.className = 'agent ' + (id.indexOf('sub') === 0 ? 'sub' : 'leaf') + (cls ? ' ' + cls : '');
  }
}
function renderWorkers(workers){
  var el = document.getElementById('workers');
  var names = Object.keys(workers);
  if (names.length === 0){ el.innerHTML = '<span style="color:#64748b">niciun worker lansat</span>'; return; }
  var html = '';
  names.forEach(function(n){
    var w = workers[n];
    html += '<div class="wkr"><span class="dot ' + statusClass(w.status) + '"></span><span class="nm">' + escapeHtml(n) + '</span><span class="st">' + escapeHtml(w.status || '?') + '</span></div>';
  });
  el.innerHTML = html;
}
var priorLogs = '';
var userScrolledUp = false;
var logsEl = document.getElementById('logs');
logsEl.addEventListener('scroll', function(){
  var st = logsEl.scrollTop;
  var max = logsEl.scrollHeight - logsEl.clientHeight;
  userScrolledUp = max > 0 && st < max - 10;
});
function refresh(){
  fetch('/api/state').then(function(res){
    if (!res.ok) throw new Error('HTTP ' + res.status);
    return res.json();
  }).then(function(data){
    document.getElementById('pid').textContent = data.watchdog_pid || '-';
    document.getElementById('contracts').textContent = data.contracts_count;
    document.getElementById('rpc').textContent = data.rpc_pending;
    document.getElementById('utc').textContent = (data.utc || '').split('T')[1] ? (data.utc.split('T')[1].split('.')[0] || '-') : '-';
    document.getElementById('log-count').textContent = (data.log_lines || 0) + ' files';
    var workers = data.workers || {};
    updateHierarchy(workers);
    renderWorkers(workers);
    document.getElementById('fs').textContent = data.fs_tree || 'Director gol.';
    var recentHtml = '';
    (data.recent || []).forEach(function(r){
      recentHtml += '<div class="r"><span class="p">' + escapeHtml(r.path) + '</span><span class="a">' + escapeHtml(r.age) + '</span></div>';
    });
    document.getElementById('recent').innerHTML = recentHtml;
    if (data.logs !== priorLogs){
      priorLogs = data.logs;
      var lines = data.logs.split('\n').filter(function(l){ return l.trim(); });
      logsEl.innerHTML = lines.length ? lines.map(renderLine).join('') : '<span style="color:#64748b">Niciun log încă.</span>';
      if (!userScrolledUp) logsEl.scrollTop = logsEl.scrollHeight;
    }
  }).catch(function(e){
    logsEl.innerHTML = '<span style="color:#f87171">Eroare încărcare: ' + escapeHtml(e.message) + '</span>';
  });
}
refresh();
setInterval(refresh, 3000);
</script>
</body>
</html>"""


def _is_source_file(name: str) -> bool:
    return not name.endswith(EXCLUDE_SUFFIXES) and not name.startswith(".")


def build_fs_tree(max_depth: int = 6, max_lines: int = 300) -> str:
    lines: list[str] = []

    def walk(dir_path: Path, depth: int):
        if depth > max_depth or len(lines) >= max_lines:
            return
        try:
            entries = sorted(dir_path.iterdir(), key=lambda p: (p.is_file(), p.name.lower()))
        except Exception:
            return
        for e in entries:
            if len(lines) >= max_lines:
                return
            if e.name in EXCLUDE_DIRS or e.name.startswith("."):
                continue
            indent = "  " * depth
            if e.is_dir():
                lines.append(f"{indent}{e.name}/")
                walk(e, depth + 1)
            elif _is_source_file(e.name):
                lines.append(f"{indent}{e.name}")

    walk(WORKSPACE, 0)
    return "\n".join(lines) if lines else "(director gol)"


def recent_files(n: int = 25) -> list[dict]:
    out: list[tuple[float, str]] = []
    now = datetime.now(timezone.utc).timestamp()
    for root, dirs, files in os.walk(WORKSPACE):
        dirs[:] = [d for d in dirs if d not in EXCLUDE_DIRS and not d.startswith(".")]
        for f in files:
            if not _is_source_file(f):
                continue
            p = Path(root) / f
            try:
                mt = p.stat().st_mtime
            except Exception:
                continue
            out.append((mt, str(p.relative_to(WORKSPACE))))
    out.sort(key=lambda x: -x[0])
    recent = []
    for mt, rel in out[:n]:
        age = int(now - mt)
        if age < 60:
            age_s = f"{age}s"
        elif age < 3600:
            age_s = f"{age // 60}m"
        else:
            age_s = f"{age // 3600}h"
        recent.append({"path": rel, "age": age_s})
    return recent


def read_workers() -> dict:
    if WORKERS_FILE.exists():
        try:
            with open(WORKERS_FILE, "r", encoding="utf-8") as f:
                return json.load(f)
        except Exception:
            pass
    return {}


def detect_running_workers() -> dict:
    out = {}
    try:
        r = subprocess.run(["ps", "-eo", "args"], capture_output=True, text=True, timeout=3)
        for line in r.stdout.splitlines():
            if "run_worker.py" not in line:
                continue
            parts = line.split()
            for i, tok in enumerate(parts):
                if tok.endswith("run_worker.py") and i + 1 < len(parts):
                    out[parts[i + 1]] = {"status": "running"}
                    break
    except Exception:
        pass
    return out


def tail_logs(lines: int = 300) -> str:
    all_lines: list[tuple[datetime, str]] = []
    if not LOGS_DIR.exists():
        return "Directorul de log-uri nu există încă."
    for log_path in list(LOGS_DIR.glob("*.log")) + list(LOGS_DIR.glob("*.txt")):
        try:
            with open(log_path, "r", encoding="utf-8", errors="ignore") as f:
                file_lines = f.readlines()
        except Exception:
            continue
        for line in file_lines:
            ts = None
            if line.startswith("20") and "T" in line[:23]:
                try:
                    ts = datetime.fromisoformat(line[:23])
                except Exception:
                    pass
            stripped = line.strip()
            if stripped.startswith(("Query:", "Warning:", "Initializing agent")):
                continue
            if "preparing " in stripped or stripped.startswith(("─", "╭", "╰")):
                continue
            all_lines.append((ts or datetime.min.replace(tzinfo=timezone.utc), f"{log_path.name}: {line.rstrip()}"))

    def to_utc(dt):
        return dt.replace(tzinfo=timezone.utc) if dt.tzinfo is None else dt.astimezone(timezone.utc)

    all_lines.sort(key=lambda x: to_utc(x[0]))
    return "\n".join([ln for _, ln in all_lines[-lines:]])


class Handler(BaseHTTPRequestHandler):
    def do_GET(self):
        if self.path == '/':
            self.send_response(200)
            self.send_header('Content-Type', 'text/html; charset=utf-8')
            self.end_headers()
            self.wfile.write(HTML.encode('utf-8'))
        elif self.path == '/api/state':
            state = {}
            if STATE_FILE.exists():
                try:
                    with open(STATE_FILE, 'r', encoding='utf-8') as f:
                        state = json.load(f)
                except Exception:
                    pass
            contracts = []
            rpc = {}
            try:
                with open(BUS_DIR / 'contracts.json', 'r', encoding='utf-8') as f:
                    contracts_data = json.load(f)
                contracts = contracts_data.get('root', {}).get('contracts', []) or contracts_data.get('contracts', [])
            except Exception:
                pass
            try:
                with open(BUS_DIR / 'rpc.json', 'r', encoding='utf-8') as f:
                    rpc = json.load(f)
            except Exception:
                pass
            try:
                logs_text = tail_logs(300)
            except Exception as e:
                logs_text = f"Eroare citire log-uri: {e}"
            workers = read_workers()
            for name, w in detect_running_workers().items():
                workers.setdefault(name, {}).update(w)
            payload = {
                "workspace": str(WORKSPACE),
                "watchdog_pid": state.get("watchdog_pid"),
                "last_tick": state.get("last_tick"),
                "agents": state.get("agents", {}),
                "workers": workers,
                "contracts_count": len(contracts),
                "rpc_pending": len(rpc.get('pending', [])),
                "log_lines": len(list(LOGS_DIR.glob("*.log"))) + len(list(LOGS_DIR.glob("*.txt"))),
                "fs_tree": build_fs_tree(),
                "recent": recent_files(),
                "logs": logs_text,
                "utc": datetime.now(timezone.utc).isoformat()
            }
            body = json.dumps(payload, ensure_ascii=False, indent=2).encode('utf-8')
            self.send_response(200)
            self.send_header('Content-Type', 'application/json; charset=utf-8')
            self.send_header('Content-Length', str(len(body)))
            self.end_headers()
            self.wfile.write(body)
        else:
            self.send_response(404)
            self.end_headers()

    def log_message(self, fmt, *args):
        pass


def main():
    server = HTTPServer(('127.0.0.1', 8080), Handler)
    print("Dashboard live la http://127.0.0.1:8080", flush=True)
    server.serve_forever()


if __name__ == "__main__":
    main()
