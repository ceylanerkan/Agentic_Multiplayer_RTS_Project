# Geri bildirim

Bu dosya senin. Gece çalışan ajan her gece bunu okur ama hiç değiştirmez. Yazdıktan sonra main'e commit ve
push yap; ajan bir sonraki gece önce bunları işler. Ajanın sana yazdıkları `MORNING.md` dosyasında.

Her seferinde tarihli bir başlık aç ve altına şunları yazabilirsin:
- Unity Console hataları (olduğu gibi yapıştır)
- Bulduğun hatalar ("Villager'lar ilk turdan sonra duruyor")
- Ajanın sorularına cevaplar (`Q-1114-1: B`)
- Test sonuçları (`Test 2 tamam`, `Test 3 olmadı: ...`)

Ajan işlediği başlıkları `docs/progress-log.md`'ye yazar. İşlenmiş başlıkları istediğin zaman silebilirsin.
Ajanın talimatları: [docs/autopilot.md](docs/autopilot.md).

<!-- Örnek:
## 2026-11-14
- Assets/Scripts/Networking/CommandRelay.cs(42,17): error CS0246: The type or namespace name 'Foo' could not be found
- Q-1113-1: A
- Test 2 tamam, Test 3 olmadı: Villager'lar ilk turdan sonra duruyor.
-->
11/11/2026
build_index + pytest tamam (35 passed)

## 2026-11-14
- Test 1 tamam: PR #2 merge edildi, main güncel.
- Test 2, derleme: CS hatası yok. `GameLauncher`, `MenuUI`, `AiGatewayClient` ve Networking scriptleri derlendi;
  Fusion/ILWeaver hatası da yok (private RPC'ler ve `NetworkString` parametreleri kabul edildi). Console'daki tek mesaj:
  `Script error (GameLauncher): Start() can not take parameters.`
  Sebep: `GameLauncher`'daki parametreli `Start(GameMode, string)` metodu. Main'de `StartSession` olarak yeniden
  adlandırıldı. Bundan sonra Unity'nin mesaj adlarını (`Start`, `Awake`, `Update`, `OnEnable`, `OnDestroy`...)
  parametreli kendi metodların için kullanma; mevcut scriptlerde başka örnek varsa düzelt.
- Test 2, RulesTests (EditMode): 13/13 geçti.
- Test 3 (Menu/Game sahneleri, ParrelSync ile Host/Join) ve 3b (prefab'lar, `GameSpawner`) henüz yapılmadı.
  Bu sahnelerin ve prefab'ların henüz var olmadığını varsay.