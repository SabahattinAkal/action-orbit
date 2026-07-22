# Action Orbit

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
- Bu çalışma ortamında x64 Windows Desktop runtime 10 bulunduğu için proje `net10.0-windows` hedefler.
- .NET 8/9 hedeflenmek istenirse ilgili x64 `Microsoft.WindowsDesktop.App` runtime/SDK kurulup `ActionOrbit.App.csproj` hedef framework değeri değiştirilebilir.

## Proje durumu

Çalışan beta temeli hazır:

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
- xUnit regresyon testleri ve Windows GitHub Actions yayın hattı

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

## MVP hedefi

İlk çalışan sürümde şu özellikler yeterlidir:

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
- .NET SDK 10 ve `Microsoft.WindowsDesktop.App` x64 runtime 10

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

## Notlar

- Mouse makro tuşunu doğrudan yakalamak yerine mouse yazılımında `Ctrl+Alt+Shift+R`, `F13` gibi bir klavye kısayoluna map etmek gerekir.
- Normal yetkiyle çalışan Action Orbit, admin yetkili pencerelere input göndermekte sınırlı kalabilir.
- Çok monitör ve DPI davranışı için temel konumlama vardır; farklı scaling senaryolarında ek test önerilir.
