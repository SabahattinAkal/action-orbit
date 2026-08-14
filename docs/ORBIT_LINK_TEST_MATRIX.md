# Orbit Link gerçek cihaz testi

Bu kontrol iki Windows bilgisayarda, aynı Action Orbit commit'iyle yapılır. Amaç yalnızca bağlantının kurulup kurulmadığını değil; tek yönlü güvenlik duvarı, bağlantı kaybı, yeniden başlatma ve veri gizliliği davranışını da doğrulamaktır.

## Hazırlık

1. İki bilgisayarda da aynı paketi aç ve **Ayarlar > Orbit Link** bölümünden cihazları eşleştir.
2. Test boyunca eşleştirme kodunu, cihaz adresini ve gerçek dosya adlarını ekran görüntülerinde gizle.
3. Ev ve ofis bilgisayarlarında aşağıdaki komutu kendi rolü, tema ve senaryosuyla çalıştır:

   ```powershell
   .\scripts\Test-OrbitLinkMatrix.ps1 `
       -Role Home `
       -PeerAddress "100.x.x.x:48731" `
       -Scenario OfficeToHome `
       -Theme Dark
   ```

Komut bağlantı sonucunu, Windows build'ini, uygulama sürümünü ve commit SHA'sını kaydeder. IP adresi, cihaz adı, kullanıcı adı, yerel dosya yolu, eşleştirme kodu ve Shelf içeriği rapora yazılmaz. Çıktı varsayılan olarak `artifacts\orbit-link-matrix` altında oluşur; bu klasör Git tarafından izlenmez.

## Ağ senaryoları

Her satır ayrı çalıştırılmalıdır. Güvenlik duvarı kuralını değiştirdikten sonra iki uygulamayı da yeniden açmak, önceki açık soketlerin sonucu etkilemesini engeller.

| Senaryo | Evden ofise | Ofisten eve | Beklenen yol |
| --- | --- | --- | --- |
| `DirectBoth` | Açık | Açık | İki yönde doğrudan |
| `OfficeToHome` | Kapalı | Açık | Ofisten eve doğrudan, evden ofise dönüş kanalı |
| `HomeToOffice` | Açık | Kapalı | Evden ofise doğrudan, ofisten eve dönüş kanalı |
| `OfflineRecovery` | Hedef kapalı | — | Öğenin sırada kalması ve açılıştan sonra teslimi |
| `VpnRecovery` | VPN kesik | — | Öğenin sırada kalması ve VPN dönünce teslimi |

Windows Güvenlik Duvarı kuralını yalnızca Action Orbit test paketi ve TCP `48731` portuyla sınırla. Kurumsal cihazda bu ayar yönetiliyorsa politikayı aşmaya çalışma; ilgili ağ yönünü “test edilemedi” olarak işaretle.

## İçerik sırası

Her ağ senaryosunda önce küçük ve ayırt edilebilir test verileri kullan:

1. Kişisel bilgi içermeyen kısa metin ve bir örnek URL.
2. Chrome'dan sürüklenen küçük bir görsel.
3. Küçük bir düz metin dosyası.
4. Tam 25 MB boyutunda rastgele test dosyası.
5. 25 MB'tan bir bayt büyük dosya ve bir klasör; ikisi de gönderilmeden reddedilmelidir.
6. Bekleyen aktarım sırasında bağlantıyı kesip geri getir; alıcıda yalnızca tek kopya bulunmalıdır.

Test dosyaları gerçek belge, müşteri verisi veya ekran görüntüsü içermemelidir.

## Yaşam döngüsü

- Aynı eşleştirme kodunu ikinci kez kullanmayı dene; kabul edilmemelidir.
- Bir aktarım sürerken bildirim alanından **Çıkış** seç; menü takılmadan kapanmalıdır.
- Alıcıda Orbit Shelf'i kapatıp gönder; aktarım açık bir red durumuyla sonuçlanmalıdır.
- İki uygulamayı kapatıp yeniden aç; eşleşen cihazlar korunmalı, eşleştirme kodu tekrar istenmemelidir.
- Test sonunda log ve tanı dosyalarında kullandığın örnek metni ve eşleştirme kodunu ara. Bulunursa dosyayı paylaşmadan güvenlik issue'su aç.

## GitHub'a sonuç ekleme

İki bilgisayarda oluşan raporlardaki kutuları doldur. Issue yorumuna raporların temizlenmiş metnini, test edilen commit SHA'sını ve bağlantı yönünü ekle. IP, cihaz adı, kullanıcı adı, şirket adı, tam dosya yolu veya eşleştirme kodu ekleme.
