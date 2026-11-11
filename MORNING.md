# Sabah notları – 2026-11-11

## Dün gece ne yaptım
- M4 birinci kısım yeniden yapıldı (önceki push kaybolmuştu): `ai/requirements.txt`, RAG retriever, SQLite session store.
- 7 araçlı salt-okunur MCP server (`ai/mcp-server/server.py`) ve araç dokümanı `docs/mcp-tools.md`.
- 24 pytest testi geçiyor (`pytest ai`).
- Gateway/agent döngüsü ve Unity kodu henüz yok.

## Bugün test et
1. Pull request'i GitHub'da merge et, sonra `git pull`.
2. `pip install -r ai/requirements.txt` ardından `python ai/rag/build_index.py` ve `pytest ai` çalıştır; hepsi geçmeli.
3. Hata olursa çıktıyı UNITY_FEEDBACK.md'ye yaz. MCP Inspector adımı TODO.md'de.

## Senin kararın gerekiyor
Yok.

## Gözden geçir
- Retriever: BM25 + (varsa) sentence-transformers; embedding dosyası yoksa sadece BM25.
- Araç hataları exception yerine `{"error": ...}` döner.

## Emin olmadığım yerler
- Hybrid (embedding) mod ve gerçek stdio MCP çalıştırması bu ortamda denenmedi.
- `mcp` 2.x API'si (`MCPServer`) kullanıldı.

## Nasıl cevap verirsin
UNITY_FEEDBACK.md dosyasına bugünün tarihiyle bir başlık aç, cevapları yaz (ör. `Q-1114-1: B`,
`Test 2 tamam`, Console hataları) ve main'e push'la. Bir sonraki gece önce bunları işlerim.
