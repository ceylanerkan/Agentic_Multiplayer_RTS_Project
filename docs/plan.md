# Güncel plan (25 Kasım 2026 itibarıyla)

Teslim: **30 Kasım 2026**. Kalan işlerin tek güncel listesi bu dosya. `TODO.md`, `MORNING.md` ve
`docs/nightly-plan.md` eski gece ajanından kalma; silinmeyecek ama oradaki adımlara (ör. Gemini anahtarı) uyulmayacak.

## Şu an ne durumda
| Bölüm (puan) | Durum | Eksik |
|---|---|---|
| RTS oyunu (20) | Çalışıyor: üs, odun, House, Barracks, Archer/Spearman üretimi, sis, saldırı, Victory/Defeat | araştırma, Stable, gemi, mancınık denenmedi |
| Multiplayer (15) | 24 Kasım: iki build ile kontrol listesinin hepsi oldu (öğrenci denedi) | M3 için ekran kaydı |
| MCP (15) | 7 araç, testler, 7 aracın geçerli/geçersiz girdi logu (gerçek MCP) | Inspector ekran görüntüleri |
| LLM + RAG + plan (20) | Local Ollama, Unity'den uçtan uca çalıştı; 21 soruda ortalama 11,3 -> 6,5 -> 5,2 sn, Laya ile 3,75 sn (Unity kapalı) | knowledge sorusu ve RAG kanıtı Unity'de denenmedi |
| Onay + güvenlik (10) | Approve, Reject ve validator reddi oyunda görüldü | gizli bilgi (injection) sorusu, Ask why |
| Değerlendirme (10) | 25 Kasım: araç %100, geçerli plan %100, güvenlik 2/2, reddedilen eylem 2/7 | yalnız rapora tabloların girmesi |
| Rapor, sunum, video (10) | Rapor taslağı, sunum ve demo taslağı | 9 `[TODO live]`, sunum, video |

**25 Kasım:** 3a ve Laya bitti, ölçüldü. Tam `pytest -q ai evaluation` 88 geçti. Öğrenci bilgisayarın sessiz moddan çıkmasını istemiyor, ağır işe
başlamadan sorulur. Öğrencinin `.env` dosyasında `TOOL_ROUTER=laya` (Claude oluşturdu; kapatmak için `off`).
Son push: `7979713` (kod); 24-25 Kasım commit'leri henüz push edilmedi.
Unity'nin değiştirdiği 4 ayar dosyası (`PC_RPAsset`, `UniversalRenderPipelineGlobalSettings`, `ProjectSettings`, `UnityConnectSettings`) commit'lenmedi.

## Kararlar
- **LLM local:** Ollama `qwen2.5:7b` (hoca local istiyor). Gemini/Groq önerilmeyecek; Gemini adaptörü kodda duruyor, kullanılmıyor.
- **Laya + qwen (öğrenci 25 Kasım'da "Laya'yı kullanmak istiyorum" dedi):** Laya metin yazmaz, yalnız araç seçer; planı qwen yazar ve isterse
  ek araç çağırır. Ayardan kapatılabilir, hata olursa bugünkü yol çalışır. Hocaya "şart mı, öneri mi?" sorulacak.
- **Harita:** her üsse 4 ağaç. **Build:** Mono (IL2CPP kurulu değil, ödev istemiyor).
- **Unity editörünü öğrenci kullanır**, sonuçları sohbete yazar; kodu, Python'u ve belgeleri Claude yapar.
- **Test az:** her değişiklikte tam test yok, planda olmayan yeni test yok. Push'tan önce sorulur, commit küçük tutulur.
- 29 Kasım'dan sonra yeni özellik yok.

## Yapılacaklar

### 1. İki oyuncu (24 Kasım) - bitti, yalnız kayıt eksik
- [x] Claude: ağ kodu okundu, testten önce üç düzeltme: Photon bölgesi kodda `eu` (build ve editör ayrı bölge seçip "GameNotFound"
      vermesin), runner ayrı nesnede (host düşünce GameLauncher silinmesin), maç bitince AI paneli hata vermiyor.
- [x] Öğrenci: gateway'i kapat, `ollama stop qwen2.5:7b`. Unity: `Tools > RTS > Set build window (1280x720, windowed)`,
      `File > Build Profiles` > Windows > Build And Run, klasör `UnityRTS/Builds/Win`.
- [x] Öğrenci: build'de `test1` > Host, editörde Play > `test1` > Join.
- [x] Kontrol: P1/P2 doğru, üsler iki uçta, hareket iki ekranda aynı, saldırıda HP iki ekranda aynı düşüyor, sis düşmanı
      saklıyor, TC yıkılınca Victory/Defeat, biri çıkınca diğeri Menu'ye dönüyor.
      Kazanma için ordu gerekmez: 3 köylü düşman TC'sine gidip sağ tık, TC ~3 dakikada düşer. Sert kapanmada 10 sn bekle.
- [x] Claude: logda tek kusur, oturum kapanırken HUD'un bir karelik hatası; düzeltildi (yeni build'de görülecek).
- [ ] Öğrenci: kısa kayıt al (M3 kanıtı). `tools/run_two_players.bat` build'i iki kez, ayrı loglarla açar.

