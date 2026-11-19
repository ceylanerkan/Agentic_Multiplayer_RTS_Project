# Sabah notları – 2026-11-19

## Dün gece ne yaptım
- `UNITY_FEEDBACK.md` 2026-11-18 işlendi: 3 obsolete uyarısı düzeltildi; TMP `NullReferenceException`'ın sebebi üretilen yazılarda fontun boş olmasıydı (`m_fontAsset: {fileID: 0}`), `RtsSetupTools` artık LiberationSans SDF atıyor. Yazılar bu yüzden görünmüyordu.
- Play artık hangi sahne açıksa açık olsun `Menu`'den başlıyor (`Editor/PlayFromMenu.cs`). Yeni komut: `Tools > RTS > Rebuild generated scenes and prefabs`.
- Okunurluk: yeni `UI/EntityVisuals.cs` her birim/binaya takım diski (mavi = benim, kırmızı = düşman) ve isim + HP etiketi, kaynaklara miktar etiketi koyuyor (prefab işi yok). Birimler daha büyük, kaynak şekilleri farklı, UI opak, fontlar ve butonlar büyük, "Raider Boat" gibi okunur isimler, host tekken "waiting for player 2...".
- Kontrol hataları: seçim halkası kapsülün içinde kalıyordu (artık ayakta); bina yerleştirme tıkı seçimi siliyordu; sağ tıkla iptal birimleri yürütüyordu; AI paneline yazarken WASD kamerayı kaydırıyordu; fare tekerleği bazı Input System ayarlarında zoom yapmıyor olabilirdi.
- İki bağımsız kod incelemesinden ~12 düzeltme: kendi yarım binaların fog altında görünmüyordu; P2'nin AI'ı çalışan Villager'ları boşta sanıyordu; aynı teknoloji iki kez ödenebiliyordu; ENEMY_SIGHTED spam'i; birimler TC'nin köşesinde doğuyordu; suyun ötesine saldırı emri birimi donduruyordu; yok olan seçili birim/inşaattan okuma hatası; AI paneli hata olunca sonsuza kadar "busy" kalıyordu; materyal sızıntıları.
- Testler: `tools/compile-check` 0 hata/0 uyarı, C# kural testleri 51/51 (4 yeni), `pytest ai evaluation` 64 geçti. Rapora "Lessons learned" maddesi eklendi.

## Plan durumu
Gece 1–15 bitti; Gece 16 (hata düzeltme) ve Gece 17 (tutarlılık: gizli anahtar yok, README/test sayıları güncel) kısmen bitti, son Unity geri bildirimini bekliyor (plan tarihlerinin ~8 gün önündeyiz). Sonraki gece: yeni `UNITY_FEEDBACK.md` notları, sonra kalan inceleme ve testler.

## Bugün test et
1. PR'ı merge et: https://github.com/ceylanerkan/Agentic_Multiplayer_RTS_Project/pull/4, sonra main'de `git pull`.
2. Unity'yi aç; Console'da kırmızı hata olmamalı. `Tools > RTS > Rebuild generated scenes and prefabs` > Rebuild. (Bu sahnelerde elle yaptığın değişiklik varsa kaybolur.) Bitince `Menu.unity`, `Game.unity`, `Assets/Prefabs` değişikliklerini main'e commit'le. `Window > General > Test Runner > EditMode > Run All`: 51 test geçmeli.
3. Game view'ın üstündeki çözünürlük menüsünden **Full HD (1920x1080)** seç (16:9). ParrelSync ile iki pencere için **1280x720** yeterli; UI pencereye göre ölçekleniyor. Windows build için `Tools > RTS > Set build window (1280x720, windowed)` çalıştır: iki pencere yan yana sığar (ProjectSettings değişir, main'e commit'le).
4. `Game` sahnesi açıkken Play'e bas: `Menu` açılmalı. Session adı yaz, Host. ParrelSync klonunda aynı adla Join. Beklenen: iki tarafta da TC + 3 Villager, altlarında mavi (kendi) / kırmızı (düşman) disk, üstlerinde isim + HP; ağaç/çalı/altın üstünde miktar; üst barda Wood/Food/Gold/Pop/Age okunur.
5. Fare tekerleğiyle zoom yap (çalışmalı); AI paneline "wasd" yaz: kamera kımıldamamalı.
6. Villager seç (ayağının altında yeşil halka görünmeli), ağaca sağ tık: ağacın etiketindeki sayı azalmalı, HUD'da Wood artmalı. House inşa et: yerleştirdikten sonra Villager seçili kalmalı, etikette "building %" görünmeli. Yerleştirirken sağ tık sadece iptal etmeli, birimleri yürütmemeli.
7. Sorun olursa `UNITY_FEEDBACK.md`'ye: Console hatası (olduğu gibi), adım numarasıyla `Test 4 tamam` / `Test 6 olmadı: ...`, mümkünse "hâlâ okunmuyor: X" gibi somut not (ekran görüntüsü yolu da yazabilirsin).

