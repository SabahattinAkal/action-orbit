# Yayın kontrol listesi

Bu liste, yeni bir sürüm etiketi oluşturmadan önce uygulanır.

## Sürüm ve belgeler

- [ ] Proje sürümü ile oluşturulacak `vX.Y.Z` etiketi aynı
- [ ] `CHANGELOG.md` içindeki `Unreleased` maddeleri sürüm başlığına taşındı
- [ ] README'deki kurulum ve güvenlik bilgileri mevcut davranışla uyumlu
- [ ] Örnek profil güncel config şemasıyla açılıyor
- [ ] Yerel Git author e-postası kişisel adres yerine GitHub noreply adresi kullanıyor

## Kaynak ve gizli bilgi kontrolü

- [ ] `git status --short` temiz
- [ ] `.env`, config, log, raf önbelleği, sertifika veya imzalama anahtarı takip edilmiyor
- [ ] Commit geçmişinde yerel kullanıcı yolu, gerçek e-posta, token veya özel anahtar yok
- [ ] GitHub Secret Scanning ve push protection açık
- [ ] Açık secret scanning veya Dependabot alarmı yok
- [ ] Yayın ZIP'inin dosya listesinde kullanıcı verisi, log, config, PDB veya sertifika yok
- [ ] README, SBOM ve checksum dışında yalnızca beklenen uygulama dosyaları paketlendi

## Otomatik kontroller

```powershell
dotnet restore ActionOrbit.slnx --locked-mode
dotnet restore src\ActionOrbit.App\ActionOrbit.App.csproj --runtime win-x64 --locked-mode
dotnet build ActionOrbit.slnx --configuration Release --no-restore
dotnet test tests\ActionOrbit.App.Tests\ActionOrbit.App.Tests.csproj --configuration Release --no-build --no-restore
dotnet list tests\ActionOrbit.App.Tests\ActionOrbit.App.Tests.csproj package --vulnerable --include-transitive
.\scripts\publish.ps1
```

- [ ] Derleme uyarısız tamamlandı
- [ ] Tüm testler geçti
- [ ] NuGet güvenlik taraması açık bulmadan tamamlandı
- [ ] `win-x64` self-contained paket üretildi

## Temel kullanım

- [ ] İlk açılışta varsayılan config oluşuyor
- [ ] İkinci uygulama örneği açılmıyor ve mevcut pencere öne geliyor
- [ ] Global hotkey kaydediliyor; çakışmada eski çalışan değer korunuyor
- [ ] Overlay fare, `1`–`9`, oklar, `Enter`, `Backspace` ve `Esc` ile kullanılabiliyor
- [ ] Ana ve klasör halkalarında tüm sayfalara ulaşılabiliyor
- [ ] İç içe klasörde geri dönüş ve açık ana klasöre tekrar tıklayarak kapatma çalışıyor
- [ ] Profil oluşturma, kopyalama, eşleştirme, varsayılan yapma ve silme çalışıyor
- [ ] Aksiyon ekleme, test etme, sıralama, klasöre taşıma ve geri alma çalışıyor
- [ ] Config ve profil içe/dışa aktarma ile bozuk JSON kurtarma akışları çalışıyor

## Güvenlik regresyonu

- [ ] `run_command` varsayılan kapalı ve her çalıştırmada onay istiyor
- [ ] Tehlikeli komut örnekleri editörde ve çalışma katmanında reddediliyor
- [ ] Riskli config/profil içe aktarmada aksiyon özeti gösteriliyor
- [ ] Mutlak/UNC ikon yolları ve sınırları aşan SVG/raster dosyaları reddediliyor
- [ ] Yerel/özel IP, güvenli olmayan yönlendirme ve MIME/imza uyuşmazlığı reddediliyor
- [ ] Parola üretici çıktısı diske veya loga yazılmıyor

## Arayüz ve Windows davranışı

- [ ] Açık, koyu ve sistem temaları kontrol edildi
- [ ] `%100`, `%125` ve `%150` DPI değerleri denendi
- [ ] Çoklu monitörde overlay doğru ekranda açılıyor
- [ ] Bas-aç, basılı-tut ve çift-bas tetikleme modları çalışıyor
- [ ] Devre dışı process listesi ve doğrudan aksiyon hotkey'leri çalışıyor
- [ ] Normal yetkili uygulamanın yönetici pencerelerine input sınırı kullanıcıya doğru aktarılıyor

## Orbit Shelf ve mini araçlar

- [ ] Chrome'dan görsel bırakma, panoya kopyalama ve dışarı sürükleme çalışıyor
- [ ] Dosya, URL ve metin bırakma çalışıyor
- [ ] Çoklu raf, yeniden adlandırma, sabitleme ve yeniden açma çalışıyor
- [ ] PNG dönüştürme, 1600 piksel küçültme ve süreli önbellek temizliği çalışıyor
- [ ] Mini araçlar açılıyor, sabitleniyor ve kapandıktan sonra kaynak bırakmıyor

## Dağıtım

- [ ] Temiz bir Windows kullanıcı hesabında paket ilk kez çalıştırıldı
- [ ] EXE zararlı yazılım taramasından geçti
- [ ] Runtime sürümü beklenen değerle aynı
- [ ] `SHA256SUMS.txt` paket özetiyle eşleşiyor
- [ ] SPDX SBOM ve GitHub attestation doğrulandı
- [ ] Authenticode yapılandırılmışsa imza geçerli; değilse Release notunda imzasız olduğu belirtiliyor
- [ ] GitHub Release varlıkları indirildi ve son kez temiz içerik taramasından geçirildi
