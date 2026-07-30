# Güvenlik politikası

Güvenlik açığını herkese açık issue olarak paylaşma. Depo sahibine GitHub Security Advisory üzerinden özel bildirim gönder; bu kanal yoksa yalnızca açığın varlığını belirten ve ayrıntı içermeyen bir issue açarak özel iletişim kanalı iste.

Action Orbit yerel komut çalıştırabildiği için içe aktarılan profil/config dosyalarını
güvenilir olmayan kaynaklardan kullanma. İçe aktarma ekranı çalıştırılabilir aksiyonları
özetler; özellikle `run_command`, `open_app`, `type_text` ve `send_hotkey` hedeflerini
onaylamadan önce incele.

`run_command` varsayılan olarak kapalıdır, içe aktarılan config bu ayarı açamaz ve her
çalıştırmada ayrıca kullanıcı onayı gerekir. Bu korumalar güvenilmeyen bir dosyayı güvenilir
hale getirmez.

Yayın paketi için `SHA256SUMS.txt`, SPDX SBOM ve GitHub artifact attestation birlikte
yayımlanır. Authenticode imzası yalnızca depo sahibinin kod imzalama sertifikası GitHub
Actions secretlarına tanımlandığında eklenir.

Desteklenen sürüm, en son GitHub Release olarak yayımlanan kararlı sürümdür.
