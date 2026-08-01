# Changelog

Bu projedeki önemli değişiklikler bu dosyada belgelenir. Sürümler
[Semantic Versioning](https://semver.org/) düzenini izler.

## [2.0.0] - 2026-08-01

### Eklendi

- Chrome ve diğer uygulamalardan görsel, dosya, URL ve metin alabilen Orbit Shelf.
- Çoklu raf, yeniden adlandırma, sabitleme, isteğe bağlı yakın geçmiş ve geçici önbellek temizliği.
- Raf öğesini panoya kopyalama, dışarı sürükleme, PNG dönüştürme ve 1600 px küçültme.
- Profil başına adlandırılmış ek halka setleri ve mouse tekerleğiyle halka değiştirme.
- Bas-aç, basılı-tut ve çift-bas tetikleme modları ile uygulama devre dışı bırakma listesi.
- URL aksiyonlarında tarayıcı seçimi ve aksiyonlara doğrudan global hotkey atama.

### Güvenlik

- Uzak raf görsellerinde protokol, yönlendirme, MIME, imza, dosya boyutu ve piksel sınırları.
- SSRF'e karşı yerel/özel/link-local/multicast/IPv4-mapped adres engeli ve bağlantı anında DNS doğrulaması.
- Sürüklenen çalıştırılabilir dosyalar otomatik çalıştırılmadan yalnızca veri olarak tutulur.
- Pro config, log, raf verisi, mutex ve başlangıç kaydı klasik sürümden ayrıldı.

### Düzeltildi

- Yeni raf görünümünün ana pencere açılışında oluşturduğu WPF kaynak çözümleme hatası.
- Klasik sürümle varsayılan hotkey çakışması; Pro varsayılanı `Ctrl+Alt+Shift+P` oldu.
- Chrome'un `data:image/...;base64,...` olarak verdiği sürüklenen görsellerin düz metin yerine gerçek görsel olarak alınması.
- Bozuk veya aşırı büyük inline görsel verisinin ham base64 metni olarak rafa eklenmesi engellendi.
- Yüzen raf, ana çalışma alanını tekrar kullanmak yerine kendi stilleri olan kompakt ve sağ kenarda açılan bir pencere olarak yeniden tasarlandı.

## [1.0.1] - 2026-07-30

### Güvenlik

- Self-contained Windows paketindeki .NET runtime 10.0.10'a güncellendi.
- Komut aksiyonları varsayılan olarak kapatıldı ve her çalıştırma için açık onay eklendi.
- Shell yorumlayıcılarının `open_app` argümanlarıyla komut filtresini dolaşması engellendi.
- Çalıştırılabilir/betik dosyalarının `open_file` üzerinden açılması engellendi.
- Config ve profil içe aktarmaya risk özeti, maksimum boyut, profil, aksiyon ve derinlik sınırları eklendi.
- Mutlak ve UNC ikon yolları engellendi; SVG/raster ikon boyut ve karmaşıklık sınırları eklendi.
- Komut içerikleri ve kontrol karakterleri loglardan çıkarıldı.
- CodeQL, dependency review, kilitli NuGet restore, SPDX SBOM ve GitHub provenance attestation eklendi.
- GitHub Actions bağımlılıkları tam commit SHA değerlerine sabitlendi.

### Değiştirildi

- Test altyapısı xUnit v3, Microsoft.NET.Test.Sdk 18.8.1 ve coverlet 10.0.1'e güncellendi.
- SDK 10.0.302, runtime 10.0.10 ve paket lock dosyalarıyla deterministik build sağlandı.

## [1.0.0] - 2026-07-24

### Eklendi

- Global hotkey ile imleç yanında açılan radial aksiyon menüsü.
- Uygulama bazlı profiller, varsayılan profil ve çalışan uygulama eşleştirmesi.
- Klasör/alt menü, kayıpsız sayfalama ve klavye navigasyonu.
- Program, dosya, klasör ve URL açma; hotkey, metin ve komut aksiyonları.
- Ana Sayfa, Ring Editörü, Aksiyon Kütüphanesi ve Ayarlar çalışma alanları.
- Canlı ring önizlemesi, sürükle-bırak sıralama ve tek adımlı geri alma.
- Açık, koyu ve sistem temaları.
- Autosave, config içe/dışa aktarma ve bozuk config kurtarma.
- Tek uygulama örneği, hotkey rollback ve komut güvenlik kontrolleri.
- Windows x64 self-contained yayın paketi ve GitHub Actions CI hattı.

### Düzeltildi

- Başarılı aksiyonlardan sonra hatalı sistem bildirimi gösterilmesi.
- Hover/focus kontrastı ve ring üzerindeki gereksiz sıra numaraları.
- Aktif profil sorgularında tekrarlı çözümleme ve log büyümesi.
- Büyük log dosyaları için 5 MB rotasyon ve sınırlı arşiv saklama.

[1.0.1]: https://github.com/SabahattinAkal/action-orbit/releases/tag/v1.0.1
[1.0.0]: https://github.com/SabahattinAkal/action-orbit/releases/tag/v1.0.0
[2.0.0]: https://github.com/SabahattinAkal/action-orbit/releases/tag/v2.0.0
