# GerakAR - Design System dan Spesifikasi Layar

**Status:** Final, menggantikan seluruh instruksi visual sebelumnya yang saling bertentangan
**Branch:** feature/natural-image-tracking
**Diperbarui:** 17 Juli 2026
**Cakupan:** Perombakan UI/UX menyeluruh untuk 7 layar (G01, G02, G03, G05, G06, G08, G09), tanpa layar atau fitur baru

---

## 0. Cara Membaca Dokumen Ini

Ada tiga generasi instruksi desain yang pernah dikirim untuk proyek ini, dan beberapa poin di antaranya saling bertentangan. Dokumen ini adalah hasil rekonsiliasi ketiganya. Kalau ada instruksi lama yang masih beredar di riwayat chat, file lain, atau ingatan siapa pun yang mengerjakan, dokumen inilah yang jadi acuan tunggal mulai sekarang.

Ringkasan evolusinya:

| Generasi | Font | Arah Palet | Status |
|---|---|---|---|
| 1, awal | Inter | Forest green sebagai identitas, tiap gerakan (SQ/DS/LD) punya warna sendiri (terracotta, teal, mustard) | Digantikan |
| 2, revisi | Poppins | Netral, dasar navy/hitam #020826 dengan aksen coklat #8C7851 | Digantikan |
| 3, final | Poppins | Green dominant, Deep Forest #12372A sebagai identitas utama, satu sistem warna untuk semua gerakan | **Berlaku sekarang** |

Kalau nemu referensi Inter, atau warna navy #020826/coklat #8C7851 di kode atau prefab lama, itu peninggalan generasi sebelumnya dan harus diganti, bukan dipertahankan cuma karena "sudah ada."

Penanda yang dipakai di seluruh dokumen:

- **Wajib**, berasal langsung dari salah satu dari tiga prompt yang sudah dikirim, tidak untuk diinterpretasi ulang.
- **Rekomendasi**, sintesis dari referensi visual/screenshot terakhir yang dikirim, belum tertulis eksplisit di prompt manapun. Boleh langsung dipakai atau diganti sesuai selera.
- **Perlu konfirmasi**, ada ambiguitas antara screenshot dan teks prompt, dikumpulkan semuanya di bagian 12 supaya gampang direview sekaligus.

---

## 1. Prinsip Desain

GerakAR adalah aplikasi AR image tracking untuk media pembelajaran gerakan olahraga dasar (Squat, Dynamic Stretching, Ladder Drill) untuk siswa SD, dikerjakan sebagai skripsi di UNP.

Arah visualnya:

- Tenang dan natural, bukan playful/kartun untuk anak-anak, bukan juga kaku dan klinis. Hijau hutan adalah identitas, bukan aksen tempelan.
- Geometris, bukan organik. Tidak ada blob, brush, atau squiggle. Semua bentuk berasal dari rounded rectangle dan lingkaran.
- Hierarki dibangun lewat font weight dan warna, bukan lewat banyak warna berbeda atau ukuran ekstrem.
- Modern dan lega, tapi lega karena disengaja, bukan karena kontennya memang belum diisi. Ini penting dibedakan karena versi sebelumnya sempat punya masalah konten kosong yang keliru terlihat seperti "desain minimalis" (lihat G06 di bagian 10).
- Tiga gerakan dibedakan lewat kode huruf, judul, dan thumbnail, bukan lewat warna yang berbeda-beda.

---

## 2. Warna

### 2.1 Token Final

| Token | Hex | Peran |
|---|---|---|
| `AppDeepForest` | `#12372A` | Identitas utama. Background full-bleed (opening, camera-denied), header sheet, elemen yang butuh bobot visual kuat. |
| `AppForestGreen` | `#1F5D42` | Primary action, active state, progress fill, badge, selected state. |
| `AppWarmCream` | `#F4F0E6` | Background pendukung, bukan warna dominan. |
| `AppWarmWhite` | `#FFFFFE` | Surface: card, bottom sheet, dialog, floating control. Near-white, bukan putih murni. |
| `AppSoftSand` | `#EADDCF` | Secondary surface: divider, track slider/progress, disabled state, badge sekunder. |
| `AppSecondaryText` | `#716040` | Body, paragraph, supporting text di atas background terang. |
| `AppError` | `#F25042` | Hanya untuk error dan permission gagal. Tidak dipakai dekoratif. |

### 2.2 Aturan Pemakaian

- Kontras teks minimal 4.5:1. Jangan turunkan opacity teks utama untuk "melembutkan" tampilan, kalau butuh nuansa lembut pakai `AppSecondaryText`, bukan opacity rendah dari warna gelap.
- Deep Forest dipakai lega, bukan cuma aksen tipis: header, background penuh di opening dan camera-denied, primary title.
- Forest Green khusus elemen dengan makna "aktif/utama": tombol primary, progress fill, badge, state terpilih. Kalau semua elemen dibikin Forest Green, hierarkinya hilang, jadi pakai secukupnya.
- Warm Cream dan Soft Sand tidak boleh mendominasi. Ini keluhan eksplisit dari generasi 3 terhadap versi sebelumnya (G01, G02, G05, G06, G08, G09 semuanya kebanyakan cream). Cara mengurangi dominasi cream: header Deep Forest, tombol primary Forest Green, badge Deep Forest/Forest Green, title Deep Forest.
- Satu sistem warna untuk SQ, DS, LD. Pembedanya kode huruf, judul, dan thumbnail, bukan warna latar.
- Live camera feed tidak boleh diberi filter warna apapun. Palet hanya berlaku untuk lapisan UI di atas kamera.

### 2.3 Token Lama (Deprecated)

