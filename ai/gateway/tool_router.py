"""Optional tool pre-selection with Laya (TOOL_ROUTER=laya in .env; off by default).

Laya (https://huggingface.co/convaiinnovations/laya, Apache 2.0) is a decision model: an encoder that reads a typed question with its
options and writes no text. Here it reads "which information is needed for the player's question?" (one option per MCP tool) in one pass
on the CPU, so the graphics card stays free for qwen. A small classifier per tool (laya_tool_weights.npz, fitted by
evaluation/tool_router/train_router.py) turns Laya's reading into "needed or not". Laya itself is not changed.

The agent calls the chosen tools through MCP before the first qwen step. qwen still sees every tool and may call more, so the tool order
is not fixed. Any problem (package not installed, checkpoint missing, ...) is logged and the question takes the normal path.
The checkpoint is pinned to a revision and nothing is downloaded at run time.
"""
import os
import threading
import time
from pathlib import Path

MODEL_ID = "convaiinnovations/laya"
REVISION = "55cf4c4ebb4ebe31b2550e8bdf3bd21b99753851"
FILES = ["rl_agent_config.json", "model.safetensors", "tokenizer/*", "encoder/*"]   # the repository holds other models too
WEIGHTS = Path(__file__).with_name("laya_tool_weights.npz")

# qwen has to write the arguments of these tools, so they are only announced to it, not called in advance
NEEDS_MODEL_ARGS = {"estimate_plan_cost"}

# The classifier is fitted on exactly these texts: changing one means running train_router.py again.
OPTIONS = {   # option label: (tool, description)
    "own resources": ("get_player_resources", "own wood, food, gold, population or age"),
    "own army": ("get_army_status", "own units, ships and buildings, orders to them"),
    "visible enemies": ("get_visible_enemies", "enemy units in sight, threats, attack, defence"),
    "technology status": ("get_technology_status", "which research is done or running"),
    "recent events": ("get_recent_events", "what happened in the last minutes"),
    "cost calculation": ("estimate_plan_cost", "price, affording, training, building or researching something"),
    "rule book": ("search_game_knowledge", "facts and rules about units, buildings, upgrades, counters, strategy"),
    "nothing": (None, "small talk, forbidden or unrelated request"),
}
TOOLS = [tool for tool, _ in OPTIONS.values() if tool]
QUESTION = {"tools": {"type": "choice", "instructions": "Which information is needed to answer the player of a strategy game?",
                      "criteria": {label: text for label, (_, text) in OPTIONS.items()}}}


def prefetch_args(tool, question, results):
    """Arguments of a tool that is called in advance. With enemies in sight the knowledge search also asks what counters them."""
    if tool != "search_game_knowledge":
        return {}
    enemies = sorted({e["type"] for e in results.get("get_visible_enemies", {}).get("visible_enemies", [])})
    return {"query": ((f"defend against {' '.join(enemies)}: what counters {' '.join(enemies)}? " if enemies else "") + question)[:200]}


def checkpoint():
    from huggingface_hub import snapshot_download
    return snapshot_download(MODEL_ID, revision=REVISION, local_files_only=True, allow_patterns=FILES)


class LayaReader:
    """Laya's reading of a question: one vector per tool option, their mean, and the mean over the words of the question."""

    def __init__(self):
        import laya
        self.agent, self.seen, self.lock = laya.Agent(checkpoint(), device="cpu"), {}, threading.Lock()
        model = self.agent.model
        model.register_forward_pre_hook(self._markers, with_kwargs=True)
        model.encoder.register_forward_hook(lambda module, args, out: self.seen.update(hidden=out.last_hidden_state[0].detach()))

    def _markers(self, module, args, kwargs):
        positions = kwargs["marker_pos"] if "marker_pos" in kwargs else args[2]
        self.seen["markers"] = positions[0].tolist()

    def features(self, question):
        """Array (3, tools, d)."""
        import numpy as np
        with self.lock:
            words = self.agent.predict(question, QUESTION)["usage"]["state_tokens"]
            hidden, markers = self.seen["hidden"], self.seen["markers"]
        options, n = hidden[markers], hidden.shape[0]   # the sequence ends with the question and one separator
        each = options[:len(TOOLS)].numpy()
        return np.stack([each, np.repeat(options.mean(0, keepdim=True).numpy(), len(TOOLS), 0),
                         np.repeat(hidden[n - 1 - words:n - 1].mean(0, keepdim=True).numpy(), len(TOOLS), 0)])


class LayaRouter:
    def __init__(self, log=None, threshold=None):
        self.log = log
        self.threshold = float(threshold if threshold is not None else os.environ.get("LAYA_THRESHOLD") or 0.4)
        self._reader, self._lock = None, threading.Lock()

    def load(self):
        with self._lock:   # the warm-up thread and a first question must not load it twice
            if self._reader is None:
                import numpy as np
                data = np.load(WEIGHTS)
                self._w, self._b = data["w"], data["b"]   # (3, tools, d) and (3, tools)
                self._reader = LayaReader()
        return self._reader

    def scores(self, question):
        """{tool: probability that it is needed}"""
        import numpy as np
        f = self.load().features(question)
        p = (1 / (1 + np.exp(-((f * self._w).sum(-1) + self._b)))).mean(0)
        return {tool: round(float(x), 3) for tool, x in zip(TOOLS, p)}

    def warm_up(self):
        self.scores("How much wood do I have?")

    def select(self, session, question):
        """Tools needed for the question, in the order of OPTIONS; [] when Laya does not work."""
        start = time.time()
        scores, selected, error = {}, [], None
        try:
            scores = self.scores(question)
            selected = [t for t in TOOLS if scores[t] >= self.threshold]
        except Exception as e:
            error = f"{type(e).__name__}: {str(e)[:200]}"
            print(f"AI gateway: Laya failed, going on without it ({error})", flush=True)
        if self.log is not None:
            self.log.write(session=session, scores=scores, selected=selected, duration_ms=round((time.time() - start) * 1000), error=error)
        return selected


def make_router(log=None):
    """The router of TOOL_ROUTER (off | laya), None when it is off."""
    name = (os.environ.get("TOOL_ROUTER") or "off").strip().lower()
    if name == "off":
        return None
    if name != "laya":
        raise ValueError(f"TOOL_ROUTER must be off or laya, not {name!r}")
    return LayaRouter(log)
