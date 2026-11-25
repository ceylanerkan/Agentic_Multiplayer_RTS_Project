"""Optional tool pre-selection with Laya (TOOL_ROUTER=laya in .env; off by default).

Laya (https://huggingface.co/convaiinnovations/laya, Apache 2.0) writes no text: it is an encoder that answers typed questions.
Here it answers one yes/no question per MCP tool ("is this tool needed for the player's question?") in one forward pass on the CPU,
so the graphics card stays free for qwen. The agent then calls the chosen tools through MCP before the first qwen step. qwen still sees
every tool and may call more, so the tool order is not fixed. Any problem (package not installed, checkpoint missing, ...) is logged
and the question takes the normal path without pre-selection.

The checkpoint is the fine-tuned one in ai/models/laya-rts when it exists (evaluation/tool_router/train_router.py writes it), otherwise
the published one, pinned to a revision. Nothing is downloaded at run time.
"""
import os
import threading
import time
from pathlib import Path

MODEL_ID = "convaiinnovations/laya"
REVISION = "55cf4c4ebb4ebe31b2550e8bdf3bd21b99753851"
TUNED = Path(__file__).resolve().parents[1] / "models" / "laya-rts"

# qwen has to write the arguments of these tools, so they are only announced to it, not called in advance
NEEDS_MODEL_ARGS = {"estimate_plan_cost"}


def _needs(what, yes, no):
    return {"type": "noul", "instructions": f"The text is a question or an order of a player in a strategy game. Is {what} needed to answer it?",
            "criteria": {"true": yes, "false": no}}


# The training (evaluation/tool_router) uses exactly these texts: changing one means training again.
TOOL_QUESTIONS = {
    "get_player_resources": _needs("the player's own wood, food, gold, population or age",
                                   "yes, the own stockpile, population limit or age matters", "no, own resources do not matter"),
    "get_army_status": _needs("the list of the player's own units and buildings",
                              "yes, own units, ships or buildings matter", "no, own units and buildings do not matter"),
    "get_visible_enemies": _needs("the list of enemy units the player can see",
                                  "yes, visible enemies, threats or a fight matter", "no, enemies do not matter"),
    "get_technology_status": _needs("the list of researched and running technologies of the player",
                                    "yes, what is already researched matters", "no, the research status does not matter"),
    "get_recent_events": _needs("the log of what happened in the last minutes",
                                "yes, it asks about recent events", "no, recent events do not matter"),
    "estimate_plan_cost": _needs("a price calculation for units, buildings or technologies",
                                 "yes, something is bought, trained, built, researched or priced", "no, nothing is bought or priced"),
    "search_game_knowledge": _needs("the rule book with facts about units, buildings, technologies, counters and strategy",
                                    "yes, a rule, a fact, a counter or strategy advice is needed", "no, the rule book is not needed"),
}


def prefetch_args(tool, question):
    return {"query": question[:200]} if tool == "search_game_knowledge" else {}


def local_checkpoint(model=None):
    """Folder of the checkpoint: the argument or LAYA_MODEL, else the fine-tuned one, else the pinned published one (from the cache)."""
    folder = model or os.environ.get("LAYA_MODEL")
    if folder and folder != "base":
        return str(folder)
    if not folder and (TUNED / "model.safetensors").exists():
        return str(TUNED)
    from huggingface_hub import snapshot_download
    return snapshot_download(MODEL_ID, revision=REVISION, local_files_only=True)


class LayaRouter:
    def __init__(self, log=None, model=None, threshold=None):
        self.log, self.model = log, model
        self.threshold = float(threshold if threshold is not None else os.environ.get("LAYA_THRESHOLD") or 0.5)
        self._agent, self._lock = None, threading.Lock()

    def load(self):
        with self._lock:   # the warm-up thread and a first question must not load it twice
            if self._agent is None:
                import laya
                self._agent = laya.Agent(local_checkpoint(self.model), device="cpu")
        return self._agent

    def scores(self, question):
        """{tool: probability that it is needed}"""
        answers = self.load().predict(question, TOOL_QUESTIONS)["answers"]
        return {tool: answers[tool]["noul"] for tool in TOOL_QUESTIONS}

    def warm_up(self):
        self.scores("How much wood do I have?")

    def select(self, session, question):
        """Tools needed for the question, in the order of TOOL_QUESTIONS; [] when Laya does not work."""
        start = time.time()
        scores, selected, error = {}, [], None
        try:
            scores = self.scores(question)
            selected = [t for t, p in scores.items() if p >= self.threshold]
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
