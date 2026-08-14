# Changelog

Bu projedeki önemli değişiklikler bu dosyada belgelenir. Sürümler
[Semantic Versioning](https://semver.org/) düzenini izler.

## [Unreleased]

### Değiştirildi

- Public belgeler güncel uygulama davranışını anlatacak şekilde sadeleştirildi; eski iç geliştirme notları depodan kaldırıldı.

### Düzeltildi

- Bildirim alanındaki `Çıkış` komutunun açık tray menüsünü dispose ederek ekranda bırakması ve Orbit Link ağ görevlerini UI iş parçacığında bekleyerek menüyü dondurması düzeltildi; menü önce kapatılıyor, dinleyici anında durdurulurken kalan temizlik arka planda tamamlanıyor.
- Tailscale IPv4 adreslerinin çift-mod ağ dinleyicisinde `::ffff:100.x.x.x` biçiminde kaydedilip sonraki Shelf gönderiminde Windows soketi tarafından reddedilmesi düzeltildi; mevcut eşleşmeler açılışta otomatik normalleştiriliyor.
- Orbit Link ayarlarındaki ana düğme stilinin ayrı WPF görünümünde çözülememesi nedeniyle uygulamanın pencere ve tray ikonu oluşturmadan arka planda kalması düzeltildi; başlangıç UI hataları artık görünür hata verip uygulamayı kapatıyor.
- Çalışan eski bir Action Orbit örneği pencereyi geri getirme isteğine yanıt vermediğinde yeni sürümün sessizce kapanması engellendi.
- Açık klasör zaten aynı klasöre yeniden tıklanarak kapatılabildiği için overlay bilgi kartındaki tekrarlı `Geri` düğmesi kaldırıldı ve panel yüksekliği sıkılaştırıldı; klavye ile geri gezinme korunuyor.
- Ring Editörü, Aksiyon Kütüphanesi ve Ayarlar çalışma alanları opak ve kırpılmış yüzeylere dönüştürüldü; Ana Sayfa içeriğinin aktif ekranın arkasından görünmesi engellendi.
- Orbit Shelf ana navigasyondan kaldırıldı; yüzen rafa üst hızlı erişim, overlay ve bildirim alanı menüsünden ulaşılmaya devam edilirken Ayarlar kısayolu `Ctrl+4` oldu.
- Ana ve klasör halkalarında sayfa değiştiren yapay aksiyon kaldırıldı; sayfalama, yalnızca gerektiğinde merkez düğmenin iki yanında beliren geri/ileri kontrollerine taşındı.
- Parola üreticinin koyu temadaki düşük kontrastlı çıktı ve seçenek alanları yüksek okunabilirlikli kart düzeniyle yenilendi.
- Klasör halkası açıldığında eklenen geri kontrolü nedeniyle alt bilgi panelinin overlay penceresi tarafından kesilmesi düzeltildi.
- Varsayılan ve önerilen ana hotkey yeniden `Ctrl+Alt+Shift+R` olarak ayarlandı.
- Hotkey çakışması mesajı, açık kalmış eski Action Orbit sürümünü kontrol etmeyi açıkça belirtiyor.
- Ayarlar çalışma alanı düşük pencere yüksekliği ve yüksek DPI'da içerik kesilmemesi için kaydırılabilir kart düzenine geçirildi.
- Ayarlardaki tema adları Türkçeleştirildi; ana hotkey aynı ekrana taşındı ve moda bağlı kontroller yalnızca ilgili tetikleme modunda etkinleşiyor.

### Eklendi

- Orbit Link'e uygulama yeniden başlatıldığında devam eden, iki öğe ve 24 saatle sınırlı şifreli aktarım kuyruğu; artan yeniden deneme aralığı, teslimat durumu, yeniden dene ve iptal kontrolleri eklendi.
- Orbit Link ile iki Action Orbit kurulumu arasında tek kullanımlık kodla cihaz eşleştirme, öğe başına şifreli Shelf aktarımı ve yeni içerikleri eşleşen cihazlara ileten Ortak Raf modu eklendi.
- Kurumsal güvenlik duvarı nedeniyle yalnızca tek yönde TCP bağlantısı kurulabilen cihazlar için kimlik doğrulamalı ters bağlantı ve güvenli aktarım sırası eklendi.
- Orbit Link hedef seçimi yüzen Shelf'e; cihaz adı, bağlantı adresi, eşleştirme kodu ve eşleşen cihaz yönetimi Ayarlar'a eklendi.
- Action Orbit'in halka ve hızlı aksiyon fikrini taşıyan yeni uygulama logosu; EXE, görev çubuğu, bildirim alanı ve yardımcı pencerelere uygulandı.
- Overlay bilgi kartına, halkayı kapatıp yüzen Orbit Shelf'i açan sabit hızlı erişim düğmesi eklendi.
- Sürüklenebilir ve isteğe bağlı üstte sabitlenebilir ortak mini araç penceresi eklendi.
- `Mini Araçlar` klasörüne zamanlayıcı, uyanık tutma, sistem durumu, güvenli hesap makinesi, ekran renk seçici, kronometre, otomatik kaydedilen hızlı not, birim dönüştürücü, metin araçları ve parola üretici eklendi.
- Ayarlara doğrulamalı vurgu rengi, hazır renkler ve görsel varsayılanlara dönüş kontrolleri eklendi.
- Config v10 göçü, mevcut kişisel aksiyonları ve özel klasör çocuklarını koruyarak eksik mini araçları varsayılan profile ekliyor.

### Güvenlik

- Bekleyen Orbit Link içerikleri yalnızca eşleşen cihaz anahtarıyla şifrelenmiş olarak diske yazılıyor; kuyrukta açık içerik, eşleştirme kodu veya anahtar tutulmuyor ve hata mesajları kişisel dosya adı içermiyor.
- Orbit Link yalnızca yerel ağ, localhost ve VPN adreslerinden bağlantı kabul eder; içerikler AES-GCM ile şifrelenir, eşleşme anahtarları Windows kullanıcı hesabıyla korunur ve gelen dosyalar çalıştırılmadan Shelf önbelleğine yazılır.
- Eşleştirme kodları beş dakika ve tek kullanım için geçerlidir; aktarım bütünlüğü SHA-256 ile doğrulanır, tekrar paketleri reddedilir ve ilk sürümde dosya boyutu 25 MB ile sınırlandırılır.
- `.gitignore`, katkı rehberi, güvenlik politikası ve yayın kontrol listesi; yerel kullanıcı verisi, imzalama materyali ve secret sızıntılarına karşı daha açık kontrollerle güncellendi.
- `mini_tool` aksiyonu yalnızca uygulama içindeki on izinli araç kimliğini çalıştırabiliyor.
- Mini hesap makinesi shell, betik veya dinamik kod çalıştırmadan yerel ifade ayrıştırıcısı kullanıyor.
- Parola üretici kriptografik güvenli rastgelelik kullanıyor; üretilen parolaları diske veya loglara yazmıyor.
- Hızlı not 128 KB sınırıyla atomik ve yalnızca yerel dosya yazımı kullanıyor.

## 2.0.0 - Yayına hazırlanıyor

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
