"""FastAPI gateway. Unity holds the session token; the LLM never sees it or any player id.

Run: uvicorn ai.gateway.app:app --port 8765   (needs GEMINI_API_KEY; tests inject a ScriptedLLM instead)
"""
import os
import re
import sys
from pathlib import Path

import jsonschema
from fastapi import FastAPI, Header, HTTPException
from pydantic import BaseModel

AI = Path(__file__).resolve().parents[1]
for p in (AI, AI / "mcp-server", AI / "rag"):
    sys.path.insert(0, str(p))

from common.session_store import SessionStore  # noqa: E402
from gateway.agent import SNAPSHOT_SCHEMA, Agent, AgentError  # noqa: E402
from gateway.logs import JsonlLog, short_id  # noqa: E402
from retriever import Retriever  # noqa: E402
from server import build_server  # noqa: E402

# Cheap pattern check: it only *logs* suspected attacks; the real protection is that no tool exposes hidden data.
INJECTION = re.compile(r"ignore (all|previous|the)? ?(rules|instructions)|reveal|hidden|system prompt|"
                       r"bypass|without approval|jailbreak|developer mode", re.I)
MAX_QUESTION_LEN = 500


class Ask(BaseModel):
    snapshot: dict
    question: str


class Why(BaseModel):
    question: str = "Why did you propose this plan?"


def create_app(store=None, llm=None, log_dir=None, snapshot_max_age_s=None):
    store = store or SessionStore(os.environ.get("SESSION_DB_PATH", "ai/data/sessions.sqlite3"))
    log_dir = Path(log_dir or os.environ.get("LOG_DIR", "ai/logs"))
    tool_log, sec_log = JsonlLog(log_dir / "tool_calls.jsonl"), JsonlLog(log_dir / "security.jsonl")
    max_age = snapshot_max_age_s or float(os.environ.get("SNAPSHOT_MAX_AGE_S", 30))
    retriever = Retriever()
    app = FastAPI(title="RTS AI gateway")
    last_plan = {}  # session -> (question, plan) for "ask why"

    def get_llm():
        if llm:
            return llm
        from gateway.llm import GeminiLLM
        return GeminiLLM(os.environ["GEMINI_API_KEY"], os.environ.get("GEMINI_MODEL", "gemini-2.5-flash"))

    def auth(token):
        if not token or not store.session_exists(token):
            raise HTTPException(401, "unknown session token")
        return token

    def answer(token, question, note=""):
        if len(question) > MAX_QUESTION_LEN:
            sec_log.write(session=short_id(token), event="rejected_input", reason="question too long")
            raise HTTPException(422, "question too long")
        if INJECTION.search(question):
            sec_log.write(session=short_id(token), event="suspected_injection", question=question[:200])
        agent = Agent(get_llm(), build_server(store, token, retriever), tool_log,
                      int(os.environ.get("AGENT_MAX_STEPS", 6)), float(os.environ.get("AGENT_TIMEOUT_S", 30)))
        try:
            plan, used = agent.run(short_id(token), question, note)
        except AgentError as e:
            sec_log.write(session=short_id(token), event="invalid_plan_or_agent_failure", detail=str(e)[:200])
            raise HTTPException(502, f"AI could not produce a valid plan: {e}")
        last_plan[token] = (question, plan)
        return {"plan": plan, "tools_used": used, "needs_approval": bool(plan["actions"])}

    @app.post("/session")
    def create_session():
        return {"token": store.create_session()}

    @app.post("/ask")
    def ask(body: Ask, x_session_token: str = Header(None)):
        token = auth(x_session_token)
        try:
            jsonschema.Draft202012Validator(SNAPSHOT_SCHEMA).validate(body.snapshot)  # unknown fields rejected
        except jsonschema.ValidationError as e:
            sec_log.write(session=short_id(token), event="rejected_snapshot", detail=e.message[:200])
            raise HTTPException(422, f"invalid snapshot: {e.message[:200]}")
        store.save_snapshot(token, body.snapshot)
        return answer(token, body.question)

    @app.post("/why")
    def why(body: Why, x_session_token: str = Header(None)):
        token = auth(x_session_token)
        if token not in last_plan:
            raise HTTPException(409, "no previous plan in this session")
        q, plan = last_plan[token]
        row = store.get_snapshot(token)
        stale = row is None or row[1] > max_age  # "ask why" reuses the old snapshot: tell the model if it is old
        extra = "\n\nWARNING: the game snapshot is stale; lower your confidence." if stale else ""
        return answer(token, body.question, f"\n\nEarlier question: {q}\nEarlier summary: {plan['summary']}" + extra)

    return app


app = None  # use create_app() (uvicorn --factory ai.gateway.app:create_app)
