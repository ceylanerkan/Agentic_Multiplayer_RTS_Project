"""How well does Laya choose the tools, before and after training? Laya alone, no qwen, no Unity, no graphics card.

--data test: the 21 evaluation prompts at the configured threshold. "needed" = the prompt's expected tools; an extra tool is not always
wrong (the expected list is the minimum), it only costs a little time. Knowledge prompts must get the knowledge search alone.
--data dev: the held-out part of questions.jsonl, at several thresholds. Choose the threshold here, never on the test prompts.
Run with the Python of the Laya environment:
  %USERPROFILE%\\.venvs\\laya\\Scripts\\python evaluation/tool_router/measure_router.py --model base|tuned [--data test|dev]
Outputs: measure_<model>_<data>.json and .md next to this file
"""
import argparse
import json
import os
import statistics
import sys
import time
from pathlib import Path

os.environ.setdefault("HF_HUB_OFFLINE", "1")
HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))

import router_data  # noqa: E402
from gateway.tool_router import TOOL_QUESTIONS, TUNED, LayaRouter  # noqa: E402


def count(rows, threshold):
    needed = found = extra = exact = 0
    for r in rows:
        chosen = {t for t, p in r["scores"].items() if p >= threshold}
        want = set(r["tools"])
        needed, found, extra = needed + len(want), found + len(want & chosen), extra + len(chosen - want)
        exact += chosen == want
    return {"threshold": threshold, "needed": needed, "found": found, "recall": round(found / needed, 3), "extra": extra,
            "extra_per_question": round(extra / len(rows), 2), "exact": exact, "questions": len(rows)}


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--model", choices=["base", "tuned"], default="base")
    ap.add_argument("--data", choices=["test", "dev"], default="test")
    ap.add_argument("--threads", type=int, default=0, help="0 = the default of PyTorch")
    args = ap.parse_args()
    if args.threads:
        import torch
        torch.set_num_threads(args.threads)
    router = LayaRouter(model="base" if args.model == "base" else str(TUNED))
    router.warm_up()
    rows, times = router_data.test_prompts() if args.data == "test" else router_data.split(router_data.load())[1], []
    for r in rows:
        start = time.time()
        r["scores"] = router.scores(r["question"])
        times.append((time.time() - start) * 1000)
    thresholds = [router.threshold] if args.data == "test" else [0.3, 0.4, 0.5, 0.6, 0.7]
    results = [count(rows, t) for t in thresholds]
    tools = []
    for t in TOOL_QUESTIONS:
        want = [r for r in rows if t in r["tools"]]
        chosen = [r for r in rows if r["scores"][t] >= router.threshold]
        tools.append((t, len(want), sum(r["scores"][t] >= router.threshold for r in want), sum(t not in r["tools"] for r in chosen)))
    lines = [f"Laya ({args.model}), {args.data} questions, median {statistics.median(times):.0f} ms per question on the CPU", "",
             "| Threshold | Needed tools found | Recall | Extra tools per question | Exactly right |", "|---|---|---|---|---|"]
    lines += [f"| {x['threshold']} | {x['found']}/{x['needed']} | {x['recall']:.0%} | {x['extra_per_question']} | {x['exact']}/{x['questions']} |" for x in results]
    lines += ["", f"| Tool (threshold {router.threshold}) | Needed | Found | Extra |", "|---|---|---|---|"]
    lines += [f"| {t} | {n} | {f} | {x} |" for t, n, f, x in tools]
    lines += ["", "| Question | Needed | Missing | Extra |", "|---|---|---|---|"]
    for r in rows:
        chosen = {t for t, p in r["scores"].items() if p >= router.threshold}
        lines.append(f"| {r['question']} | {' '.join(r['tools']) or '-'} | {' '.join(sorted(set(r['tools']) - chosen)) or '-'} | "
                     f"{' '.join(sorted(chosen - set(r['tools']))) or '-'} |")
    name = f"measure_{args.model}_{args.data}"
    (HERE / f"{name}.md").write_text("\n".join(lines) + "\n", encoding="utf-8")
    (HERE / f"{name}.json").write_text(json.dumps({"median_ms": round(statistics.median(times)), "results": results, "rows": rows}, indent=1), encoding="utf-8")
    print("\n".join(lines[: 4 + len(results)]))


if __name__ == "__main__":
    main()
