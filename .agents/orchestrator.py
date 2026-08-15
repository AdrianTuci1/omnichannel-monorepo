#!/usr/bin/env python3
"""
Orchestrator loop: rulează Root Planner-ul la fiecare N minute, detached.
Înlocuiește cron-ul Hermes (care necesită gateway) pentru execuție autonomă.
Tick-ul rulează run_worker.py root_planner, care spaunează hermes chat -q cu
prompt-ul orchestratorului; acesta citește blackboard-ul, dispecerizează workerii
(prin dispatch_worker.py) și actualizează statusurile.
"""
import sys
import subprocess
import time
from pathlib import Path
from datetime import datetime, timezone

WORKSPACE = Path("/root/omnichannel-monorepo")
AGENTS = WORKSPACE / ".agents"
INTERVAL = 300  # secunde (5 minute)
LOG = AGENTS / "logs" / "orchestrator.log"


def log(msg: str):
    line = f"{datetime.now(timezone.utc).isoformat()} [ORCHESTRATOR] {msg}"
    print(line, flush=True)
    with open(LOG, "a", encoding="utf-8") as f:
        f.write(line + "\n")


def tick():
    log("Tick: rulez Root Planner (run_worker.py root_planner)")
    try:
        subprocess.run(
            [sys.executable, str(AGENTS / "run_worker.py"), "root_planner",
             str(AGENTS / "plans" / "root_planner.prompt")],
            cwd=WORKSPACE,
            timeout=900,
        )
    except subprocess.TimeoutExpired:
        log("Tick expirat (900s)")
    except Exception as e:
        log(f"Eroare tick: {e}")


def main():
    log(f"Orchestrator pornit. Interval {INTERVAL}s. Primul tick imediat.")
    while True:
        try:
            tick()
        except Exception as e:
            log(f"Eroare buclă: {e}")
        time.sleep(INTERVAL)


if __name__ == "__main__":
    main()
