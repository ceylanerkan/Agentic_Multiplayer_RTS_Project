"""The knowledge base is split into chunks exactly once: a malformed heading in rules.md would silently drop a rule from RAG."""
import json
import re

from retriever import KNOWLEDGE_DIR, MAX_TOP_K, Retriever, load_chunks


def test_every_record_and_rule_section_becomes_one_chunk():
    chunks = load_chunks()
    ids = [c["source_id"] for c in chunks]
    assert len(ids) == len(set(ids)), "source ids must be unique (they are the evidence the AI cites)"
    records = sum(len(json.loads((KNOWLEDGE_DIR / f"{n}.json").read_text(encoding="utf-8"))) for n in ("units", "buildings", "technologies"))
    headings = re.findall(r"^## ", (KNOWLEDGE_DIR / "rules.md").read_text(encoding="utf-8"), re.M)
    assert len(chunks) == records + len(headings), "a rules.md heading is not in the form '## [source_id] Title'"


def test_search_returns_ranked_cited_chunks():
    r = Retriever(index_dir="/nonexistent")  # BM25 only, like a setup without sentence-transformers
    res = r.search("Iron Weapons attack", top_k=MAX_TOP_K + 10)
    assert 1 <= len(res) <= MAX_TOP_K
    scores = [x["score"] for x in res]
    assert scores == sorted(scores, reverse=True) and all(0 < s <= 1 for s in scores)
    assert r.search("zzzz qqqq", top_k=3) == []  # nothing matches: no made-up evidence
