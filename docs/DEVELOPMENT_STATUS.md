# Geliştirme durumu

## Tamamlanan son işler

- [x] Kullanıcı hesabına kurulan, .NET içeren Windows setup
- [x] Kurulumda Windows ile başlatma ve iki ayrı kısayol tercihi
- [x] Kurulum tercihinin ilk uygulama açılışında ayarlara aktarılması
- [x] Windows oturum açılışında ayarlar penceresi açılmadan bildirim alanında çalışma
- [x] Dar editör panelinde satır oklarının kesilmesinin giderilmesi
- [x] Halka ayarlarının açılır bölümde toplanması ve aksiyon listesine alan kazandırılması
- [x] Otomatik kayıt sonrasında aksiyon sıralama ve geri alma bağlantılarının korunması
- [x] LG ikinci ekranda dar pencere ve tam ekran editör kontrolü
- [x] Satır oku, otomatik kayıt, geri alma ve panel içi kaydırmanın gerçek arayüzde denenmesi
- [x] Setup üretimi, başlangıç tercihleri ve kaldırma testinin CI ve Release akışına eklenmesi
- [x] Setup, checksum ve temizlenmiş test özetinin Release varlıklarına eklenmesi
- [x] Yayın girdisinde config, log, özel anahtar, PDB ve beklenmeyen dosyaların engellenmesi
- [x] Git index içeriğinde yerel kullanıcı yolu ve yaygın anahtar biçimlerinin kontrol edilmesi
- [x] Shelf'in imlecin bulunduğu monitörde açılması; LG ikinci monitörde doğrulama
- [x] Shelf ve mini araç konumlandırmasında %100/%125/%150 için otomatik sınır testleri
- [x] Ring Editörü'nde ad ve eylem türüne göre aksiyon arama; Ctrl+F ve Esc desteği
- [x] Klasör alt aksiyonlarını editörde açıp kapatma; aramada klasör bağlamının korunması
- [x] Taşıma sonrasında seçili satırın görünür tutulması ve Alt+ok tuşlarının düzeltilmesi
- [x] Arama ve klasör kontrollerinin gerçek WPF penceresiyle otomatik açılış testine eklenmesi

## Açık işler

- [ ] [Orbit Link gerçek cihaz matrisi — #13](https://github.com/SabahattinAkal/action-orbit/issues/13): iki fiziksel Windows bilgisayarda bağlantı yönleri, VPN kesintisi ve yeniden teslim
- [ ] Farklı ölçeklendirmeli ekranlarda %125 ve %150 DPI kontrolü
- [ ] Gerçek temiz Windows hesabında kurulum ve Windows yeniden başlatıldıktan sonra başlangıç davranışı
- [ ] Yeni arama ve klasör kontrollerinin LG ekranında görsel kabulü

Yerel otomatik testler, gerçek cihaz veya Windows yeniden başlatma testinin yerine tamamlanmış sayılmaz. Ayrıntılı sürüm kontrol listesi `RELEASE_CHECKLIST.md`, ağ senaryoları `docs/ORBIT_LINK_TEST_MATRIX.md` içindedir.
