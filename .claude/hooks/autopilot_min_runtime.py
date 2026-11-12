"""
Stop hook for the nightly autopilot routine. It runs only in cloud sessions (see .claude/settings.json).

docs/autopilot.md tells the agent to keep working for about 50 minutes, but the model sometimes ends its run
after a few minutes. When the autopilot agent tries to finish too early, this hook blocks the stop and sends it
back to the nightly plan. In every other session it does nothing.
"""
import json
import os
import re
import sys
import tempfile
import time
from datetime import datetime, timezone

MIN_MINUTES = 50                  # docs/autopilot.md: keep working until about 50 minutes
MARKER = "daily autopilot agent"  # text from the routine's prompt; identifies an autopilot run
MAX_BLOCKS = 25                   # safety net: never block more often than this in one run
QUICK_STOP_SECONDS = 90           # a stop this soon after the previous block means no real work was done
MAX_QUICK_STOPS = 3               # after this many quick stops in a row the agent is stuck, so let it end

REASON = (
    "Autopilot minimum run time: this run started at {start} UTC and only {done} of the required {need} minutes "
    "have passed ({left} minutes left). Do not end the run yet (docs/autopilot.md section 2.3). Continue with the "
    "earliest unchecked item in docs/nightly-plan.md, even if its night is in the future; if every night is done, "
    "use the plan's 'When the plan is finished early' list. Work in finished, tested chunks. After each chunk update "
    "docs/nightly-plan.md, TODO.md, docs/progress-log.md and MORNING.md, then commit and push. Do not start a new "
    "chunk after about 50 minutes."
)


def run_start(transcript_path):
    """Unix time of the routine's prompt, or None when this session is not an autopilot run."""
    try:
        with open(transcript_path, encoding="utf-8") as f:
            for i, line in enumerate(f):
                if i > 300:
                    break
                try:
                    entry = json.loads(line)
                except ValueError:
                    continue
                if entry.get("type") == "user" and MARKER in json.dumps(entry.get("message", "")):
                    stamp = entry.get("timestamp")
                    if not stamp:
                        return None
                    return datetime.fromisoformat(stamp.replace("Z", "+00:00")).timestamp()
    except OSError:
        pass
    return None


def load_state(path):
    try:
        with open(path, encoding="utf-8") as f:
            return json.load(f)
    except (OSError, ValueError):
        return {"blocks": 0, "quick": 0, "last": 0}


def main():
    data = json.load(sys.stdin)
    start = run_start(data.get("transcript_path") or "")
    if start is None:
        return  # not the autopilot routine

    now = time.time()
    minutes = (now - start) / 60
    if minutes >= MIN_MINUTES:
        return  # the hour is (nearly) over: let it finish

    # Count the blocks of this run, so that a stuck agent cannot be sent back forever.
    session = re.sub(r"[^\w-]", "_", str(data.get("session_id") or "unknown"))
    state_path = os.path.join(tempfile.gettempdir(), "autopilot-stop-" + session + ".json")
    state = load_state(state_path)
    state["quick"] = state["quick"] + 1 if now - state["last"] < QUICK_STOP_SECONDS else 0
    if state["blocks"] >= MAX_BLOCKS or state["quick"] >= MAX_QUICK_STOPS:
        return
    state["blocks"] += 1
    state["last"] = now
    with open(state_path, "w", encoding="utf-8") as f:
        json.dump(state, f)

    reason = REASON.format(
        start=datetime.fromtimestamp(start, timezone.utc).strftime("%H:%M"),
        done=int(minutes),
        need=MIN_MINUTES,
        left=MIN_MINUTES - int(minutes),
    )
    print(json.dumps({"decision": "block", "reason": reason}))


if __name__ == "__main__":
    try:
        main()
    except Exception as e:  # a broken hook must never break or trap the run
        print("autopilot_min_runtime hook error: " + repr(e), file=sys.stderr)