| Token Lama | Hex | Asal | Alasan Dihapus |
|---|---|---|---|
| Headline/Dark Stroke | `#020826` | Gen 2 | Digantikan Deep Forest sebagai warna gelap utama |
| Primary/Highlight | `#8C7851` | Gen 2 | Digantikan Forest Green sebagai warna aksi utama |
| Dark Forest | `#0D2E24` | Gen 1 | Cukup satu token gelap (Deep Forest) |
| Surface Cream | `#FBF8F0` | Gen 1 | Digantikan Warm White |
| Charcoal | `#202620` | Gen 1 | Tidak dipakai |
| Soft Sage | `#A9BEA2` | Gen 1 | Tidak dipakai |
| Terracotta | `#B8684A` | Gen 1 | Dulu warna khusus Squat, sekarang satu sistem warna |
| Muted Teal | `#3F7C78` | Gen 1 | Dulu warna khusus Dynamic Stretching, sekarang satu sistem warna |
| Muted Mustard | `#C3A24B` | Gen 1 | Dulu warna khusus Ladder Drill, sekarang satu sistem warna |
| Putih polos | `#FFFFFF` | Gen 1 dan 2 | Diganti Warm White `#FFFFFE` |

Biru dan ungu tidak boleh muncul di UI sama sekali, termasuk di corner viewfinder, slider, dan track.

---

## 3. Tipografi

Font final: **Poppins** (menggantikan Inter dari generasi 1). Static TTF dari sumber resmi Google Fonts, 4 weight: Regular 400, Medium 500, SemiBold 600, Bold 700.

Implementasi: simpan di `Assets/App/UI/Fonts/Poppins/`, sertakan file SIL Open Font License, buat TextMeshPro Font Asset (SDF) terpisah untuk tiap weight, jangan fake bold, jangan unduh font saat runtime, pastikan karakter Bahasa Indonesia dan tanda baca lengkap.

### 3.1 Skala Tipografi

| Level | Ukuran (logical units) | Weight |
|---|---|---|
| Opening / app title | 42-48 | Bold 700 |
| Display title | 28-30 | Bold 700 |
| Screen title | 22-24 | SemiBold 600 |
| Movement title | 18-20 | SemiBold 600 |
| Section heading | 16-18 | SemiBold 600 |
| Body | 14-16 | Regular 400 |
| Supporting | 12-13 | Regular 400 atau Medium 500 |
| Button | 14-16 | SemiBold 600 |
| Chip | 11-12 | SemiBold 600 |

### 3.2 Aturan

- Body pakai line height sekitar 1.4.
- Jangan pakai satu weight untuk seluruh UI, weight adalah alat hierarki utama.
- Jangan TMP Auto Size tanpa batas ketat, dan jangan kecilkan font supaya muat, perbaiki container dan wrapping-nya.
- Teks penting tidak boleh di bawah 12 logical units, dan tidak boleh dipotong pakai ellipsis. Ini bukan cuma aturan di atas kertas, tapi bug nyata yang kejadian di dua layar berbeda, lihat G03 dan G09 di bagian 10.
- Deskripsi movement card maksimal dua baris.

---

## 4. Ikon

Lucide Icons, format SVG, diimpor jadi Sprite lewat package resmi Unity Vector Graphics yang kompatibel Unity 6.5, ditampilkan lewat `UnityEngine.UI.Image` dengan Preserve Aspect aktif dan Raycast Target dimatikan di child icon. Ukuran visual 20-24 logical units, touch target parent minimal 44-48. Kalau Vector Graphics tidak kompatibel, pakai PNG transparan 96x96 sebagai fallback, tetap simpan SVG sebagai source.

Simpan di `Assets/App/UI/Icons/Lucide/`, sertakan `LICENSE.txt` dari Lucide.

### 4.1 Pemetaan Ikon

| Ikon (Lucide) | Fungsi |
|---|---|
| `book-open` | Materi / konten gerakan |
| `play` | Audio: mulai |
| `pause` | Audio: jeda |
| `rotate-ccw` | Kembali ke mode Gerak Otomatis |
| `circle-help` | Panduan / bantuan |
| `arrow-left` | Kembali (navigasi antar layar) |
| `x` | Tutup |
| `x-circle` | Item pada daftar "Hindari Ini" |
| `scan-line` | Indikator scan pada instruction card |
| `camera` | Ikon kamera (permission, camera-denied) |
| `info` | Notice informasi |
| `shield-check` | Notice keselamatan |
| `chevron-right` | Buka detail / item list yang bisa ditekan |
| `chevron-down` | Warning collapsed (tertutup), lihat G08 |
| `chevron-up` | Warning expanded (terbuka), lihat G08 |
| `refresh-cw` | Coba lagi |

Catatan: `chevron-down` dan `chevron-up` belum pernah ada di daftar SVG manapun dari tiga generasi prompt sebelumnya, padahal dibutuhkan untuk perilaku warning collapsible di G08 yang baru diminta di generasi 3. Tambahkan dua file ini ke folder ikon.

Sudah tidak dipakai dari generasi 1 (audio dulu berbasis volume, sekarang berbasis play/pause): `volume-2`, `volume-x`, `headphones`.

Dilarang membuat ikon dari huruf, teks TextMeshPro, emoji, atau primitive yang menyerupai huruf. Ini termasuk memakai karakter seperti `[!]` sebagai pengganti ikon, lihat catatan regresi di G06 pada bagian 10.

---

## 5. Bentuk dan Radius

Tidak ada brush, blob, squiggle, oval lancip, vertical decorative color bar, atau bentuk organik lainnya.

Sprite 9-slice rounded rectangle, disimpan di `Assets/App/UI/Sprites/Shapes/`, Image Type Sliced, warna diatur lewat `Image.color`, jangan bikin PNG terpisah untuk tiap warna:

| Sprite | Radius | Dipakai untuk |
|---|---|---|
| RoundedRect-08 | 8 | Elemen kecil, dasar chip |
| RoundedRect-12 | 12 | Instruction card |
| RoundedRect-16 | 16 | General card, movement card |
| RoundedRect-24 | 24 (maksimal, sudut atas saja) | Bottom sheet |

