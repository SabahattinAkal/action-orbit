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

Orbit Link yalnızca güvendiğin bilgisayarlarla ve güvendiğin yerel ağ veya VPN üzerinde kullanılmalıdır. Eşleştirme kodunu yalnızca hedef bilgisayara aktar; ekran görüntüsünde veya hata bildiriminde paylaşma. Artık kullanmadığın cihazı Ayarlar'daki eşleşen cihazlar listesinden kaldır. `orbit-link.json` cihaz adları ve yerel ağ adresleri içerir; `orbit-link-queue.json` ise en fazla iki bekleyen aktarımın AES-GCM şifreli verisini 24 saate kadar tutabilir. Aktarım anahtarı Windows kullanıcı hesabına bağlı korunduğu için kuyruk başka hesaba taşındığında açılamaz. Bu dosyaları hata raporlarına ekleme.

## Yayın bütünlüğü

Release paketleri `SHA256SUMS.txt`, SPDX SBOM ve desteklenen sürümlerde GitHub build provenance attestation ile yayımlanır. Authenticode imzası yalnızca kod imzalama sertifikası Release iş akışında yapılandırılmışsa bulunur.
