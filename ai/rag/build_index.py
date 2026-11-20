"""Build ai/rag/index/embeddings.json (reproducible; run: python ai/rag/build_index.py).

Without sentence-transformers the retriever still works with BM25 only.
"""
import json
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).parent))
from retriever import INDEX_DIR, load_chunks  # noqa: E402

MODEL = "sentence-transformers/all-MiniLM-L6-v2"

if __name__ == "__main__":
    try:
        from sentence_transformers import SentenceTransformer
    except ImportError:
        # optional dependency: say what to do instead of a traceback (retrieval falls back to BM25)
        print("sentence-transformers is not installed: no embeddings built, retrieval uses BM25 only.\n"
              "Install it with: pip install \"sentence-transformers>=3.0\"")
        sys.exit(1)
    chunks = load_chunks()
    vecs = SentenceTransformer(MODEL).encode([c["text"] for c in chunks], normalize_embeddings=True)
    INDEX_DIR.mkdir(exist_ok=True)
    (INDEX_DIR / "embeddings.json").write_text(json.dumps(
        {"model": MODEL, "ids": [c["source_id"] for c in chunks], "vectors": [v.tolist() for v in vecs]}))
    print(f"indexed {len(chunks)} chunks")
