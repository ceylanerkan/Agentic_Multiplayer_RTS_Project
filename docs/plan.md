# Güncel plan (24 Kasım 2026 sabahı itibarıyla)

Teslim: **30 Kasım 2026**. Kalan işlerin tek güncel listesi bu dosya. `TODO.md`, `MORNING.md` ve
`docs/nightly-plan.md` eski gece ajanından kalma; silinmeyecek ama oradaki adımlara (ör. Gemini anahtarı) uyulmayacak.

## Şu an ne durumda
| Bölüm (puan) | Durum | Eksik |
|---|---|---|
| RTS oyunu (20) | Tek oyunculu çalışıyor: üs, odun, House, Barracks, Archer/Spearman üretimi, sis | savaş, araştırma, gemi, mancınık, kazanma denenmedi |
| Multiplayer (15) | Kod hazır | **iki oyuncu hiç denenmedi** |
| MCP (15) | 7 araç, testler, log | Inspector ekran görüntüleri |
| LLM + RAG + plan (20) | Local Ollama, Unity'den uçtan uca çalıştı (11-30 sn) | knowledge sorusu ve RAG kanıtı Unity'de denenmedi |
| Onay + güvenlik (10) | Approve, Reject ve validator reddi oyunda görüldü | gizli bilgi (injection) sorusu, Ask why |
| Değerlendirme (10) | Araç hazır | 21 soruluk canlı çalıştırma |
| Rapor, sunum, video (10) | Rapor taslağı, sunum ve demo taslağı | 9 `[TODO live]`, sunum, video |

Son push: `7979713` (kod); 24 Kasım commit'leri henüz push edilmedi. Tam `pytest` en son PR #5'te çalıştı (88 geçti), ondan sonraki değişikliklerde çalışmadı.

## Kararlar
- **LLM local:** Ollama `qwen2.5:7b` (hoca local istiyor). Gemini/Groq önerilmeyecek; Gemini adaptörü kodda duruyor, kullanılmıyor.
- **Harita:** her üsse 4 ağaç. **Build:** Mono (IL2CPP kurulu değil, ödev istemiyor).
- **Unity editörünü öğrenci kullanır**, sonuçları sohbete yazar; kodu, Python'u ve belgeleri Claude yapar.
- **Test az:** her değişiklikte tam test yok, planda olmayan yeni test yok. Push'tan önce sorulur, commit küçük tutulur.
- 29 Kasım'dan sonra yeni özellik yok.

## Yapılacaklar

### 1. İki oyuncu (24-25 Kasım) - sıradaki iş, en riskli
- [x] Claude: ağ kodu okundu, testten önce üç düzeltme: Photon bölgesi kodda `eu` (build ve editör ayrı bölge seçip "GameNotFound"
      vermesin), runner ayrı nesnede (host düşünce GameLauncher silinmesin), maç bitince AI paneli hata vermiyor.
- [ ] Öğrenci: gateway'i kapat, `ollama stop qwen2.5:7b`. Unity: `Tools > RTS > Set build window (1280x720, windowed)`,
      `File > Build Profiles` > Windows > Build And Run, klasör `UnityRTS/Builds/Win`.
- [ ] Öğrenci: build'de `test1` > Host, editörde Play > `test1` > Join.
- [ ] Kontrol: P1/P2 doğru, üsler iki uçta, hareket iki ekranda aynı, saldırıda HP iki ekranda aynı düşüyor, sis düşmanı
      saklıyor, TC yıkılınca Victory/Defeat, biri çıkınca diğeri Menu'ye dönüyor.
      Kazanma için ordu gerekmez: 3 köylü düşman TC'sine gidip sağ tık, TC ~3 dakikada düşer. Sert kapanmada 10 sn bekle.
- [ ] Claude: çıkan hataları düzelt. Öğrenci: kısa kayıt al (M3 kanıtı).

### 2. Oyunun denenmemiş kısımları (25 Kasım)
- [ ] Öğrenci: araştırma (Age II, Iron Weapons), Stable/Cavalry, Blacksmith/Catapult, Dock/gemi, TC'den Villager.
- [ ] Claude: hatalar. Bir özellik zor düzeliyorsa (gemiler önce) demodan çıkarılır, raporda sınır olarak yazılır.

