# Action Orbit Pro

<p align="center">
  <img src="src/ActionOrbit.App/Assets/Brand/ActionOrbitLogo.png" width="128" alt="Action Orbit Pro logosu" />
</p>

[![CI](https://github.com/SabahattinAkal/action-orbit/actions/workflows/ci.yml/badge.svg)](https://github.com/SabahattinAkal/action-orbit/actions/workflows/ci.yml)
[![CodeQL](https://github.com/SabahattinAkal/action-orbit/actions/workflows/codeql.yml/badge.svg)](https://github.com/SabahattinAkal/action-orbit/actions/workflows/codeql.yml)
[![GitHub Release](https://img.shields.io/github/v/release/SabahattinAkal/action-orbit)](https://github.com/SabahattinAkal/action-orbit/releases/latest)
[![License: MIT](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)

Action Orbit Pro, Windows için mouse makro tuşuyla açılan radial/oval aksiyon menüsünü uygulamalar arası geçici içerik rafıyla birleştirir.

Amaç: Logitech Actions Ring benzeri ama daha esnek, klasör mantıklı, uygulama bazlı profil destekli, hızlı ve görsel olarak daha modern bir sistem yapmak.

## Ana fikir

Kullanıcı mouse üzerindeki makro tuşuna örneğin `F13` veya `Ctrl+Alt+Shift+R` atar. Action Orbit Pro bu kısayolu dinler ve imlecin olduğu yerde oval/radial bir menü açar.

Menü içerisindeki aksiyonlar:

- Program açma
- Dosya açma
- Klasör açma
- URL açma
- Klavye kısayolu gönderme
- Metin yazdırma
- PowerShell / CMD komutu çalıştırma
- Alt menü / klasör içine girme
- Uygulama bazlı farklı menü gösterme

## Hedef platform

- Windows 10 ve Windows 11
- WPF
- Proje `net10.0-windows` hedefler.
- GitHub Release paketi self-contained olduğu için son kullanıcıda ayrıca .NET kurulumu gerekmez.
- .NET 8/9 hedeflenmek istenirse ilgili x64 `Microsoft.WindowsDesktop.App` runtime/SDK kurulup `ActionOrbit.App.csproj` hedef framework değeri değiştirilebilir.

## Proje durumu

Güncel Pro geliştirme sürümü `v2.0.0`:

- Chrome, Explorer ve diğer Windows uygulamalarından görsel, dosya, bağlantı ve metin kabul eden topmost **Orbit Shelf**
- Raf öğesini başka uygulamaya geri sürükleme veya panoya kopyalayıp yapıştırma
- Çoklu raf, adlandırma, açık sabitleme, isteğe bağlı yakın geçmiş ve süreli geçici önbellek
- Görselleri güvenli biçimde PNG'ye dönüştürme ve 1600 px sınırına küçültme
- Profil başına adlandırılmış birden fazla halka; mouse tekerleğiyle halka değiştirme
- Bas-aç, basılı-tut/bırakınca çalıştır ve çift-bas tetikleme modları
- Belirlenen uygulamalarda halkayı devre dışı bırakan process listesi
- URL aksiyonunu sistem tarayıcısı, Chrome, Edge, Firefox veya Brave ile açma
- Her aksiyona isteğe bağlı doğrudan global klavye kısayolu atama
- Halkadan açılan, sürüklenebilir ve isteğe bağlı üstte sabitlenebilir mini araçlar: zamanlayıcı, uyanık tutma, sistem durumu, hesap makinesi, renk seçici, kronometre, hızlı not, birim dönüştürücü, metin araçları ve parola üretici
- Pro sürümünün config, log, mutex ve başlangıç kaydını klasik sürümden ayıran izolasyon

- `src/ActionOrbit.App` altında WPF uygulaması
- JSON config oluşturma/yükleme/reload
- Bozuk config yedekleme ve default config fallback
- Dosya loglama
- `RegisterHotKey` ile global hotkey
- Aktif pencere process adına göre profil seçimi
- Transparent, topmost radial overlay
- Folder/alt menü ve geri navigasyonu
- `open_app`, `open_file`, `open_folder`, `open_url`, `mini_tool`, `send_hotkey`, `type_text`, `run_command` action handler'ları
- Ana Sayfa, Ring Editörü, Aksiyon Kütüphanesi ve Ayarlar çalışma alanları
- Aranabilir/kategorili hazır aksiyon kütüphanesi ve canlı ring önizlemesi
- Hazır aksiyonu profile ekleme ve seçili aksiyona uygulama için ayrı kontroller
- Profil kopyalama, varsayılan profil atama ve uygulama eşleştirme
- Silme, sıralama ve klasör taşıma işlemleri için tek adımlı geri alma
- Ana/klasör halkalarında kayıpsız sayfalama
- Fareye ek olarak `1–9`, oklar, `Enter`, `Backspace` ve `Esc` ile overlay kontrolü
- Ana pencerede gerçek light/dark/system tema ve canlı Windows tema takibi
- Ana pencere kısayolları: `Ctrl+1…4`, `Ctrl+Z` ve `Ctrl+S`
- Tek uygulama örneği, hotkey rollback ve güvenli config/profil içe aktarma
- Editör ve çalıştırma katmanında ortak aksiyon doğrulama/tehlikeli komut filtresi
- Komut aksiyonlarında varsayılan kapalı güvenli mod ve çalıştırma öncesi açık onay
- Config/profil içe aktarmada çalıştırılabilir aksiyon özeti ve dosya/kaynak sınırları
- Harici/UNC ikon yollarını engelleyen güvenli özel ikon dizini
- xUnit v3 regresyon testleri, CodeQL ve Windows GitHub Actions yayın hattı

## İndirme

En güncel Windows x64 paketini [GitHub Releases](https://github.com/SabahattinAkal/action-orbit/releases/latest) sayfasından indir:

1. `ActionOrbitPro-v2.0.0-win-x64.zip` dosyasını indir.
2. Arşivi istediğin bir klasöre çıkar.
3. `ActionOrbit.App.exe` dosyasını çalıştır.

Paket self-contained'dır ve .NET 10.0.10 runtime içerir. Dosya bütünlüğü Release
sayfasındaki `SHA256SUMS.txt`, SPDX SBOM ve GitHub build provenance attestation ile
doğrulanabilir:

```powershell
gh attestation verify ActionOrbitPro-v2.0.0-win-x64.zip --repo SabahattinAkal/action-orbit
```

Yayın hattı, depo secretlarında Authenticode sertifikası yapılandırıldığında EXE'yi
otomatik imzalar. Sertifika henüz yapılandırılmadıysa Windows SmartScreen bilinmeyen
yayıncı uyarısı gösterebilir; provenance doğrulaması paketin bu depodaki GitHub Actions
iş akışında üretildiğini kanıtlar.

## Kullanım örneği

- Chrome açıkken mouse makro tuşuna bas:
  - Yeni sekme
  - Sekmeyi kapat
  - YouTube aç
  - Geliştirici araçları
  - İndirilenler
  - ChatGPT

- VS Code açıkken aynı tuşa bas:
  - Terminal aç
  - Command Palette
  - Format Document
  - Git menüsü
  - Proje klasörü aç

- Masaüstünde aynı tuşa bas:
  - Belgeler
  - İndirilenler
  - Ekran görüntüsü
  - Görev yöneticisi
  - Terminal
  - Not defteri

- Chrome'daki bir görseli Orbit Shelf penceresine sürükle:
  - Görsel güvenli geçici önbelleğe alınır
  - `Kopyala` ile panoya koyup hedef uygulamada `Ctrl+V` kullanabilirsin
  - Öğeyi raftan doğrudan başka bir uygulamaya sürükleyebilirsin
  - İstersen PNG'ye dönüştürebilir veya 1600 px'e küçültebilirsin

- `Mini Araçlar` klasörünü aç:
  - 1, 5, 10 veya 25 dakikalık zamanlayıcı başlat
  - Bilgisayarın uykuya geçmesini 15 dakika–2 saat ya da sen kapatana kadar engelle
  - İşlemci, bellek ve pil durumunu tek bakışta gör
  - Komut çalıştırmadan matematiksel ifade hesapla
  - İmlecin altındaki ekran rengini yakalayıp HEX/RGB olarak kopyala
  - Kronometreyi başlat, duraklat ve tur sürelerini kaydet
  - Otomatik ve yerel kaydedilen hızlı nota geçici metin bırak
  - Uzunluk, ağırlık, sıcaklık ve veri birimlerini dönüştür
  - Türkçe büyük/küçük/başlık dönüşümü yap, boşlukları temizle ve kelime say
  - Diske yazılmayan, kriptografik güçlü ve özelleştirilebilir parola üret

## Pro özellik kapsamı

Pro sürüm temel Action Orbit kapsamına şunları ekler:

1. Uygulamalar arası görsel/dosya/URL/metin rafı.
2. Çoklu ve adlandırılmış halka setleri.
3. Alternatif tetikleme davranışları.
4. Uygulama bazlı devre dışı bırakma listesi.
5. Tarayıcı seçimi.
6. Aksiyon bazlı doğrudan hotkey.
7. On yerel araç içeren ortak, sabitlenebilir mini araç pencereleri.
8. Kaydırılabilir, doğrulamalı ve bağlama duyarlı gelişmiş ayarlar çalışma alanı.

## Çalıştırma

Önkoşul:

- Windows
- `global.json` ile sabitlenen .NET SDK 10.0.302

Derleme:

```powershell
dotnet build ActionOrbit.slnx
```

Test:

```powershell
dotnet test tests\ActionOrbit.App.Tests\ActionOrbit.App.Tests.csproj
```

Self-contained Windows x64 paketi:

```powershell
.\scripts\publish.ps1
```

Çalıştırma:

```powershell
dotnet run --project src\ActionOrbit.App\ActionOrbit.App.csproj
```

Veya derlenmiş exe:

```powershell
src\ActionOrbit.App\bin\Debug\net10.0-windows\ActionOrbit.App.exe
```

Varsayılan global hotkey:

```text
Ctrl+Alt+Shift+R
```

Config ve log konumları:

```text
%AppData%\ActionOrbitPro\config.json
%AppData%\ActionOrbitPro\logs\actionorbit.log
%AppData%\ActionOrbitPro\shelves.json
%AppData%\ActionOrbitPro\shelf-cache\
```

İlk açılışta default config otomatik oluşturulur. Config bozuksa `config.broken.yyyyMMddHHmmss.json` olarak yedeklenir ve default config yeniden yazılır.

`run_command` aksiyonları varsayılan olarak kapalıdır. Ayarlar bölümünden açıkça
etkinleştirilse bile her komut çalıştırılmadan önce tam komut gösterilerek onay istenir.
İçe aktarılan config dosyaları bu ayarı otomatik olarak açamaz.

## Notlar

- Mouse makro tuşunu doğrudan yakalamak yerine mouse yazılımında `Ctrl+Alt+Shift+R`, `F13` gibi bir klavye kısayoluna map etmek gerekir.
- Normal yetkiyle çalışan Action Orbit, admin yetkili pencerelere input göndermekte sınırlı kalabilir.
- Çok monitör ve DPI davranışı için temel konumlama vardır; farklı scaling senaryolarında ek test önerilir.
