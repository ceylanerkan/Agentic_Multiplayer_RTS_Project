"""Provider-neutral LLM interface. The agent only knows `LLM.step(...)`.

LLM_PROVIDER in .env picks the model: "ollama" (default: local model, no key) or "gemini" (needs GEMINI_API_KEY).
"""
import json
import os
import urllib.request
from dataclasses import dataclass, field


@dataclass
class LLMReply:
    text: str | None = None                                  # final answer (JSON text) when no tool calls
    tool_calls: list[dict] = field(default_factory=list)     # [{"name": ..., "args": {...}}]


class LLM:
    def step(self, system, history, tools):
        """history: list of {"role": "user"|"model"|"tool", ...}. Returns LLMReply."""
        raise NotImplementedError


class ScriptedLLM(LLM):
    """Deterministic fake for tests and the mock evaluation mode: replays prepared replies."""

    def __init__(self, replies):
        self.replies, self.calls = list(replies), 0

    def step(self, system, history, tools):
        self.calls += 1
        return self.replies.pop(0) if self.replies else LLMReply(text="{}")


class GeminiLLM(LLM):
    """Gemini via google-genai function calling. Not exercised in tests (no API key in CI)."""

    def __init__(self, api_key, model):
        from google import genai
        self.client, self.model = genai.Client(api_key=api_key), model

    def step(self, system, history, tools):
        from google.genai import types
        decls = [types.FunctionDeclaration(name=t["name"], description=t["description"],
                                           parameters_json_schema=t["schema"]) for t in tools]
        contents = []
        for h in history:
            if h["role"] == "user":
                contents.append(types.Content(role="user", parts=[types.Part(text=h["text"])]))
            elif h["role"] == "model":
                parts = [types.Part.from_function_call(name=c["name"], args=c["args"]) for c in h["calls"]]
                contents.append(types.Content(role="model", parts=parts or [types.Part(text=h["text"])]))
            else:
                contents.append(types.Content(role="user", parts=[types.Part.from_function_response(
                    name=h["name"], response={"result": h["result"]})]))
        resp = self.client.models.generate_content(
            model=self.model, contents=contents,
            config=types.GenerateContentConfig(system_instruction=system,
                                               tools=[types.Tool(function_declarations=decls)]))
        calls = [{"name": c.name, "args": dict(c.args or {})} for c in (resp.function_calls or [])]
        return LLMReply(text=None if calls else resp.text, tool_calls=calls)


# 4096 is enough for a question with all tools (about 2000 tokens) and leaves more of the model on a 6 GB GPU than 8192
OPTIONS = {"temperature": 0, "num_ctx": 4096}
KEEP_ALIVE = "30m"   # the default (5 min) unloads the model between two questions of a match


class OllamaLLM(LLM):
    """Local model through Ollama's /api/chat with tools (https://ollama.com). Standard library only, no extra package."""

    def __init__(self, model, base_url="http://127.0.0.1:11434", timeout_s=120):
        self.model, self.url, self.timeout_s = model, base_url.rstrip("/") + "/api/chat", timeout_s

    def warm_up(self):
        """Loads the model into memory (about 10 s), so the first question of a match does not wait for it."""
        body = {"model": self.model, "messages": [], "keep_alive": KEEP_ALIVE, "options": OPTIONS}
        req = urllib.request.Request(self.url, json.dumps(body).encode(), {"Content-Type": "application/json"})
        urllib.request.urlopen(req, timeout=self.timeout_s).close()

    def step(self, system, history, tools):
        messages = [{"role": "system", "content": system}]
        for h in history:
            if h["role"] == "user":
                messages.append({"role": "user", "content": h["text"]})
            elif h["role"] == "model":
                messages.append({"role": "assistant", "content": h["text"] or "", "tool_calls": [
                    {"function": {"name": c["name"], "arguments": c["args"]}} for c in h["calls"]]})
            else:
                messages.append({"role": "tool", "tool_name": h["name"], "content": json.dumps(h["result"])})
        body = {"model": self.model, "messages": messages, "stream": False,
                "keep_alive": KEEP_ALIVE, "options": OPTIONS}
        if len(history) > 1 and history[-1]["role"] == "user":  # the agent's repair retry: force the plan schema
            from gateway.agent import PLAN_SCHEMA
            body["format"] = PLAN_SCHEMA
        else:
            body["tools"] = [{"type": "function", "function": {
                "name": t["name"], "description": t["description"], "parameters": t["schema"]}} for t in tools]
        req = urllib.request.Request(self.url, json.dumps(body).encode(), {"Content-Type": "application/json"})
        with urllib.request.urlopen(req, timeout=self.timeout_s) as resp:
            msg = json.load(resp)["message"]
        calls = [{"name": c["function"]["name"], "args": dict(c["function"].get("arguments") or {})}
                 for c in msg.get("tool_calls") or []]
        return LLMReply(text=None if calls else msg.get("content"), tool_calls=calls)


def provider_name(provider=None):
    name = (provider or os.environ.get("LLM_PROVIDER") or "ollama").strip().lower()
    if name not in ("gemini", "ollama"):
        raise ValueError(f"LLM_PROVIDER must be gemini or ollama, not {name!r}")
    return name


def agent_timeout_s(provider=None):
    """AGENT_TIMEOUT_S, or 30 s for Gemini and 120 s for a local model."""
    return float(os.environ.get("AGENT_TIMEOUT_S") or (120 if provider_name(provider) == "ollama" else 30))


def make_llm(provider=None):
    if provider_name(provider) == "ollama":
        return OllamaLLM(os.environ.get("OLLAMA_MODEL") or "qwen2.5:7b",
                         os.environ.get("OLLAMA_URL") or "http://127.0.0.1:11434", agent_timeout_s(provider))
    return GeminiLLM(os.environ["GEMINI_API_KEY"], os.environ.get("GEMINI_MODEL") or "gemini-2.5-flash")