Ukuran spesifik: chip tinggi 28-32 radius 8-10 (radius tidak boleh setengah tinggi, jangan sampai jadi pill), code badge 40x40 radius 10, general/movement card radius 14-16, bottom control radius 16-18, bottom sheet radius atas maksimal 24.

Satu-satunya pengecualian aturan "tidak boleh pill" adalah track slider di G05, karena itu kontrol interaktif, bukan badge atau chip. Detail di 7.8.

### 5.1 Sistem Garis Lurus, Anti-Taper

Bagian paling ditekankan di prompt generasi 3, disebut eksplisit sebagai **acceptance blocker**: progress bar dan detection sweep yang meruncing di ujung (bentuk kerucut) tidak boleh lolos.

Buat satu sprite reusable bernama `UISolidRectangle`: texture minimal 4x4, seluruh pixel putih solid, alpha 1 di semua pixel, tidak ada transparent padding, tidak ada gradient, Pivot Center, Mesh Type Full Rect, Wrap Mode Clamp, Filter Mode Bilinear, Compression None.

Semua garis lurus (progress bar, detection sweep, fill body slider) wajib pakai sprite ini dengan aturan: Image Type Simple, Preserve Aspect Off, localScale (1,1,1), rotation 0. Panjang berubah lewat `anchorMax.x` atau `sizeDelta.x`, bukan lewat Transform Scale X. Ketebalan seragam ujung ke ujung, kedua ujung datar dan identik.

Dilarang: brush sprite, gradient alpha, radial fill, sprite yang sudah meruncing dari sumbernya, transparent padding, custom mesh, diagonal mask, non-uniform scale, 9-slice dengan border kiri-kanan tidak simetris, material dissolve/soft-edge, radius lebih besar dari setengah tinggi.

Validasi wajib pada progress 10%, 25%, 50%, 75%, dan 100%: tinggi fill sama persis dari kiri sampai kanan, kedua ujung identik bentuknya, tidak boleh ada bagian yang mengecil saat panjangnya berubah. Kalau memungkinkan, tambahkan validasi otomatis di generator/build yang gagal kalau sprite garis bukan `UISolidRectangle`, localScale bukan (1,1,1), ada material gradient/dissolve, atau fill memakai Transform Scale X.

---

## 6. Spacing dan Layout

Semua angka ukuran di dokumen ini logical units, bukan pixel fisik perangkat.

Canvas Scaler: Scale With Screen Size, Reference Resolution 360x800, Screen Match Mode Match Width Or Height dengan Match 0.5, tidak pakai Constant Pixel Size, tidak mengatur posisi dari koordinat layar absolut.

Spacing scale: 4 (micro), 8 (icon/teks), 12 (komponen compact), 16 (padding standar), 20 (margin layar), 24 (section), 32 (pemisah besar).

`SafeAreaController`: membaca `Screen.safeArea`, mengubahnya jadi `anchorMin`/`anchorMax`, memperbarui layout saat orientasi atau resolusi berubah, tidak membatasi camera background full-screen.

Struktur canvas:

```
Canvas
├── CameraBackground
├── FullScreenBackground
└── SafeArea
    ├── TopContent
    ├── CenterContent
    ├── FloatingActions
    └── BottomContent
```

Top content anchor ke atas, bottom control anchor ke bawah, floating button anchor ke kanan, card/list stretch horizontal, viewfinder dihitung dari luas Safe Area, bottom sheet pakai persentase tinggi Safe Area.

Tablet: max content width 480, container dipusatkan, card jangan diregangkan sampai terlalu lebar.

Validasi wajib di beberapa Game View: 320x568, 360x800, 393x873, 412x915, 800x1280. Yang divalidasi cuma clipping, overlap, Safe Area, wrapping, dan touch target, bukan estetika.

---

## 7. Sistem Komponen

### 7.1 Button

Primary: background Forest Green, teks Warm White, tinggi 48-52, teks benar-benar center horizontal dan vertikal, padding kiri-kanan simetris. Secondary: background Warm Cream atau Warm White dengan border Soft Sand, teks Deep Forest. Kalau button punya ikon opsional, pastikan ada varian tanpa ikon yang teksnya tetap center sempurna, jangan menyisakan ruang kosong bekas slot ikon (lihat bug tombol MULAI di G02, bagian 10).

### 7.2 Chip

Tinggi 28-32, radius 8-10, tidak boleh jadi pill.

### 7.3 Card

Instruction card radius 12. General/movement card radius 14-16, stretch horizontal, padding 16. Movement card berisi badge 40x40, judul 16-18 SemiBold, body 13-14 maksimal dua baris, ikon `chevron-right`, tanpa brush/blob/vertical color bar.

### 7.4 Badge

Number badge 28-32, background Forest Green, angka Poppins SemiBold, teks Warm White. Code badge (SQ/DS/LD) 40x40 radius 10.

### 7.5 Floating Icon Button

```
IconButton
├── Background : Image
└── Icon : Image
```

Ukuran visual button 44-48, ikon 20-24, gap vertikal antar tombol 12-16, susun pakai VerticalLayoutGroup, alignment center, posisi mengikuti Safe Area, tidak menempel tepi layar. Background Deep Forest atau Forest Green, ikon Warm White. Murni ikon, tanpa label teks di bawahnya.

### 7.6 Bottom Sheet (Base)

State Closed, Half (~48% Safe Area), Full (~94% Safe Area). Radius atas maksimal 24, drag handle 36x4, padding 20. Tidak boleh menghentikan camera feed pada bagian layar yang masih terlihat, khususnya saat state Half.

### 7.7 StraightProgressBar

Dipakai di G01. Struktur Track dan Fill, keduanya pakai sprite `UISolidRectangle` (lihat 5.1). Track warna Soft Sand tinggi 4-5. Fill warna Forest Green atau Warm White tergantung kontras background, tinggi identik dengan Track, panjang berubah lewat `anchorMax.x`.

