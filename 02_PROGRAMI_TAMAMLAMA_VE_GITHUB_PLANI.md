# 02 — Programı Tamamlama ve Sonra GitHub Hazırlama Planı

Bu planın ana kuralı: önce uygulama kullanılabilir public beta seviyesine getirilecek, sonra GitHub temizliği ve yayın hazırlığı yapılacak.

Codex bu sırayı bozmayacak.

---

## Ana hedef

Action Orbit, Windows’ta herhangi bir mouse ile kullanılabilecek, hotkey/macro tuşu ile açılan, uygulama bazlı radial action ring uygulaması olacak.

Konumlandırma:

> Windows için açık kaynak, uygulama bazlı, mouse-first action ring.

İlk public beta’da hedeflenen kullanıcı deneyimi:

1. Kullanıcı uygulamayı açar.
2. Ne işe yaradığını ilk ekranda anlar.
3. Hotkey/mouse macro mantığını görür.
4. Varsayılan profili test eder.
5. Aktif uygulamayı profile ekleyebilir.
6. Profil ve aksiyon düzenleyebilir.
7. Aksiyon ekleyebilir, test edebilir, silebilir, sıralayabilir.
8. Klasör/alt aksiyon mantığını anlayabilir.
9. Config/profil import-export yapabilir.
10. Uygulamayı tray’de arka planda çalıştırabilir.
11. Hata olursa çökme yerine anlaşılır uyarı görür.

---

# Faz 0 — Keşif ve güvenli başlangıç

Codex önce kodu inceleyecek ve kısa Türkçe rapor verecek.

Kontrol edilecekler:

- Proje build oluyor mu?
- .NET hedefi doğru mu?
- MainWindow açılıyor mu?
- Overlay açılıyor mu?
- Hotkey register oluyor mu?
- Config oluşuyor mu?
- Aksiyonlar çalışıyor mu?
- Tray davranışı çalışıyor mu?
- Hangi dosyalar riskli?

Bu fazda büyük değişiklik yapılmayacak.

Beklenen çıktı:

- Mevcut çalışan özellikler.
- Eksik UI parçaları.
- Kırık/riskli alanlar.
- İlk uygulanacak değişiklik listesi.

---

# Faz 1 — Ana ekranı ürün gibi tamamla

## 1.1 Üst bilgi alanı

Şu an üst bölümde başlık, hotkey rozetleri ve birkaç buton var. Burası daha anlaşılır olacak.

Eklenecekler:

- Kısa açıklama: “Mouse makro tuşuna bağladığın hotkey ile imlecin yanında action ring açılır.”
- Hotkey aktif/pasif durumu gerçek duruma göre renk değiştirsin.
- “Önizle” butonu daha belirgin olsun.
- “Arka planda çalıştır” butonu anlaşılır kalsın.
- Kaydet / Otomatik kaydedildi / Kaydedilmedi durumu net görünsün.

## 1.2 İlk kullanım rehberi

Yeni kullanıcı için küçük rehber kartı:

- 1. Mouse yazılımında makro tuşuna `Ctrl+Alt+Shift+R` ata.
- 2. Action Orbit’i arka planda çalıştır.
- 3. Her uygulama için farklı profil kullan.
- 4. Önizle ile menüyü test et.

Bu rehber çok yer kaplamasın, kapatılabilir olabilir.

## 1.3 Ayarlar bölümü

Mevcut ana ekran içinde bir ayarlar kartı veya tab bölümü eklenmeli.

Minimum ayarlar:

- Hotkey text/edit alanı.
- Hotkey kaydet/test et.
- Startup with Windows toggle.
- Kapatınca tray’e at toggle.
- Config dosyasını aç.
- Log dosyasını aç.
- Config klasörünü aç.

---

# Faz 2 — Hotkey ve mouse-first deneyimi

## 2.1 Hotkey editor

Eklenecekler:

