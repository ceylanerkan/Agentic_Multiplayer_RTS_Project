# Sabah notları – 2026-11-11

## Dün gece ne yaptım
- M4 birinci kısım yeniden yapıldı (önceki push kaybolmuştu): `ai/requirements.txt`, RAG retriever, SQLite session store.
- 7 araçlı salt-okunur MCP server (`ai/mcp-server/server.py`) ve araç dokümanı `docs/mcp-tools.md`.
- 24 pytest testi geçiyor (`pytest ai`).
- Gateway (`ai/gateway/`): `/session`, `/ask`, `/why`, agent döngüsü (max 6 adım, 30 sn), JSONL tool log ve security log; 33 test geçiyor.
- Değerlendirme: 3 fixture, 16 prompt, `evaluation/run_eval.py` (mock ve gerçek Gemini modu), `evaluation/security-tests.md`.
- Unity için saf kural kodu: `GameData`, `DamageCalculator`, `GameActionValidator` + EditMode testleri (derlenmedi!).

## Bugün test et
1. Pull request'i GitHub'da merge et, sonra `git pull`.
2. `pip install -r ai/requirements.txt` ardından `python ai/rag/build_index.py` ve `pytest ai` çalıştır; hepsi geçmeli.
3. Unity'de projeyi aç, Console'da derleme hatası var mı bak, Test Runner > EditMode testlerini çalıştır (`RulesTests`).
4. Hata olursa çıktıyı UNITY_FEEDBACK.md'ye yaz. MCP Inspector adımı TODO.md'de.

## Senin kararın gerekiyor
Yok.

## Gözden geçir
- Retriever: BM25 + (varsa) sentence-transformers; embedding dosyası yoksa sadece BM25.
- Araç hataları exception yerine `{"error": ...}` döner.

## Emin olmadığım yerler
- C# kodu burada derlenemedi (dotnet yok). `RTS.Core.asmdef` içindeki `Unity.Nuget.Newtonsoft-Json` referans adı hafızadan; yanlışsa Console'da görürsün.
- Scriptler artık asmdef içinde (`RTS.Core`); ileride Fusion kodu için ayrı asmdef açılırsa `AssembliesToWeave` listesine eklenmeli.
- `GeminiLLM` gerçek API ile hiç denenmedi; agent MCP server'ı süreç içinde çağırıyor (stdio client yok).
- Hybrid (embedding) mod ve gerçek stdio MCP çalıştırması bu ortamda denenmedi.
- `mcp` 2.x API'si (`MCPServer`) kullanıldı.

## Nasıl cevap verirsin
UNITY_FEEDBACK.md dosyasına bugünün tarihiyle bir başlık aç, cevapları yaz (ör. `Q-1114-1: B`,
`Test 2 tamam`, Console hataları) ve main'e push'la. Bir sonraki gece önce bunları işlerim.
