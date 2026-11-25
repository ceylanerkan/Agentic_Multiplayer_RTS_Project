# Güncel plan (25 Kasım 2026 itibarıyla)

Teslim: **30 Kasım 2026**. Kalan işlerin tek güncel listesi bu dosya. `TODO.md`, `MORNING.md` ve
`docs/nightly-plan.md` eski gece ajanından kalma; silinmeyecek ama oradaki adımlara (ör. Gemini anahtarı) uyulmayacak.

## Şu an ne durumda
| Bölüm (puan) | Durum | Eksik |
|---|---|---|
| RTS oyunu (20) | Çalışıyor: üs, odun, House, Barracks, Archer/Spearman üretimi, sis, saldırı, Victory/Defeat; 25 Kasım: araştırma, Stable, mancınık, Dock, gemiler, serbest bina yerleştirme ve yol bulma otomatik maçta (81/81), fare build'de (sanal fare) | - |
| Multiplayer (15) | 24 Kasım: iki build ile öğrenci denedi; 25 Kasım: yeni kodla otomatik iki oyunculu maç 35/35, iki taraf arasında 0 fark | - |
| MCP (15) | 7 araç, testler, 7 aracın geçerli/geçersiz girdi logu (gerçek MCP) | Inspector ekran görüntüleri |
| LLM + RAG + plan (20) | Local Ollama, Unity'den uçtan uca çalıştı; 21 soruda ortalama 11,3 -> 6,5 -> 5,2 sn, Laya ile 3,75 sn (Unity kapalı); Unity ile birlikte 1,4-3,8 sn | istem değişti, 21 soru yeniden ölçülecek |
| Onay + güvenlik (10) | Approve, Reject, validator reddi, gizli bilgi sorusu ve Ask why oyunda çalıştı | - |
| Değerlendirme (10) | 25 Kasım: araç %100, geçerli plan %100, güvenlik 2/2, reddedilen eylem 2/7 | yalnız rapora tabloların girmesi |
| Rapor, sunum, video (10) | Rapor taslağı, sunum ve demo taslağı | 9 `[TODO live]`, sunum, video |

**25 Kasım:** 3a ve Laya bitti, ölçüldü. Tam `pytest -q ai evaluation` 88 geçti. Öğrenci bilgisayarın sessiz moddan çıkmasını istemiyor, ağır işe
başlamadan sorulur. Öğrencinin `.env` dosyasında `TOOL_ROUTER=laya` (Claude oluşturdu; kapatmak için `off`).
Son push: `c712f50`.
Unity'nin 4 ayar dosyası öğrencinin onayıyla commit'lendi (`ba75c28`). İstemin BUILD satırı ve Dock bilgisi serbest yerleştirmeyle değişti; 21 soruluk
ölçüm bu değişiklikten önceki istemle yapıldı. **Öğrenci (25 Kasım): önce oyun bitsin, ölçüm sonra.**

## Kararlar
- **LLM local:** Ollama `qwen2.5:7b` (hoca local istiyor). Gemini/Groq önerilmeyecek; Gemini adaptörü kodda duruyor, kullanılmıyor.
- **Laya + qwen (öğrenci 25 Kasım'da "Laya'yı kullanmak istiyorum" dedi):** Laya metin yazmaz, yalnız araç seçer; planı qwen yazar ve isterse
  ek araç çağırır. Ayardan kapatılabilir, hata olursa bugünkü yol çalışır.
- **Harita:** her üsse 4 ağaç. **Build:** Mono (IL2CPP kurulu değil, ödev istemiyor).
- **Unity editörünü öğrenci kullanır**, sonuçları sohbete yazar; kodu, Python'u ve belgeleri Claude yapar. Öğrencinin isteğiyle (25 Kasım) Claude editör
  kapalıyken penceresiz otomatik maç (`AutoPlayTest`) çalıştırabilir: yaklaşık 9 dk, AI'sız hafif, AI ile boş RAM 0,3 GB'a iner.
- **Test az:** her değişiklikte tam test yok, planda olmayan yeni test yok. Push'tan önce sorulur, commit küçük tutulur.
- **Hocaya mesaj işi yok** (öğrenci, 25 Kasım: "tamamen unut, önemsiz"): tekrar önerilmeyecek.
- Öğrenci oyunu deneyip eklenecekleri söyleyecek (25 Kasım); AI testleri ondan sonra.
- 29 Kasım'dan sonra yeni özellik yok.

## Yapılacaklar

### 1. İki oyuncu (24 Kasım) - bitti
- [x] Claude: ağ kodu okundu, testten önce üç düzeltme: Photon bölgesi kodda `eu` (build ve editör ayrı bölge seçip "GameNotFound"
      vermesin), runner ayrı nesnede (host düşünce GameLauncher silinmesin), maç bitince AI paneli hata vermiyor.
- [x] Öğrenci: gateway'i kapat, `ollama stop qwen2.5:7b`. Unity: `Tools > RTS > Set build window (1280x720, windowed)`,
      `File > Build Profiles` > Windows > Build And Run, klasör `UnityRTS/Builds/Win`.
- [x] Öğrenci: build'de `test1` > Host, editörde Play > `test1` > Join.
- [x] Kontrol: P1/P2 doğru, üsler iki uçta, hareket iki ekranda aynı, saldırıda HP iki ekranda aynı düşüyor, sis düşmanı
      saklıyor, TC yıkılınca Victory/Defeat, biri çıkınca diğeri Menu'ye dönüyor.
      Kazanma için ordu gerekmez: 3 köylü düşman TC'sine gidip sağ tık, TC ~3 dakikada düşer. Sert kapanmada 10 sn bekle.
- [x] Claude: logda tek kusur, oturum kapanırken HUD'un bir karelik hatası; düzeltildi (yeni build'de görülecek).
- [x] M3 kanıtı: ödev "Two-instance test video/log" diyor, log yeter: `evaluation/unity_two_players.md`. Kayıt istenmiyor (öğrenci çalıştığını biliyor).