- `HotkeyInput` veya basit text field + doğrulama.
- Destek örnekleri: `Ctrl+Alt+Shift+R`, `F13`, `F14`, `Ctrl+Space`, `Alt+Q`.
- Kaydet deyince config’e yaz, hotkey’i yeniden register et.
- Hata olursa eski hotkey’i bozmadan kullanıcıya bildir.

## 2.2 Mouse macro rehberi

V1’de doğrudan raw mouse button yakalama şart değil. Ama kullanıcıya şu açık anlatılmalı:

- Mouse yazılımında macro tuşuna Action Orbit hotkey’i atanır.
- Attack Shark, Logitech, Razer, SteelSeries gibi yazılımlarda mantık benzerdir.
- Uygulama her mouse ile çalışır çünkü mouse tuşu klavye hotkey’e map edilir.

## 2.3 İleri seviye opsiyonlar sonraya bırakılacak

Şimdilik yapılmayacaklar:

- Low-level mouse hook.
- Raw input ile ekstra mouse button yakalama.
- Driver seviyesinde entegrasyon.

---

# Faz 3 — Profil yönetimini tamamla

## 3.1 Profile listesi

Eklenecekler:

- Seçili profil daha belirgin gösterilsin.
- Default profil badge’i.
- Profil silerken onay.
- Son profil silinemiyor; bu uyarı UI’da güzel verilsin.

## 3.2 Process eşleşmeleri

Eklenecekler:

- “Aktif uygulamayı bu profile ekle” butonu.
- Aktif process adı chip olarak gösterilsin.
- Match listesi comma text alanı yerine tag/chip listesine dönüştürülebilir.
- Manuel text düzenleme yine kalabilir ama kullanıcı dostu hale getirilmeli.

## 3.3 Profil import/export

Eklenecekler:

- Seçili profili JSON dışa aktar.
- Profil JSON içe aktar.
- ID çakışmalarını güvenli çöz.
- Bozuk dosyada uygulama çökmesin.

---

# Faz 4 — Aksiyon editörünü tamamla

## 4.1 Empty state

Seçili aksiyon yoksa sağ panelde boş binding alanları görünmeyecek.

Gösterilecek mesaj:

- “Düzenlemek için soldan bir aksiyon seç veya yeni aksiyon ekle.”

## 4.2 Aksiyon alanları

Aksiyon tipine göre alanlar daha anlaşılır olacak.

- `open_app`: exe yolu + Gözat.
- `open_file`: dosya yolu + Gözat.
- `open_folder`: klasör yolu + Gözat.
- `open_url`: URL alanı + validation.
- `send_hotkey`: hotkey alanı + örnekler.
- `type_text`: metin alanı multiline olabilir.
- `run_command`: komut + argüman + güvenlik uyarısı.
- `folder`: target/arguments gizlenebilir, children odaklı açıklama gösterilir.

## 4.3 Aksiyon test etme

Eklenecek:

- “Aksiyonu test et” butonu.
- Test sonucu status/toast/log.
- Folder için test butonu “Önizlemede açılır” gibi davranabilir veya pasif olabilir.

## 4.4 Sıralama ve klasörleme

Mevcut yukarı/aşağı ve drag-to-folder korunacak.

İyileştirilecekler:

- Drag-to-folder ipucu düzgün Türkçe yazılsın.
- Klasör satırı daha net görünsün.
- Alt aksiyonlar indent ile daha okunur olsun.
- Geri alma zorunlu değil ama yanlış taşıma için “Klasörden çıkar” seçeneği eklenebilir.

---

# Faz 5 — Overlay’i public beta seviyesine getir

## 5.1 Profil ve folder bilgisi

Eklenecekler:

- Overlay’de aktif profil adı görünmeli.
- Folder açıldığında folder adı görünmeli.
- Center button açıklaması tooltip dışında da anlaşılır olmalı.
- Default profile toggle daha net olmalı.

## 5.2 Folder davranışı

Mevcut satellite child mantığı korunabilir.

Tamamlanacaklar:

