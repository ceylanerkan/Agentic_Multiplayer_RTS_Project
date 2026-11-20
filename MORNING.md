# Sabah notları – 2026-11-20

## Dün gece ne yaptım
- `UNITY_FEEDBACK.md` 2026-11-19 işlendi. Bulduğun sebep doğruydu: `GameSpawner` runner'a kaydedilmiyordu, bu yüzden Player/TC/Villager oluşmuyordu. Artık runner çalışınca `NetworkRunner.AddGlobal(this)` çağırıyor; tek başına Host iken de üs oluşur. Üs oluşturma hata verirse Console bir kez yazar, AI paneli "Base spawn failed: ..." der; diğer durumlarda panel eksik adımı söylüyor ("Match not started yet" yok).
- `Tools > RTS > 2` ve `3` komutları kurulu sahnede "Already set up" deyip hiçbir şey eklemiyor (çift nesnelerin sebebi). `docs/setup.md` §3: ParrelSync yerine Windows build = Host + editör = Join, ya da Unity'nin Multiplayer Play Mode'u.
- Bağımsız inceleme, ilk kez çalışacak kodda 4 hata: (1) iki oyuncu da P1 olabiliyordu (Fusion host'a SON numarayı veriyor) — artık host ilk boş slotu verip saklıyor; (2) birkaç Villager biten kaynağı okuyup hata veriyordu; (3) her şey havada doğacaktı (ışın nesnenin kendisine çarpıyordu); (4) birimler input authority ile doğup client'ta zıplayabilirdi.
- Villager bir ağaç bitince, depoladıktan sonra yeni ağacı TC'nin etrafında arıyordu (orman uzaksa boşta kalıyordu); artık biten ağacın yanında arıyor.
- Küçükler (ikinci bağımsız inceleme dahil): yerleştirme hayaleti gerçekten yarı saydam (build'de de, `Sprites/Default`); görünmeyen düşmana sağ tık artık onu ele vermiyor.
- Yeni testler: sahnedeki her `SimulationBehaviour` `AddGlobal` çağırmalı; `GeminiLLM` isteği gerçek google-genai tipleriyle (sahte istemci); `make_session.py`; `PlayerSlots.FirstFree`; brief tabloları / zone isimleri / `unit_group` / tech ve hasar sabitleri JSON ve `rules.md` ile tutarlı mı. Rapor, mimari ve savunma notları güncellendi.
- Testler: `tools/compile-check` 0 hata / 0 uyarı, `pytest -q ai evaluation` 86 geçti, C# kural testleri 54/54.

## Plan durumu
Gece 1–15 bitti; Gece 16 (hata düzeltme) ve 17 (tutarlılık) kısmen bitti, Unity testini bekliyor. Plan tarihlerine göre ~7 gün öndeyiz. Sonraki gece: yeni `UNITY_FEEDBACK.md` notları, sonra kalan inceleme/test işleri.

## Bugün test et
1. PR'ı merge et: https://github.com/ceylanerkan/Agentic_Multiplayer_RTS_Project/pull/5, sonra main'de `git pull`.
2. Çift nesneli `Game.unity`'yi at: `Tools > RTS > Rebuild generated scenes and prefabs` > Rebuild (sadece bunu çalıştır, 1/2/3'ü kurulu sahnede çalıştırma). Console'da kırmızı hata olmamalı. `Game.unity`'de tek `MapAndSystems` ve tek `GameCanvas` olmalı. Sahneleri ve `Assets/Prefabs`'ı main'e commit'le.
3. Tek başına dene: Play > session adı > Host. Console'da `GameSpawner registered with the NetworkRunner (host).` görünmeli; TC + 3 Villager (mavi disk, etiket) yere oturmuş halde (havada değil) oluşmalı; üst barda "P1". AI paneline soru yaz: gateway kapalıysa bağlantı hatası, açıksa cevap gelmeli; "Match not started yet" artık çıkmamalı.
4. İki oyuncu: ParrelSync klonu yine kapanırsa `docs/setup.md` §3'teki yolu izle: `File > Build Profiles` > Windows > Build And Run (klasör: `UnityRTS/Builds/Win`, git-ignore'da), build'de Host, editörde aynı adla Join. Host üst barda "P1", client "P2" görmeli; üsler haritanın iki ucunda (x = −65 / +65) olmalı. 3 Villager'ı aynı çalıya gönder: çalı bitince hata çıkmamalı, Villager'lar yandaki çalıya geçmeli. Kod değişince build'i yeniden al. Build istemiyorsan alternatif: Unity 6'nın **Multiplayer Play Mode** paketi (`docs/setup.md` §3; Photon'un Fusion 2 örnekleri kullanıyor, bu projede denenmedi).
5. Sonra geçen günkü test listesinin kalanı (`TODO.md` [You] 2): Villager seç, ağaca sağ tık, House inşa et, TC'de Villager üret.
6. Sorun olursa `UNITY_FEEDBACK.md`'ye: Console hatası olduğu gibi, adım numarasıyla `Test 3 tamam` / `Test 4 olmadı: ...`. Klon/build için `Player.log` veya `Editor.log`'un son satırları da işe yarar.