### 7.8 PoseSlider

Dipakai di G05. Komponen paling kompleks di sistem ini, harus mengikuti referensi visual yang lebih modern (kapsul horizontal, bukan balok tinggi vertikal) sekaligus tetap anti-taper di bagian fill.

```
PoseSlider
├── TrackContainer
│   ├── TrackBackground
│   ├── ActiveFill
│   │   ├── LeftCap
│   │   ├── FillBody
│   │   └── RightCap
│   └── NodeContainer
│       ├── Node01
│       ├── Node02
│       └── NodeNN
├── HandleTouchArea
│   └── HandleVisual
└── LabelRow
```

TrackContainer: anchor stretch mengikuti lebar SliderCard, tinggi visual 28-32, padding horizontal internal 12-16, tidak ikut membesar vertikal saat card berubah tinggi.

TrackBackground: bentuk kapsul horizontal (pengecualian aturan anti-pill di bagian 5), warna Soft Sand, tidak taper, ujung kiri-kanan identik.

ActiveFill: tinggi 6-8, warna Forest Green, center vertikal dalam TrackContainer. FillBody pakai `UISolidRectangle`, berubah panjang mengikuti nilai slider, bukan Transform Scale X. LeftCap dan RightCap berbentuk lingkaran solid, diameter sama dengan tinggi FillBody, tidak ikut diregangkan. Hanya FillBody yang berubah panjang, RightCap bergerak mengikuti ujung FillBody.

HandleVisual: lingkaran, width sama dengan height, ukuran visual 26-30, warna Warm White, shadow Deep Forest opacity rendah. Bukan oval vertikal, bukan capsule, bukan balok tinggi. HandleTouchArea 44-48, transparan, lebih besar dari visual tapi tidak mengubah bentuk visualnya.

Node: lingkaran, width sama dengan height, ukuran visual 9-11, tidak stretch. Warna belum-dilewati `AppSecondaryText`, warna aktif/dilewati `AppForestGreen`. Posisi dihitung dari normalized frame position 0-1 berdasarkan AvailableTrackWidth (lebar area track dikurangi padding kiri-kanan), otomatis menyesuaikan saat card atau resolusi berubah, tidak boleh posisi pixel absolut.

LabelRow: "Mulai" rata kiri, "Geser untuk memeriksa pose" rata tengah, "Selesai" rata kanan, Poppins 12-13, warna `AppSecondaryText`.

Validasi wajib di nilai minimum, node pertama, pertengahan, node terakhir, dan nilai maksimum: active fill tetap rata, cap tidak meregang, handle tetap lingkaran, node tetap lingkaran.

### 7.9 Viewfinder dan DetectionSweep

```
ViewfinderRoot
├── TopLeftCorner
├── TopRightCorner
├── BottomLeftCorner
├── BottomRightCorner
└── DetectionSweep
```

Corner: bentuk L, warna solid Warm White, ketebalan sekitar 3, panjang 22-26, inner corner sedikit tumpul (radius visual 4-6), simetris dan rapat ke area target. Ini poin yang eksplisit direvisi di generasi 3: **tidak boleh ada transparansi, blur, backdrop blur, frosted-glass, feather/glow, shadow blur, atau opacity rendah pada corner**, sekalipun generasi sebelumnya sempat meminta scanner surface yang lembut. Corner harus tetap solid dan tajam walau sudutnya sedikit tumpul.

DetectionSweep: sebelum target terdeteksi, tidak aktif sama sekali, cuma corner statis yang tampil. Setelah TargetConfirmed, tampilkan garis horizontal solid Warm White bergerak satu kali dari atas ke bawah, durasi 0.8-1.2 detik, ease-in-out ringan, fade-in dan fade-out, tinggi sekitar 2, tidak looping. Pakai sprite `UISolidRectangle` yang sama dengan progress bar, gerakan lewat `anchoredPosition.y`, bukan scale. Boleh diputar ulang hanya kalau target benar-benar hilang melewati grace period lalu ditemukan lagi. Sweep murni efek konfirmasi, kamera dan tracking tetap jalan seperti biasa.

---

## 8. Kamera, Rendering, dan Anti-Aliasing

### 8.1 Camera Stack (Non-Negotiable)

Urutan render: live camera ARUnityX, lalu objek 3D, lalu UI.

Base Camera: Render Type Base, presenter ARUnityX tetap enabled, texture tetap terpasang, frame tetap bertambah, tidak pernah dinonaktifkan saat target ditemukan. Overlay Camera: Render Type Overlay, terdaftar di stack Base Camera, hanya merender ARContent, tidak membersihkan color buffer, background transparan, tidak jadi Base Camera kedua.

Event TargetFound/TargetConfirmed hanya boleh mengaktifkan model, update pose, update status, dan memutar DetectionSweep satu kali. Event ini tidak boleh menonaktifkan camera presenter, mengganti clear color, mengaktifkan background sebagai pengganti kamera, mengganti target texture, atau menghentikan frame kamera.

### 8.2 Camera Readiness Gate

Kamera dinyatakan siap kalau permission granted, texture tidak null, dimensi valid, texture terpasang ke renderer, frame counter berubah, minimal 5 frame berurutan diterima, dan frame bukan warna hijau/kuning seragam. Sampling pixel untuk cek ini hanya dilakukan saat startup, bukan terus-menerus.

Setelah siap, tunggu satu render frame, crossfade overlay 250-350ms, baru tampilkan scanner. Kalau kamera tidak siap dalam 10-12 detik, tampilkan state timeout (lihat G09b di bagian 10).

