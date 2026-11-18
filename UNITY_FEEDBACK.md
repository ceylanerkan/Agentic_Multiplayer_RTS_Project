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
- Bu uyarıları düzelt (PR #3 sonrası Unity derlemesi):

```
Assets\Scripts\Buildings\ZoneMap.cs(19,57): warning CS0618: 'FindObjectsSortMode' is obsolete: 'FindObjectsSortMode has been deprecated. Use the FindObjectsByType overloads that do not take a FindObjectsSortMode parameter.'

Assets\Scripts\Buildings\ZoneMap.cs(19,27): warning CS0618: 'Object.FindObjectsByType<T>(FindObjectsSortMode)' is obsolete: 'FindObjectsByType with FindObjectsSortMode parameter has been deprecated. Use FindObjectsByType<T>() or FindObjectsByType<T>(FindObjectsInactive) instead. InstanceID will be replaced in the future with EntityId and previous sort order cannot be maintained.'

Assets\Scripts\Editor\RtsSetupTools.cs(203,13): warning CS0618: 'Object.FindFirstObjectByType<T>()' is obsolete: 'FindFirstObjectByType has been deprecated because it relies on instance ID ordering. Use FindAnyObjectByType instead, which does not depend on ordering.'
```
- Test 3 (`Tools > RTS > 0 Create Menu and Game scenes`): Console'da aynı hata 2 kez çıktı, düzelt:

```
NullReferenceException: Object reference not set to an instance of an object
TMPro.MaterialReference..ctor (System.Int32 index, TMPro.TMP_FontAsset fontAsset, TMPro.TMP_SpriteAsset spriteAsset, UnityEngine.Material material, System.Single padding) (at ./Library/PackageCache/com.unity.ugui@1f2d1ab0d950/Runtime/TMP/MaterialReferenceManager.cs:526)
TMPro.TextMeshProUGUI.SetArraySizes (TMPro.TMP_Text+TextProcessingElement[] textProcessingArray) (at ./Library/PackageCache/com.unity.ugui@1f2d1ab0d950/Runtime/TMP/TextMeshProUGUI.cs:1758)
TMPro.TMP_Text.ParseInputText () (at ./Library/PackageCache/com.unity.ugui@1f2d1ab0d950/Runtime/TMP/TMP_Text.cs:2048)
TMPro.TMP_Text.GetPreferredWidth () (at ./Library/PackageCache/com.unity.ugui@1f2d1ab0d950/Runtime/TMP/TMP_Text.cs:3699)
TMPro.TMP_Text.get_preferredWidth () (at ./Library/PackageCache/com.unity.ugui@1f2d1ab0d950/Runtime/TMP/TMP_Text.cs:1511)
UnityEngine.UI.LayoutUtility+<>c.<GetPreferredWidth>b__4_1 (UnityEngine.UI.ILayoutElement e) (at ./Library/PackageCache/com.unity.ugui@1f2d1ab0d950/Runtime/UGUI/UI/Core/Layout/LayoutUtility.cs:68)
UnityEngine.UI.LayoutUtility.GetLayoutProperty (UnityEngine.RectTransform rect, System.Func`2[T,TResult] property, System.Single defaultValue, UnityEngine.UI.ILayoutElement& source) (at ./Library/PackageCache/com.unity.ugui@1f2d1ab0d950/Runtime/UGUI/UI/Core/Layout/LayoutUtility.cs:161)
UnityEngine.UI.LayoutUtility.GetLayoutProperty (UnityEngine.RectTransform rect, System.Func`2[T,TResult] property, System.Single defaultValue) (at ./Library/PackageCache/com.unity.ugui@1f2d1ab0d950/Runtime/UGUI/UI/Core/Layout/LayoutUtility.cs:129)
UnityEngine.UI.LayoutUtility.GetPreferredWidth (UnityEngine.RectTransform rect) (at ./Library/PackageCache/com.unity.ugui@1f2d1ab0d950/Runtime/UGUI/UI/Core/Layout/LayoutUtility.cs:68)
UnityEngine.UI.LayoutUtility.GetPreferredSize (UnityEngine.RectTransform rect, System.Int32 axis) (at ./Library/PackageCache/com.unity.ugui@1f2d1ab0d950/Runtime/UGUI/UI/Core/Layout/LayoutUtility.cs:31)
UnityEngine.UI.HorizontalOrVerticalLayoutGroup.GetChildSizes (UnityEngine.RectTransform child, System.Int32 axis, System.Boolean controlSize, System.Boolean childForceExpand, System.Single& min, System.Single& preferred, System.Single& flexible) (at ./Library/PackageCache/com.unity.ugui@1f2d1ab0d950/Runtime/UGUI/UI/Core/Layout/HorizontalOrVerticalLayoutGroup.cs:233)
UnityEngine.UI.HorizontalOrVerticalLayoutGroup.CalcAlongAxis (System.Int32 axis, System.Boolean isVertical) (at ./Library/PackageCache/com.unity.ugui@1f2d1ab0d950/Runtime/UGUI/UI/Core/Layout/HorizontalOrVerticalLayoutGroup.cs:104)
UnityEngine.UI.HorizontalLayoutGroup.CalculateLayoutInputHorizontal () (at ./Library/PackageCache/com.unity.ugui@1f2d1ab0d950/Runtime/UGUI/UI/Core/Layout/HorizontalLayoutGroup.cs:19)
UnityEngine.UI.LayoutRebuilder+<>c.<Rebuild>b__12_0 (UnityEngine.Component e) (at ./Library/PackageCache/com.unity.ugui@1f2d1ab0d950/Runtime/UGUI/UI/Core/Layout/LayoutRebuilder.cs:85)
UnityEngine.UI.LayoutRebuilder.PerformLayoutCalculation (UnityEngine.RectTransform rect, UnityEngine.Events.UnityAction`1[T0] action) (at ./Library/PackageCache/com.unity.ugui@1f2d1ab0d950/Runtime/UGUI/UI/Core/Layout/LayoutRebuilder.cs:160)
UnityEngine.UI.LayoutRebuilder.Rebuild (UnityEngine.UI.CanvasUpdate executing) (at ./Library/PackageCache/com.unity.ugui@1f2d1ab0d950/Runtime/UGUI/UI/Core/Layout/LayoutRebuilder.cs:85)
UnityEngine.UI.CanvasUpdateRegistry.PerformUpdate () (at ./Library/PackageCache/com.unity.ugui@1f2d1ab0d950/Runtime/UGUI/UI/Core/CanvasUpdateRegistry.cs:181)
UnityEngine.Canvas:SendWillRenderCanvases()
```
  İpucu: projede `HorizontalLayoutGroup`'u sadece `RtsSetupTools.cs:208`'deki `HudBar` kullanıyor; içindeki 5 TMP
  `Label()` ile yaratılıyor ve font/material açıkça atanmıyor. TMP Essential Resources projede var
  (`Assets/TextMesh Pro`, varsayılan font LiberationSans SDF).
- Test 4 olmadı:
  - Oyun açılıyor ama başlamıyor.
  - Grafikler çok kötü, neyin ne olduğu belli olmuyor.
  - Yazılar da aynı şekilde sorunlu.
  - Hangi en-boy oranında (aspect) açılması gerektiği belli değil; her şey küçük görünüyor.
  - İstek: oyunun başlamamasını düzelt. Hedef çözünürlüğü/aspect'i belirle ve Game view'da ne seçmem gerektiğini
    `MORNING.md`'ye yaz. Birimler, binalar ve kaynaklar birbirinden kolayca ayırt edilsin; yazılar okunur olsun.