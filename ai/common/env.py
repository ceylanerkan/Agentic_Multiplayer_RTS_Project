"""Load the repository's .env file (GEMINI_API_KEY and the other settings) into os.environ.

Existing environment variables win over the file, so a shell export or a test monkeypatch is never overridden.
A missing file or a missing python-dotenv package is not an error.
"""
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]


def load_env(path=None):
    path = Path(path) if path else ROOT / ".env"
    if not path.exists():
        return False
    try:
        from dotenv import load_dotenv
    except ImportError:
        return False
    return load_dotenv(path, override=False)