**Catatan risiko baru** (murni hasil cross-check dengan screenshot terbaru, belum ada di prompt manapun): dulu, layar hijau/kuning seragam dipakai sebagai penanda kamera belum siap. Sekarang, warna identitas resmi UI justru Deep Forest Green yang solid. Ini berarti kalau nanti di device asli kamera gagal siap dan fallback-nya kebetulan terlihat serupa dengan Deep Forest, bisa salah kira "sudah benar" padahal kamera belum menyala. Screenshot `g03_scanner_actual.png` yang dikirim terakhir tampil hijau rata dari atas ke bawah tanpa tekstur kamera, kemungkinan besar karena diambil dari Unity Editor tanpa kamera fisik (device sedang dilepas), jadi wajar untuk saat ini. Begitu device tersambung lagi, pastikan verifikasi dilakukan dengan benar-benar menggerakkan kamera dan mengecek ada perubahan feed di baliknya, bukan cuma mengandalkan warnanya sudah hijau forest.

### 8.3 Anti-Aliasing dan Kualitas

MSAA 4x pada URP Asset Android, Camera Allow MSAA aktif, Render Scale 1.0, OpenGLES3. Overlay Camera mengikuti setting MSAA Base Camera. HDR dan post-processing berat dimatikan kalau tidak perlu. Fallback ke MSAA 2x hanya kalau 4x tidak didukung, dan hanya setelah pengujian performa langsung di device. Jangan tambah FXAA/SMAA bersamaan dengan MSAA tanpa pengukuran nyata. UI pakai TextMeshPro SDF, Lucide lewat Vector Graphics kalau kompatibel, Bilinear filtering, jangan perbesar PNG kecil, jangan pakai half-pixel anchored position untuk garis tipis.

---

## 9. Flow dan State Aplikasi

Fresh install: Opening (G01) lalu Sebelum Mulai (G02) lalu permission kamera lalu opening cover menyala di belakang layar sambil kamera disiapkan lalu Scanner (G03) atau Non-AR Catalog (G08) kalau device tidak mendukung AR. Kunjungan berikutnya: Opening (G01) langsung ke Scanner (G03) atau Non-AR Catalog (G08).

Flag onboarding: `gerakar.onboarding.completed.v1`, disimpan cuma setelah tombol MULAI ditekan, bukan setelah G02 sekadar ditampilkan. Kalau app ditutup sebelum tombol MULAI ditekan, onboarding tampil lagi. Reset flag ini cuma lewat editor/development utility.

Tidak ada screen loading kamera terpisah. Alur yang salah: Opening lalu Loading Kamera lalu Scanner. Alur yang benar: Opening Cover langsung ke Scanner yang kameranya sudah siap.

Dari Scanner (G03), begitu target terdeteksi, viewfinder digantikan tampilan Player (G05). Dari Player, tap tombol Materi memunculkan Bottom Sheet Materi (G06) menutupi sebagian layar (state Half secara default). Dari Non-AR Catalog (G08), tap movement card langsung membuka G06 tanpa lewat G03/G05.

---

## 10. Spesifikasi per Layar

### G01, Opening

**Fungsi:** Cover pembuka, menahan tampilan sampai kamera siap, sekaligus tempat loading indicator. Tidak ada transisi ke screen loading kamera terpisah.

**Masalah pada versi saat ini** (bukti: `gerakar-current-g01-opening.png`):
- [ ] Layout tampil seperti card/popup terpusat dengan margin di sekitarnya, bukan full-bleed penuh layar.
- [ ] Progress bar meruncing di kedua ujung, bentuknya kerucut bukan persegi rata. Ini acceptance blocker, bukan cacat kecil.
- [ ] Ilustrasi tengah berupa deretan bentuk menyerupai sendok, belum merepresentasikan apapun yang berkaitan dengan gerakan olahraga.

**Spec target (Wajib):**

```
G01Opening
├── FullBleedCoverImage
├── TopIdentity
├── CenterVisual
└── BottomLoading
    └── StraightProgressBar (lihat 7.7)
```

FullBleedCoverImage stretch ke seluruh layar, tanpa outer margin, tanpa rounded popup, tanpa white card di tengah, tanpa drop shadow card. Pakai aspect fill/envelope, crop aman kalau rasio device berbeda. Seluruh background nantinya diganti satu image asset penuh oleh pemilik project, jadi struktur ini harus siap menerima itu tanpa perubahan layout. Sebelum gambar cover final ada, pakai background Deep Forest solid penuh. TopIdentity: "Media Pembelajaran" / "Skripsi Pendidikan SD", logo UNP kecil kanan atas kalau asetnya tersedia. BottomLoading: judul "GerakAR" Warm White, subtitle Soft Sand/Warm Cream, StraightProgressBar, helper text "Memuat pengalaman belajar" tanpa titik tiga animasi dan tanpa angka persentase.

Unity Splash Screen dan Unity Logo dimatikan kalau lisensi mengizinkan, tanpa patch/hack. Android launch background disamakan dengan Deep Forest supaya tidak ada flash warna saat app pertama dibuka.

**Checklist verifikasi:**
- [ ] Full-bleed, tidak ada popup/card mengambang
- [ ] Progress bar rata kedua ujung pada progress 10%, 25%, 50%, 75%, 100%
- [ ] Tidak ada ellipsis animasi atau angka persentase
- [ ] Background siap diganti satu image asset tunggal tanpa restrukturisasi layout

---

### G02, Onboarding (Sebelum Mulai)

**Fungsi:** Muncul sekali di instalasi pertama, berisi 3 poin keselamatan sebelum mulai belajar, lalu meminta permission kamera.

**Masalah pada versi saat ini** (bukti: `gerakar-current-g02-onboarding.png`):
- [ ] Judul "Sebelum Mulai" masih warna gelap generik, belum Deep Forest.
- [ ] Number badge masih warna gelap generik, belum Forest Green.
- [ ] Tombol MULAI berwarna coklat-mustard (peninggalan token lama), belum Forest Green.
- [ ] Ada ikon kamera di tombol MULAI, seharusnya teks murni.
- [ ] Karena ada ikon di kiri, teks "MULAI" jadi tidak benar-benar center.

