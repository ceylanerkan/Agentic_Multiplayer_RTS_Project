"""Evaluation runner. Mock mode (default) uses a keyword-based fake LLM, so it runs offline and in CI.
Real mode (--real) uses Gemini and needs GEMINI_API_KEY.

Metrics: tool-call accuracy (all expected tools called, no tool outside the allowed set), valid-plan rate, latency,
invalid-action rate (actions that the Python copy of GameActionValidator rejects on the fixture state) and the confidence
of stale-state answers. Outputs: results.json, results.csv, results_table.md.
Run: python evaluation/run_eval.py [--real]
"""
import argparse
import csv
import json
import re
import sys
import tempfile
import time
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(Path(__file__).resolve().parent))
for p in (ROOT / "ai", ROOT / "ai" / "mcp-server", ROOT / "ai" / "rag"):
    sys.path.insert(0, str(p))

from common.session_store import SessionStore  # noqa: E402
from gateway.agent import Agent, AgentError  # noqa: E402
from gateway.llm import LLM, LLMReply  # noqa: E402
from gateway.logs import JsonlLog  # noqa: E402
import plan_checks  # noqa: E402
from common.env import load_env  # noqa: E402
from retriever import Retriever  # noqa: E402
from server import build_server  # noqa: E402

HERE = Path(__file__).parent


class MockLLM(LLM):
    """Picks tools from keywords in the question, then answers with a read-only plan."""

    def step(self, system, history, tools):
        q = history[0]["text"].lower()
        if len(history) == 1:
            calls = []
            if any(w in q for w in ("gold do i", "how much", "overview")): calls.append("get_player_resources")
            if any(w in q for w in ("army", "overview")): calls.append("get_army_status")
            if any(w in q for w in ("enemy", "enemies", "see", "overview")): calls.append("get_visible_enemies")
            if "recently" in q: calls.append("get_recent_events")
            if "researched" in q: calls.append("get_technology_status")
            if any(w in q for w in ("what does", "counter", "response", "fletching", "can ships")): calls.append("search_game_knowledge")
            if any(w in q for w in ("afford", "cost", "response", "what should i do")): calls.append("estimate_plan_cost")
            if "what should i do" in q: calls.append("search_game_knowledge")
            if "my data may be out of date" in q: calls.append("get_player_resources")
            if re.search(r"\btrain \d+", q): calls.append("estimate_plan_cost")
            if "my archers" in q: calls.append("get_army_status")
            if "research" in q: calls.append("get_technology_status")
            args = {"get_recent_events": {}, "search_game_knowledge": {"query": history[0]["text"][:150]},
                    "estimate_plan_cost": {"items": [{"kind": "unit", "id": "spearman", "count": 6}]}}
            return LLMReply(tool_calls=[{"name": c, "args": args.get(c, {})} for c in dict.fromkeys(calls)])
        stale = "snapshot is stale" in history[0]["text"]
        return LLMReply(text=json.dumps({"summary": "Mock answer based on tool results.", "confidence": 0.3 if stale else 0.6,
                                         "evidence": [h["name"] for h in history if h["role"] == "tool"],
                                         "actions": self.actions(q)}))

    @staticmethod
    def actions(q):
        # The mock proposes whatever the player literally asked for, so the validator can be measured on it.
        m = re.search(r"train (\d+) (\w+?)s?\b", q)
        if m:
            return [{"action_type": "TRAIN_UNIT", "unit_type": {"archer": "archer", "spearmen": "spearman", "spearman": "spearman"}.get(m.group(2), m.group(2)),
                     "count": int(m.group(1)), "reason": "requested by the player"}]
        if "opensea" in q:
            return [{"action_type": "MOVE_GROUP", "unit_group": "archer", "target_zone": "OpenSea", "reason": "requested by the player"}]
        if "research fletching" in q:
            return [{"action_type": "RESEARCH", "tech_type": "fletching", "reason": "requested by the player"}]
        return []


CSV_COLUMNS = ["id", "category", "tool_ok", "valid_plan", "latency_s", "actions", "invalid_actions", "invalid_reasons", "confidence", "used", "missing"]


def write_csv(path, rows):
    with open(path, "w", newline="", encoding="utf-8") as f:
        w = csv.DictWriter(f, fieldnames=CSV_COLUMNS, extrasaction="ignore")
        w.writeheader()
        for r in rows:
            w.writerow(dict(r, used=" ".join(r["used"]), missing=" ".join(r["missing"])))