## Senin kararın gerekiyor
**Q-1120-1:** Kaynak yerleşimi: design brief'e göre bütün odun EastForest'ta (P2'nin yanı, x=+65), bütün altın WestMine'da (P1'in yanı, x=−65). P1 her odun turu için ~140 m yürüyor, P2 altın için aynı; erken ekonomi iki taraf için farklı.
- **A:** Olduğu gibi bırak. Brief, RAG bilgisi ve AI cevapları değişmez; demo çalışır ama oyun dengesiz.
- **B:** Her üssün yanına küçük bir orman (ör. 4 ağaç, `P1Base`/`P2Base` içinde) ekle; EastForest/WestMine kalır. Değişecekler: `RtsSetupTools` (ağaç yerleşimi, build slot'larıyla çakışmayacak yerde), brief'in kaynak satırı, `rules.md` (RAG) ve harita EditMode testi; şemalar ve zone isimleri değişmez. Sonra senin Rebuild yapman gerekir (ben kodu yaparım).
- **C:** Haritayı aynala: her iki tarafa da bir orman + bir maden. Zone isimleri ve AI bilgi tabanı daha çok değişir.
- Önerim: **B** (küçük değişiklik, iki tarafa eşit erken odun). Bekleyen: sadece harita/kaynak düzeni; diğer işler devam ediyor.

**Q-1120-2:** `units.json`'ta bazı `weaknesses` girdileri karşı birimin `strengths`'inde yok (Raider Boat "catapult'a zayıf", Scout ve Villager "spearman/archer'a zayıf"), ama hasar çarpanı sadece `strengths`'ten geliyor; AI bu zayıflıkları kanıt olarak söyleyebilir.
- **A:** `weaknesses`'i `strengths`'e uydur (çarpanı olmayanları sil). Oyun aynı kalır, AI'nın söylediği hasar kurallarıyla tutarlı olur; RAG indeksini yeniden kurarsın. Aynı düzeltmede `rules.md`'deki "Units automatically attack enemies ... while idle" cümlesine "(Villagers never start fights)" eklerim (kod böyle çalışıyor).
- **B:** Eksik `strengths` ekle (ör. Catapult → Raider Boat). Oyun dengesi değişir.
- **C:** Bırak.
- Önerim: **A**. Bekleyen: yok (sadece veri tutarlılığı).

## Gözden geçir
- Iron Weapons artık sadece Spearman, Archer ve Cavalry'ye +2 saldırı veriyor (`technologies.json` böyle diyor, AI de bunu okuyor); önce Scout, Catapult ve gemilere de veriyordu. Brief'te "+2 attack for military" yazıyor; tersini istersen JSON'u değiştiririz.
- Birim ve binalar artık input authority olmadan doğuyor (sahiplik zaten `[Networked] Owner`'da, emirler RPC ile gidiyor). Kendi birimlerinin client'ta akıcı hareket ettiğini kontrol et.
- Spawn yüksekliği: ışın ağ nesnelerini (birim, bina, kaynak) atlayıp en yüksek diğer yüzeye (zemin/deniz) bakıyor.
- `GameSpawner` kendini `GameLauncher.Instance.Runner`'a kaydediyor (runner'ı `GameLauncher`'dan bulmak yerine Fusion'a bırakmak mümkün değildi). Kayıt `Update`'te deneniyor, böylece sahne yüklenme sırası önemli değil.
- AI paneli durum mesajları İngilizce (diğer UI yazılarıyla aynı).

## Emin olmadığım yerler
- Üs oluşturma bir kez hata verirse (ör. prefab tablosu) o oyuncu için tekrar denenmiyor; düzeltip Play'i yeniden başlat.
- Düzeltme gerçek Unity'de denenmedi (sadece stub'larla derlendi). `AddGlobal` ve `RemoveGlobal` Fusion 2.0.13'ün `Fusion.Runtime.xml` belgesinde var; yine de Console'da Fusion'dan bir uyarı görürsen yaz.
- ParrelSync klonunun kapanma sebebini bulamadım (log'da çökme yok). Build = Host yolu bunu tamamen atlıyor.
- Gemini ile canlı çalıştırma hâlâ yapılmadı.
- `AiCommanderPanel` URL'si `http://127.0.0.1:8765` kaldıkça sorun yok: Unity localhost'u "Allow downloads over HTTP = Not allowed" kuralından muaf tutuyor. Gateway'i başka bir PC'nin IP'sine yönlendirirsen `Project Settings > Player > Other Settings > Allow downloads over HTTP` ayarını `Always allowed` yapman gerekir.

## Nasıl cevap verirsin
UNITY_FEEDBACK.md dosyasına bugünün tarihiyle bir başlık aç, cevapları yaz (ör. `Q-1114-1: B`,
`Test 2 tamam`, Console hataları) ve main'e push'la. Bir sonraki gece önce bunları işlerim.