**Spec target (Wajib):** Title "Sebelum Mulai" Deep Forest, supporting text "Ayo bergerak dengan aman dan nyaman." Secondary Text. List 3 poin (pakai di tempat luas, minta pendampingan guru/orang tua, izinkan kamera): card Warm White radius 12-14, number badge 28-32 Forest Green, gap badge-teks 12, tanpa vertical color bar. Tombol MULAI: hanya teks "MULAI" tanpa ikon apapun, background Forest Green, teks Warm White, tinggi 48-52, benar-benar center horizontal dan vertikal.

**Checklist verifikasi:**
- [ ] Title dan badge hijau, bukan gelap generik
- [ ] Tombol MULAI hijau, tanpa ikon, teks center sempurna
- [ ] Flag `gerakar.onboarding.completed.v1` cuma tersimpan setelah MULAI ditekan

---

### G03, Scanner

**Fungsi:** Live camera full-screen dengan viewfinder, menunggu target gambar terdeteksi.

**Masalah pada versi lama** (bukti: `gerakar-current-g03-scanner.png`):
- [ ] Background masih cream, seharusnya live camera penuh layar tanpa filter warna.
- [ ] Corner viewfinder terlalu tipis/pudar, nyaris tidak terlihat di atas background cream.
- [ ] Instruction card di bawah memotong teks dengan ellipsis padahal bisa dibuat wrapping dua baris.

**Sudah sesuai di versi actual** (bukti: `g03_scanner_actual.png`): background sudah Deep Forest, judul dan subtitle sudah pakai warna yang benar, corner viewfinder sudah terlihat jelas berbentuk L, teks instruksi bawah sudah tidak terpotong.

**Masih perlu diperbaiki di versi actual:**
- [ ] Panjang dan jarak corner viewfinder terlihat lebih lebar dari target spec (22-26 logical units, rapat ke area target). Perlu diukur ulang dan dirapatkan.
- [ ] Belum jelas apakah instruction card punya surface Deep Forest tersendiri yang terpisah dari background scanner, atau menyatu begitu saja karena backgroundnya kebetulan sewarna. Pastikan begitu live camera asli aktif di baliknya, teks instruksi tetap terbaca jelas.

**Spec target (Wajib):** Live camera full-screen tanpa filter warna, gradient tipis di atas dan bawah kalau perlu. Instruction card: background Deep Forest, headline Warm White, supporting Soft Sand, ikon `scan-line`, margin 20, radius 12, teks "Arahkan kamera ke gambar gerakan" / "Pastikan seluruh gambar terlihat jelas". Viewfinder dan DetectionSweep: lihat 7.9. Floating actions kanan: Materi (`book-open`), Audio (`play`/`pause`), Panduan (`circle-help`), Kembali (`arrow-left`). Mode default "Gerak Otomatis", berubah jadi "Mode Per Frame" saat slider disentuh, tombol kembali pakai `rotate-ccw` label "Kembali Otomatis".

**Checklist verifikasi:**
- [ ] Live camera terlihat penuh, tanpa filter
- [ ] Corner solid, tanpa blur/transparansi, panjang 22-26, rapat ke target
- [ ] Sweep hanya aktif setelah TargetConfirmed, satu kali, tidak looping
- [ ] Teks instruksi tidak terpotong ellipsis

---

### G05, Player

**Fungsi:** Tampil setelah target terdeteksi. Viewfinder digantikan model 3D yang di-tracking, dilengkapi floating buttons dan slider timeline pose.

**Masalah pada versi saat ini** (bukti: `gerakar-current-g05-player.png`):
- [ ] Floating buttons (Audio, Materi, Tutup) masih pakai label teks di bawah tiap ikon, seharusnya ikon murni tanpa label.
- [ ] Background floating buttons masih gelap generik, belum Deep Forest/Forest Green.
- [ ] Handle slider berbentuk oval panjang vertikal, seharusnya lingkaran.
- [ ] Ada chip "LOOP" di kartu slider yang belum dibahas di prompt manapun, lihat item 1 di bagian 12.
- [ ] Track slider ikut jadi tinggi/besar mengikuti handle yang salah bentuk.

**Spec target (Wajib):** Floating buttons ikon murni tanpa label, background Deep Forest/Forest Green, ikon Warm White, ukuran visual 44-48, ikon 20-24, gap vertikal 12-16, urutan dari atas Play/Pause, Materi, Tutup. Audio tidak pakai ikon volume/speaker, murni `play`/`pause`, audio tidak mulai otomatis. Slider card: background Warm White, tinggi mengikuti content sekitar 112-128, heading Deep Forest, body Secondary Text, elemen aktif Forest Green. PoseSlider: lihat 7.8. Label "Mulai" kiri, "Geser untuk memeriksa pose" tengah, "Selesai" kanan.

**Checklist verifikasi:**
- [ ] Floating button tanpa label, warna hijau
- [ ] Handle slider lingkaran, bukan oval vertikal
- [ ] Active fill tidak taper di semua posisi slider
- [ ] Track responsif mengikuti lebar card, tidak hardcoded

---

### G06, Materi (Bottom Sheet)

**Fungsi:** Detail gerakan, muncul dari tap tombol Materi di G05 (mode AR) atau tap movement card di G08 (mode Non-AR). AR dan Non-AR memakai komponen materi yang sama.

**Masalah pada versi lama** (bukti: `gerakar-current-g06-material.png`):
- [ ] Tombol X di header terlalu besar secara visual, mendominasi.
- [ ] Sebagian besar section cuma menampilkan heading tanpa isi, bukan desain minimalis, ini bug konten belum diisi.