## Senin kararın gerekiyor
Yok.

## Gözden geçir
- Takım renkleri göreli: herkes kendini mavi, rakibini kırmızı görür (P1/P2 sabit renk yerine). Kaynak etiketleri açık sarı.
- Disk ve etiket nesnenin çocuğu değil, onu `LateUpdate`'te takip ediyor (binalar düzleştirilmiş küp; çocuk olsalardı esnerdi). Görünürlüğü gövdenin `Renderer`'ından kopyalıyor, böylece fog of war onları da saklıyor.
- Yeni birim ölçekleri (Villager 1.2 … Warship 2.4), kamera başlangıç yüksekliği 24, zemin rengi zeytin yeşili: hepsi `RtsSetupTools.cs` başındaki tablolarda, istediğin gibi değiştir.
- `Play from Menu scene` varsayılan açık (EditorPrefs); bir sahneyi tek başına denemek istersen menüden kapat.
- `ENEMY_SIGHTED` aynı düşman için ancak 10 s görüş dışında kaldıktan sonra tekrar yazılıyor (`FogOfWar.RearmSeconds`, veride yok).
- Suyun ötesindeki hedefe saldırı emri kıyıda takılırsa emir bırakılıyor (birim durur, sonra kendiliğinden saldırabilir).
- Birimler binanın yanında (çarpıştırıcı yarı boyu + 1.5 m, güneyde) doğuyor; başlangıç Villager'ları TC'nin önünde sıra halinde.
- `run_eval.py --out` artık klasör de kabul ediyor.

## Emin olmadığım yerler
- Bugünkü Unity scriptleri (`EntityVisuals`, `PlayFromMenu`, yeni menü komutları, `AiGatewayClient` 401 tekrarı) gerçek Unity'de derlenmedi (sadece stub'larla). Özellikle TMP'nin `outlineWidth`/`outlineColor` ayarı ve 3D `TextMeshPro`'nun font boyutu (birim 8, bina 11) ekranda çok küçük/büyük çıkabilir; söylersen ayarlarım.
- Rebuild sonrası prefab GUID'leri değişir; Fusion'ın Network Prefab Table'ı genelde kendini yeniler, yenilemezse Fusion menüsünden "Rebuild Prefab Table" çalıştır.
- Etiketler için `TMP_Settings.defaultFontAsset` kullanılıyor (TMP Settings `Resources` altında, build'e girer); boş gelirse etiketler görünmez, haber ver.
- `Tools > RTS > 1 Create placeholder prefabs` mevcut prefab'ların üstüne yazmadan önce artık soruyor: yazarsan mevcut sahnedeki `GameSpawner` referansları Missing olabilir; Rebuild güvenli yol.
- Gemini ile canlı çalıştırma hâlâ yapılmadı.

## Nasıl cevap verirsin
UNITY_FEEDBACK.md dosyasına bugünün tarihiyle bir başlık aç, cevapları yaz (ör. `Q-1114-1: B`,
`Test 2 tamam`, Console hataları) ve main'e push'la. Bir sonraki gece önce bunları işlerim.
