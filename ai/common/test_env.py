import os

from common.env import load_env


def test_env_file_is_loaded_but_never_overrides(tmp_path, monkeypatch):
    f = tmp_path / ".env"
    f.write_text("RTS_TEST_A=from_file\nRTS_TEST_B=from_file\n")
    monkeypatch.delenv("RTS_TEST_A", raising=False)
    monkeypatch.setenv("RTS_TEST_B", "from_shell")
    assert load_env(f) is True
    assert os.environ["RTS_TEST_A"] == "from_file"
    assert os.environ["RTS_TEST_B"] == "from_shell"
    monkeypatch.delenv("RTS_TEST_A")


def test_missing_env_file_is_fine(tmp_path):
    assert load_env(tmp_path / "nope.env") is False
