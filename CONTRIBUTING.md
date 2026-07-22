# Katkıda bulunma

Katkıdan önce mevcut issue ve pull request'leri kontrol et. Büyük davranış veya veri modeli değişiklikleri için önce kısa bir issue açılması tercih edilir.

## Yerel kontrol

```powershell
dotnet restore ActionOrbit.slnx
dotnet build ActionOrbit.slnx
dotnet test tests\ActionOrbit.App.Tests\ActionOrbit.App.Tests.csproj
```

- UI metinlerini anlaşılır Türkçe tut.
- Hotkey, config ve overlay akışlarında eski davranışı koruyan test ekle.
- Yerel config, log, `bin`, `obj` ve yayın çıktılarını commit etme.
- PR açıklamasında kullanıcı etkisini ve yaptığın manuel kontrolleri belirt.
