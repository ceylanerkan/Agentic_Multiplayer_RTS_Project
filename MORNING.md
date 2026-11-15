# Sabah notları – 2026-11-15

## Dün gece ne yaptım
- Gece 3–12'nin tüm agent maddelerini yazdım (ZoneMap/BuildSlot/ResourceNode/gather, seçim + sağ tık komutları + kamera, üretim kuyruğu + inşaat + HUD, savaş + panel, teknolojiler + zafer ekranı, fog of war + olay günlüğü, AI snapshot + komutan paneli + onay akışı, siege/naval kuralları).
- Rapor taslağı hazır: `docs/report/report.md` (tüm bölümler; canlı sayı gereken yerler `[TODO live]`), `docs/architecture.md` ağ bölümü, `docs/demo-script.md`, `docs/slides-outline.md`, `docs/defense-notes.md`.
- Python: gerçek stdio MCP istemcisi (`MCP_TRANSPORT=stdio`), güvenlik testleri S10–S16, snapshot sözleşme testi, değerlendirmeye 5 yeni prompt + invalid-action oranı + CSV/tablo. `pytest ai evaluation`: 57 geçti.
- C# kural kodu .NET ile derlendi: 47/47 EditMode testi geçti. Unity'ye özel scriptler `tools/compile-check` (sahte Unity/Fusion tipleri) ile derlendi: 0 hata. Bu gerçek Unity derlemesi değil.
- Bulduğum hata: bina HP'si 0 okunuyordu (`buildings.json`'da `hp` üst seviyede); düzeltildi. `UNITY_FEEDBACK.md` 2026-11-14 notları işlendi (`Start` adı zaten main'de düzeltilmişti).

## Plan durumu
Gece 1–15'in agent maddeleri bitti (plan tarihlerinin yaklaşık 11 gün önünde; Gece 16–18 kaldı: hata düzeltme, tutarlılık, son çalışma). Sonraki gece: senin geri bildirimine göre düzeltmeler, sonra kod incelemesi/test eklemek.

## Bugün test et
1. PR'ı merge et: https://github.com/ceylanerkan/Agentic_Multiplayer_RTS_Project/pull/3 , sonra `git pull`.
2. Unity'yi aç, Console'daki derleme hatalarını `UNITY_FEEDBACK.md`'ye yapıştır (yeni scriptler hiç Unity'de derlenmedi; en olası: Fusion API ayrıntıları). EditMode testlerini çalıştır (47 beklenir).
3. `TODO.md` bölüm 1: `Assets/TutorialInfo` ve `Assets/Readme.asset`'i sil, sonra tek komut: `Tools > RTS > 0 Create Menu and Game scenes` (Menu + Game sahneleri, prefab'lar, harita, zone'lar, slot'lar, kaynaklar, `GameSpawner`, kamera, UI, Build Settings). Console'daki hataları yapıştır.
4. ParrelSync ile iki örnek: Host/Join, birimleri seç/taşı, ağaca sağ tık (odun artmalı), House inşa et, Villager üret.
5. Zaman kalırsa: Barracks + Spearman, düşman TC'yi yık (Victory/Defeat), fog of war. Sonuçları `UNITY_FEEDBACK.md`'ye `Test N tamam` / `Test N olmadı: ...` diye yaz.
6. Python tarafı (isteğe bağlı): `pip install -r ai/requirements.txt`, `pytest ai evaluation`.

## Senin kararın gerekiyor
Yok.

## Gözden geçir
- Saldırı bekleme süresi 1.0 s, Catapult 3.0 s (veride yok); menzil payı: birim +1.2, bina +3.0 (`GameConstants`).
- Harita düzeni (kara z < 45, kuzeyde deniz, üsler x = ±65) benim seçimim; menü komutu bunu kurar, istediğin gibi değiştir.
- Onayda geçerli aksiyonlar uygulanır, geçersizler atlanıp sebebi gösterilir (hepsi-ya-hiç değil).
- Karada/suda hareket: NavMesh yerine zone haritası kuralı (kara birimi su zone'una girmez, gemi kara zone'una girmez); birimler şimdilik düz çizgide yürür.
- Doğrulayıcıya yeni kural: düşman üssüne bina yapılamaz (`PlayerState.Slot`).
- Bir BUILD aksiyonu sadece zone söyler; host o zone'daki en yakın boş `BuildSlot`'u seçer.
- Olay günlüğü: host hangi olayı kimin bilebileceğine karar verir ve RPC ile sahibine yollar; her peer sadece kendi günlüğünü tutar.

## Emin olmadığım yerler
- `Assets/Scripts/Editor/RtsSetupTools.cs` (dört menü komutu) da derlenmedi; hata verirse Console çıktısını yapıştır, elle adımlar TODO.md'de duruyor.
- Yeni Unity/Fusion scriptleri gerçek Unity'de derlenmedi. Kullandığım Fusion üyelerinin hepsinin var olduğunu repodaki `Fusion.Runtime.xml` ile doğruladım (`NetworkId.IsValid/Raw`, `Runner.Despawn/Spawn/TryFindObject`, `NetworkTransform`, `IPlayerJoined`, `RpcTargets.InputAuthority`, `NetworkString` dönüşümü). Doğrulayamadığım: `[Rpc(StateAuthority → InputAuthority)]`'nin host kendi oyuncusuna gönderirken davranışı (host için RPC kullanmıyorum, doğrudan ekliyorum) ve Fusion'ın weaver'ının bazı kalıplara tepkisi.
- `Shader.Find("Universal Render Pipeline/Unlit")` build'de ayıklanabilir (halka, hayalet, fog için); gerekirse `Sprites/Default`'a düşer. Build'de sorun olursa haber ver.
- `FogOfWar` her 0.25 s'de 96×77 piksel boyar; yavaş gelirse `resolution`'ı düşür.
- Client'ta `BuildSlot.occupied` bilgisi eski olabilir (host doğru olanı kontrol eder); hayalet bazen yanlışlıkla yeşil olabilir.
- Gemini ile canlı çalıştırma hiç yapılmadı (`GeminiLLM` gerçek API'de test edilmedi).

## Nasıl cevap verirsin
UNITY_FEEDBACK.md dosyasına bugünün tarihiyle bir başlık aç, cevapları yaz (ör. `Q-1114-1: B`,
`Test 2 tamam`, Console hataları) ve main'e push'la. Bir sonraki gece önce bunları işlerim.