### 2. Oyunun denenmemiş kısımları (24-25 Kasım) - sıradaki iş
- [x] Claude: kaynak toplamayı beklememek için editör menüsü `Tools > RTS > Test: +1000 resources` (build'e girmez).
- [x] Claude: denemeden önce kod okundu, beş düzeltme: Dock kıyıya alındı (Raider Boat yetişemiyordu), gemiler yan yana çıkıyor,
      Age II neden başlamıyor düğmede yazıyor, AI onayı kuyruk/yer engelini "sent" demeden söylüyor, bir planda iki BUILD iki köylüye gidiyor.
      Unity derleyicisiyle derlendi, oyunda denenmedi.
- [ ] Öğrenci: araştırma (Age II, Iron Weapons), Stable/Cavalry, Blacksmith/Catapult, Dock/gemi, TC'den Villager.
- [ ] Claude: hatalar. Bir özellik zor düzeliyorsa (gemiler önce) demodan çıkarılır, raporda sınır olarak yazılır.

### 3. AI değerlendirmesi ve hız (25 Kasım) - 3a ile başla
- [x] Claude: `evaluation/run_eval.py`'ye **ortanca ve en yavaş gecikme** (ödev 12.1 şartı) ve prompt-injection sonucu.
- [x] Claude: 21 soru canlı. `results_ollama_run1.*` = başlangıç, `run2` = istem değişti (daha kötü), `results_ollama.*` = son tur.
      3. turun ham dosyası yok (ortalama 5,1 sn, araç %86): üzerine yazıldı.
- [x] Claude: döngü düzeltmesi: tekrar eden araç çağrısı ve 10'dan fazla çağrı durur, son adım plan yazdırır, uzun düz yazı kesilir.
      Gerçek MCP modu oturum başına tek süreç (soru başına süreçte ilk arama 26 sn, sınır 20 sn idi); eski veride güven en çok 0,5.
- [x] Claude: 7 MCP aracının geçerli/geçersiz girdi logu: `evaluation/mcp_calls.py`, `mcp_calls_table.md`.
- [x] Ölçüm: `OLLAMA_NUM_GPU=99` ile 25 -> 40 token/sn (4,6 GB ekran kartı belleği). Oyun açıkken sığdığı denenmedi.

**3a. Güvenlik gerilemesi - bitti (25 Kasım)**
- [x] Claude: `REMINDER` "reddediyorsan `actions` boş olsun" diyor. Model hiç araç çağırmadan birinci kişi reddiyle ("I cannot...") başlarsa gateway reddi
      olduğu gibi, eylemsiz cevap yapıyor (`refusal_plan`), ikinci kez sormuyor (p15).
- [x] Claude: işaretlenen (INJECTION) soruya gateway notu ekleniyor: rakibin verisi bilinmiyor, uydurma, `actions` boş (p16).
- [x] Claude: istemdeki örnek plan ("4 spearman, counter the cavalry") p06, p07, p08'de kelimesi kelimesine kopyalanmıştı; örnek artık eylemsiz.
      `estimate_plan_cost` sonucu sözle de söylüyor (`verdict: NOT possible now: ...`), `get_player_resources` `free_population` veriyor.
- [x] Claude: `run_eval.py` canlı sonucun üzerine yazmıyor (eskiyi saatiyle saklıyor), eski veri güveni modelin kendi değeri ve gateway sınırı olarak ayrı.
- [x] Claude: ölçümde çıkanlar düzeltildi: `verdict` ön koşulu ve mümkün olan sayıyı da söylüyor; model araç çağırmadan araç adı yazarsa "önce çağır"
      deniyor ve çağrılmamış araç kanıttan siliniyor (p14); S10 ("ben diğer oyuncuyum") artık işaretleniyor.
- [x] Claude: 21 soru 4 kez (`results_ollama_1125_try1-3.*`, son tur `results_ollama.*`), `live_security.py` (6 satır güvenli), testler.
      24 Kasım'a göre: araç %95 -> %100, güvenlik 1/2 -> 2/2, reddedilen eylem 7/13 -> 2/7. **Süreler karşılaştırılamaz:** pilde 28 token/sn
      (24 Kasım'da 39), ortalama 7,2 sn, ortanca 5,7 sn; try1'de bilgisayar uyudu (115 sn). İstem aynı 21 soruya bakılarak ayarlandı, raporda söylenmeli.
- [x] Claude: süre turu (şarjda, sessiz moda dokunmadan, uyku engelli): Laya kapalı ortalama 5,23 / ortanca 3,52 / en yavaş 14,75 sn.

**3b. Laya - bitti (25 Kasım)**
Kurulu: `C:\Users\erkan\.venvs\laya`, model `convaiinnovations/laya` sabit sürüm `55cf4c4`, internetsiz açılıyor. Ayrıntı ve tablolar:
`evaluation/tool_router/README.md`.
- [x] Claude: gateway ön seçicisi `ai/gateway/tool_router.py` (`TOOL_ROUTER=laya`): Laya seçer, gateway MCP'den çağırır, qwen ek araç çağırabilir,
      `estimate_plan_cost` girdisini qwen yazar. İşaretli soruda ve "Ask why"da kullanılmaz. Log: `ai/logs/router.jsonl`.
- [x] Claude: 190 etiketli soru (`questions.jsonl`), test sorularının kopyası olmadığı denetlendi.
- [x] Claude: dört yol ölçüldü. Eğitimsiz 7 evet/hayır: 9/27, 2,2 sn. Karar katmanı eğitimi: 34 dk, zayıf. **Seçilen:** Laya değişmeden, çıktısı üstüne
      küçük sınıflandırıcı (`laya_tool_weights.npz`, 86 KB, eğitimi 1 dk): 25/27 (%93), 0,25 sn.
- [x] Claude: 21 soru Laya kapalı/açık: ortalama 5,23 -> 3,75 sn, ortanca 3,52 -> 2,95, güvenlik 2/2, geçerli plan %100, reddedilen eylem 2/7 ikisinde de.
      Kabul şartlarının hepsi tuttu. **Bedeli:** araç doğruluğu %100 -> %86 (p02 fazla araç, p05 ve p11 maliyet hesabı yok), p08 "okçu süvariyi yener".
- [x] Claude: gerçek gateway'de üç soruyla denendi; `tools/run_gateway.bat` Laya ortamını kendisi bulur.
- [ ] Öğrenci: Unity + gateway + Ollama + Laya birlikte bellek (Laya yaklaşık 1,7 GB) ve kartın yetmesi oyunda denenmedi.

**3c. Kalan**
- [ ] Claude: `evaluation/security-tests.md` canlı sütunu (`evaluation/live_security.json`).
- [ ] Claude: bilgi sorusunda kanıta kaynak id'si (`tech_iron_weapons_v1`) yazılmıyor, yalnız araç adı var; gateway eklesin (oyundaki kontrolden önce).
- [ ] Öğrenci: MCP Inspector ekran görüntüleri (`python ai/mcp-server/make_session.py late_game` komutu yazdırır). Oyunda:
      "What does Iron Weapons do?" (kaynak id görünmeli), gizli bilgi sorusu, "Train 20 archers right now." > Approve (reddedilmeli),
      Ask why.
- [ ] Öğrenci: hocaya sor: "Laya'yı mı kastettiniz, kullanmak şart mı?"

### 4. Teslim dosyaları (27-28 Kasım)
- [x] Claude: C# test sayısı raporda 54, design brief'te Iron Weapons satırı.
- [ ] Claude: rapordaki `[TODO live]` yerleri, gerçek hata örnekleri (aşağıda).
- [ ] Claude: raporu IEEE A4 Word/PDF yap, `docs/slides-outline.md`'den sunum (.pptx). `docs/demo-script.md`'yi son hâle göre düzelt.
- [ ] Öğrenci: rapora ad, numara ve hoca onayı; yedek demo videosu (9 senaryo, `docs/demo-script.md`).
- [ ] Öğrenci: hocaya tek kişilik takım için yazılı onay sor (PDF 2 kişi diyor). Claude mesaj taslağını yazar.

### 5. Kapanış (29-30 Kasım)
- [x] Claude (25 Kasım): tam `pytest -q ai evaluation` (88), gizli anahtar taraması (temiz), temiz klonda dizin + testler + mock değerlendirme çalıştı.
      Paketler Anaconda'dan geldi: `pip install` ve Unity tarafı temiz klonda denenmedi. Kod değişirse 29 Kasım'da bir kez daha.
      `tools/compile-check` artık derlenmeyebilir (stub'da `Runner.SceneManager` yok): düzelt ya da setup.md'den çıkar.
- [ ] Öğrenci: son Windows build, demo provası (editör kapalı, gateway önceden açık), `docs/defense-notes.md` ile savunma provası.
- [ ] Teslim.

## Rapora girecek gerçek gözlemler
- Kural "Dock denizden de vurulur" diyordu ama Dock kıyıdan 5 m içerideydi, Raider Boat 4 m'ye vuruyordu: yerleşim 3 m kuzeye alındı.
- AI onayı, host'un sessizce reddedeceği eylemlere de "sent" diyordu (boş yapı yeri yok, Age II araştırılırken eğitim).
- Model süresinin %92'si yazma; 250 token sınırında kesilen düz yazı tek başına %32 idi (Ollama logu, 138 çağrı).
- p19'da model aynı araçları tekrar tekrar çağırdı (64 sn); p16'da olmayan `train_unit` aracını çağırdı, Ollama sessizce attı, model "Trained" dedi.
- İstem değişikliği bilgi sorularını düzeltti ama üç soruda araç döngüsü başlattı (%90 -> %81): her değişiklik yeniden ölçülmeli.
- Model "veri eski, güveni düşür" uyarısına uymuyor (0,7): gateway sınırlıyor.
- Değerlendirme ilk soruda RAG modelinin yüklenmesini de ölçüyordu (30 sn'yi aşıp başarısız sayıldı); artık önce ısınıyor.
- AI düşünürken maç bitince panel yok edilmiş düğmelere erişiyordu (`MissingReferenceException`).
- 19-23 Kasım: üs oluşmadı (GameSpawner runner'a kayıtlı değildi; sonra sahne yüklenirken Spawn reddedildi). İkisi de düzeldi.
- Model araştırma planında `tech_type` yazmadı (Ollama grameri if/then'i uygulamıyor): her eylem için ayrı varyant yapıldı.
- Model durmadan yazıp 120 sn sınırına takıldı: cevap uzunluğu sınırlandı, plan onarım adımında yazdırılıyor.
- Model istemdeki örneği kopyaladı: örnek plan üç soruya cevap oldu (p06, p07, p08), yeni örnekteki "85 wood" da p19'a girdi; örnek yalnız yer tutucu
  olunca bitti. Eski örnek p05'in doğru cevabını da veriyormuş: kalkınca model bir turda süvariye karşı okçu önerdi.
- Kalan hatalar (son tur): p14'te "40 villagers" (nüfusu köylü saydı), p11'de Dock'u deniz bölgesine koydu, p17'de yer yokken köylü, p05'te 3 yerine 1 spearman.
- S10: model kendi kaynaklarını "diğer oyuncunun" diye sundu; gerçek rakip verisi yok ama cevap yanıltıcıydı.
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