### 3. AI değerlendirmesi (26 Kasım)
- [x] Claude: `evaluation/run_eval.py`'ye **ortanca ve en yavaş gecikme** (ödev 12.1 şartı) ve prompt-injection sonucu. (Modeller artık ölçümden önce ısınıyor.)
- [ ] Claude: `C:\anaconda\python evaluation/run_eval.py --real` (21 soru, tahminen 10 dk, Unity kapalı). Çıktı
      `evaluation/results_ollama.*`, commit'lenir. `MCP_TRANSPORT=stdio` ile bir kez gerçek MCP çalıştırması.
- [ ] Claude: 7 MCP aracının geçerli/geçersiz girdi logu; `evaluation/security-tests.md` canlı sütunu.
- [ ] Öğrenci: MCP Inspector ekran görüntüleri (`python ai/mcp-server/make_session.py late_game` komutu yazdırır). Oyunda:
      "What does Iron Weapons do?" (kaynak id görünmeli), gizli bilgi sorusu, "Train 20 archers right now." > Approve (reddedilmeli),
      Ask why.

### 4. Teslim dosyaları (27-28 Kasım)
- [x] Claude: C# test sayısı raporda 54, design brief'te Iron Weapons satırı.
- [ ] Claude: rapordaki `[TODO live]` yerleri, gerçek hata örnekleri (aşağıda).
- [ ] Claude: raporu IEEE A4 Word/PDF yap, `docs/slides-outline.md`'den sunum (.pptx). `docs/demo-script.md`'yi son hâle göre düzelt.
- [ ] Öğrenci: rapora ad, numara ve hoca onayı; yedek demo videosu (9 senaryo, `docs/demo-script.md`).
- [ ] Öğrenci: hocaya tek kişilik takım için yazılı onay sor (PDF 2 kişi diyor). Claude mesaj taslağını yazar.

### 5. Kapanış (29-30 Kasım)
- [ ] Claude: bir kez tam `pytest -q ai evaluation`, gizli anahtar taraması, temiz klondan kurulum (`docs/setup.md`).
      `tools/compile-check` artık derlenmeyebilir (stub'da `Runner.SceneManager` yok): düzelt ya da setup.md'den çıkar.
- [ ] Öğrenci: son Windows build, demo provası (editör kapalı, gateway önceden açık), `docs/defense-notes.md` ile savunma provası.
- [ ] Teslim.

## Rapora girecek gerçek gözlemler
- Değerlendirme ilk soruda RAG modelinin yüklenmesini de ölçüyordu (30 sn'yi aşıp başarısız sayıldı); artık önce ısınıyor.
- AI düşünürken maç bitince panel yok edilmiş düğmelere erişiyordu (`MissingReferenceException`).
- 19-23 Kasım: üs oluşmadı (GameSpawner runner'a kayıtlı değildi; sonra sahne yüklenirken Spawn reddedildi). İkisi de düzeldi.
- Model araştırma planında `tech_type` yazmadı (Ollama grameri if/then'i uygulamıyor): her eylem için ayrı varyant yapıldı.
- Model durmadan yazıp 120 sn sınırına takıldı: cevap uzunluğu sınırlandı, plan onarım adımında yazdırılıyor.
- Model, aracın "yer yok" demesine rağmen istenen planı aynen öneriyor; validator reddediyor (`not enough population capacity`).
- Local model 96 sn'den 11-30 sn'ye indi (küçük context, modeli bellekte tutma, önceden yükleme, RAG'in internete çıkmaması).

## Sorun çıkınca bakılacak yerler
- Unity editör: `%LOCALAPPDATA%\Unity\Editor\Editor.log` (son `GameSpawner registered` satırından itibaren). Build:
  `%USERPROFILE%\AppData\LocalLow\DefaultCompany\UnityRTS\Player.log`; onay kayıtları aynı klasörde `decision-log.jsonl`.
- AI: `ai/logs/tool_calls.jsonl`, `ai/logs/security.jsonl`, `%LOCALAPPDATA%\Ollama\server.log` (`api/chat` süreleri).
- Son oyun durumu: `ai/data/sessions.sqlite3`, `snapshots` tablosu.
- Gateway: `tools/run_gateway.bat`, `local model is ready` yazana kadar bekle. Bilgisayar: 16 GB RAM, RTX 4050 6 GB; model ve
  Unity aynı kartı paylaşır.
- Python: `C:\anaconda\python`. Unity: 6000.4.9f1.
