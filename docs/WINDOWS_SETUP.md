# Windows kurulumu

`ActionOrbitPro-…-Setup-win-x64.exe` dosyasını bilgisayara kopyalayıp çift tıkla. Kurulum .NET bileşenlerini de içerir; ayrıca .NET kurman gerekmez.

Kurulumdan önce bildirim alanındaki Action Orbit menüsünden **Çıkış** seçeneğini kullan. Kurulum sihirbazı dosyaları kullanıcı hesabındaki `AppData\Local\Programs\Action Orbit Pro` klasörüne yerleştirir. Yönetici yetkisi istemez. Başlat menüsüne kısayol ekler; masaüstü kısayolu isteğe bağlıdır.

Önceki sürüm aynı Windows hesabında kullanılmışsa profiller, raflar ve eşleşmeler `%AppData%\ActionOrbitPro` klasöründen okunur. Başka bir bilgisayara kurmak kişisel ayarları otomatik taşımaz. Profilleri uygulamanın içe/dışa aktarma seçenekleriyle taşıyabilirsin.

Güncelleme için yeni setup dosyasını çalıştır. Kurulumu Windows **Ayarlar > Uygulamalar** bölümünden kaldırabilirsin. Kaldırma, kişisel ayarları ve raf verilerini silmez.

Paket şu an kod imzası içermez. Windows SmartScreen uyarısı gösterebilir. Yalnızca güvendiğin kaynaktan aldığın dosyayı çalıştır; `.sha256` dosyası indirilen dosyanın bütünlüğünü karşılaştırmak içindir.

## Setup oluşturma

.NET SDK sürümü `global.json` dosyasında belirtilir. Ayrıca [Inno Setup 6](https://jrsoftware.org/isdl.php) gerekir.

```powershell
.\scripts\build-setup.ps1
```

Araçlar PATH üzerinde değilse `-DotNetPath` ve `-IsccPath` ile tam yollarını ver. Sonuç ve SHA-256 dosyası `artifacts\setup` klasörüne yazılır. Her derleme temiz bir yayın klasörü kullanır; kişisel config ve raf dosyaları kurulum paketine eklenmez.

## Kurulumu test etme

```powershell
.\scripts\Test-Setup.ps1 -SetupPath .\artifacts\setup\ActionOrbitPro-2.0.0-COMMIT-Setup-win-x64.exe
```

Test, geçici bir klasöre kurar, aynı kurulumun üzerine güncelleme yapar, yüklenen uygulamanın pencere ve bildirim alanı kontrollerini çalıştırır ve kaldırır. Logların bulunduğu geçici klasörü sonuçta gösterir. Bilgisayarda setup ile kurulmuş bir kopya varsa test durur; bu durumda ayrı bir Windows test hesabı kullan.