def markdown_table(summary):
    """Summary table for the report (docs/report/report.md, Evaluation)."""
    rows = summary["rows"]
    lines = ["| Metric | Value |", "|---|---|", f"| Mode | {summary['mode']} |", f"| Prompts | {summary['prompts']} |",
             f"| Tool-call accuracy | {summary['tool_accuracy']:.0%} |", f"| Valid-plan rate | {summary['valid_plan_rate']:.0%} |",
             f"| Average latency (s) | {summary['avg_latency_s']} |", f"| Actions proposed | {summary['actions_proposed']} |"]
    rate = summary["invalid_action_rate"]
    lines.append(f"| Invalid-action rate | {'n/a' if rate is None else format(rate, '.0%')} |")
    st = summary["stale_low_confidence_rate"]
    lines.append(f"| Stale answers with confidence <= 0.5 | {'n/a' if st is None else format(st, '.0%')} |")
    lines += ["", "| Category | Prompts | Tool accuracy |", "|---|---|---|"]
    for cat in sorted({r["category"] for r in rows}):
        sel = [r for r in rows if r["category"] == cat]
        lines.append(f"| {cat} | {len(sel)} | {sum(r['tool_ok'] for r in sel) / len(sel):.0%} |")
    return "\n".join(lines) + "\n"


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--real", action="store_true")
    ap.add_argument("--out", default=str(HERE / "results.json"))
    args = ap.parse_args()
    load_env()  # GEMINI_API_KEY from .env for --real
    if args.real:
        import os
        from gateway.llm import GeminiLLM
        llm = GeminiLLM(os.environ["GEMINI_API_KEY"], os.environ.get("GEMINI_MODEL", "gemini-2.5-flash"))
    else:
        llm = MockLLM()
    retriever, rows = Retriever(), []
    log = JsonlLog(Path(tempfile.mkdtemp()) / "tools.jsonl")
    for line in (HERE / "prompts.jsonl").read_text().splitlines():
        p = json.loads(line)
        store = SessionStore(":memory:")
        sid = store.create_session()
        store.save_snapshot(sid, json.loads((HERE / "fixtures" / f"{p['fixture']}.json").read_text()))
        agent = Agent(llm, build_server(store, sid, retriever), log)
        t0, ok_plan, used, plan = time.time(), True, [], {"actions": [], "confidence": None}
        stale_note = "\n\nWARNING: the game snapshot is stale; lower your confidence." if p.get("stale") else ""  # same text as the gateway
        try:
            plan, used = agent.run("eval", p["question"], stale_note)
        except AgentError:
            ok_plan = False
        state = json.loads((HERE / "fixtures" / f"{p['fixture']}.json").read_text())
        reasons = plan_checks.validate_plan(plan, state)
        missing = [t for t in p["expected_tools"] if t not in used]
        rows.append({"id": p["id"], "category": p["category"], "used": used, "missing": missing,
                     "valid_plan": ok_plan, "latency_s": round(time.time() - t0, 3),
                     "actions": len(reasons), "invalid_actions": sum(r is not None for r in reasons),
                     "invalid_reasons": "; ".join(r for r in reasons if r), "confidence": plan.get("confidence"),
                     "stale": bool(p.get("stale")), "expect_valid": bool(p.get("expect_valid")),
                     "tool_ok": not missing and (p["category"] != "knowledge" or set(used) == {"search_game_knowledge"})})
    total_actions = sum(r["actions"] for r in rows)
    stale_rows = [r for r in rows if r["stale"] and r["confidence"] is not None]
    summary = {"mode": "real" if args.real else "mock", "prompts": len(rows),
               "actions_proposed": total_actions,
               "invalid_action_rate": round(sum(r["invalid_actions"] for r in rows) / total_actions, 3) if total_actions else None,
               "stale_low_confidence_rate": round(sum(r["confidence"] <= 0.5 for r in stale_rows) / len(stale_rows), 3) if stale_rows else None,
               "tool_accuracy": round(sum(r["tool_ok"] for r in rows) / len(rows), 3),
               "valid_plan_rate": round(sum(r["valid_plan"] for r in rows) / len(rows), 3),
               "avg_latency_s": round(sum(r["latency_s"] for r in rows) / len(rows), 3), "rows": rows}
    out = Path(args.out)
    out.write_text(json.dumps(summary, indent=1))
    write_csv(out.with_suffix(".csv"), rows)
    out.with_name(out.stem + "_table.md").write_text(markdown_table(summary), encoding="utf-8")
    print({k: v for k, v in summary.items() if k != "rows"})
    for r in rows:
        if not r["tool_ok"] or not r["valid_plan"]:
            print("FAIL", r["id"], "missing", r["missing"], "used", r["used"])


if __name__ == "__main__":
    main()
