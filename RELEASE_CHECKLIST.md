# Action Orbit yayın kontrol listesi

## Otomatik kontroller

- `dotnet build ActionOrbit.slnx --configuration Release`
- `dotnet test tests\ActionOrbit.App.Tests\ActionOrbit.App.Tests.csproj --configuration Release`
- `dotnet restore ActionOrbit.slnx --locked-mode`
- `dotnet list tests\ActionOrbit.App.Tests\ActionOrbit.App.Tests.csproj package --vulnerable --include-transitive`
- `scripts\publish.ps1` ile self-contained `win-x64` paketini üret
- Temiz bir Windows kullanıcı hesabında uygulamayı ilk kez çalıştır

## Manuel regresyon

- İkinci uygulama örneğinin açılmadığını ve ilk pencerenin öne geldiğini doğrula
- Hotkey çakışmasında eski hotkey'in çalışmaya devam ettiğini doğrula
- Overlay'i fare, `1–9`, ok tuşları, `Enter`, `Backspace` ve `Esc` ile dene
- 8'den fazla ana aksiyon ve 9'dan fazla klasör aksiyonunda tüm sayfaları dolaş
- Config/profil içe aktarma, dışa aktarma ve bozuk JSON geri dönüşünü dene
- Bir profili kopyala, varsayılan yap ve uygulama eşleşmesini değiştir
- Aksiyon silme, sıralama, klasöre taşıma ve profil silme sonrasında `Ctrl+Z` ile geri al
- Aksiyon kütüphanesindeki “Profile Ekle” ve “Seçili Aksiyona Uygula” davranışlarını ayrı ayrı dene
- `run_command` için güvenli bir komutun çalıştığını, yıkıcı örneklerin editörde engellendiğini doğrula
- Komut aksiyonlarının varsayılan kapalı olduğunu ve her çalıştırmada onay istediğini doğrula
- Riskli config/profil içe aktarmada hedef ve aksiyon özetinin gösterildiğini doğrula
- UNC/mutlak ikon yolunun yüklenmediğini, büyük SVG/raster dosyasının reddedildiğini doğrula
- %100, %125, %150 DPI ve çoklu monitör senaryolarını kontrol et
- Açık/koyu/sistem temalarını, canlı Windows tema geçişini ve animasyon kapalı durumunu kontrol et

## Dağıtım

- `src\ActionOrbit.App\bin\publish\win-x64` çıktısını zararlı yazılım taramasından geçir
- Sürüm numarası ve değişiklik notlarını hazırla
- GitHub Actions tarafından üretilen `ActionOrbit-win-x64` artifact'ını doğrula
- Release paketindeki runtime marker'ın 10.0.10 olduğunu doğrula
- SPDX SBOM, `SHA256SUMS.txt` ve GitHub provenance attestation dosyalarını doğrula
- Authenticode secretları tanımlıysa imzayı, tanımlı değilse Release'deki imzasız uyarısını doğrula