### 2. Oyunun denenmemiş kısımları (24-25 Kasım) - sıradaki iş
- [x] Claude: kaynak toplamayı beklememek için editör menüsü `Tools > RTS > Test: +1000 resources` (build'e girmez).
- [x] Claude: denemeden önce kod okundu, beş düzeltme: Dock kıyıya alındı (Raider Boat yetişemiyordu), gemiler yan yana çıkıyor,
      Age II neden başlamıyor düğmede yazıyor, AI onayı kuyruk/yer engelini "sent" demeden söylüyor, bir planda iki BUILD iki köylüye gidiyor.
      Unity derleyicisiyle derlendi, oyunda denenmedi.
- [x] Claude: otomatik maç `Assets/Scripts/Editor/AutoPlayTest.cs` (build'e girmez; komut `docs/setup.md` bölüm 5): host oturumu, gerçek komutlar, 2. oyuncu
      vekil. Villager, House, Barracks, Age II, Iron Weapons, Fletching, Horse Collar, Stable/Cavalry, Catapult, Dock/gemi, toplama, AI onayı, zafer ve retler:
      **81/81** (`evaluation/unity_autoplay.md`), Unity logunda hata yok.
- [x] Claude: otomatik maçın bulduğu üç hata düzeltildi ve yeniden oynandı: (1) AI ve en yakın yer seçimi kıyı yerini Barracks/Blacksmith'e veriyordu, Dock
      kurulamıyordu; (2) Raider Boat düşman Dock'una ulaşamıyordu (düz yol kıyıda kesiliyordu, artık kıyı boyunca kayıyor); (3) bir bina 4'ten fazla birlik
      üretince birlikler iç içe çıkıyordu.
- [x] Claude: iki oyunculu otomatik maç (`Testing/AutoMatch.cs` yalnız test build'inde, `tools/run_auto_match.ps1`, 2-3 dk, penceresiz): host + katılan, katılan sanal
      fareyle oynar (seçim, kutu, düğmeler, hayalet yeşil/kırmızı, sağ tık, sis, saldırı, VICTORY/DEFEAT, menüye dönüş). 35/35; iki tarafın gördüğü 1143 nesne ve
      202 oyuncu değerinde 0 fark (`evaluation/unity_two_players.md`). Ekran resimlerine bakıldı: düğmeler, HUD ve AI paneli 1280x720'de düzgün.
- [x] Claude: oyunun Windows build'i alındı (`UnityRTS/Builds/UnityRTS.exe`, 25 Kasım 15:56, son kod). Komut: `AutoBuild.Game`, menü `Tools > RTS > Build Windows game`.
- [x] Claude (öğrencinin isteği, 25 Kasım): **yol bulma** (`PathGrid`: 1 m ızgarada A*, yalnız host; bina ve kaynakların etrafından, kara/su ayrı; yolda bina
      kurulursa yol yenilenir). Önceden birlikler düz gidip binaların içinden geçiyordu. C# testleri 55/55, en uzun arama 1,1 ms. Sınır: birlikler birbirinden kaçınmaz.
- Kod değişirse yeniden build (`AutoBuild.Game`).
- [x] Claude (öğrencinin denemesinden, 25 Kasım): yeni menü ve bekleme odası (maç ikinci oyuncuyla başlar, "Practice alone" ayrı), düzensiz ve aynalı kaynaklar
      (`MapLayout`), balık + Fishing Boat, zemine yapışan sis (`Resources/FogOfWar.shader`), kıvrımlı kıyı ve tepeler (`TerrainShape`, `GroundMesh`).
      **Öğrenci kendisi test ediyor** (Claude'un otomatik maçları bu değişikliklerle henüz çalışmadı). Sahne: `Tools > RTS > Refresh map resources and menu`.
- [ ] En son: görünüş asset ve material'larla değiştirilecek (öğrencinin isteği).
- [x] Claude (öğrencinin isteği, 25 Kasım): **serbest yerleştirme**. Her bina düşman üssü dışında karada her boş yere kurulur (`BuildSite`, `BuildPlaces`); Dock
      yalnız kıyıya (kendi üssü veya NorthGate, suya en çok 2,5 m), gemiler Dock'un önündeki suda çıkar. Sabit bina yerleri (`BuildSlot`) sahneden ve koddan
      kalktı; eskiden 7 yer yüzünden nüfus 20'de kalıyordu. C# testleri 54/54, otomatik maç 76/76. Eski build ve yeni editör uyuşmaz: yeniden build.

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
- [x] Claude: Unity (penceresiz) + gateway + Ollama (`OLLAMA_NUM_GPU=99`) + Laya birlikte, 13 dk: sığdı. Kart 4,6 / 6,1 GB, gateway+Laya 2,2 GB, Unity 2,3 GB,
      boş RAM en az 0,3 GB (VS Code açıkken). Beş cevap 1,4-3,8 sn (`evaluation/unity_autoplay_ai.md`). Pencereli oyunun kart kullanımı ölçülmedi.

**3c. Kalan**
- [ ] Claude: `evaluation/security-tests.md` canlı sütunu (`evaluation/live_security.json`).
- [x] Claude: bilgi sorusunda kanıta kaynak id'si (`tech_iron_weapons_v1`) eklendi (`agent.evidence`); getirilmemiş kaynak kanıttan siliniyor. Canlı modelle denenmedi.
- [x] Claude: oyunda (otomatik maç) "What does Iron Weapons do?", gizli bilgi sorusu, "Train 20 archers right now." ve Ask why çalıştı. 20 okçuda model artık
      eylem önermiyor, yani validator reddi bu soruyla gösterilemez: demoya başka örnek gerek. "Enemy cavalry..." sorusunda da eylem önermedi (boş yer 2).
- MCP Inspector görüntüsü isteğe bağlı: ödev 6.1 "MCP Inspector or your own test client ... screenshots/logs" diyor, `evaluation/mcp_calls_table.md` var.

### 4. Teslim dosyaları (27-28 Kasım)
- [x] Claude: C# test sayısı raporda 54, design brief'te Iron Weapons satırı.
- [ ] Claude: rapordaki `[TODO live]` yerleri, gerçek hata örnekleri (aşağıda).
- [ ] Claude: raporu IEEE A4 Word/PDF yap, `docs/slides-outline.md`'den sunum (.pptx). `docs/demo-script.md`'yi son hâle göre düzelt.
- [ ] Öğrenci: rapora ad ve numara; yedek demo videosu (9 senaryo, `docs/demo-script.md`).

### 5. Kapanış (29-30 Kasım)
- [x] Claude (25 Kasım): tam `pytest -q ai evaluation` (88), gizli anahtar taraması (temiz), temiz klonda dizin + testler + mock değerlendirme çalıştı.
      Paketler Anaconda'dan geldi: `pip install` ve Unity tarafı temiz klonda denenmedi. Kod değişirse 29 Kasım'da bir kez daha.
- [x] Claude: `tools/compile-check/check.ps1`: bütün C# kodunu Unity'nin içindeki derleyiciyle, gerçek Unity/Fusion dosyalarına karşı derler (18 sn, Unity açılmaz,
      .NET SDK gerekmez). Eski stub dosyaları kalktı.
- [ ] Öğrenci: son Windows build, demo provası (editör kapalı, gateway önceden açık), `docs/defense-notes.md` ile savunma provası.
- [ ] Teslim.

## Rapora girecek gerçek gözlemler
- Otomatik maç üç hata buldu (kıyı yeri, Raider Boat'un yolu, iç içe birlikler); hiçbirini birim testleri ve kod okuma göstermemişti.
- Oyundaki canlı hata: "Training a House would add 5 free population, allowing you to train 2 archers" (House eğitilmez, 2 + 5 = 7 olur).
- "Ask why" cevabı planın özetini aynen tekrar etti.
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
