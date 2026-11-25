"""Labelled questions for the Laya tool router (questions.jsonl): loading, checks, train/dev split.

Run: python evaluation/tool_router/router_data.py     checks the file and prints how many questions need each tool (no model involved)
"""
import json
import re
import sys
from pathlib import Path

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE.parents[1] / "ai"))

from gateway.tool_router import TOOL_QUESTIONS  # noqa: E402

TEST_PROMPTS = HERE.parent / "prompts.jsonl"   # the 21 evaluation prompts: never used for training
DEV_EVERY = 6                                  # every 6th question of a group is held out
MAX_OVERLAP = 0.75                             # share of common words from which a question counts as a copy of a test prompt


def words(text):
    return set(re.findall(r"[a-z0-9]+", text.lower()))


def overlap(a, b):
    a, b = words(a), words(b)
    return len(a & b) / len(a | b)


def read_jsonl(path):
    return [json.loads(line) for line in path.read_text(encoding="utf-8").splitlines() if line.strip()]


def test_prompts():
    return [{"question": p["question"], "tools": p["expected_tools"], "group": p["category"], "id": p["id"]} for p in read_jsonl(TEST_PROMPTS)]


def load():
    """The labelled questions, checked: known tools, no duplicates, no copy of a test prompt."""
    rows, tests, seen = read_jsonl(HERE / "questions.jsonl"), test_prompts(), {}
    for r in rows:
        unknown = set(r["tools"]) - set(TOOL_QUESTIONS)
        if unknown:
            raise ValueError(f"unknown tools {sorted(unknown)} in {r['question']!r}")
        key = " ".join(sorted(words(r["question"])))
        if key in seen:
            raise ValueError(f"duplicate question: {r['question']!r} and {seen[key]!r}")
        seen[key] = r["question"]
        for t in tests:
            if overlap(r["question"], t["question"]) >= MAX_OVERLAP:
                raise ValueError(f"too close to test prompt {t['id']}: {r['question']!r}")
    return rows


def split(rows):
    """(train, dev): the same split in every run, every group is in both."""
    train, dev, seen = [], [], {}
    for r in rows:
        n = seen[r["group"]] = seen.get(r["group"], 0) + 1
        (dev if n % DEV_EVERY == 0 else train).append(r)
    return train, dev


def to_laya(rows):
    """The row format of laya.train: one state, one yes/no question per tool, the expected answers."""
    return [{"state": r["question"], "questions": TOOL_QUESTIONS,
             "expected": {t: t in r["tools"] for t in TOOL_QUESTIONS}} for r in rows]


if __name__ == "__main__":
    rows = load()
    train, dev = split(rows)
    print(f"{len(rows)} questions ({len(train)} train, {len(dev)} dev)")
    for tool in TOOL_QUESTIONS:
        print(f"  {tool:24} {sum(tool in r['tools'] for r in train):4} train {sum(tool in r['tools'] for r in dev):4} dev")
    for g in dict.fromkeys(r["group"] for r in rows):
        print(f"  group {g:18} {sum(r['group'] == g for r in rows):4}")
