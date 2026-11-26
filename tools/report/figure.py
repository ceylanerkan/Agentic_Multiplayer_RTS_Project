"""Fig. 1 of the report: the architecture, one IEEE column wide (3.45 in)."""
import sys
import matplotlib
matplotlib.use("Agg")
import matplotlib.pyplot as plt
from matplotlib.patches import FancyBboxPatch

plt.rcParams.update({"font.family": "Times New Roman", "font.size": 7})
W, H = 3.45, 4.15
fig = plt.figure(figsize=(W, H), dpi=300)
ax = fig.add_axes([0, 0, 1, 1])
ax.set_xlim(0, 100)
ax.set_ylim(0, 120)
ax.axis("off")

INK, UNITY, PY, LLM = "#222222", "#E8EEF7", "#EEF5EA", "#F7EFE3"


def box(x, y, w, h, color, title=None, lines=(), bold=False, size=7):
    ax.add_patch(FancyBboxPatch((x, y), w, h, boxstyle="round,pad=0.4,rounding_size=1.5", fc=color, ec=INK, lw=0.6))
    ty = y + h - 3.2 if title else y + h / 2 + (len(lines) - 1) * 2.0
    if title:
        ax.text(x + w / 2, ty, title, ha="center", va="center", fontsize=size, fontweight="bold")
        ty -= 4.6
    for ln in lines:
        ax.text(x + w / 2, ty, ln, ha="center", va="center", fontsize=6.3)
        ty -= 4.0


def arrow(x1, y1, x2, y2, label=None, lx=None, ly=None, both=False, ha="left"):
    ax.annotate("", xy=(x2, y2), xytext=(x1, y1),
                arrowprops=dict(arrowstyle="<|-|>" if both else "-|>", lw=0.6, color=INK, mutation_scale=6, shrinkA=0, shrinkB=0))
    if label:
        ax.text(lx if lx is not None else (x1 + x2) / 2 + 1.5, ly if ly is not None else (y1 + y2) / 2, label,
                ha=ha, va="center", fontsize=6, style="italic")


# Unity (top)
box(2, 80, 96, 38, UNITY, "Unity 6 game (host or client, Photon Fusion 2)")
box(5, 84, 43, 27, "white", None, ["AiSnapshotBuilder +", "VisibilityService.FilterFor", "→ AiSafeSnapshot", "(no hidden data)"])
box(52, 98, 43, 13, "white", None, ["Approve → GameActionValidator", "(current state) → CommandRelay RPC", "→ host validates again"])
box(52, 84, 43, 10, "white", None, ["AiCommanderPanel: plan, evidence,", "Approve / Reject / Ask why"])
arrow(73.5, 94.6, 73.5, 97.6)

# Gateway (middle)
box(2, 40, 60, 30, PY, "AI gateway (Python, FastAPI)")
box(5, 54, 54, 9.5, "white", None, ["snapshot schema check, session token,", "SQLite store, injection log"])
box(5, 43, 54, 9.5, "white", None, ["agent loop (max. 6 steps, 120 s) → RTSActionPlan;", "checks: schema, game rules, counters"])
arrow(20, 83.6, 20, 70.4, "POST /ask: snapshot + question", lx=21.5, ly=76.5)
arrow(57, 70.4, 57, 83.6, "plan (JSON)", lx=58.5, ly=76.5)

# LLM (right)
box(68, 42, 30, 12, LLM, "Ollama (local GPU)", ["qwen2.5:7b, temp. 0"])
arrow(59.4, 47.7, 67.6, 47.7, None, both=True)
ax.text(63.5, 50, "tools", ha="center", va="center", fontsize=6, style="italic")

# MCP (bottom)
box(2, 2, 96, 26, PY, "MCP server (stdio): 7 read-only tools, no player or session argument")
box(5, 4.5, 42, 14.5, "white", None, ["resources, army, visible enemies,", "technologies, recent events,", "plan cost (from the snapshot)"])
box(53, 4.5, 42, 14.5, "white", None, ["search_game_knowledge: RAG,", "BM25 + MiniLM, 36 chunks", "with source ids"])
arrow(32, 42.6, 32, 28.4, "MCP over stdio", lx=33.5, ly=35)

out = sys.argv[1] if len(sys.argv) > 1 else "fig1.png"
fig.savefig(out, dpi=300)
print(out)