**Sudah sesuai di versi actual** (bukti: `g06_bottom_sheet_actual.png`): section "Tentang Gerakan" dan "Cara Melakukan" sudah terisi konten nyata, numbered badge di "Cara Melakukan" sudah Forest Green dengan card putih.

**Regresi yang perlu diperbaiki di versi actual:**
- [ ] Notice keselamatan sekarang memakai teks "[!]" sebagai pengganti ikon, padahal versi lama sempat sudah benar memakai ikon `shield-check`. Kembalikan ke ikon, jangan text glyph.
- [ ] Section "Hindari Ini" menampilkan kotak warna merah polos tanpa ikon, seharusnya tiap item punya card dengan ikon `x-circle`.
- [ ] Section "Otot yang Terlatih" dirender sebagai pill/kapsul panjang bertumpuk vertikal, ini melanggar aturan eksplisit "tidak menggunakan pill panjang" untuk section ini. Ganti jadi compact card atau grid.

**Spec target (Wajib):** State Closed, Half (~48% Safe Area), Full (~94% Safe Area), radius atas maksimal 24, drag handle 36x4, padding 20. Header Deep Forest, heading Warm White. Tombol X: visual background 34-40, ikon 18-20, touch target tetap 44-48, background Forest Green kalau di atas header Deep Forest. Body sheet Warm White, section heading Deep Forest, list card Warm Cream atau Warm White, badge/icon Forest Green. Urutan section Full: Tentang Gerakan, Cara Melakukan, Hindari Ini, Otot/Bagian Tubuh yang Dilatih, Gerakan Serupa, semuanya wajib berisi konten nyata. Cara Melakukan: numbered card badge 28-32 Forest Green. Hindari Ini: card terpisah per item dengan ikon `x-circle`. Otot yang Terlatih: compact card atau grid, bukan pill. Gerakan Serupa: card dengan thumbnail, judul, supporting text, `chevron-right`.

**Rekomendasi** (dari referensi `g06_bottom_sheet_ui.png`):
- Header sheet menampilkan kicker kecil "GERAKAN UTAMA" di atas judul besar nama gerakan, diikuti dua tombol berdampingan: pill "Kembali" (Forest Green, rounded full, dipakai kalau sheet dibuka lewat navigasi dari "Gerakan Serupa" di gerakan lain) dan tombol bulat X (tutup sheet sepenuhnya). Ini pola yang lebih modern dibanding tombol "Kembali" full-width terpisah di bagian bawah seperti versi lama.
- Area di atas sheet saat state Half, dalam konteks AR, memang sudah seharusnya menampilkan live camera plus model 3D yang di-tracking (aturan Wajib di 8.1, mockup ini cuma mengonfirmasi visualnya). Untuk konteks Non-AR yang tidak punya feed kamera, area yang sama sebaiknya diisi render statis karakter 3D stylized yang sedang dipakai (pria, tracksuit forest green, konsisten dengan pipeline aset AR Flipbook), bukan foto stok seperti di mockup referensi, supaya satu sistem visual dengan thumbnail movement card di G08.
- Safety notice sebaiknya pakai tint hijau lembut (sekitar 8% opacity dari Forest Green) sebagai background, border Soft Sand, bukan cream polos.

**Checklist verifikasi:**
- [ ] Semua section berisi konten nyata
- [ ] Ikon shield-check dan x-circle dipakai dengan benar, bukan text glyph atau kotak warna polos
- [ ] Otot yang Terlatih bukan pill
- [ ] Tombol X ukuran wajar, tidak mendominasi header

---

### G08, Non-AR Catalog

**Fungsi:** Fallback kalau device tidak mendukung AR. Daftar 3 gerakan bisa dipelajari lewat materi dan audio tanpa kamera.

**Masalah pada versi saat ini** (bukti: `gerakar-current-g08-catalog.png`):
- [ ] Warning card langsung tampil penuh dan panjang begitu layar dibuka, seharusnya collapsed default.
- [ ] Copy yang terlihat di screenshot ("MODE PEMBELAJARAN MANDIRI", chip "NON-AR MODE") tidak sama dengan copy yang diminta konsisten di tiga generasi prompt. Lihat item 5 di bagian 12.
- [ ] Badge kode gerakan dan chevron masih warna coklat-mustard lama, belum Forest Green.
- [ ] Tombol kembali di bawah masih warna cream polos tanpa aksen.

**Spec target (Wajib):** Header heading "Belajar Gerakan", subheading "Pilih gerakan yang ingin kamu pelajari.", chip "Mode Tanpa Kamera", header background Deep Forest, heading Warm White.

Perilaku baru yang paling penting di layar ini: warning card **default collapsed**, cuma menampilkan ikon `info`, judul singkat "Mode tanpa kamera", supporting singkat "Ketuk untuk melihat penjelasan", dan `chevron-down`. Saat ditekan, expand dengan animasi tinggi halus, tampilkan teks lengkap "Perangkat belum mendukung mode AR. Kamu tetap dapat mempelajari seluruh gerakan, materi, dan panduan audio tanpa kamera.", chevron berubah jadi `chevron-up`. State expanded tersimpan selama screen aktif, bukan popup terpisah.

Movement card stretch horizontal radius 16 padding 16 gap 12, code badge Forest Green, judul 16-18 SemiBold, body 13-14 maksimal dua baris, `chevron-right` dengan container Forest Green. Tombol kembali background Deep Forest/Forest Green, teks/ikon Warm White.

**Checklist verifikasi:**
- [ ] Warning collapsed by default, expand hanya saat ditekan
- [ ] Copy heading dan chip sesuai spec final
- [ ] Tidak ada warna biru atau coklat-mustard lama tersisa

---

### G09, Camera Denied dan Timeout

**Fungsi:** Ditampilkan kalau permission kamera ditolak, atau kamera gagal siap dalam 10-12 detik. Ini dua state berbeda yang berbagi komponen visual yang sama.

**G09a, Permission Ditolak** (bukti: `gerakar-current-g09-camera-denied.png`)

