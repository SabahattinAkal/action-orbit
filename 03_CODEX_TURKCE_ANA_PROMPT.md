# 03 — Codex Türkçe Ana Prompt

Aşağıdaki metni Codex’e doğrudan ver. Codex’in Türkçe cevap vermesi özellikle istenmiştir.

---

Sen bu repodaki mevcut Action Orbit projesini geliştireceksin. Bu proje sıfırdan yazılacak bir proje değil; çalışan bir WPF/.NET Windows uygulaması var. Öncelik GitHub hazırlığı değil, önce programı gerçek kullanıcıya verilebilir public beta seviyesine getirmek.

Lütfen bana sadece Türkçe cevap ver. Kod, dosya adı, class adı, commit mesajı, public README içeriği gibi yerlerde İngilizce gerekirse kullanabilirsin ama açıklamalarını ve raporunu Türkçe yaz.

Önce şu 3 dosyayı oku ve ana brief olarak kullan:

- `01_MEVCUT_DURUM_VE_EKSIK_ANALIZI.md`
- `02_PROGRAMI_TAMAMLAMA_VE_GITHUB_PLANI.md`
- `03_CODEX_TURKCE_ANA_PROMPT.md`

Eski `README.md` dosyası projede kalacak. README’yi hemen değiştirme. Önce uygulamayı tamamla; GitHub hazırlığına geçtiğimiz zaman README’yi güncellersin.

## En önemli kural

Önce programı tamamla. Sonra GitHub için ortamı hazırla.

Yani ilk işin README süslemek, license eklemek, repo temizlemek veya dokümantasyon yazmak değil. Önce uygulamanın eksik UI ve ürün akışlarını tamamlayacaksın.

## Başlamadan önce yapacağın keşif

Önce projeyi incele ve bana kısa Türkçe rapor ver:

1. Mevcut çalışan özellikler neler?
2. Mevcut UI’da eksik veya yarım duran yerler neler?
3. Kodda riskli veya fazla büyümüş dosyalar neler?
4. İlk geliştirme turunda hangi eksikleri tamamlayacaksın?
5. Hangi işleri sonraki tura bırakacaksın?

Bu keşif raporundan sonra uygulamaya geç.

## Uygulamada tamamlanacak ana eksikler

Aşağıdaki eksikleri mümkün olduğunca sırayla tamamla:

### 1. İlk açılış ve ana ekran

- Kullanıcı uygulamayı açınca ne işe yaradığını anlasın.
- Mouse macro tuşu + hotkey mantığı kısa anlatılsın.
- “Önizle”, “Arka planda çalıştır”, “Kaydet”, “JSON Aç”, “Log Aç” akışları daha anlaşılır olsun.
- Hotkey aktif/pasif durumu gerçek durumla tutarlı görünsün.
- Kaydedildi / otomatik kaydediliyor / hata var gibi durumlar net görünsün.

### 2. Hotkey ayarı

- Kullanıcı UI’dan hotkey değiştirebilsin.
- `Ctrl+Alt+Shift+R`, `F13`, `F14`, `Ctrl+Space` gibi değerler desteklensin.
- Hotkey kaydedilince config güncellensin ve global hotkey yeniden register edilsin.
- Hotkey çakışması veya parse hatasında eski çalışan hotkey bozulmasın.
- Mouse tuşunu doğrudan yakalamak ilk sürümde şart değil; kullanıcıya mouse yazılımında bu hotkey’i macro tuşuna ataması gerektiğini anlat.

### 3. Profil yönetimi

- “Aktif uygulamayı bu profile ekle” butonu ekle.
- Aktif process adı kullanıcıya net gösterilsin.
- Process eşleşmeleri duplicate olmasın.
- Profil silerken onay iste.
- Son profil silinemesin ve düzgün uyarı verilsin.
- Default profil bilgisi daha net gösterilsin.

### 4. Aksiyon editörü

