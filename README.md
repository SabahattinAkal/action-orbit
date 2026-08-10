# Action Orbit Pro

<p align="center">
  <img src="src/ActionOrbit.App/Assets/Brand/ActionOrbitLogo.png" width="128" alt="Action Orbit Pro logosu" />
</p>

[![CI](https://github.com/SabahattinAkal/action-orbit/actions/workflows/ci.yml/badge.svg)](https://github.com/SabahattinAkal/action-orbit/actions/workflows/ci.yml)
[![CodeQL](https://github.com/SabahattinAkal/action-orbit/actions/workflows/codeql.yml/badge.svg)](https://github.com/SabahattinAkal/action-orbit/actions/workflows/codeql.yml)
[![GitHub Release](https://img.shields.io/github/v/release/SabahattinAkal/action-orbit)](https://github.com/SabahattinAkal/action-orbit/releases/latest)
[![License: MIT](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)

Action Orbit Pro, sık kullandığın işlemleri imlecin yanında açılan bir halkada toplar. Tek bir global kısayolla program açabilir, dosya veya klasöre gidebilir, klavye kısayolu gönderebilir ve uygulamaya özel menüler kullanabilirsin.

Uygulama yalnızca Windows içindir. Ayarlar, profiller ve geçici raf verileri bilgisayarında saklanır.

## Neler sunuyor?

### Aksiyon halkası

- İmlecin bulunduğu ekranda açılan radial/oval menü
- Uygulamaya göre otomatik profil seçimi
- Profil başına birden fazla adlandırılmış halka
- Klasörler, alt aksiyonlar ve kayıpsız sayfalama
- Fare, klavye ve mouse tekerleğiyle kullanım
- Her aksiyona ayrı global kısayol atama
- Açık, koyu ve sistem teması

Desteklenen aksiyon türleri:

- Program, dosya, klasör ve URL açma
- Klavye kısayolu gönderme
- Metin yazdırma
- Kullanıcı onayıyla PowerShell veya CMD komutu çalıştırma
- Mini araç açma
- Alt menü oluşturma

### Orbit Shelf

Orbit Shelf, uygulamalar arasında taşımak istediğin geçici içerikler için yüzen bir raftır.

- Görsel, dosya, bağlantı ve metin kabul eder
- Öğeyi panoya kopyalayabilir veya başka bir uygulamaya sürükleyebilirsin
- Birden fazla raf oluşturabilir, adlandırabilir ve sabitleyebilirsin
- Görselleri PNG'ye dönüştürebilir veya 1600 piksele küçültebilirsin
- Geçici önbelleği süreye ve boyuta göre temizler

Bir internet adresinden görsel alındığında yalnızca kullanıcının bıraktığı adres istenir. Yerel, özel ve link-local ağ adresleri güvenlik nedeniyle kabul edilmez.

### Mini araçlar

Halkadan açılan küçük araçlar ayrı pencerelerde çalışır ve istenirse üstte tutulabilir:

- Zamanlayıcı ve kronometre
- Uyanık tutma
- Sistem durumu
- Hesap makinesi
- Ekran renk seçici
- Hızlı not
- Birim dönüştürücü
- Metin araçları
- Parola üretici

## Kurulum

Kararlı Windows x64 paketini [Releases](https://github.com/SabahattinAkal/action-orbit/releases/latest) sayfasından indir.

1. En yeni `.zip` paketini indir.
2. Arşivi yazma iznin olan bir klasöre çıkar.
3. `ActionOrbit.App.exe` dosyasını çalıştır.

Release paketleri self-contained hazırlanır; ayrıca .NET kurman gerekmez. Authenticode sertifikası bulunmayan sürümlerde Windows SmartScreen bilinmeyen yayıncı uyarısı gösterebilir.

`main` dalı yayımlanmamış değişiklikler içerebilir. Günlük kullanım için Releases sayfasındaki son kararlı paketi tercih et.

### Paket doğrulama

Release sayfasındaki `SHA256SUMS.txt` ile paketin özetini karşılaştırabilirsin. Attestation yayımlanmış sürümlerde GitHub CLI ile üretim kaynağını da doğrulayabilirsin:

```powershell
gh attestation verify <indirilen-zip> --repo SabahattinAkal/action-orbit
```

## İlk kullanım

1. Uygulamayı aç ve **Ayarlar** bölümünden global kısayolu kontrol et.
2. Mouse yazılımındaki bir makro tuşuna aynı kısayolu ata. Varsayılan değer `Ctrl+Alt+Shift+R`'dir.
3. **Ring Editörü** bölümünden profilini ve aksiyonlarını düzenle.
4. **Önizle** ile halkayı açıp dene.
5. Uygulamayı bildirim alanında çalışır bırak.

Action Orbit mouse tuşlarını düşük seviyede dinlemez. Logitech, Razer, SteelSeries veya benzeri mouse yazılımlarında seçtiğin tuşu bir klavye kısayoluna eşleştirmen gerekir.

## Halka kontrolleri

| Girdi | Davranış |
| --- | --- |
| Fare | Aksiyonu seçer veya klasörü açar |
| `1`–`9` | Görünen aksiyonlardan birini seçer |
| Ok tuşları | Seçimi halkada hareket ettirir |
| `Enter` | Seçili aksiyonu çalıştırır |
| `Backspace` | İç içe klasörde bir üst seviyeye döner |
| `Esc` | Klasörden çıkar veya halkayı kapatır |
| Mouse tekerleği | Birden fazla halka varsa halkalar arasında geçer |

Açık bir ana klasöre tekrar tıklamak klasörü kapatır.

## Güvenlik ve yerel veriler

Action Orbit telemetri veya kullanım analitiği göndermez. Uygulama verileri varsayılan olarak şu klasörde tutulur:

```text
%AppData%\ActionOrbitPro\
```

Başlıca dosyalar:

```text
config.json
logs\actionorbit.log
shelves.json
shelf-cache\
```

Bu dosyalarda kişisel klasör yolları, açtığın bağlantılar veya rafa eklediğin içerikler bulunabilir. Hata bildirirken config ve log dosyalarını doğrudan yükleme; yalnızca gerekli bölümü paylaş ve kişisel bilgileri temizle.

`run_command` aksiyonları varsayılan olarak kapalıdır. Etkinleştirildiklerinde bile her çalıştırmadan önce komutun tamamı gösterilir ve kullanıcı onayı istenir. İnternetten aldığın profil veya config dosyasını içe aktarmadan önce çalıştırılabilir aksiyonları incele.

Bir güvenlik açığı bildirmek için [SECURITY.md](SECURITY.md) dosyasını kullan.

## Kaynaktan çalıştırma

Gereksinimler:

- Windows 10 veya Windows 11
- `global.json` dosyasında belirtilen .NET SDK

Bağımlılıkları geri yükle, derle ve testleri çalıştır:

```powershell
dotnet restore ActionOrbit.slnx --locked-mode
dotnet build ActionOrbit.slnx --configuration Release --no-restore
dotnet test tests\ActionOrbit.App.Tests\ActionOrbit.App.Tests.csproj --configuration Release --no-build --no-restore
```

Uygulamayı kaynak koddan başlat:

```powershell
dotnet run --project src\ActionOrbit.App\ActionOrbit.App.csproj
```

Self-contained Windows x64 paketi üret:

```powershell
.\scripts\publish.ps1
```

## Depo yapısı

```text
src/ActionOrbit.App/          WPF uygulaması
tests/ActionOrbit.App.Tests/  Otomatik testler
samples/profiles/             Örnek profil dosyaları
scripts/                      Yayın ve imzalama araçları
.github/workflows/            CI, CodeQL ve Release iş akışları
```

## Bilinen sınırlar

- Normal yetkiyle çalışan uygulama, yönetici yetkili pencerelere input göndermeyebilir.
- Çoklu monitör ve farklı DPI oranları desteklenir; sıra dışı ekran düzenlerinde konum davranışı değişebilir.
- Uygulama Windows'a ve WPF'e bağlıdır; macOS veya Linux sürümü yoktur.

## Katkı ve lisans

Katkıda bulunmadan önce [CONTRIBUTING.md](CONTRIBUTING.md) dosyasına göz at. Kullanıcıyı etkileyen değişiklikler [CHANGELOG.md](CHANGELOG.md) içinde tutulur.

Proje [MIT lisansı](LICENSE) ile yayımlanır.
