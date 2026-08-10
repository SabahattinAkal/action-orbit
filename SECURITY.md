# Güvenlik

## Açık bildirme

Bir güvenlik açığı bulduysan ayrıntıları herkese açık issue, discussion veya pull request içinde paylaşma. Bunun yerine GitHub üzerinden [özel güvenlik bildirimi oluştur](https://github.com/SabahattinAkal/action-orbit/security/advisories/new).

Bildirimde mümkünse şunlara yer ver:

- Etkilenen sürüm veya commit
- Sorunun etkisi
- Tekrarlama adımları
- Varsa güvenli bir örnek dosya
- Önerdiğin düzeltme veya geçici önlem

Gerçek parola, erişim anahtarı, kişisel dosya veya aktif kimlik bilgisi gönderme. Örnek gerekiyorsa geçersiz ve yapay değerler kullan.

## Desteklenen sürüm

Güvenlik düzeltmeleri öncelikle [son kararlı GitHub Release](https://github.com/SabahattinAkal/action-orbit/releases/latest) için hazırlanır. `main` dalı yayımlanmamış değişiklikler içerebilir.

## Kullanıcıların dikkat etmesi gerekenler

Action Orbit yerel program ve komut çalıştırabildiği için güvenmediğin kaynaklardan gelen profil veya config dosyalarını içe aktarma. İçe aktarma ekranındaki çalıştırılabilir aksiyon özetini dikkatle incele; özellikle `run_command`, `open_app`, `type_text` ve `send_hotkey` hedeflerini kontrol et.

`run_command` varsayılan olarak kapalıdır. İçe aktarılan config bu ayarı açamaz ve her komut için ayrıca kullanıcı onayı gerekir. Bu korumalar, güvenilmeyen bir dosyayı güvenilir hâle getirmez.

Hata bildirirken `%AppData%\ActionOrbitPro` altındaki config, log ve raf dosyalarını doğrudan paylaşma. Gerekli bölümü kopyala ve kişisel yolları, bağlantıları ve içerikleri temizle.

## Yayın bütünlüğü

Release paketleri `SHA256SUMS.txt`, SPDX SBOM ve desteklenen sürümlerde GitHub build provenance attestation ile yayımlanır. Authenticode imzası yalnızca kod imzalama sertifikası Release iş akışında yapılandırılmışsa bulunur.
