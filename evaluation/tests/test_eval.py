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


def test_category_mix_and_new_cases():
    rows = [json.loads(x) for x in (E / "prompts.jsonl").read_text().splitlines()]
    cats = {r["category"] for r in rows}
    assert {"single", "compound", "knowledge", "security", "cost", "stale", "invalid_action", "valid_action"} <= cats
    assert len(rows) >= 20 and len({r["id"] for r in rows}) == len(rows)


def test_mock_run_metrics_csv_and_table(tmp_path):
    out = tmp_path / "r.json"
    subprocess.run([sys.executable, str(E / "run_eval.py"), "--out", str(out)], check=True, capture_output=True)
    s = json.loads(out.read_text())
    rows = {r["id"]: r for r in s["rows"]}
    assert rows["p18"]["invalid_actions"] == 1 and "population" in rows["p18"]["invalid_reasons"]  # 20 archers: no capacity
    assert rows["p19"]["invalid_actions"] == 1 and "water" in rows["p19"]["invalid_reasons"]       # archers cannot enter water
    assert rows["p20"]["invalid_actions"] == 0 and rows["p21"]["invalid_actions"] == 0
    assert s["invalid_action_rate"] == 0.5 and s["stale_low_confidence_rate"] == 1.0
    assert s["tool_accuracy"] == 1.0
    assert (tmp_path / "r.csv").read_text().splitlines()[0].startswith("id,category")
    assert "| Invalid-action rate | 50% |" in (tmp_path / "r_table.md").read_text()


def test_out_may_be_a_folder(tmp_path):
    # `--out results/` used to crash with IsADirectoryError; now the three files are written inside the folder
    subprocess.run([sys.executable, str(E / "run_eval.py"), "--out", str(tmp_path)], check=True, capture_output=True)
    assert {"results.json", "results.csv", "results_table.md"} <= {p.name for p in tmp_path.iterdir()}