**Masalah pada versi saat ini:**
- [ ] Background dan tombol utama masih memakai warna gelap generik, belum Deep Forest/Forest Green.
- [ ] Helper text di bawah terpotong ellipsis, padahal ini instruksi penting untuk minta bantuan.

**Spec target (Wajib):** Full background Deep Forest, central card Warm White. Icon container Soft Sand atau Forest Green, ikon `camera`. Title "Kamera Belum Aktif" Deep Forest, body "Izinkan akses kamera agar GerakAR dapat melihat gambar gerakan." Secondary Text, wrapping penuh tidak boleh terpotong. Primary "BUKA PENGATURAN" Forest Green teks Warm White. Secondary "Coba Lagi" Warm Cream teks Deep Forest. Helper text "Minta bantuan guru atau orang tua jika diperlukan." wrapping penuh, tidak boleh ellipsis.

**G09b, Kamera Timeout** (belum ada bukti screenshot, murni dari spec tertulis, tandai Perlu konfirmasi)

Kondisi ini beda dari G09a: bukan permission ditolak, tapi kamera gagal siap dalam 10-12 detik. Title "Kamera belum dapat dibuka", primary "Coba Lagi", secondary "Belajar Tanpa Kamera" (mengarah ke G08). Komponen visual sama seperti G09a, cuma copy dan dua tombol aksinya beda.

**Checklist verifikasi:**
- [ ] Background Deep Forest penuh, bukan hitam/navy
- [ ] Tidak ada teks terpotong ellipsis
- [ ] G09b dibuat sebagai varian dari komponen yang sama, bukan screen terpisah baru

---

## 11. Guardrail Repository dan Proses

Bukan soal visual, tapi wajib dipatuhi siapa pun yang mengeksekusi implementasi:

- Kerjakan hanya di branch `feature/natural-image-tracking`. Jangan bikin repository baru, jangan ubah `main`, `archive/ar-foundation`, atau `feature/vuforia`, jangan merge ke `main`.
- Jangan reset, force checkout, force push, atau rebase destruktif.
- Audit dulu sumber pembentukan UI (scene, prefab, runtime builder, atau `SetupAndBuild.cs`) sebelum mengubah apapun. Kalau UI dibentuk generator, perbaiki generator dan prefab sumbernya, bukan cuma scene hasil generate, supaya perubahan tidak hilang saat regenerate.
- Pertahankan ARUnityX, dual ABI (ARMv7 + ARM64), target C5 dengan physical width 120mm, dan camera stack Base+Overlay yang sudah berfungsi.
- Alur kerja: implementasi menyeluruh dulu tanpa review per halaman, verifikasi teknis sekali (git diff --check, compile, regenerate scene, build APK), ambil screenshot, baru berhenti. Penilaian estetika sepenuhnya di tangan pemilik project.
- Kalau device fisik sedang tidak tersambung, jangan polling ADB dan jangan menganggap physical test gagal, cukup laporkan APK siap diuji begitu device kembali.

---

## 12. Perlu Konfirmasi Pemilik Project

Enam hal ini ambigu antara screenshot dan teks prompt, dikumpulkan di sini supaya bisa direview sekaligus:

1. **Chip "LOOP" di G05.** Versi lama punya chip ini di kartu slider, tidak dibahas eksplisit di prompt manapun. Kemungkinan sudah digantikan sistem mode "Gerak Otomatis"/"Mode Per Frame". Dihapus dan digantikan indikator mode, atau tetap dipertahankan?
2. **Tombol "Kembali" di header G06.** Referensi mockup terbaru menaruhnya sebagai pill berdampingan dengan X di header sheet, beda dari versi lama yang menaruh "Kembali" sebagai tombol full-width di bawah. Dokumen ini mengasumsikan pola header dipakai untuk navigasi antar-gerakan lewat "Gerakan Serupa", X untuk menutup materi sepenuhnya. Asumsi ini benar?
3. **Visual di atas sheet G06 untuk mode Non-AR.** Rekomendasi di bagian 10 memakai render karakter 3D stylized yang sama dengan aset movement card, perlu dikonfirmasi.
4. **Sumber thumbnail "Gerakan Serupa".** Referensi mockup pakai foto asli sebagai placeholder. Rekomendasi memakai render 3D stylized yang konsisten dengan sistem visual lain. Setuju?
5. **Copy header G08.** Implementasi terakhir pakai "MODE PEMBELAJARAN MANDIRI" dan chip "NON-AR MODE", beda dari spec tertulis yang konsisten di tiga generasi ("Belajar Gerakan", chip "Mode Tanpa Kamera"). Mana yang final?
6. **Copy dan aksi G09b (timeout).** Belum ada bukti screenshot, dokumen ini menyusunnya murni dari spec tertulis. Perlu direview begitu ada screenshot atau hasil build terbaru.

---

## 13. Ringkasan Cepat

**Wajib ada:** Poppins semua weight, Lucide Icons ikon murni (bukan huruf/teks), Deep Forest sebagai identitas utama, garis lurus anti-taper di semua progress bar dan sweep, handle dan node slider berbentuk lingkaran, warning G08 collapsed by default, camera stack Base+Overlay utuh.

**Tidak boleh ada:** Font Inter, warna navy #020826, warna coklat #8C7851 lama, putih murni #FFFFFF, warna berbeda per gerakan, brush/blob/squiggle, pill di luar track slider dan chip, teks terpotong ellipsis untuk instruksi penting, ikon dari karakter seperti `[!]` atau huruf, label teks di floating button, progress bar atau sweep yang meruncing di ujung, transparansi/blur di corner viewfinder, merge ke branch `main`.

---

Dokumen ini dirancang jadi acuan tunggal. Semua keputusan warna dan font di sini final sampai ada instruksi baru yang eksplisit menyatakan menggantikan dokumen ini.
