# Sabah notları – 2026-11-12

## Dün gece ne yaptım
- .NET SDK kuruldu (`apt-get install dotnet-sdk-8.0`; dot.net scripti engelli). `RTS.Core` ve `RulesTests` derlendi: hata yok, 12/12 test geçti.
- `GameDataProvider`: knowledge JSON'ları editörde repodan, build'de `StreamingAssets/knowledge` içinden okur (`KnowledgeBuildCopy` build öncesi kopyalar, git'e girmez).
- `GameLauncher` (Host/Join, `GameMode.Host/Client`, Game sahnesi yükleme, rakip ayrılınca menüye dönüş) ve `MenuUI`.
- `PlayerSlots`: en düşük id P1, sonraki P2 (host ve client aynı sonucu bulur) + 2 test.
- Gece 2 kodu da yazıldı: `NetworkPlayerState`, `NetworkUnit`, `NetworkBuilding`, `CommandRelay` (host'ta validator kontrolü), `GameSpawner` (TC + 3 Villager), `GameConstants`.
- `UNITY_FEEDBACK.md` "11/11/2026" notu işlendi.

## Plan durumu
Gece 1 ve Gece 2'nin agent maddeleri bitti (1 gün önde). Sonraki: Gece 3 (`ZoneMap`, `ResourceNode`, nüfus).

## Bugün test et
1. Autopilot PR'ını GitHub'da merge et, sonra `git pull`.
2. Unity'de Console'da derleme hatası var mı bak (özellikle `GameLauncher.cs`, `MenuUI.cs`, `AiGatewayClient.cs`). `RulesTests` EditMode testlerini çalıştır.
3. TODO.md'deki **[You]** "Menu/Game scenes" adımlarını yap (sahneler, Build Profiles, Launcher, Canvas). ParrelSync ile birisi Host, diğeri Join: ikisi de `Game` sahnesine geçmeli; clone'u kapatınca diğeri `Menu`ye dönmeli.
3b. Gece 2 prefab adımları TODO.md'de (Player, Villager, TownCenter prefab'ları + `GameSpawner`); hepsi önce Menu/Game adımına bağlı.
4. Sorun olursa Console çıktısını UNITY_FEEDBACK.md'ye bugünün başlığıyla yaz.
5. Hocaya tek kişilik takım e-postasını henüz atmadıysan at.

## Senin kararın gerekiyor
Yok.

## Gözden geçir
- Script klasörleri asmdef'siz (`Assembly-CSharp`); yeni `Data/`, `Editor/` klasörleri açıldı.
- Oyuncu slotu `PlayerRef.RawEncoded` sırasından türetilir; ağ üzerinden mesaj gerekmez.

## Emin olmadığım yerler
- `GameLauncher` Fusion 2.0.13 API'lerini (`StartGameArgs`, `INetworkRunnerCallbacks` imzaları, `runner.Shutdown()` awaitable) FusionBootstrap'tan kopyalayarak yazdım; Unity'de derlenmedi.
- RPC metodları private + `NetworkString` parametreli; Fusion bunu kabul etmezse Console'da hata görürsün. `PlayerState` adı Core'da çakıştığı için ağ sınıfı `NetworkPlayerState`.
- `AiGatewayClient.cs` hâlâ derlenmedi (UnityWebRequest gerekiyor).

## Nasıl cevap verirsin
UNITY_FEEDBACK.md dosyasına bugünün tarihiyle bir başlık aç, cevapları yaz (ör. `Q-1114-1: B`,
`Test 2 tamam`, Console hataları) ve main'e push'la. Bir sonraki gece önce bunları işlerim.
