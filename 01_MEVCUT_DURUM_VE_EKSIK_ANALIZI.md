# 01 — Mevcut Durum ve Eksik Analizi

Bu dosya Action Orbit projesinin mevcut kodları incelenerek hazırlanmıştır. Amaç önce uygulamayı gerçek kullanıcıya verilebilir seviyeye getirmek, sonra GitHub hazırlığını yapmaktır.

## Kısa karar

Proje sıfırdan yazılacak seviyede değil; çalışan bir temel var. Fakat şu an “GitHub public beta” için erken. Önce ürün akışı ve arayüz tamamlanmalı.

Mevcut kodda şunlar güçlü:

- WPF uygulama iskeleti hazır.
- `ConfigService`, `HotkeyService`, `ActiveWindowService`, `OverlayService`, `ActionExecutionService`, `LogService` gibi temel servisler ayrılmış.
- JSON config oluşturma, yükleme, bozuk config yedekleme ve last-good fallback var.
- `RegisterHotKey` ile global klavye kısayolu çalışıyor.
- Aktif pencere process adına göre profil seçme var.
- Overlay radial/oval menü var.
- Klasör/alt aksiyon mantığı kısmen var.
- Tray icon eklenmiş.
- Profil ve aksiyon düzenleme ekranı başlamış.
- Drag/drop ile aksiyonu klasöre taşıma başlamış.
- Icon catalog ve özel ikon içe aktarma başlamış.

Ama ürün hâlâ tamamlanmış değil. Aşağıdaki eksikler bitmeden GitHub hazırlığına geçilmemeli.

---

## Kritik ürün eksikleri

### 1. İlk açılış deneyimi eksik

Kullanıcı uygulamayı açınca ne yapacağını tam anlamıyor. İlk ekran “profile/action editor” gibi duruyor ama onboarding yok.

Tamamlanması gerekenler:

- İlk açılışta kısa “Bu uygulama ne yapar?” alanı.
- Varsayılan hotkey’in nasıl kullanılacağı açık görünmeli.
- Mouse makro tuşunu `Ctrl+Alt+Shift+R` veya `F13` gibi bir tuşa nasıl map edeceğini anlatan mini rehber.
- “Önizle”, “JSON Aç”, “Arka planda çalıştır” butonlarının ne işe yaradığını kullanıcı anlamalı.
- Config yolu ve log yolu daha düzenli bir ayarlar kartında gösterilmeli.

### 2. Hotkey ayarı sadece gösteriliyor, düzenlenemiyor

`HotkeyDisplay` UI’da gösteriliyor ama kullanıcı arayüzden değiştiremiyor. Public beta için en azından temel hotkey düzenleme gerekir.

Tamamlanması gerekenler:

- Hotkey editor alanı ekle.
- Kullanıcı `Ctrl+Alt+Shift+R`, `F13`, `F14`, `Ctrl+Space` gibi değerleri yazabilsin veya seçebilsin.
- Kaydedince config güncellensin ve hotkey yeniden register edilsin.
- Hotkey çakışırsa anlaşılır hata verilsin.
- Mouse-first ürün olduğu için “Mouse tuşunu bu hotkey’e map et” açıklaması eklenmeli.

Not: V1’de ham mouse button hook şart değil. Kullanıcı mouse yazılımından macro tuşunu hotkey’e map edebilir. Ama UI bunu net anlatmalı.

### 3. Uygulama bazlı profil eşleştirme akışı yarım

Process eşleşmeleri manuel text alanıyla düzenleniyor. Bu çalışır ama kullanıcı dostu değil.

Tamamlanması gerekenler:

- “Aktif uygulamayı bu profile ekle” butonu.
- Aktif process adı görünürken tek tıkla profile match olarak eklenmeli.
- Duplicate process adı eklenmemeli.
- Process adı örnekleri daha temiz gösterilmeli: `chrome.exe`, `Code.exe`, `explorer.exe`, `mstsc.exe`.
- Seçili profilin hangi uygulamalara bağlı olduğu chip/tag görünümünde gösterilebilir.

### 4. Aksiyon editörü tamamlanmamış hissi veriyor

Şu an aksiyon düzenleme yapılabiliyor ama bazı alanlar gizlenmiş veya akış eksik:

- İkon ve ID alanları grid’de `Width=0` ve `Visibility=Collapsed` ile saklanmış.
- Seçili aksiyon yokken sağ panel boş/garip durabilir.
- Aksiyon hedefi için browse/test/validation yok.
- Aksiyon tipine göre alanlar yeterince değişmiyor.

Tamamlanması gerekenler:

- Seçili aksiyon yoksa düzgün empty state göster.
- `open_app`, `open_file`, `open_folder` için “Gözat” butonu ekle.
- `open_url` için URL validation ekle.
- `send_hotkey` için örnekler ve validation ekle.
- `run_command` için risk uyarısı ve daha açık açıklama ekle.
- “Bu aksiyonu test et” butonu ekle.
- Aksiyon silme ve profil silme için confirmation ekle.
- Aksiyon türü değişince gereksiz alanlar gizlenmeli veya açıklaması değişmeli.