- Seçili aksiyon yoksa sağ panelde düzgün empty state göster.
- `open_app`, `open_file`, `open_folder` için Gözat butonu ekle.
- `open_url` için basit URL validation ekle.
- `send_hotkey` için örnek ve validation ekle.
- `type_text` için metin alanı daha uygun olsun.
- `run_command` için güvenlik/risk uyarısı göster.
- “Aksiyonu test et” butonu ekle.
- Aksiyon silerken onay iste.
- Aksiyon türüne göre hedef/argüman alanlarının açıklamaları doğru değişsin.

### 5. Profil ve config import/export

- Tüm config’i dışa aktar.
- Tüm config’i içe aktar.
- Seçili profili dışa aktar.
- Profil JSON içe aktar.
- ID çakışması olursa benzersiz ID üret.
- Bozuk JSON’da uygulama çökmesin, anlaşılır hata ver.

### 6. Overlay tamamlaması

- Overlay’de aktif profil adı görünür olsun.
- Folder açıldığında folder adı veya breadcrumb görünür olsun.
- Center button’ın default profile toggle anlamı daha anlaşılır olsun.
- Folder aç/kapat davranışı net olsun.
- 9’dan fazla child action varsa sessizce gizleme yapma; en azından `+N daha` göstergesi veya güvenli fallback ekle.
- Görsel kaliteyi artır ama çalışan yapıyı kırma.

### 7. Tema ve ayarlar

- Basit light/dark/system ayarı eklenebilir.
- Accent color preset veya text alanı eklenebilir.
- Overlay button size ve radius ayarları UI’dan düzenlenebilir.
- Startup with Windows toggle ekle.
- Kapatınca tray’e at davranışı ayarlanabilir olsun.

### 8. Hata yönetimi ve stabilite

- Aksiyon çalışmazsa sadece log’a yazma, kullanıcıya da status/toast benzeri bilgi göster.
- Config save/load/import/export hataları çökmeden yönetilsin.
- Dosya/klasör/app hedefi yoksa anlaşılır mesaj ver.
- `run_command` için boş komut ve hatalı komut durumlarını yakala.

## Refactor kuralları

- Projeyi sıfırdan yazma.
- Çalışan hotkey, overlay, config ve action execution akışını koru.
- Refactor yapacaksan küçük ve güvenli yap.
- `MainWindow.xaml`, `MainWindowViewModel.cs`, `IconCatalog.cs` büyük dosyalar ama önce ürünü tamamla, sonra gerekirse düşük riskli parçala.
- XAML binding’leri gereksiz yere kırma.
- Her büyük değişiklikten sonra build al.

## GitHub hazırlığı ne zaman yapılacak?

Uygulama kullanılabilir hale geldikten sonra GitHub hazırlığına geç:

- `bin/`, `obj/`, `.vs/`, log, temp, local config dosyalarını temizle.
- `.gitignore` dosyasını genişlet.
- Eski gereksiz md dosyaları zaten silinmiş olabilir; yeni gereksiz md oluşturma.
- README’yi en son güncelle.
- LICENSE ekle. Uygunsa MIT kullan.
- CONTRIBUTING.md kısa ve düzgün olabilir.
- SECURITY.md gerekirse eklenebilir.
- Sample profiles klasörü eklenebilir.
- Release checklist ekle.

## Yapma

- Telemetry ekleme.
- Analytics ekleme.
- Ücretli API veya AI özelliği ekleme.
- Fake screenshot, fake star, fake kullanıcı, fake badge ekleme.
- Local path hardcode etme.
- Secrets ekleme.
- Büyük mimari rewrite yapma.
- Program tamamlanmadan README/GitHub hazırlığına odaklanma.

## Sonunda bana Türkçe rapor ver

İş bitince şu formatta rapor ver:

1. Bulduğun UI/ürün eksikleri
2. Tamamladığın özellikler
3. Değiştirdiğin dosyalar
4. Oluşturduğun dosyalar
5. Sildiğin dosyalar
6. Build/test sonucu
7. Manuel test etmem gereken yerler
8. Sonraki geliştirme turu için önerilen prompt

Unutma: Önce programı tamamla, sonra GitHub hazırlığı.
