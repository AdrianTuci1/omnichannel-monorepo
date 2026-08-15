#!/usr/bin/env python3
"""
Dispecerizare DETACHED a unui worker. Pornește run_worker.py într-o sesiune separată
(start_new_session) și revine imediat, lăsând workerul să ruleze autonom după ce
orchestratorul (Root Planner) își încheie rularea. Scrie PID-ul în state/dispatched/.
"""
import sys
import json
import subprocess
from pathlib import Path
from datetime import datetime, timezone

WORKSPACE = Path("/root/omnichannel-monorepo")
AGENTS = WORKSPACE / ".agents"
STATE_DIR = AGENTS / "state" / "dispatched"
STATE_DIR.mkdir(parents=True, exist_ok=True)
(AGENTS / "logs").mkdir(parents=True, exist_ok=True)


def main():
    if len(sys.argv) < 3:
        print("Usage: dispatch_worker.py <name> <prompt_file>", file=sys.stderr)
        sys.exit(1)
    name, prompt_file = sys.argv[1], sys.argv[2]

    logf = open(AGENTS / "logs" / f"{name}.dispatch.log", "a", encoding="utf-8")
    proc = subprocess.Popen(
        [sys.executable, str(AGENTS / "run_worker.py"), name, prompt_file],
        cwd=WORKSPACE,
        stdout=logf,
        stderr=subprocess.STDOUT,
        start_new_session=True,
    )
    (STATE_DIR / f"{name}.json").write_text(json.dumps({
        "name": name,
        "pid": proc.pid,
        "started_at": datetime.now(timezone.utc).isoformat(),
        "prompt_file": prompt_file,
    }, indent=2, ensure_ascii=False))
    logf.close()
    print(f"Dispatched {name} pid={proc.pid}")


if __name__ == "__main__":
    main()
