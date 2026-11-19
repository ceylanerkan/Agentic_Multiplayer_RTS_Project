# Sabah notları – 2026-11-19

## Dün gece ne yaptım
- `UNITY_FEEDBACK.md` 2026-11-18 başlığını işledim: 3 obsolete uyarısı düzeltildi (`FindObjectsByType<T>()`, `FindAnyObjectByType`).
- TMP `NullReferenceException`'ın sebebi: üretilen bütün TMP yazılarında font boştu (`Game.unity`'de `m_fontAsset: {fileID: 0}`). `RtsSetupTools` artık yazıdan önce LiberationSans SDF fontunu atıyor. Yazılar bu yüzden de görünmüyordu.
- Play artık hangi sahne açık olursa olsun `Menu`'den başlıyor (`Editor/PlayFromMenu.cs`, `EditorSceneManager.playModeStartScene`; kapatmak için `Tools > RTS > Play from Menu scene`).
- Ayırt edilebilirlik: yeni `UI/EntityVisuals.cs` her birimin/binanın altına takım diski (mavi = benim, kırmızı = düşman) ve üstüne isim etiketi koyuyor ("Villager 25/25", "House building 40%", "Wood 180"). Çalışma zamanında ekleniyor, prefab'larda elle iş yok. Fog of war etiketleri de gizliyor.
- Birimler büyütüldü, kaynaklar farklı şekiller aldı (uzun koyu yeşil ağaç, pembe çalı, geniş sarı altın kubbesi), UI panelleri daha opak, fontlar büyük.
- Yeni komut: `Tools > RTS > Rebuild generated scenes and prefabs` (Menu, Game ve `Assets/Prefabs`'ı silip yeniden üretir). `tools/compile-check`: 0 hata, 0 uyarı.

## Plan durumu
Gece 1–15 bitti; Gece 16 (hata düzeltme günü) senin geri bildirimlerinle sürüyor (plan tarihlerinin ~8 gün önündeyiz). Sonraki gece: yeni `UNITY_FEEDBACK.md` notları, sonra kod incelemesi ve eksik testler.

## Bugün test et
1. PR'ı merge et: https://github.com/ceylanerkan/Agentic_Multiplayer_RTS_Project/pulls (başlık "Autopilot: daily progress"), sonra main'de `git pull`.
2. Unity'yi aç; Console'da kırmızı hata olmamalı. `Tools > RTS > Rebuild generated scenes and prefabs` > Rebuild. (Bu sahnelerde elle yaptığın değişiklik varsa kaybolur.) Bitince `Menu.unity`, `Game.unity`, `Assets/Prefabs` değişikliklerini main'e commit'le.
3. Game view'ın üstündeki çözünürlük menüsünden **Full HD (1920x1080)** seç (16:9). ParrelSync ile iki pencere için **1280x720** yeterli; UI pencereye göre ölçekleniyor. Windows build'de de 1920x1080 hedef.
4. `Game` sahnesi açıkken Play'e bas: `Menu` açılmalı. Session adı yaz, Host. ParrelSync klonunda aynı adla Join. Beklenen: iki tarafta da TC + 3 Villager, altlarında mavi (kendi) / kırmızı (düşman) disk, üstlerinde isim + HP; ağaç/çalı/altın üstünde miktar; üst barda Wood/Food/Gold/Pop/Age okunur.
5. Villager seç, ağaca sağ tık: ağacın etiketindeki sayı azalmalı, HUD'da Wood artmalı. House inşa et: etikette "building %" görünmeli.
6. Sorun olursa `UNITY_FEEDBACK.md`'ye: Console hatası (olduğu gibi), `Test 5 tamam` / `Test 5 olmadı: ...`, mümkünse "hâlâ okunmuyor: X" gibi somut not (ekran görüntüsü yolu da yazabilirsin).

## Senin kararın gerekiyor
Yok.

## Gözden geçir
- Takım renkleri göreli: herkes kendini mavi, rakibini kırmızı görür (P1/P2 sabit renk yerine). Kaynak etiketleri açık sarı.
- Disk ve etiket nesnenin çocuğu değil, onu `LateUpdate`'te takip ediyor (binalar düzleştirilmiş küp; çocuk olsalardı esnerdi). Görünürlüğü gövdenin `Renderer`'ından kopyalıyor, böylece fog of war onları da saklıyor.
- Yeni birim ölçekleri (Villager 1.2 … Warship 2.4), kamera başlangıç yüksekliği 24, zemin rengi zeytin yeşili: hepsi `RtsSetupTools.cs` başındaki tablolarda, istediğin gibi değiştir.
- `Play from Menu scene` varsayılan açık (EditorPrefs); bir sahneyi tek başına denemek istersen menüden kapat.

## Emin olmadığım yerler
- `EntityVisuals.cs` ve `PlayFromMenu.cs` gerçek Unity'de derlenmedi (sadece stub'larla). Özellikle TMP'nin `outlineWidth`/`outlineColor` ayarı ve 3D `TextMeshPro`'nun font boyutu (6–9) ekranda çok küçük/büyük çıkabilir; söylersen ayarlarım.
- Rebuild sonrası prefab GUID'leri değişir; Fusion'ın Network Prefab Table'ı genelde kendini yeniler, yenilemezse Fusion menüsünden "Rebuild Prefab Table" çalıştır.
- Etiketler için `TMP_Settings.defaultFontAsset` kullanılıyor (TMP Settings `Resources` altında, build'e girer); boş gelirse etiketler görünmez, haber ver.
- Gemini ile canlı çalıştırma hâlâ yapılmadı.

## Nasıl cevap verirsin
UNITY_FEEDBACK.md dosyasına bugünün tarihiyle bir başlık aç, cevapları yaz (ör. `Q-1114-1: B`,
`Test 2 tamam`, Console hataları) ve main'e push'la. Bir sonraki gece önce bunları işlerim.
