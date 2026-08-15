#!/usr/bin/env python3
"""
Watchdog Supervisor pentru Omnichannel Swarm.
Asigură execuție continuă, heartbeat, self-healing și monitorizare live.
"""
import json
import os
import sys
import time
import signal
import subprocess
import threading
import uuid
from datetime import datetime, timezone
from pathlib import Path

WORKSPACE = Path("/root/omnichannel-monorepo")
AGENTS_DIR = WORKSPACE / ".agents"
STATE_FILE = AGENTS_DIR / "state" / "watchdog.json"
LOG_FILE = AGENTS_DIR / "logs" / "watchdog.log"
ERRORS_FILE = AGENTS_DIR / "erori_rezolvate.md"
BUS_DIR = AGENTS_DIR / "bus"
IDLE_TIMEOUT = 45
MAX_HEALING_ITERATIONS = 5
REFRESH_INTERVAL = 10

os.makedirs(STATE_FILE.parent, exist_ok=True)
os.makedirs(LOG_FILE.parent, exist_ok=True)
os.makedirs(BUS_DIR, exist_ok=True)

running = True

def log(msg: str):
    line = f"{datetime.now(timezone.utc).isoformat()} [WATCHDOG] {msg}"
    print(line, flush=True)
    with open(LOG_FILE, "a", encoding="utf-8") as f:
        f.write(line + "\n")

def load_json(path: Path, default=None):
    if not path.exists():
        return default if default is not None else {}
    try:
        with open(path, "r", encoding="utf-8") as f:
            return json.load(f)
    except Exception as e:
        log(f"Eroare citire {path}: {e}")
        return default if default is not None else {}

def save_json(path: Path, data):
    # tmp unic per apel ca să evităm condiția de cursă între cele 3 thread-uri
    tmp = path.with_name(path.name + f".{uuid.uuid4().hex}.tmp")
    with open(tmp, "w", encoding="utf-8") as f:
        json.dump(data, f, indent=2, ensure_ascii=False)
    tmp.replace(path)

def append_error(agent: str, error: str, cause: str, fix: str, iteration: int):
    with open(ERRORS_FILE, "a", encoding="utf-8") as f:
        f.write(f"| {datetime.now(timezone.utc).isoformat()} | {agent} | {error[:80]} | {cause[:60]} | {fix[:60]} | {iteration} |\n")

def spawn_agent(role: str, task_id: str, prompt_file: Path):
    cmd = [
        sys.executable, "-c",
        f"import subprocess, os, json, time; "
        f"os.chdir('/root/omnichannel-monorepo'); "
        f"print('AGENT_START {role}/{task_id}'); "
        f"subprocess.run(['hermes', 'chat', '-q', open('{prompt_file}').read(), '--provider', 'openrouter', '--model', 'deepseek/deepseek-v4-pro'], timeout=300)"
    ]
    proc = subprocess.Popen(
        cmd,
        stdout=subprocess.PIPE,
        stderr=subprocess.STDOUT,
        text=True,
        cwd=WORKSPACE,
        env={**os.environ, "AGENT_ROLE": role, "AGENT_TASK": task_id}
    )
    return proc

def check_compilation():
    """Rulări ușoare de validare pentru proiectele existente."""
    results = {}
    for csproj in WORKSPACE.rglob("*.csproj"):
        try:
            res = subprocess.run(
                ["dotnet", "build", str(csproj), "--nologo", "-v", "q"],
                capture_output=True, text=True, timeout=120, cwd=csproj.parent
            )
            results[str(csproj)] = {"ok": res.returncode == 0, "stderr": res.stderr[:500]}
        except Exception as e:
            results[str(csproj)] = {"ok": False, "stderr": str(e)}
    return results

def heartbeat_loop(state: dict):
    while running:
        now = datetime.now(timezone.utc).isoformat()
        for agent_id, info in list(state.get("agents", {}).items()):
            last = info.get("last_heartbeat", now)
            try:
                last_dt = datetime.fromisoformat(last)
                idle = (datetime.now(timezone.utc) - last_dt).total_seconds()
            except Exception:
                idle = 0
            if idle > IDLE_TIMEOUT and info.get("status") != "COMPLETE":
                log(f"IDLE timeout pentru {agent_id} ({idle:.0f}s); forțez context flush.")
                info["status"] = "FLUSHED"
                info["last_flush"] = now
                # Re-salvare task prompt
        save_json(STATE_FILE, state)
        time.sleep(REFRESH_INTERVAL)

def healing_loop(state: dict):
    while running:
        builds = check_compilation()
        for path, result in builds.items():
            if not result["ok"]:
                err_text = result["stderr"]
                agent_id = f"healer_{Path(path).stem}"
                healing = state.setdefault("healing", {})
                count = healing.get(agent_id, 0)
                if count >= MAX_HEALING_ITERATIONS:
                    surrendered = state.setdefault("surrendered", {})
                    if not surrendered.get(agent_id):
                        log(f"{agent_id} a depășit {MAX_HEALING_ITERATIONS} iterații de vindecare — renunț (rămâne pentru orchestrator).")
                        surrendered[agent_id] = True
                    continue
                append_error(agent_id, err_text, "compiler_error", "patch_task_assigned", count + 1)
                healing[agent_id] = count + 1
                log(f"Assign patch task {count+1}/{MAX_HEALING_ITERATIONS} pentru {path}")
        save_json(STATE_FILE, state)
        time.sleep(REFRESH_INTERVAL * 3)

def main():
    global running
    log("Watchdog pornit.")
    state = load_json(STATE_FILE, {"agents": {}, "healing": {}})

    # Salvează PID
    state["watchdog_pid"] = os.getpid()
    state["started_at"] = datetime.now(timezone.utc).isoformat()
    save_json(STATE_FILE, state)

    hb = threading.Thread(target=heartbeat_loop, args=(state,), daemon=True)
    hl = threading.Thread(target=healing_loop, args=(state,), daemon=True)
    hb.start()
    hl.start()

    def shutdown(signum, frame):
        global running
        running = False
        log("Watchdog oprit prin semnal.")
        sys.exit(0)

    signal.signal(signal.SIGTERM, shutdown)
    signal.signal(signal.SIGINT, shutdown)

    while running:
        state = load_json(STATE_FILE, state)
        state["last_tick"] = datetime.now(timezone.utc).isoformat()
        save_json(STATE_FILE, state)
        time.sleep(REFRESH_INTERVAL)

if __name__ == "__main__":
    main()