### 5. Import/export akışı eksik

Product hedefinde profil paylaşımı var ama UI’da profil import/export yok.

Tamamlanması gerekenler:

- Tüm config’i dışa aktar.
- Tüm config’i içe aktar.
- Seçili profili dışa aktar.
- Profil JSON içe aktar.
- İçe aktarılan profil ID çakışırsa otomatik benzersiz ID ver.
- Bozuk JSON’da uygulama çökmesin, kullanıcıya anlaşılır hata versin.

### 6. Overlay bilgisi eksik

`OverlayViewModel` içinde `ProfileName`, `SelectedFolderTitle`, `HasSatellites` gibi alanlar var ama XAML’de görünür şekilde kullanılmıyor.

Tamamlanması gerekenler:

- Overlay merkezinde veya küçük bir rozet içinde aktif profil adı görünmeli.
- Folder açıldığında folder adı/breadcrumb görünmeli.
- Folder açıkken geri/kapat davranışı kullanıcıya anlaşılır olmalı.
- Center button’ın “varsayılan profile geç / geri dön” anlamı daha net olmalı.
- Tooltip tek başına yeterli değil.

### 7. Folder/alt menü davranışı public beta için yeterince net değil

Folder tıklanınca children “satellite” olarak açılıyor. Fikir güzel ama kullanıcı hangi klasörde olduğunu ve nasıl geri döneceğini göremeyebilir.

Tamamlanması gerekenler:

- Açık folder vurgusu daha net olmalı.
- Folder kapatma/geri dönme davranışı görünür olmalı.
- 9’dan fazla child varsa şu an `Take(9)` ile fazlası gizleniyor. Bu tehlikeli.
- 9+ child için “+N” göstergesi, sayfalama veya liste fallback eklenmeli.

### 8. Tema sistemi config’te var ama UI’da tam uygulanmıyor

`ThemeConfig.Mode`, `Accent`, `ButtonSize`, `RadiusX`, `RadiusY` var. Fakat:

- MainWindow sabit light tema.
- Overlay çoğunlukla hardcoded renkler kullanıyor.
- ButtonSize ve radius değerleri clamp ile çok dar aralığa sıkıştırılmış.
- Ayarlar ekranından tema değişmiyor.

Tamamlanması gerekenler:

- Basit tema ayarı ekle: light/dark/system olabilir.
- Accent color ayarı ekle.
- Overlay buton boyutu ve radius ayarları UI’dan değiştirilebilsin.
- MainWindow ve Overlay görsel dili tutarlı olsun.
- İlk sürümde aşırı tema editörü gerekmez; temiz ve stabil temel ayar yeterli.

### 9. Tray var ama ayarlar eksik

Tray icon eklenmiş, iyi. Ama kullanıcı açısından tamamlanması gerekenler var:

- Windows başlangıcında çalıştır toggle’ı.
- Minimize/close davranışı ayarı: kapatınca tray’e at / tamamen kapat.
- Tray menüsünde “Kısayol aktif mi?” bilgisi veya kısa status.
- İlk kapatmada balon gösterimi tamam ama bunu tekrar tekrar göstermemek için ayar eklenebilir.

### 10. Hata geri bildirimi sadece status/log seviyesinde kalıyor

Action çalışmazsa kullanıcı genelde log’a bakmaz. Public beta’da en azından küçük UI feedback gerekir.

Tamamlanması gerekenler:

- Aksiyon çalıştırma sonucu başarısızsa toast/status göster.
- Config validation hataları kullanıcıya açık yazılsın.
- Import/export hataları açıklayıcı olsun.
- `run_command`, `open_app`, `open_file`, `open_folder` hedef bulunamazsa net hata versin.

### 11. Kod yapısında büyüyen dosyalar var

Şu dosyalar fazla büyümüş:

- `MainWindow.xaml` yaklaşık 750 satır.
- `MainWindowViewModel.cs` yaklaşık 850 satır.
- `IconCatalog.cs` yaklaşık 980 satır.

Tamamlanması gerekenler:

- Önce ürünü kırmadan tamamla.
- Sonra düşük riskli refactor yap.
- Profile editor, action editor, settings/onboarding bölümleri ayrı UserControl olabilir.
- `MainWindowViewModel` içinden import/export, hotkey settings, profile editor mantığı parçalara ayrılabilir.
- Refactor, UI tamamlandıktan sonra yapılmalı.

### 12. Repo temizliği gerekli ama son aşama

Zip içinde `bin/` ve `obj/` build çıktıları var. `.gitignore` mevcut ama build çıktıları repo paketine girmiş.

GitHub hazırlığında yapılacaklar:

- `bin/`, `obj/`, `.vs/`, log, temp, local config çıktıları temizlenecek.
- `.gitignore` genişletilecek.
- Eski gereksiz md dosyaları silinecek.
- README en son güncellenecek.
- License, contributing, issue template, release checklist en son eklenecek.

Ama bunlar uygulama tamamlanmadan yapılmayacak.
