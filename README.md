# Action Orbit

[![CI](https://github.com/SabahattinAkal/action-orbit/actions/workflows/ci.yml/badge.svg)](https://github.com/SabahattinAkal/action-orbit/actions/workflows/ci.yml)
[![CodeQL](https://github.com/SabahattinAkal/action-orbit/actions/workflows/codeql.yml/badge.svg)](https://github.com/SabahattinAkal/action-orbit/actions/workflows/codeql.yml)
[![GitHub Release](https://img.shields.io/github/v/release/SabahattinAkal/action-orbit)](https://github.com/SabahattinAkal/action-orbit/releases/latest)
[![License: MIT](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)

Action Orbit, Windows için mouse makro tuşuyla açılan gelişmiş bir radial/oval aksiyon menüsü uygulamasıdır.

Amaç: Logitech Actions Ring benzeri ama daha esnek, klasör mantıklı, uygulama bazlı profil destekli, hızlı ve görsel olarak daha modern bir sistem yapmak.

## Ana fikir

Kullanıcı mouse üzerindeki makro tuşuna örneğin `F13` veya `Ctrl+Alt+Shift+R` atar. Action Orbit bu kısayolu dinler ve imlecin olduğu yerde oval/radial bir menü açar.

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

Güncel kararlı sürüm `v1.0.1`:

- `src/ActionOrbit.App` altında WPF uygulaması
- JSON config oluşturma/yükleme/reload
- Bozuk config yedekleme ve default config fallback
- Dosya loglama
- `RegisterHotKey` ile global hotkey
- Aktif pencere process adına göre profil seçimi
- Transparent, topmost radial overlay
- Folder/alt menü ve geri navigasyonu
- `open_app`, `open_file`, `open_folder`, `open_url`, `send_hotkey`, `type_text`, `run_command` action handler'ları
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

1. `ActionOrbit-v1.0.1-win-x64.zip` dosyasını indir.
2. Arşivi istediğin bir klasöre çıkar.
3. `ActionOrbit.App.exe` dosyasını çalıştır.

Paket self-contained'dır ve .NET 10.0.10 runtime içerir. Dosya bütünlüğü Release
sayfasındaki `SHA256SUMS.txt`, SPDX SBOM ve GitHub build provenance attestation ile
doğrulanabilir:

```powershell
gh attestation verify ActionOrbit-v1.0.1-win-x64.zip --repo SabahattinAkal/action-orbit
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

## v1.0 kapsamı

İlk kararlı sürüm aşağıdaki temel kapsamı karşılar:

1. Global hotkey dinleme.
2. Hotkey ile cursor konumunda overlay menü açma.
3. Menüde 6-10 aksiyon gösterme.
4. Aksiyon tıklanınca çalıştırma.
5. JSON config dosyasından menüleri okuma.
6. Aktif uygulamaya göre profil seçme.
7. Alt menü desteği.
8. Basit ayarlar ekranı.

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
%AppData%\ActionOrbit\config.json
%AppData%\ActionOrbit\logs\actionorbit.log
```

İlk açılışta default config otomatik oluşturulur. Config bozuksa `config.broken.yyyyMMddHHmmss.json` olarak yedeklenir ve default config yeniden yazılır.

`run_command` aksiyonları varsayılan olarak kapalıdır. Ayarlar bölümünden açıkça
etkinleştirilse bile her komut çalıştırılmadan önce tam komut gösterilerek onay istenir.
İçe aktarılan config dosyaları bu ayarı otomatik olarak açamaz.

## Notlar

- Mouse makro tuşunu doğrudan yakalamak yerine mouse yazılımında `Ctrl+Alt+Shift+R`, `F13` gibi bir klavye kısayoluna map etmek gerekir.
- Normal yetkiyle çalışan Action Orbit, admin yetkili pencerelere input göndermekte sınırlı kalabilir.
- Çok monitör ve DPI davranışı için temel konumlama vardır; farklı scaling senaryolarında ek test önerilir.
