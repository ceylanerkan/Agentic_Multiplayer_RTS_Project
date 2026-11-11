"""Hybrid retriever over the knowledge base: BM25 (+ embeddings when an index exists).

One chunk per JSON record and per `## [source_id]` section of rules.md.
"""
import json
import re
from pathlib import Path

from rank_bm25 import BM25Okapi

KNOWLEDGE_DIR = Path(__file__).parent / "knowledge"
INDEX_DIR = Path(__file__).parent / "index"
MAX_TOP_K = 5


def tokenize(text):
    return re.findall(r"[a-z0-9]+", text.lower())


def load_chunks(knowledge_dir=KNOWLEDGE_DIR):
    chunks = []
    for name in ("units", "buildings", "technologies"):
        for rec in json.loads((knowledge_dir / f"{name}.json").read_text(encoding="utf-8")):
            # Plain-text rendering of the record so that both BM25 and embeddings can use it.
            text = f"{rec['name']}. {rec.get('description', '')} {rec.get('effect', '')} " + json.dumps(
                {k: v for k, v in rec.items() if k not in ("id", "type", "name", "description", "effect")})
            chunks.append({"source_id": rec["id"], "text": text.strip(), "title": rec["name"]})
    rules = (knowledge_dir / "rules.md").read_text(encoding="utf-8")
    for m in re.finditer(r"^## \[([a-z0-9_]+)\] (.+?)\n(.*?)(?=^## |\Z)", rules, re.S | re.M):
        chunks.append({"source_id": m.group(1), "text": f"{m.group(2)}. {m.group(3).strip()}"})
    return chunks


class Retriever:
    def __init__(self, chunks=None, index_dir=INDEX_DIR):
        self.chunks = chunks or load_chunks()
        self.bm25 = BM25Okapi([tokenize(c.get("title", "") + " " + c.get("title", "") + " " + c["text"]) for c in self.chunks])  # title counted extra: exact names should win
        self.embeddings = None
        self._embedder = None
        emb_file = Path(index_dir) / "embeddings.json"
        if emb_file.exists():  # written by build_index.py when sentence-transformers is installed
            data = json.loads(emb_file.read_text())
            if data["ids"] == [c["source_id"] for c in self.chunks]:
                self.embeddings = data["vectors"]
                self._model_name = data["model"]

    def _embed_query(self, query):
        if self._embedder is None:
            from sentence_transformers import SentenceTransformer
            self._embedder = SentenceTransformer(self._model_name)
        return self._embedder.encode([query], normalize_embeddings=True)[0].tolist()

    def search(self, query, top_k=3):
        top_k = max(1, min(MAX_TOP_K, int(top_k)))
        scores = list(self.bm25.get_scores(tokenize(query)))
        top = max(scores) or 1.0
        scores = [s / top for s in scores]  # 0..1
        if self.embeddings:
            q = self._embed_query(query)
            sims = [sum(a * b for a, b in zip(q, v)) for v in self.embeddings]
            scores = [0.5 * s + 0.5 * max(0.0, e) for s, e in zip(scores, sims)]
        order = sorted(range(len(scores)), key=lambda i: -scores[i])[:top_k]
        return [{"source_id": self.chunks[i]["source_id"], "score": round(scores[i], 4),
                 "text": self.chunks[i]["text"]} for i in order if scores[i] > 0]
