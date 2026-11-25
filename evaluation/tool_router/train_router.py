"""Fits the small classifiers of the Laya tool router on questions.jsonl (about 1-2 minutes, CPU, no graphics card).

Laya reads every question once; its reading (tool_router.LayaReader) is the input of three logistic regressions per tool, whose
probabilities are averaged. 6-fold cross-validation shows how good that is on questions it was not fitted on; the threshold is chosen
there. The 21 evaluation prompts are not used here. Few threads and a low process priority keep the computer quiet.
Outputs: ai/gateway/laya_tool_weights.npz (used by the gateway) and train_report.md next to this file
Run with the Python of the Laya environment:
  %USERPROFILE%\\.venvs\\laya\\Scripts\\python evaluation/tool_router/train_router.py [--threads 4]
"""
import argparse
import os
import sys
import time
from pathlib import Path

os.environ.setdefault("HF_HUB_OFFLINE", "1")
HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))

import router_data  # noqa: E402  (also puts ai/ on the path)
from gateway.tool_router import TOOLS, WEIGHTS, LayaReader  # noqa: E402

C = 0.003   # strong regularisation: 190 questions, 1024 numbers per reading


def be_quiet(threads):
    import torch
    torch.set_num_threads(threads)
    if sys.platform == "win32":
        import ctypes
        kernel = ctypes.windll.kernel32
        kernel.GetCurrentProcess.restype = ctypes.c_void_p
        kernel.SetPriorityClass(ctypes.c_void_p(kernel.GetCurrentProcess()), 0x4000)   # below normal


def fit(x, y):
    """Weights and bias of one logistic regression, with the standardisation folded in."""
    from sklearn.linear_model import LogisticRegression
    mean, std = x.mean(0), x.std(0) + 1e-9
    model = LogisticRegression(C=C, max_iter=1000, class_weight="balanced").fit((x - mean) / std, y)
    w = model.coef_[0] / std
    return w, model.intercept_[0] - (w * mean).sum()


def fit_all(features, labels):
    import numpy as np
    fits = [[fit(features[:, kind, t], labels[:, t]) for t in range(len(TOOLS))] for kind in range(features.shape[1])]
    return np.array([[w for w, _ in row] for row in fits], dtype="float32"), np.array([[b for _, b in row] for row in fits], dtype="float32")


def probabilities(features, w, b):
    import numpy as np
    return (1 / (1 + np.exp(-((features * w[None]).sum(-1) + b[None])))).mean(1)


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--threads", type=int, default=4)
    args = ap.parse_args()
    os.environ.update(OMP_NUM_THREADS=str(args.threads), OPENBLAS_NUM_THREADS=str(args.threads), MKL_NUM_THREADS=str(args.threads))
    rows = router_data.load()
    be_quiet(args.threads)
    import numpy as np
    from sklearn.model_selection import StratifiedKFold
    start, reader = time.time(), LayaReader()
    features = np.stack([reader.features(r["question"]) for r in rows])
    labels = np.array([[t in r["tools"] for t in TOOLS] for r in rows], dtype=int)
    held_out = np.zeros(labels.shape)
    for train, test in StratifiedKFold(6, shuffle=True, random_state=0).split(np.zeros(len(rows)), [r["group"] for r in rows]):
        held_out[test] = probabilities(features[test], *fit_all(features[train], labels[train]))
    w, b = fit_all(features, labels)
    np.savez(WEIGHTS, w=w, b=b)
    lines = [f"{len(rows)} questions, {time.time() - start:.0f} s. 6-fold cross-validation (every question predicted by classifiers that did not see it):", "",
             "| Threshold | Needed tools found | Extra tools per question | Exactly right |", "|---|---|---|---|"]
    for threshold in (0.3, 0.35, 0.4, 0.45, 0.5):
        chosen = held_out >= threshold
        lines.append(f"| {threshold} | {(chosen & (labels == 1)).sum() / labels.sum():.0%} | {(chosen & (labels == 0)).sum() / len(rows):.2f} | "
                     f"{(chosen == (labels == 1)).all(1).sum()}/{len(rows)} |")
    (HERE / "train_report.md").write_text("\n".join(lines) + "\n", encoding="utf-8")
    print("\n".join(lines))


if __name__ == "__main__":
    main()
