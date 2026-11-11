import json
import subprocess
import sys
from pathlib import Path

E = Path(__file__).resolve().parents[1]


def test_prompts_and_fixtures_valid():
    import jsonschema
    schema = json.loads((E.parent / "ai/schemas/safe_snapshot.schema.json").read_text())
    for f in (E / "fixtures").glob("*.json"):
        jsonschema.Draft202012Validator(schema).validate(json.loads(f.read_text()))
    rows = [json.loads(x) for x in (E / "prompts.jsonl").read_text().splitlines()]
    assert len(rows) >= 14 and all((E / "fixtures" / f"{r['fixture']}.json").exists() for r in rows)


def test_mock_run_completes(tmp_path):
    out = tmp_path / "r.json"
    subprocess.run([sys.executable, str(E / "run_eval.py"), "--out", str(out)], check=True, capture_output=True)
    s = json.loads(out.read_text())
    assert s["prompts"] >= 14 and s["valid_plan_rate"] == 1.0
