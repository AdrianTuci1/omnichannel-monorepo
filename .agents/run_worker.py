#!/usr/bin/env python3
"""
Wrapper pentru workeri. Rulează un prompt Hermes ca one-shot și scrie output-ul într-un fișier log.
Folosit de cron jobs pentru a expune acțiunile în dashboard.
"""
import os
import sys
import subprocess
import json
from datetime import datetime, timezone
from pathlib import Path

WORKSPACE = Path("/root/omnichannel-monorepo")
AGENTS_DIR = WORKSPACE / ".agents"
LOGS_DIR = AGENTS_DIR / "logs"
STATE_DIR = AGENTS_DIR / "state"
WORKERS_FILE = STATE_DIR / "workers.json"
LOGS_DIR.mkdir(parents=True, exist_ok=True)
STATE_DIR.mkdir(parents=True, exist_ok=True)

def log(name: str, msg: str):
    line = f"{datetime.now(timezone.utc).isoformat()} [{name.upper()}] {msg}\n"
    print(line, end="", flush=True)
    with open(LOGS_DIR / f"{name}.log", "a", encoding="utf-8") as f:
        f.write(line)

def update_worker_status(name: str, **fields):
    """Scrie statusul workerului în .agents/state/workers.json (citit de dashboard)."""
    data = {}
    if WORKERS_FILE.exists():
        try:
            with open(WORKERS_FILE, "r", encoding="utf-8") as f:
                data = json.load(f)
        except Exception:
            data = {}
    entry = data.setdefault(name, {})
    entry.update(fields)
    tmp = WORKERS_FILE.with_suffix(".tmp")
    with open(tmp, "w", encoding="utf-8") as f:
        json.dump(data, f, indent=2, ensure_ascii=False)
    tmp.replace(WORKERS_FILE)

def main():
    if len(sys.argv) < 3:
        print("Usage: run_worker.py <worker_name> <prompt_file> [extra context file]", file=sys.stderr)
        sys.exit(1)

    name = sys.argv[1]
    prompt_file = Path(sys.argv[2])
    extra = Path(sys.argv[3]) if len(sys.argv) > 3 else None

    log(name, f"Pornesc worker. Prompt: {prompt_file}")
    update_worker_status(name, status="running", pid=os.getpid(),
                         started_at=datetime.now(timezone.utc).isoformat(),
                         prompt_file=str(prompt_file))

    if not prompt_file.exists():
        log(name, f"Eroare: fișier prompt lipsă {prompt_file}")
        sys.exit(2)

    prompt = prompt_file.read_text(encoding="utf-8")

    # Încarcă contextul din blackboard în prompt
    try:
        with open(AGENTS_DIR / "bus" / "contracts.json", "r", encoding="utf-8") as f:
            contracts = json.load(f)
        prompt += f"\n\n[BLACKBOARD contracts.json]\n{json.dumps(contracts, indent=2, ensure_ascii=False)}"
    except Exception as e:
        log(name, f"Nu am putut încărca contracts.json: {e}")

    try:
        with open(AGENTS_DIR / "bus" / "rpc.json", "r", encoding="utf-8") as f:
            rpc = json.load(f)
        prompt += f"\n\n[BLACKBOARD rpc.json]\n{json.dumps(rpc, indent=2, ensure_ascii=False)}"
    except Exception as e:
        log(name, f"Nu am putut încărca rpc.json: {e}")

    if extra and extra.exists():
        prompt += f"\n\n[EXTRA CONTEXT]\n{extra.read_text(encoding='utf-8')}"

    log(name, "Invoc hermes chat -q ...")
    try:
        proc = subprocess.Popen(
            ["hermes", "chat", "-Q", "-q", prompt, "--provider", "deepseek", "--model", "deepseek-v4-pro"],
            cwd=WORKSPACE,
            stdout=subprocess.PIPE,
            stderr=subprocess.STDOUT,
            text=True,
            bufsize=1,
        )
        assert proc.stdout is not None
        for line in proc.stdout:
            line = line.rstrip("\n")
            if line.strip():
                log(name, line)
        proc.wait(timeout=600)
        code = proc.returncode
        log(name, f"Exit code: {code}")
        update_worker_status(name, status="done" if code == 0 else "failed",
                             exit_code=code,
                             finished_at=datetime.now(timezone.utc).isoformat())
    except subprocess.TimeoutExpired:
        proc.kill()
        log(name, "Timeout după 600s")
        update_worker_status(name, status="timeout",
                             finished_at=datetime.now(timezone.utc).isoformat())
    except Exception as e:
        log(name, f"Eroare execuție: {e}")
        update_worker_status(name, status="error", error=str(e),
                             finished_at=datetime.now(timezone.utc).isoformat())

if __name__ == "__main__":
    main()
