"""Evaluation runner. Mock mode (default) uses a keyword-based fake LLM, so it runs offline and in CI.
Real mode (--real) uses Gemini and needs GEMINI_API_KEY.

Metrics: tool-call accuracy (all expected tools called, no tool outside the allowed set),
valid-plan rate, latency. Run: python evaluation/run_eval.py [--real]
"""
import argparse
import json
import sys
import tempfile
import time
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
for p in (ROOT / "ai", ROOT / "ai" / "mcp-server", ROOT / "ai" / "rag"):
    sys.path.insert(0, str(p))

from common.session_store import SessionStore  # noqa: E402
from gateway.agent import Agent, AgentError  # noqa: E402
from gateway.llm import LLM, LLMReply  # noqa: E402
from gateway.logs import JsonlLog  # noqa: E402
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
            args = {"get_recent_events": {}, "search_game_knowledge": {"query": history[0]["text"][:150]},
                    "estimate_plan_cost": {"items": [{"kind": "unit", "id": "spearman", "count": 6}]}}
            return LLMReply(tool_calls=[{"name": c, "args": args.get(c, {})} for c in dict.fromkeys(calls)])
        return LLMReply(text=json.dumps({"summary": "Mock answer based on tool results.", "confidence": 0.5,
                                         "evidence": [h["name"] for h in history if h["role"] == "tool"], "actions": []}))


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--real", action="store_true")
    ap.add_argument("--out", default=str(HERE / "results.json"))
    args = ap.parse_args()
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
        t0, ok_plan, used = time.time(), True, []
        try:
            _, used = agent.run("eval", p["question"])
        except AgentError:
            ok_plan = False
        missing = [t for t in p["expected_tools"] if t not in used]
        rows.append({"id": p["id"], "category": p["category"], "used": used, "missing": missing,
                     "valid_plan": ok_plan, "latency_s": round(time.time() - t0, 3),
                     "tool_ok": not missing and (p["category"] != "knowledge" or set(used) == {"search_game_knowledge"})})
    summary = {"mode": "real" if args.real else "mock", "prompts": len(rows),
               "tool_accuracy": round(sum(r["tool_ok"] for r in rows) / len(rows), 3),
               "valid_plan_rate": round(sum(r["valid_plan"] for r in rows) / len(rows), 3),
               "avg_latency_s": round(sum(r["latency_s"] for r in rows) / len(rows), 3), "rows": rows}
    Path(args.out).write_text(json.dumps(summary, indent=1))
    print({k: v for k, v in summary.items() if k != "rows"})
    for r in rows:
        if not r["tool_ok"] or not r["valid_plan"]:
            print("FAIL", r["id"], "missing", r["missing"], "used", r["used"])


if __name__ == "__main__":
    main()
