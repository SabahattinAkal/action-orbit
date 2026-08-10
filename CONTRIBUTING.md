# Katkıda bulunma

Hata düzeltmeleri, kullanılabilirlik iyileştirmeleri ve test katkıları memnuniyetle karşılanır. Büyük bir davranış veya config şeması değişikliği düşünüyorsan kod yazmaya başlamadan önce kısa bir issue aç; böylece yaklaşımı birlikte netleştirebiliriz.

## Geliştirme ortamı

Proje Windows ve WPF kullanır. Gerekli .NET SDK sürümü `global.json` dosyasında belirtilir.

```powershell
dotnet restore ActionOrbit.slnx --locked-mode
dotnet build ActionOrbit.slnx --configuration Release --no-restore
dotnet test tests\ActionOrbit.App.Tests\ActionOrbit.App.Tests.csproj --configuration Release --no-build --no-restore
```

## Değişiklik hazırlarken

- Bir pull request'i tek bir konu etrafında tut.
- Kullanıcıya görünen metinleri kısa ve anlaşılır Türkçe yaz.
- Hotkey, config, overlay veya güvenlik davranışı değişiyorsa regresyon testi ekle.
- Config şeması değişiyorsa mevcut kullanıcı verisini koruyan migration ekle.
- Kullanıcı davranışı değiştiyse `CHANGELOG.md` dosyasındaki `Unreleased` bölümünü güncelle.
- Yalnızca biçim değişikliği yapan büyük ve ilgisiz düzenlemelerden kaçın.

## Hassas bilgiler

Yerel config, log, raf önbelleği, sertifika, `.env`, `bin`, `obj` ve yayın çıktıları commit edilmemelidir. Log veya ekran görüntüsü paylaşırken kullanıcı adlarını, dosya yollarını, bağlantıları ve pano içeriğini temizle.

Bir güvenlik açığını herkese açık issue içinde anlatma. Özel bildirim adımları için [SECURITY.md](SECURITY.md) dosyasını kullan.

## Pull request

PR açıklamasında şu üç bilgiyi belirt:

1. Kullanıcı açısından ne değişti?
2. Hangi otomatik testler çalıştı?
3. Hangi manuel akışlar kontrol edildi?

Arayüz değişikliklerinde kişisel bilgi içermeyen bir önce/sonra görüntüsü yararlı olur.
