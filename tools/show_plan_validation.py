"""Demo scenario 5 (assignment 9): one valid ActionPlan and invalid ones, checked with the gateway's JSON Schema.

Run from the repository root: python tools/show_plan_validation.py
No model and no network: it uses the same validate_plan() that every plan passes before it reaches Unity.
"""
import json
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[1] / "ai"))
import jsonschema  # noqa: E402

from gateway.agent import validate_plan  # noqa: E402

VALID = {"summary": "Train spearmen against the cavalry at NorthGate.", "confidence": 0.8, "evidence": ["unit_spearman_v1"],
         "actions": [{"action_type": "TRAIN_UNIT", "unit_type": "spearman", "count": 2, "reason": "Spearmen beat cavalry."}]}


def changed(**action):
    plan = json.loads(json.dumps(VALID))
    plan["actions"][0].update(action)
    return plan


CASES = [
    ("valid plan", VALID),
    ("unknown action type", changed(action_type="DELETE_UNIT")),
    ("negative unit count", changed(count=-3)),
    ("unknown zone", {**VALID, "actions": [{"action_type": "MOVE_GROUP", "unit_group": "spearman", "target_zone": "Moon", "reason": "x"}]}),
    ("hidden field", {**VALID, "enemy_gold": 500}),
]

for name, plan in CASES:
    try:
        validate_plan(plan)
        print(f"{name:22} ACCEPTED")
    except jsonschema.ValidationError as e:
        print(f"{name:22} REJECTED  {e.message[:90]}")
