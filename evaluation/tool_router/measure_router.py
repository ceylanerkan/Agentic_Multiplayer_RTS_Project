"""How well does the Laya tool router choose the tools of the 21 evaluation prompts? Laya alone, no qwen, no Unity, no graphics card.

"needed" = the prompt's expected tools; an extra tool is not always wrong (the expected list is the minimum), it only costs a little time.
Knowledge prompts should get the knowledge search alone. The threshold is chosen in train_router.py, never on these prompts.
Run with the Python of the Laya environment:
  %USERPROFILE%\\.venvs\\laya\\Scripts\\python evaluation/tool_router/measure_router.py [--threads 4]
Outputs: measure_test.json and measure_test.md next to this file
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
from gateway.tool_router import TOOLS, LayaRouter  # noqa: E402


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--threads", type=int, default=0, help="0 = the default of PyTorch")
    args = ap.parse_args()
    if args.threads:
        import torch
        torch.set_num_threads(args.threads)
    router = LayaRouter()
    router.warm_up()
    rows, times = router_data.test_prompts(), []
    for r in rows:
        start = time.time()
        r["scores"] = router.scores(r["question"])
        times.append((time.time() - start) * 1000)
        r["chosen"] = [t for t in TOOLS if r["scores"][t] >= router.threshold]
    needed = sum(len(r["tools"]) for r in rows)
    found = sum(len(set(r["tools"]) & set(r["chosen"])) for r in rows)
    extra = sum(len(set(r["chosen"]) - set(r["tools"])) for r in rows)
    lines = [f"21 evaluation prompts, threshold {router.threshold}: {found}/{needed} needed tools found ({found / needed:.0%}), "
             f"{extra / len(rows):.2f} extra tools per question, median {statistics.median(times):.0f} ms per question on the CPU", "",
             "| Tool | Needed | Found | Extra |", "|---|---|---|---|"]
    for t in TOOLS:
        lines.append(f"| {t} | {sum(t in r['tools'] for r in rows)} | {sum(t in r['tools'] and t in r['chosen'] for r in rows)} | "
                     f"{sum(t not in r['tools'] and t in r['chosen'] for r in rows)} |")
    lines += ["", "| Prompt | Question | Missing | Extra |", "|---|---|---|---|"]
    for r in rows:
        lines.append(f"| {r['id']} | {r['question']} | {' '.join(sorted(set(r['tools']) - set(r['chosen']))) or '-'} | "
                     f"{' '.join(sorted(set(r['chosen']) - set(r['tools']))) or '-'} |")
    (HERE / "measure_test.md").write_text("\n".join(lines) + "\n", encoding="utf-8")
    (HERE / "measure_test.json").write_text(json.dumps({"median_ms": round(statistics.median(times)), "found": found, "needed": needed,
                                                        "extra": extra, "rows": rows}, indent=1), encoding="utf-8")
    print("\n".join(lines))


if __name__ == "__main__":
    main()
