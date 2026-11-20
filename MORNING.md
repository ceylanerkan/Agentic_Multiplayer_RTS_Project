# Sabah notları – 2026-11-20

## Dün gece ne yaptım
- `UNITY_FEEDBACK.md` 2026-11-19 işlendi. Senin bulduğun sebep doğruydu: `GameSpawner` (sahnedeki `SimulationBehaviour`) runner'a hiç kaydedilmiyordu, `Runner` null kalıyordu, bu yüzden TC/Villager ve Player nesnesi oluşmuyordu. Artık `GameSpawner.Update` runner çalışır çalışmaz `NetworkRunner.AddGlobal(this)` çağırıyor (yok olunca `RemoveGlobal`). Tek başına Host iken de üs oluşur.
- AI paneli artık "Match not started yet" yerine eksik adımı yazıyor: bağlı değil / bağlanıyor / sahnede `GameSpawner` yok / oyuncu henüz oluşmadı / host'un üssü oluşturması bekleniyor.
- `Tools > RTS > 2 Setup open Game scene` ve `3 Create Game UI`, sahnede kurulum zaten varsa "Already set up" uyarısı verip hiçbir şey eklemiyor (Game.unity'deki çift nesnelerin sebebi buydu).
- Bina yerleştirme hayaleti artık gerçekten yarı saydam (URP Unlit malzemesi opaktı, slotu kapatıyordu).
- `docs/setup.md` bölüm 3: ParrelSync çalışmazsa Windows build = Host, editör = Join, adım adım.
- Testler: `tools/compile-check` 0 hata / 0 uyarı, `pytest -q ai evaluation` 68 geçti (yeni: sahnedeki her `SimulationBehaviour` `AddGlobal` çağırmalı), C# kural testleri 53/53.

## Plan durumu
Gece 1–15 bitti; Gece 16 (hata düzeltme) ve 17 (tutarlılık) kısmen bitti, Unity testini bekliyor. Plan tarihlerine göre ~7 gün öndeyiz. Sonraki gece: yeni `UNITY_FEEDBACK.md` notları, sonra kalan inceleme/test işleri.

## Bugün test et
1. PR'ı merge et: https://github.com/ceylanerkan/Agentic_Multiplayer_RTS_Project/pull/5, sonra main'de `git pull`.
2. Çift nesneli `Game.unity`'yi at: `Tools > RTS > Rebuild generated scenes and prefabs` > Rebuild (sadece bunu çalıştır, 1/2/3'ü kurulu sahnede çalıştırma). Console'da kırmızı hata olmamalı. `Game.unity`'de tek `MapAndSystems` ve tek `GameCanvas` olmalı. Sahneleri ve `Assets/Prefabs`'ı main'e commit'le.
3. Tek başına dene: Play > session adı > Host. Console'da `GameSpawner registered with the NetworkRunner (host).` görünmeli; TC + 3 Villager (mavi disk, etiket) oluşmalı. AI paneline soru yaz: gateway kapalıysa bağlantı hatası, açıksa cevap gelmeli; "Match not started yet" artık çıkmamalı.
4. İki oyuncu: ParrelSync klonu yine kapanırsa `docs/setup.md` §3'teki yolu izle: `File > Build Profiles` > Windows > Build And Run (`Builds/Win` gibi `Assets` dışı bir klasör), build'de Host, editörde aynı adla Join. İki tarafta da kendi TC + 3 Villager'ın görünmeli. Kod değişince build'i yeniden al.
5. Sonra geçen günkü test listesinin kalanı (`TODO.md` [You] 2): Villager seç, ağaca sağ tık, House inşa et, TC'de Villager üret.
6. Sorun olursa `UNITY_FEEDBACK.md`'ye: Console hatası olduğu gibi, adım numarasıyla `Test 3 tamam` / `Test 4 olmadı: ...`. Klon/build için `Player.log` veya `Editor.log`'un son satırları da işe yarar.

## Senin kararın gerekiyor
Yok.

## Gözden geçir
- `GameSpawner` kendini `GameLauncher.Instance.Runner`'a kaydediyor (runner'ı `GameLauncher`'dan bulmak yerine Fusion'a bırakmak mümkün değildi). Kayıt `Update`'te deneniyor, böylece sahne yüklenme sırası önemli değil.
- AI paneli durum mesajları İngilizce (diğer UI yazılarıyla aynı).

## Emin olmadığım yerler
- Düzeltme gerçek Unity'de denenmedi (sadece stub'larla derlendi). `AddGlobal` ve `RemoveGlobal` Fusion 2.0.13'ün `Fusion.Runtime.xml` belgesinde var; yine de Console'da Fusion'dan bir uyarı görürsen yaz.
- ParrelSync klonunun kapanma sebebini bulamadım (log'da çökme yok). Build = Host yolu bunu tamamen atlıyor.
- Gemini ile canlı çalıştırma hâlâ yapılmadı.

## Nasıl cevap verirsin
UNITY_FEEDBACK.md dosyasına bugünün tarihiyle bir başlık aç, cevapları yaz (ör. `Q-1114-1: B`,
`Test 2 tamam`, Console hataları) ve main'e push'la. Bir sonraki gece önce bunları işlerim.
