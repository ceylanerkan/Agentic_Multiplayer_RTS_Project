"""Contract test: a snapshot exported by Unity (AiSnapshotBuilder) must pass the gateway's schema check.

ai/gateway/tests/data/unity_export_sample.json is produced by the C# test
Snapshot_ExportSampleForPythonContractTest (set RTS_SNAPSHOT_OUT to the file path). A snapshot exported from a real
match (menu Tools > RTS > Export AI snapshot, written to ai/exports/snapshot.json) is checked too when it exists.
"""
import copy
import json
from pathlib import Path

import jsonschema
import pytest
from gateway.agent import SNAPSHOT_SCHEMA

HERE = Path(__file__).resolve().parent
AI = HERE.parents[1]
FILES = [HERE / "data" / "unity_export_sample.json"] + sorted((AI / "exports").glob("*.json"))
VALIDATOR = jsonschema.Draft202012Validator(SNAPSHOT_SCHEMA)


@pytest.mark.parametrize("path", FILES, ids=lambda p: p.name)
def test_exported_snapshot_matches_schema(path):
    VALIDATOR.validate(json.loads(path.read_text(encoding="utf-8")))


def _sample():
    return json.loads(FILES[0].read_text(encoding="utf-8"))


def test_hidden_fields_are_rejected():
    # If Unity ever adds a field with hidden information, the gateway must refuse the snapshot.
    snap = _sample()
    snap["opponent_resources"] = {"wood": 999, "food": 999, "gold": 999}
    with pytest.raises(jsonschema.ValidationError):
        VALIDATOR.validate(snap)
    snap = _sample()
    snap["visible_enemies"][0]["exact_position"] = [1.0, 2.0]
    with pytest.raises(jsonschema.ValidationError):
        VALIDATOR.validate(snap)


def test_unknown_zone_and_too_many_events_are_rejected():
    snap = _sample()
    snap["own_army"][0]["zone"] = "EnemyBase"
    with pytest.raises(jsonschema.ValidationError):
        VALIDATOR.validate(copy.deepcopy(snap))
    snap = _sample()
    snap["recent_events"] = [{"type": "UNIT_LOST", "seconds_ago": 1}] * 21
    with pytest.raises(jsonschema.ValidationError):
        VALIDATOR.validate(snap)