- Açık folder vurgusu.
- Folder kapatma/geri dönme için net kontrol.
- 9’dan fazla child varsa gizleme yapılmayacak; en azından “+N daha” item’ı gösterilecek veya fallback liste yapılacak.

## 5.3 Görsel kalite

- Buton boyutları ve ring mesafesi config ile daha uyumlu olacak.
- Overlay ekran sınırında düzgün konumlanacak.
- Çok monitör ve DPI için mevcut mantık bozulmayacak.
- Animasyon varsa hafif ve stabil olacak; yoksa abartılmayacak.

---

# Faz 6 — Tema ve ayarlar

Minimum public beta ayarları:

- Accent color seçimi veya birkaç preset.
- Light/dark/system tema seçimi.
- Overlay button size.
- Ring radius ayarı.
- Startup with Windows.
- Close to tray.

Aşırı gelişmiş tema editörü yapılmayacak.

---

# Faz 7 — Stabilite ve hata yönetimi

Tamamlanacaklar:

- Config save atomic veya güvenli hale getirilmeli.
- Import/export hataları yakalanmalı.
- Aksiyon target validation eklenmeli.
- `run_command` için boş/tehlikeli/yanlış komut durumları ele alınmalı.
- `Process.Start` hataları kullanıcıya düzgün dönmeli.
- Loglama korunmalı.
- UI status mesajları Türkçe ve anlaşılır olmalı.

---

# Faz 8 — Düşük riskli refactor

Uygulama kullanılabilir hale geldikten sonra yapılacak.

Önerilen parçalama:

- `Views/ProfileEditorView.xaml`
- `Views/ActionEditorView.xaml`
- `Views/SettingsView.xaml`
- `Services/ProfileImportExportService.cs`
- `Services/StartupService.cs`
- `Services/HotkeyCaptureService.cs` veya hotkey validation helper

Refactor kuralları:

- Çalışan overlay/hotkey akışı kırılmayacak.
- Büyük mimari rewrite yapılmayacak.
- Her refactor sonrası build alınacak.

---

# Faz 9 — GitHub hazırlığı

Bu faz uygulama tamamlandıktan sonra yapılacak.

Yapılacaklar:

- `bin/`, `obj/`, `.vs/`, local logs, temp dosyaları temizle.
- `.gitignore` genişlet.
- Eski gereksiz md dosyalarını silinmiş varsay; sadece README ve bu 3 md kalabilir.
- README’yi en son güncelle.
- README public GitHub için profesyonel olsun.
- LICENSE ekle. Öneri: MIT.
- CONTRIBUTING.md ekle ama çok uzun olmasın.
- SECURITY.md gerekirse ekle.
- Issue template eklenebilir.
- Sample profiles klasörü eklenebilir.
- Release checklist ekle.

README en son yazılacak. Çünkü ürün tamamlanmadan README yazılırsa yanlış vaat verir.

---

## Yapılmayacaklar

İlk public beta için bunları yapma:

- Cloud sync.
- Hesap sistemi.
- Telemetry/analytics.
- Ücretli API entegrasyonu.
- Yapay AI action builder.
- Plugin marketplace.
- Otomatik update.
- Raw mouse hook zorlaması.
- Büyük mimari rewrite.

---

## Kabul kriterleri

Public beta hazır sayılması için:

- Proje build olacak.
- Uygulama açılacak.
- İlk çalıştırmada default config oluşacak.
- Hotkey ayarlanıp register edilebilecek.
- Önizle ile overlay açılacak.
- Aktif uygulamaya göre profil seçilecek.
- Profil oluşturma/düzenleme/silme çalışacak.
- Aksiyon ekleme/düzenleme/silme/sıralama çalışacak.
- Folder/alt menü çalışacak.
- En az 6 temel action tipi çalışacak.
- Import/export temel olarak çalışacak.
- Tray davranışı düzgün olacak.
- Hata durumunda uygulama çökmeden kullanıcıya bilgi verecek.
- Repo temizlenecek.
- README gerçeği anlatacak, olmayan şeyi vaat etmeyecek.
