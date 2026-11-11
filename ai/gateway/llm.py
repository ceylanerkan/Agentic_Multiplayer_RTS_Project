"""Provider-neutral LLM interface. The agent only knows `LLM.step(...)`."""
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
