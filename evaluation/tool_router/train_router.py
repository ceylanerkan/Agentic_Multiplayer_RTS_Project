"""Fine-tunes the decision layer of Laya on questions.jsonl, so that it chooses the MCP tools better (not faster).

Only the decision layer is trained (freeze_encoder), on the CPU, with few threads and a low process priority so that the computer stays
usable and quiet: more --threads is faster and louder. The 21 evaluation prompts are not used here. The result is written to
ai/models/laya-rts (git-ignored, about 800 MB), where ai/gateway/tool_router.py finds it.
Run with the Python of the Laya environment:
  %USERPROFILE%\\.venvs\\laya\\Scripts\\python evaluation/tool_router/train_router.py [--threads 4] [--epochs 8]
"""
import argparse
import json
import os
import shutil
import sys
import tempfile
import time
from pathlib import Path

os.environ.setdefault("HF_HUB_OFFLINE", "1")
HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))

import router_data  # noqa: E402  (also puts ai/ on the path)
from gateway.tool_router import TUNED, local_checkpoint  # noqa: E402


def be_quiet(threads):
    import torch
    torch.set_num_threads(threads)
    if sys.platform == "win32":
        import ctypes
        kernel = ctypes.windll.kernel32
        kernel.GetCurrentProcess.restype = ctypes.c_void_p
        kernel.SetPriorityClass(ctypes.c_void_p(kernel.GetCurrentProcess()), 0x4000)   # below normal


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--threads", type=int, default=4)
    ap.add_argument("--epochs", type=int, default=8)
    args = ap.parse_args()
    rows = router_data.load()
    train, dev = router_data.split(rows)
    be_quiet(args.threads)
    from laya.train import TrainConfig, finetune

    start = time.time()
    with tempfile.TemporaryDirectory() as tmp:
        files = {}
        for name, part in (("train", train), ("dev", dev)):
            files[name] = Path(tmp) / f"{name}.jsonl"
            files[name].write_text("\n".join(json.dumps(r) for r in router_data.to_laya(part)), encoding="utf-8")
        config = TrainConfig(epochs=args.epochs, micro_batch=8, grad_accum=2, freeze_encoder=True, loss="soft-ce",
                             label_smoothing=0.05, amp=False, gradient_checkpointing=False, log_every=20, eval_data=str(files["dev"]))
        result = finetune(str(files["train"]), local_checkpoint("base"), str(TUNED), config, device="cpu")
    shutil.rmtree(TUNED / "checkpoint_latest", ignore_errors=True)   # a second copy of the weights
    report = {"minutes": round((time.time() - start) / 60, 1), "train_questions": len(train), "dev_questions": len(dev),
              "epoch_loss": result["epoch_loss"], "dev": result["train_report"]["comparison"],
              "dev_before": result["train_report"]["before"], "dev_after": result["train_report"]["after"]}
    (HERE / "train_report.json").write_text(json.dumps(report, indent=1, default=str), encoding="utf-8")
    print(f"done in {report['minutes']} minutes: {TUNED}")


if __name__ == "__main__":
    main()
