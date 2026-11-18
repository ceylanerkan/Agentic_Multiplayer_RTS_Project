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

## 2026-11-18
- Test 2 (PR #3 merge edildikten sonra Unity derlemesi), Console çıktısı:

```
Assets\Tests\EditMode\RulesTests.cs(370,27): error CS0103: The name 'Newtonsoft' does not exist in the current context

Assets\Tests\EditMode\RulesTests.cs(370,77): error CS0103: The name 'Newtonsoft' does not exist in the current context

Assets\Scripts\Buildings\ZoneMap.cs(19,57): warning CS0618: 'FindObjectsSortMode' is obsolete: 'FindObjectsSortMode has been deprecated. Use the FindObjectsByType overloads that do not take a FindObjectsSortMode parameter.'

Assets\Scripts\Buildings\ZoneMap.cs(19,27): warning CS0618: 'Object.FindObjectsByType<T>(FindObjectsSortMode)' is obsolete: 'FindObjectsByType with FindObjectsSortMode parameter has been deprecated. Use FindObjectsByType<T>() or FindObjectsByType<T>(FindObjectsInactive) instead. InstanceID will be replaced in the future with EntityId and previous sort order cannot be maintained.'

Assets\Scripts\Editor\RtsSetupTools.cs(203,13): warning CS0618: 'Object.FindFirstObjectByType<T>()' is obsolete: 'FindFirstObjectByType has been deprecated because it relies on instance ID ordering. Use FindAnyObjectByType instead, which does not depend on ordering.'

(GetStatus) Cannot get non-existing progress id 8.
```
- Newtonsoft hatası main'de düzeltildi: `RTS.Tests.EditMode.asmdef`'te `overrideReferences: true` olduğu için
  `precompiledReferences`'a `"Newtonsoft.Json.dll"` eklendi (paket asmdef değil, hazır DLL). Bundan sonra
  `overrideReferences: true` olan asmdef'lerde hazır DLL'leri `precompiledReferences`'a yaz. CS0618 uyarıları
  (`FindObjectsSortMode`, `FindFirstObjectByType`) hâlâ duruyor, sen düzelt.