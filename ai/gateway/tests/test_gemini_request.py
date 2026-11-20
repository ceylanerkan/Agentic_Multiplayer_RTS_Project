"""GeminiLLM builds a real google-genai request; only the network call is faked (no API key, no traffic).

The live run is the first time this class meets Gemini, so wrong type names or arguments should fail here instead.
"""
from types import SimpleNamespace

import pytest

genai_types = pytest.importorskip("google.genai.types")

from gateway.llm import GeminiLLM  # noqa: E402

TOOL = {"name": "get_player_resources", "description": "Own resources.",
        "schema": {"type": "object", "properties": {}, "additionalProperties": False}}


class FakeModels:
    def __init__(self, response):
        self.response, self.kwargs = response, None

    def generate_content(self, **kwargs):
        self.kwargs = kwargs
        return self.response


def make_llm(response):
    llm = GeminiLLM.__new__(GeminiLLM)          # skip __init__: it would create a real client
    llm.client = SimpleNamespace(models=FakeModels(response))
    llm.model = "gemini-test"
    return llm


HISTORY = [
    {"role": "user", "text": "How much wood do I have?"},
    {"role": "model", "text": None, "calls": [{"name": "get_player_resources", "args": {}}]},
    {"role": "tool", "name": "get_player_resources", "result": {"wood": 200}},
]


def test_request_has_system_prompt_tools_and_all_history_roles():
    llm = make_llm(SimpleNamespace(function_calls=None, text='{"answer": "200 wood"}'))
    reply = llm.step("system prompt", HISTORY, [TOOL])
    sent = llm.client.models.kwargs
    assert sent["model"] == "gemini-test"
    assert [c.role for c in sent["contents"]] == ["user", "model", "user"]
    assert sent["contents"][1].parts[0].function_call.name == "get_player_resources"
    assert sent["contents"][2].parts[0].function_response.response == {"result": {"wood": 200}}
    cfg = sent["config"]
    assert cfg.system_instruction == "system prompt"
    assert cfg.tools[0].function_declarations[0].name == "get_player_resources"
    assert reply.text == '{"answer": "200 wood"}' and reply.tool_calls == []


def test_function_calls_become_tool_calls_and_text_is_dropped():
    call = SimpleNamespace(name="get_player_resources", args=None)   # Gemini may send args=None
    llm = make_llm(SimpleNamespace(function_calls=[call], text=None))
    reply = llm.step("s", HISTORY[:1], [TOOL])
    assert reply.text is None
    assert reply.tool_calls == [{"name": "get_player_resources", "args": {}}]
