# AGENTS.md — GerakAR

## 0. Kedudukan Dokumen

Dokumen ini adalah **sumber instruksi utama dan tunggal** untuk seluruh coding agent yang bekerja pada repository GerakAR. Cakupannya berlaku untuk semua file dan subfolder di repository ini.

Panduan pengembangan lama dan prompt implementasi lama telah digabungkan, dikoreksi, dan digantikan oleh dokumen ini. Jika ditemukan komentar, mockup, source lama, commit lama, atau dokumentasi lain yang bertentangan dengan `AGENTS.md`, gunakan urutan prioritas berikut:

1. Instruksi eksplisit terbaru dari pemilik project.
2. `AGENTS.md` ini.
3. Asset nyata yang tersedia dalam repository.
4. Dokumentasi resmi Unity, AR Foundation, dan ARCore untuk versi yang dipakai.
5. Dokumentasi lama hanya sebagai konteks historis, bukan sumber keputusan.

Jangan menghidupkan kembali keputusan yang sudah diganti hanya karena masih terlihat pada riwayat Git.

---

## 1. Identitas dan Tujuan Project

Nama aplikasi: **GerakAR**.

GerakAR adalah aplikasi mobile Augmented Reality untuk membantu anak Sekolah Dasar mempelajari gerakan olahraga melalui gambar target pada flipbook. Anak mengarahkan kamera ke gambar target, aplikasi mengenali gambar tersebut, lalu menampilkan model 3D yang memperagakan gerakan terkait.

Tiga materi utama:

1. **Squat**.
2. **Dynamic Stretching**.
3. **Ladder Drill**.

Pengguna:

- Pengguna utama: anak SD.
- Pengguna pendamping: guru, orang tua, atau fasilitator.
- Platform utama: Android, dikemas sebagai **AR Optional** agar tetap berguna pada perangkat yang tidak mendukung ARCore.
- Orientasi: portrait.
- Bahasa antarmuka: Bahasa Indonesia.

Tujuan versi awal:

- Memindai tepat tiga jenis gambar target.
- Menampilkan tepat satu model AR aktif pada satu waktu.
- Memutar animasi gerakan secara looping.
- Membolehkan anak memeriksa pose melalui timeline.
- Menampilkan materi singkat dan gerakan terkait.
- Menyediakan fallback materi non-AR untuk perangkat yang tidak mendukung AR.
- Menjaga privasi anak dan dapat digunakan secara offline setelah terpasang.

Project ini bukan game kompetitif. Jangan menambahkan skor, leaderboard, achievement, avatar, akun, iklan, atau gamifikasi kompleks.

---

## 2. Keputusan Terbaru yang Menggantikan Instruksi Lama

Desain HTML terbaru yang diberikan pemilik project adalah acuan visual dan interaksi. Keputusan berikut menggantikan aturan lama:

1. Terdapat **9 frame/state utama**, G01 sampai G09.
2. G01 memakai komposisi poster Green Forest dengan identitas Universitas Negeri Padang dan branding GerakAR.
3. G03–G05 menampilkan header GerakAR sesuai mockup terbaru.
4. Setelah target terbaca, G04 menampilkan konfirmasi singkat sebelum masuk ke G05.
5. G05 menyediakan tiga floating control:
   - Audio Play/Pause.
   - Materi.
   - Tutup/scan ulang.
6. Bottom sheet materi dibuka langsung ke posisi **Full sekitar 94%**, bukan Half terlebih dahulu.
7. G08 adalah fallback **Mode Pembelajaran Mandiri / Non-AR**.
8. G09 menangani izin kamera yang belum aktif.
9. Perangkat yang tidak mendukung ARCore tidak boleh masuk scene kamera. Tampilkan notice singkat bergaya G04, lalu arahkan otomatis ke G08.
10. G08 bukan halaman error. G08 adalah pengalaman belajar lengkap dengan preview gerakan, timeline pose, audio panduan, materi utama, dan gerakan terkait; hanya pemindaian gambar dan penempatan AR yang tidak tersedia.

Resolusi perbedaan dengan mockup:

- HTML/Tailwind hanya acuan visual. Aplikasi tetap diimplementasikan dengan Unity uGUI dan TextMeshPro.
- Crest UNP pada mockup adalah placeholder. Jangan menjadikannya logo resmi.
- Gunakan logo resmi **Universitas Negeri Padang (UNP)** dari folder `components` jika tersedia.
- Jangan menggambar ulang, mengarang, atau memodifikasi lambang resmi UNP.
- Konten demo Jumping Jacks dan Lunges pada G08 bukan materi final. Ganti dengan tiga materi sebenarnya: Squat, Dynamic Stretching, dan Ladder Drill.
- Emoji pada mockup bukan icon final. Gunakan sprite/icon yang konsisten.
- Link simulasi developer pada G02 tidak boleh tampil pada production build.

### 2.1 Keputusan kompatibilitas perangkat

Project wajib menggunakan strategi **AR Optional**, bukan AR Required, karena materi harus tetap dapat dipakai pada HP non-ARCore.

- Atur `Edit > Project Settings > XR Plug-in Management > ARCore > Requirement` menjadi `Optional`.
- Lakukan `ARSession.CheckAvailability()` sebelum memuat atau mengaktifkan `XROrigin`, `ARSession`, `ARCameraManager`, `ARCameraBackground`, dan `ARTrackedImageManager`.
- Jika state `NeedsInstall`, panggil `ARSession.Install()` hanya setelah hasil availability menyatakan perangkat memang mendukung provider tersebut.
- Jika hasil akhir `Unsupported`, instalasi gagal, atau layanan AR tidak dapat dipakai, jangan memaksa kamera AR dan jangan melakukan retry tanpa batas.
- Memasang Google Play Services for AR dari browser/APK tidak membuat perangkat yang tidak tersertifikasi menjadi kompatibel. Dukungan tergantung model perangkat, sistem operasi, driver, sensor, dan profil perangkat ARCore.
- Permission kamera dan kompatibilitas AR adalah dua gate yang berbeda. Jangan pernah memetakan keduanya ke satu state `Unsupported`.
- Mode Non-AR tidak meminta izin kamera dan tidak membuat AR subsystem.

Urutan gate production:

```text
Intro / onboarding
  -> CheckAvailability
     -> Unsupported: notice singkat -> NonARCatalog
     -> NeedsInstall: Install -> periksa ulang
        -> gagal/Unsupported: notice singkat -> NonARCatalog
        -> Ready: periksa permission kamera
     -> Ready: periksa permission kamera
        -> granted: load MainAR -> Scanning
        -> denied: CameraDenied
```

---

## 3. Mode Kerja Agent — Wajib

Agent tidak boleh berhenti pada analisis, rencana, atau tutorial. Agent harus mengubah project secara nyata dan bergerak secara bertahap sampai acceptance criteria terpenuhi atau ditemukan blocker nyata.

Siklus kerja wajib:

```text
Audit repository dan status Git
  -> baca AGENTS.md seluruhnya
  -> periksa asset nyata
  -> pilih satu perubahan logis
  -> implementasikan
  -> format dan validasi
  -> jalankan test/build yang relevan
  -> periksa git diff
  -> commit kecil
  -> push ke branch aktif
  -> lanjutkan perubahan logis berikutnya
```

### 3.1 Otonomi

Agent harus:

- Membaca seluruh `AGENTS.md` sebelum mengubah source.
- Memeriksa struktur repository dan `git status` sebelum bekerja.
- Melanjutkan pekerjaan rutin tanpa meminta konfirmasi berulang.
- Menggunakan placeholder apabila asset final belum tersedia.
- Menjaga setiap perubahan kecil, teruji, dan dapat dibatalkan.
- Memberikan update singkat setelah milestone, lalu melanjutkan pekerjaan.
- Memeriksa Console/build log sebelum mengklaim keberhasilan.
- Membedakan fakta terverifikasi, asumsi, placeholder, dan pekerjaan yang belum selesai.

Agent tidak boleh:

- Mengganti stack menjadi Kotlin/Jetpack Compose.
- Membuat project Android native baru.
- Mengganti AR Foundation dengan Vuforia tanpa bukti pengujian dan persetujuan pemilik.
- Mengunduh asset acak dari internet.
- Mengarang nama/path asset yang tidak ada.
- Mengklaim build berhasil tanpa menjalankan build atau validasi yang sesuai.
- Mengklaim model, cover, logo, audio, atau target final tersedia jika file tersebut belum ada.
- Menumpuk banyak fitur tidak terkait dalam satu commit.
- Menghapus perubahan pengguna yang tidak terkait.

### 3.2 Kondisi berhenti

Agent hanya boleh berhenti dan meminta bantuan apabila:

- Membutuhkan credential yang tidak tersedia.
- Push ditolak karena autentikasi atau hak akses.
- Keputusan baru akan mengubah arsitektur utama.
- Asset wajib tidak dapat digantikan placeholder dengan aman.
- Unity/package/build gagal setelah penyebab telah ditelusuri dan alternatif aman telah dicoba.
- Terdapat konflik Git atau perubahan pengguna yang tumpang tindih dan tidak aman diselesaikan otomatis.

Saat berhenti, jelaskan:

1. Apa yang sudah selesai.
2. Perintah/test yang sudah dijalankan.
3. Error persis yang tersisa.
4. Penyebab paling mungkin.
5. Satu tindakan paling kecil yang dibutuhkan dari pemilik project.

### 3.3 Baseline audit repository 15 Juli 2026

Audit read-only terhadap `origin/main` menemukan baseline berikut. Agent berikutnya wajib memverifikasi ulang karena source dapat berubah, lalu memperbaiki item yang masih relevan:

1. `ARAvailabilityChecker.cs` sudah memanggil `ARSession.CheckAvailability()` dan `Install()`, tetapi hasil unsupported hanya membuka panel; belum ada routing ke pengalaman Non-AR lengkap.
2. `PermissionController.cs` memetakan camera denied ke `AppState.Unsupported`. Ini salah; ubah ke `CameraDenied`.
3. `AppState.cs` belum memiliki `UnsupportedNotice`, `NonARCatalog`, `NonARMovementPlayer`, dan `ARInstallFailed` yang terpisah.
4. `OnboardingController.cs` menunggu state `Scanning` sebelum memuat `MainAR`. Pisahkan keputusan route dari state UI kamera agar `Scanning` hanya terjadi di scene AR yang siap.
5. `SetupAndBuild.cs` membuat `ARCameraBackground` untuk jalur AR, tetapi juga menghasilkan UI sederhana dengan font Inter dan `CanvasScaler` default. Output generator ini bukan implementasi visual final.
6. `app/ProjectSettings/EditorBuildSettings.asset` pada baseline hanya memuat `Assets/Scenes/SampleScene.unity`. Perbaiki daftar scene sebelum Build and Run biasa.
7. `AR Core Settings.asset` pada baseline memakai `m_Requirement: 0`. Verifikasi di Inspector dan ubah eksplisit menjadi `Optional`.
8. `ARUIController.cs` menghubungkan tombol audio ke pause/resume animasi. Pisahkan: audio button hanya mengontrol audio.
9. Extension `ActiveId()` pada baseline selalu mengembalikan `null`. Hapus extension palsu dan gunakan satu `ActiveMovementContext` yang authoritative.
10. `BottomSheetController.cs` masih memiliki Closed/Half/Full dan `Open()` menuju Half. Ubah agar tombol materi langsung membuka Full sekitar 94%.
11. Scene runtime belum tersimpan di `Assets/App/Scenes`; hanya `.gitkeep` pada baseline. Jangan mengandalkan scene yang hanya pernah dibuat sementara oleh editor script.

Jangan menambal gejala layar hitam dengan menaruh background gambar di belakang kamera AR. Perbaiki route compatibility dan lifecycle-nya.

---

## 4. Repository dan Workspace

Repository utama:

```text
https://github.com/ajmaaaa/gerakar.git
```

Struktur tingkat repository yang dituju:

```text
gerakar/
├── AGENTS.md
├── README.md
├── components/             # Source asset dari pemilik project
├── app/                    # Project Unity
│   ├── Assets/
│   ├── Packages/
│   └── ProjectSettings/
└── docs/                   # Hanya jika dokumentasi tambahan benar-benar diperlukan
```

Agent dijalankan dari root repository agar dapat membaca `AGENTS.md`, folder `components`, dan project Unity `app` sekaligus.

Jika folder `app` belum tersedia, buat project menggunakan Unity Hub dengan template **AR Mobile** dan nama folder `app`. Jangan membuat nested path `app/app`.

---

## 5. Tech Stack Final

| Bagian | Keputusan |
| --- | --- |
| Engine | Unity 6.5, editor `6000.5.3f1` atau patch stabil kompatibel yang sudah dipasang |
| Template | AR Mobile |
| Bahasa | C# |
| AR abstraction | AR Foundation 6.5.x atau versi released yang cocok dengan Unity 6.5 |
| Android provider | Google ARCore XR Plugin dengan minor version yang selaras dengan AR Foundation |
| XR management | XR Plug-in Management |
| Image tracking | `XRReferenceImageLibrary` + `ARTrackedImageManager` |
| Rendering | URP mobile dari AR Mobile template |
| UI | Unity uGUI + TextMeshPro |
| Input | Unity Input System; Active Input Handling disesuaikan dengan dependency template |
| Model | FBX; GLB hanya melalui glTFast jika benar-benar diperlukan |
| Penyimpanan konten | Asset lokal dan `ScriptableObject` |
| Android build | IL2CPP, ARM64 |
| Source control | Git + GitHub |

Modul Unity Hub yang wajib tersedia:

- Android Build Support.
- OpenJDK.
- Android SDK & NDK Tools.

Android Studio tidak wajib. Antigravity dapat digunakan untuk coding, terminal, Git, dan agent workflow. Unity Editor tetap sumber utama untuk scene, prefab, Inspector, package, XR settings, dan build.

### 5.1 Aturan package

- Catat versi package di `Packages/manifest.json` dan `packages-lock.json`.
- Jangan memperbarui package hanya karena versi baru tersedia.
- Samakan seri minor AR Foundation dan ARCore XR Plugin.
- Jangan memasang ARCore Extensions karena project tidak memakainya.
- ARKit hanya dipertahankan jika template membutuhkannya atau iOS masuk scope.
- Jangan memasang Vuforia bersamaan dengan AR Foundation.
- Hapus package yang tidak digunakan hanya setelah memastikan template dan build tetap valid.

### 5.2 Konfigurasi Android dan scene wajib

- `ARCore Requirement`: `Optional`.
- Minimum API boleh tetap 26 untuk baseline project saat ini; jangan menurunkannya tanpa pengujian dependency.
- Scripting backend release: IL2CPP.
- Target architecture release: ARM64; ARMv7 boleh ditambahkan hanya jika benar-benar perlu mendukung device 32-bit.
- Build Settings tidak boleh menyisakan `Assets/Scenes/SampleScene.unity` sebagai satu-satunya scene.
- Urutan scene production minimum:
  1. `Assets/App/Scenes/Bootstrap.unity`.
  2. `Assets/App/Scenes/MainAR.unity`.
  3. `Assets/App/Scenes/NonAR.unity` bila fallback dipisahkan menjadi scene. Jika fallback berada di Bootstrap, scene ketiga tidak wajib.
- `Bootstrap` tidak boleh memiliki komponen AR aktif. Scene ini bertanggung jawab atas intro, onboarding, pemeriksaan kompatibilitas, permission, dan routing.
- `MainAR` hanya boleh dimuat setelah availability `Ready` dan permission kamera granted.
- `NonAR` tidak boleh memiliki `ARSession`, `XROrigin`, `ARCameraBackground`, atau `ARTrackedImageManager`.

### 5.3 Kapan Vuforia boleh dipertimbangkan

Vuforia hanya boleh menjadi prototype pembanding jika:

- Target yang sudah diperbaiki tetap sulit terdeteksi.
- Perangkat sasaran penting tidak mendukung ARCore.
- Pengujian pencahayaan sekolah menunjukkan tracking AR Foundation tidak memenuhi kebutuhan.

Perpindahan provider memerlukan persetujuan pemilik. Gunakan Image Targets, bukan Model Targets.

---

## 6. Source of Truth Desain

Desain terbaru menggunakan frame Android portrait `360 × 800`. Implementasi Unity harus mempertahankan proporsi ini tetapi responsif terhadap rasio layar lain.

Konfigurasi Canvas yang direkomendasikan:

```text
Canvas Render Mode        : Screen Space - Overlay
Canvas Scaler             : Scale With Screen Size
Reference Resolution      : 360 × 800
Screen Match Mode         : Match Width Or Height
Match                     : 0.5, lalu uji perangkat ekstrem
Reference Pixels Per Unit : 100
```

Wajib gunakan `SafeAreaFitter` atau mekanisme setara untuk notch, punch-hole, dan gesture area. Jangan menyalin frame 360×800 sebagai gambar statis untuk seluruh UI.

HTML visual reference tidak boleh dimasukkan ke runtime. Jangan memakai WebView, Tailwind, browser engine, atau JavaScript untuk mereplikasi UI.

### 6.1 Design tokens

| Token | Nilai | Fungsi |
| --- | --- | --- |
| Deep Forest | `#12372A` | Background gelap, tombol utama, header |
| Forest Green | `#1F5D42` | CTA, state aktif, progress |
| Moss Green | `#607D4F` | Elemen sekunder, joint marker |
| Soft Sage | `#A9BEA2` | Track, surface ringan, dekorasi |
| Warm Cream | `#F4F0E6` | Surface utama, bottom sheet |
| Charcoal | `#202620` | Teks dan border gelap |
| White | `#FFFFFF` | Teks/ikon di surface gelap |
| Terracotta | `#B8684A` | Kategori Squat |
| Muted Teal | `#3F7C78` | Kategori Dynamic Stretching |
| Muted Mustard | `#C3A24B` | Kategori Ladder Drill |
| Camera Scrim | `rgba(18,55,42,0.32)` | Scrim di bawah sheet |

Jangan menambahkan neon, rainbow gradient, purple dominan, atau warna kategori sebagai background layar penuh.

### 6.2 Tipografi

- Font utama: Poppins.
- Display/brand: Poppins Bold/SemiBold.
- Heading: Poppins SemiBold.
- Body: Poppins Regular/Medium.
- Button: Poppins SemiBold.
- Jangan memakai lebih dari dua keluarga font.
- Jika Poppins belum tersedia dalam `components`, gunakan font fallback lokal sementara dan buat `ThemeConfig` agar penggantian terpusat.

Skala acuan:

| Peran | Ukuran acuan |
| --- | --- |
| Brand G01 | 34 px |
| Screen title | 24 px |
| Movement title | 20 px |
| Section heading | 12–14 px |
| Body | 13–14 px |
| Helper | 10–12 px |

### 6.3 Shape dan spacing

- Grid dasar: 4 px.
- Margin layar: 20 px.
- CTA utama: tinggi 56 px, radius 16 px.
- Kartu: radius 14–18 px.
- Floating button: 52 px.
- Bottom sheet: radius atas 28 px.
- Grab handle: 40 × 4 px.
- Gap floating button: 10–12 px.
- Touch area minimum: 48 × 48 px; target utama 52–56 px.
- Shadow lembut; jangan gunakan bevel, outline tebal, atau glassmorphism berat.

### 6.4 Icon

- Gunakan satu keluarga rounded outline icon.
- Stroke visual sekitar 2–2.2 px.
- Jangan memakai emoji sebagai icon production.
- Icon yang dibutuhkan: shield-check, scan image, X, open book, audio active, audio muted, arrow-left, camera-off, warning/info.

### 6.5 Kontrak replikasi native dari HTML

HTML pada `Pasted text(2).txt` adalah golden visual reference. Unity harus terasa seperti aplikasi yang sama, bukan interpretasi bebas.

- Bangun UI sebagai prefab uGUI per komponen: app header, notice/toast, scan guide, movement HUD, floating action stack, timeline, catalog card, full sheet, safety card, dan related card.
- Gunakan TextMeshPro dengan asset Poppins lokal. Jangan mengganti ke Inter hanya karena tersedia dari template.
- Jangan memakai Unicode/emoji seperti `✔`, `✕`, `⏸`, atau emoji buku sebagai icon final. Gunakan sprite icon lokal yang konsisten.
- Semua ukuran, margin, radius, warna, hierarki teks, dan posisi komponen harus diturunkan dari frame 360 × 800.
- Root Canvas wajib `Scale With Screen Size`, reference 360 × 800, match 0.5. Terapkan safe area pada content root, tetapi background kamera/poster tetap edge-to-edge.
- Gunakan 9-sliced sprites untuk card/button/sheet, `Mask`/`RectMask2D` untuk clipping, dan shadow sprite ringan.
- Poster, model, thumbnail, logo resmi, dan audio yang belum final menggunakan slot placeholder yang jelas. Jangan mengubah layout hanya karena asset masih kosong.
- Source gambar final selalu dicari dari folder root `components`, lalu disalin/import ke `app/Assets/App/...`. Runtime tidak boleh mereferensikan file di luar `Assets`.
- `SetupAndBuild.cs` boleh membantu bootstrap teknis, tetapi tidak boleh menjadi generator final seluruh UI. Prefab dan scene yang direview harus disimpan sebagai asset nyata.

Validasi visual setiap frame:

1. Jalankan Game View 360 × 800 dan ambil screenshot tanpa gizmo/editor overlay.
2. Bandingkan berdampingan dengan frame HTML yang sama.
3. Periksa safe margin, header, card, floating button, timeline, dan sheet.
4. Periksa font, weight, size, line-height, wrapping, dan alignment.
5. Periksa warna menggunakan nilai hex token.
6. Uji lagi pada 360 × 780, 393 × 873, 412 × 915, serta perangkat dengan notch.
7. Frame belum diterima jika ada perbedaan struktur besar, elemen terpotong, font pengganti, atau pergeseran lebih dari 4 px pada komponen utama di reference 360 × 800.

Agent tidak boleh mengklaim “sudah mirip” hanya karena palet hijau sama. Screenshot before/after wajib dicantumkan dalam laporan perubahan UI.

---

## 7. Spesifikasi 9 Frame UI

### G01 — Opening Poster dan Loading

Tujuan: memperkenalkan media pembelajaran dan menutup waktu bootstrap singkat.

Layout:

- Poster memenuhi seluruh layar.
- Background Deep Forest dengan ilustrasi Green Forest/olahraga.
- Identitas institusi ditempatkan rapi pada safe area atas.
- Logo yang dimaksud adalah logo resmi **Universitas Negeri Padang**.
- Jarak sisi atas dan sisi samping harus konsisten, acuan 20 px.
- Logo tidak boleh mepet sudut, tidak terlalu besar, dan tidak mendominasi poster.
- Jika logo resmi belum tersedia, gunakan placeholder netral bertuliskan `UNP` dan `Universitas Negeri Padang`; jangan mengarang crest.
- Branding `GerakAR` dan tagline `Belajar Gerak Jadi Seru` berada pada area bawah sesuai mockup terbaru.
- Progress memakai rounded linear bar modern, bukan tiga titik.

Perilaku:

- Tidak ada tombol wajib.
- Intro berpindah otomatis setelah bootstrap minimum selesai.
- Jangan memakai timer buta jika dependency belum siap.
- Minimum display sekitar 1.2–1.5 detik agar tidak berkedip.
- Maksimum normal sekitar 3 detik; jika lebih lama, tampilkan status nyata atau error yang ramah.
- Animasi progress harus loop halus dan menghormati pengaturan reduced-motion jika tersedia.

Asset:

- Poster final akan berasal dari `components`.
- Placeholder poster dapat dibuat dari shape dan mannequin sederhana.
- Jangan mengunduh poster dari internet.

### G02 — Petunjuk Sebelum Mulai

Tampil pertama kali atau ketika status onboarding belum disimpan.

Konten:

```text
Sebelum Mulai
Ayo bergerak dengan aman dan nyaman.

1. Gunakan di tempat yang cukup luas.
2. Minta guru atau orang tua mendampingi.
3. Izinkan kamera untuk melihat gerakan.

[ MULAI ]
```

Layout:

- Warm Cream background.
- Aksen abstract Soft Sage pada sudut, tidak ramai.
- Shield-check icon pada surface lembut.
- Tiga kartu keselamatan vertikal.
- CTA `MULAI` di area bawah aman.

Perilaku:

- `MULAI` memulai pemeriksaan dukungan AR dan izin kamera.
- Simpan `hasSeenOnboarding` melalui wrapper penyimpanan lokal, bukan membaca `PlayerPrefs` tersebar di banyak class.
- Link `Simulasi Non-AR` dan `Simulasi Kendala Kamera` hanya boleh ada pada development/debug menu, bukan production.
- Jangan meminta nama, umur, sekolah, email, atau data pribadi anak.

### G03 — Kamera dan Pemindaian Target

Layout:

- Kamera memenuhi layar.
- Header GerakAR dan tagline mengikuti mockup terbaru.
- Central scan guide 232 × 232 berbentuk empat corner bracket.
- Scan line Soft Sage bergerak vertikal dengan easing lembut.
- Label `PINDAI TARGET GAMBAR` berada dekat scan guide.
- Instruction card:

```text
Arahkan kamera ke gambar gerakan
Pastikan seluruh gambar terlihat
```

Perilaku:

- Tidak ada aksi palsu ketika layar diketuk pada production.
- Transisi hanya terjadi setelah event tracked image valid.
- Saat target hilang, tampilkan `Arahkan kembali ke gambar`.
- Jangan menyimpan frame kamera.

### G04 — Konfirmasi Gerakan Ditemukan

Tujuan: memberi feedback keberhasilan yang sangat singkat dan mudah dipahami anak.

Layout:

- Kamera tetap terlihat dengan Deep Forest scrim ringan.
- Card Warm Cream di tengah.
- Checkmark Forest Green dengan pulse lembut.
- Copy:

```text
Gerakan Ditemukan!
[NAMA GERAKAN]
```

- Warna chip mengikuti kategori.

Perilaku:

- Tampil sekitar 600–900 ms.
- Jangan membutuhkan tap untuk melanjutkan.
- Setelah durasi minimum dan model siap, masuk G05.
- Jika target hilang sebelum model siap, batalkan transisi dan kembali G03/TrackingLost.
- Jangan memunculkan G04 terus-menerus akibat update tracking yang sama.

### G05 — Tracking, Animasi, Timeline, dan Floating Controls

Ini adalah state utama setelah target stabil terdeteksi.

Layout:

- Kamera full-screen.
- Header GerakAR dan tagline konsisten dengan G03.
- Model 3D berada di tengah dan anchored pada target.
- Kolom floating control di sisi kanan bawah:
  1. Audio Play/Pause.
  2. Materi/open book.
  3. Tutup/scan ulang.
- Floating button 52 × 52 px.
- Di bawah terdapat kartu Warm Cream transparan berisi:
  - Chip nama gerakan.
  - Status `Loop` atau `Periksa Pose`.
  - Timeline.
  - Enam marker contoh; jumlah final 5–8 sesuai clip.
  - Label `Mulai`, `Geser untuk memeriksa pose`, `Selesai`.

Perilaku animasi:

- Default langsung looping.
- Tidak ada tombol replay atau repeat.
- Menyentuh timeline otomatis masuk `InspectingPose`.
- Slider mengontrol `normalizedTime` 0–1.
- Saat dilepas, pose ditahan sekitar 2.5 detik, blend ke awal 0.3–0.5 detik, kemudian loop dilanjutkan.
- Jika target hilang, batalkan semua coroutine/timer pose.

Perilaku audio:

- Tombol mengubah state Play/Pause audio panduan, bukan Play/Pause animasi.
- Label dan icon harus jelas agar tidak dikira sebagai kontrol animasi.
- Jika `audioGuide` belum tersedia, tombol tampil disabled/muted atau disembunyikan berdasarkan keputusan `ThemeConfig`; jangan memutar audio palsu.
- Jangan auto-restart audio setiap tracking update.

Floating action:

- Materi membuka G06 langsung ke full sheet.
- X menonaktifkan movement aktif dan kembali G03.

### G06 — Materi Utama Full Bottom Sheet

Keputusan terbaru: sheet dibuka langsung ke **94% tinggi layar**.

Layout:

- Sisakan sekitar 6% konteks kamera di atas.
- Camera Scrim `rgba(18,55,42,0.32)`.
- Warm Cream sheet dengan radius atas 28 px.
- Grab handle 40 × 4 px.
- Header kategori, nama gerakan, deskripsi, dan X.
- Body scrollable tanpa scrollbar visual yang mengganggu.

Urutan konten:

1. Tentang Gerakan.
2. Cara Melakukan — tiga langkah.
3. `Ingat, Ya!` — safety card.
4. Hindari Ini — dua atau tiga kesalahan umum.
5. Bagian tubuh yang dilatih, bila ruang memungkinkan.
6. Gerakan Serupa — horizontal cards.

Perilaku:

- Animasi 3D dan audio dijeda saat sheet terbuka.
- Timeline tidak interaktif.
- X atau scrim menutup sheet dan kembali G05.
- Setelah menutup, reset animasi ke awal lalu jalankan loop.
- Menekan kartu terkait membuka G07 tanpa mengganti model AR utama.

### G07 — Materi Tambahan / Gerakan Terkait

Layout:

- Menggunakan sheet full yang sama dengan G06.
- Label `MATERI TAMBAHAN`.
- Nama variasi gerakan.
- Visual referensi ringan atau placeholder lokal.
- Tiga langkah singkat.
- Safety card.
- Aksi `Kembali ke materi [Gerakan Utama]`.

Perilaku:

- Model AR utama tidak berubah.
- Tracking movement tetap movement awal.
- X menutup seluruh sheet dan kembali G05.
- Tombol kembali hanya mengganti konten sheet ke G06.

Gerakan terkait:

| Utama | Materi tambahan |
| --- | --- |
| Squat | Bodyweight Squat, Squat Jump, Pistol Squat, Front Squat |
| Dynamic Stretching | Standing Toe Touch, Diagonal Reach, Trunk Rotation, High Knee March |
| Ladder Drill | One-In, Two-In, Side Shuffle, Two-Foot Hop |

### G08 — Mode Pembelajaran Mandiri / Non-AR

G08 digunakan ketika AR tidak didukung atau instalasi layanan AR gagal. Ini adalah mode belajar penuh, bukan sekadar katalog bacaan dan bukan dead-end.

#### Notice sebelum G08

Setelah availability menyatakan unsupported, tampilkan overlay transient bergaya visual G04 selama sekitar 1.4–2 detik, lalu arahkan otomatis ke G08. Overlay ini tidak dihitung sebagai frame baru dan harus berada pada Bootstrap/Non-AR shell, bukan di atas kamera hitam.

Copy ramah anak:

```text
Mode AR Belum Tersedia
Tenang, kamu tetap bisa mempelajari semua gerakan tanpa kamera AR.
```

- Gunakan icon informasi/perangkat, bukan checkmark sukses dan bukan tanda error merah.
- Jangan menampilkan kata `ARCore`, nama layanan Google, error code, stack trace, atau instruksi sideload kepada anak.
- Tidak memerlukan tombol; routing otomatis. Sediakan aksi `Lanjutkan` hanya jika aturan aksesibilitas membutuhkan waktu baca manual.

Layout:

- Warm Cream background.
- Header GerakAR.
- Label `MODE PEMBELAJARAN MANDIRI` dan badge `NON-AR MODE`.
- Notice ramah bahwa perangkat belum mendukung AR.
- Katalog tiga gerakan utama:
  - Squat.
  - Dynamic Stretching.
  - Ladder Drill.
- Jangan memakai Jumping Jacks/Lunges sebagai isi final.
- Jangan memakai emoji sebagai icon final.

Perilaku:

- Semua tiga gerakan harus dapat dibuka karena fallback tidak boleh mengunci pembelajaran.
- Memilih kartu membuka **Non-AR Movement Player** yang menggunakan stage Green Forest netral, bukan background kamera palsu.
- Player menampilkan model/placeholder gerakan di tengah, animasi loop default, timeline pose yang dapat digeser, marker pose, floating audio, floating materi, dan tombol kembali ke katalog.
- Audio panduan harus tersedia pada mode ini ketika asset audio sudah ada. Tombol audio hanya mengontrol audio, bukan animasi.
- Materi utama G06 dan gerakan terkait G07 harus dapat dibuka dengan komponen sheet yang sama.
- Related movement adalah materi/preview lokal; tidak perlu mengubah movement AR karena AR subsystem memang tidak aktif.
- Implementasikan model player melalui shared presenter, bukan menyalin seluruh controller AR.
- Membuka materi menggunakan komponen content sheet/detail yang sama tanpa dependency pada camera background.
- Sediakan kembali ke petunjuk atau opening secara sederhana.
- Jangan meminta permission kamera.
- Jangan membuat atau mengaktifkan AR subsystem secara tersembunyi.
- Jangan meminta pengguna memasang aplikasi lain dan jangan mengarahkan sideload.

### G09 — Kamera Belum Aktif

Layout:

- Warm Cream background dengan aksen Soft Sage.
- Camera-off icon.
- Copy:

```text
Kamera Belum Aktif
Izinkan akses kamera agar GerakAR dapat melihat gambar gerakan.

[ BUKA PENGATURAN ]
Coba Lagi

Minta bantuan guru atau orang tua jika diperlukan.
```

Perilaku:

- `BUKA PENGATURAN` membuka application settings Android melalui mekanisme yang aman.
- `Coba Lagi` memeriksa permission lagi.
- Jangan langsung masuk G03 jika permission masih ditolak.
- Jangan menampilkan stack trace, istilah ARCore, atau error code kepada anak.

---

## 8. State Machine Aplikasi

Gunakan state eksplisit. Jangan menyebarkan boolean state di banyak MonoBehaviour.

```csharp
public enum AppState
{
    Bootstrapping,
    Intro,
    Onboarding,
    CheckingAR,
    RequestingPermission,
    LoadingARScene,
    Scanning,
    TargetConfirmed,
    TrackingLoop,
    InspectingPose,
    ShowingMaterial,
    ShowingRelatedMaterial,
    TrackingLost,
    UnsupportedNotice,
    NonARCatalog,
    NonARMovementPlayer,
    CameraDenied,
    ARInstallFailed
}
```

Transisi utama:

| Dari | Pemicu | Ke |
| --- | --- | --- |
| Bootstrapping | Dependency minimum siap | Intro |
| Intro | Onboarding belum pernah dilihat | Onboarding |
| Intro | Onboarding sudah dilihat | CheckingAR |
| Onboarding | MULAI | CheckingAR |
| CheckingAR | AR tersedia, permission belum ada | RequestingPermission |
| CheckingAR | AR tidak didukung | UnsupportedNotice |
| CheckingAR | Membutuhkan instalasi | CheckingAR selama proses instalasi yang terukur |
| CheckingAR | Instalasi gagal | UnsupportedNotice |
| UnsupportedNotice | Durasi baca selesai | NonARCatalog |
| RequestingPermission | Diizinkan | LoadingARScene |
| RequestingPermission | Ditolak | CameraDenied |
| LoadingARScene | MainAR selesai dimuat dan subsystem siap | Scanning |
| Scanning | Target valid ditemukan | TargetConfirmed |
| TargetConfirmed | Feedback selesai dan model siap | TrackingLoop |
| TrackingLoop | Slider disentuh | InspectingPose |
| InspectingPose | Release + hold + blend selesai | TrackingLoop |
| TrackingLoop | Materi ditekan | ShowingMaterial |
| ShowingMaterial | Gerakan terkait ditekan | ShowingRelatedMaterial |
| ShowingRelatedMaterial | Kembali | ShowingMaterial |
| ShowingMaterial/Related | X/scrim | TrackingLoop |
| TrackingLoop/InspectingPose | Target hilang | TrackingLost |
| TrackingLost | Target kembali dalam grace period | TrackingLoop |
| TrackingLost | Timeout | Scanning |
| CameraDenied | Permission kemudian diberikan | LoadingARScene |
| NonARCatalog | Gerakan dipilih | NonARMovementPlayer |
| NonARMovementPlayer | Kembali | NonARCatalog |
| NonARMovementPlayer | Materi ditekan | ShowingMaterial |

Prioritas:

1. Lifecycle/permission/unsupported.
2. Tracking lost.
3. Material sheet.
4. Pose inspection.
5. Loop.

Setiap transisi harus membatalkan coroutine, tween, audio, dan event subscription yang tidak lagi relevan.

Material sheet harus menyimpan `returnState`. Jika dibuka dari AR, X kembali ke `TrackingLoop`; jika dibuka dari fallback, X kembali ke `NonARMovementPlayer`. Jangan hardcode seluruh penutupan sheet ke state AR.

---

## 9. Arsitektur Kode

Hindari satu `GameManager` besar. Pisahkan tanggung jawab:

### 9.1 Core

- `AppBootstrapper`: bootstrap, intro, onboarding, scene flow.
- `AppStateMachine`: state dan transisi tervalidasi.
- `AppLifecycleController`: pause/resume/focus.
- `LocalPreferences`: wrapper penyimpanan onboarding/settings.
- `AppEvents`: event terpilih, jangan menjadi global event dump.

### 9.2 AR

- `ARAvailabilityService`: memeriksa dukungan/instalasi.
- `CameraPermissionService`: permission dan settings redirect.
- `ImageTrackingController`: subscription tracked image.
- `MovementResolver`: nama reference image ke `MovementData`.
- `TrackedContentAnchor`: posisi/rotasi/skala content pada target.
- `TrackingStabilityController`: grace period dan stabilitas.

Kontrak availability:

- Service tidak boleh menyalakan/mematikan panel secara langsung; kembalikan hasil domain kepada state machine.
- Gunakan hasil terpisah: `Ready`, `NeedsInstall`, `Unsupported`, `InstallFailed`, dan `CheckFailed`.
- `CheckFailed` karena timeout/jaringan tidak otomatis berarti hardware unsupported. Tawarkan retry yang terbatas atau fallback Non-AR yang aman.
- Jangan memanggil `Install()` jika state sudah `Unsupported`.
- Catat diagnostic teknis ke log development, tetapi terjemahkan ke copy ramah pada production UI.
- Setelah route Non-AR dipilih, jangan load `MainAR` pada background.
- Setelah route AR dipilih, mintalah permission kamera, load scene, tunggu initialization, lalu baru tampilkan G03.

### 9.3 Movement dan animasi

- `MovementController`: movement aktif dan lifecycle model.
- `MovementModelPool`: reuse prefab, bukan instantiate berulang.
- `MovementAnimationController`: loop/scrub/blend.
- `PoseTimelineController`: nilai slider dan marker.
- `AudioGuideController`: audio guide dan state button.
- `MovementPresentationCoordinator`: menghubungkan movement data dengan host AR atau Non-AR tanpa menduplikasi logika animasi, timeline, dan audio.
- `IMovementPresentationHost`: kontrak host dengan implementasi `ARMovementHost` dan `NonARMovementHost`.

### 9.4 UI

- `UIStateController`: mapping `AppState` ke view.
- `IntroView`.
- `OnboardingView`.
- `ScanningView`.
- `TargetConfirmedView`.
- `TrackingHUDView`.
- `MovementMaterialSheet`.
- `RelatedMovementView`.
- `NonARCatalogView`.
- `UnsupportedNoticeView`.
- `NonARMovementPlayerView`.
- `CameraDeniedView`.
- `SafeAreaFitter`.

### 9.5 Content

- `MovementData`.
- `RelatedMovementData`.
- `MovementRepository`.
- `ThemeConfig`.
- `AppAssetConfig`.

Controller tidak boleh mencari object berkali-kali dengan `FindObjectOfType` saat runtime. Dependency dihubungkan melalui Inspector, bootstrap composition root, atau pattern sederhana yang dapat diuji.

---

## 10. Struktur Data

Gunakan `ScriptableObject` agar asset/konten dapat diganti dari Inspector.

```csharp
[CreateAssetMenu(menuName = "GerakAR/Movement Data")]
public sealed class MovementData : ScriptableObject
{
    [Header("Identity")]
    public string movementId;
    public string referenceImageName;
    public string displayName;
    public MovementCategory category;

    [Header("AR Content")]
    public GameObject modelPrefab;
    public AnimationClip animationClip;
    public Vector3 localPosition;
    public Vector3 localEulerAngles;
    public Vector3 localScale = Vector3.one;

    [Header("Pose Inspection")]
    public List<KeyPoseData> keyPoses;

    [Header("Education")]
    [TextArea] public string shortDescription;
    public List<string> steps;
    public List<string> trainedAreas;
    public List<string> commonMistakes;
    public List<string> safetyTips;
    public List<RelatedMovementData> relatedMovements;

    [Header("Audio")]
    public AudioClip audioGuide;
}

public enum MovementCategory
{
    Squat,
    DynamicStretching,
    LadderDrill
}

[System.Serializable]
public sealed class KeyPoseData
{
    [Range(0f, 1f)] public float normalizedTime;
    public string label;
}

[System.Serializable]
public sealed class RelatedMovementData
{
    public string id;
    public string title;
    public Sprite thumbnail;
    [TextArea] public string shortDescription;
    public List<string> steps;
    public List<string> safetyTips;
}
```

Identitas target:

```text
squat_target            -> MovementData_Squat
dynamic_stretch_target  -> MovementData_DynamicStretching
ladder_drill_target     -> MovementData_LadderDrill
```

Nama `referenceImageName` harus sama persis dengan nama di `XRReferenceImageLibrary`. Jangan mapping berdasarkan index.

---

## 11. Image Tracking

Gunakan `ARTrackedImageManager`.

### 11.1 Added

1. Ambil `referenceImage.name`.
2. Resolve `MovementData`.
3. Pastikan target memenuhi tracking state yang diterima.
4. Nonaktifkan movement lama.
5. Tampilkan G04 satu kali untuk target baru.
6. Aktifkan/reuse prefab.
7. Anchor model ke target.
8. Masuk G05 setelah model siap.

### 11.2 Updated

- Perbarui pose target.
- Jangan memicu ulang G04 untuk setiap update.
- Bedakan tracking penuh dan limited.
- Gunakan grace period 0.5–1 detik untuk mencegah flicker.
- Jika target berbeda menjadi aktif, tutup sheet, reset timeline/audio, lalu ganti movement.

### 11.3 Removed/lost

- Batalkan pose inspection.
- Jeda/stop audio.
- Tutup material sheet.
- Sembunyikan HUD dan model setelah grace period.
- Kembali scanning jika timeout.

### 11.4 Aturan multi-target

- Hanya satu movement aktif.
- Jika dua gambar terlihat, gunakan kebijakan deterministic: target terbaru dengan tracking state terbaik, lalu pertahankan sampai hilang atau diganti eksplisit.
- Jangan instantiate model pada setiap callback.
- Cache/pool maksimal tiga prefab.

---

## 12. Sistem Animasi

### 12.1 Loop

- `Animator.applyRootMotion = false` secara default.
- Clip memakai Loop Time.
- Loop Pose hanya jika memperhalus clip.
- Hindari frame akhir duplikat yang menyebabkan jeda.
- Model dimulai dari normalized time 0.

### 12.2 Inspect Pose

Jangan memecah clip menjadi 52 file atau 52 tombol. Timeline mengakses clip utuh.

```csharp
float normalizedTime = poseSlider.value;
animator.speed = 0f;
animator.Play(animationStateHash, 0, normalizedTime);
animator.Update(0f);
```

Flow:

```text
PointerDown slider
  -> state InspectingPose
  -> pause audio
  -> animator.speed = 0
  -> scrub normalizedTime

PointerUp slider
  -> hold pose 2.5 s
  -> blend ke pose awal 0.3–0.5 s
  -> slider = 0
  -> animator.speed = 1
  -> state TrackingLoop
```

Semua timer harus menggunakan cancellation token/coroutine handle yang dapat dibatalkan ketika target hilang, sheet terbuka, scene berubah, atau object disable.

### 12.3 Key pose

- Jumlah 5–8 per movement.
- Nilai ditentukan dari clip final, bukan dibagi rata secara buta.
- Marker harus mudah terlihat tetapi tidak mengganggu handle.
- Area sentuh slider diperbesar tanpa membuat track terlalu tebal.

---

## 13. Audio Guide

Mockup terbaru menampilkan audio Play/Pause di G05. Implementasikan struktur sekarang, tetapi jangan membuat audio final palsu.

Aturan:

- Audio berasal dari `MovementData.audioGuide`.
- Audio dimulai hanya dari tindakan pengguna atau aturan yang telah disetujui, bukan setiap tracking update.
- Button state: AvailableStopped, Playing, Paused, Unavailable.
- Saat unavailable, icon/label harus menunjukkan disabled dengan jelas.
- Audio pause saat inspect pose atau sheet dibuka.
- Audio stop/reset saat target hilang atau movement berganti.
- Jangan menambahkan permission microphone.
- Jangan merekam audio pengguna.

Event:

```text
OnMovementDetected(movementId)
OnLoopStarted(movementId)
OnPoseInspectionStarted(normalizedTime)
OnPoseInspectionEnded()
OnMaterialOpened(movementId)
OnTrackingLost(movementId)
OnAudioStateChanged(movementId, state)
```

---

## 14. Asset dan Folder Components

Semua source asset dari pemilik project berasal dari folder root `components`.

Aturan:

1. Periksa isi nyata sebelum menulis path.
2. Jangan mengarang nama file.
3. Jangan mengubah asset asli tanpa alasan.
4. Jangan mengunduh asset pengganti dari internet.
5. Jangan memasukkan logo UNP hasil rekonstruksi AI sebagai logo resmi.
6. Jika asset belum tersedia, buat placeholder procedural/local.
7. Tandai placeholder secara jelas pada Inspector/configuration.
8. Hubungkan asset melalui `AppAssetConfig` atau `MovementData`.
9. Dokumentasikan import ke `app/Assets`.

Status awal yang harus diasumsikan sampai diverifikasi:

| Asset | Asumsi aman |
| --- | --- |
| Poster/cover | Belum final |
| Logo resmi UNP | Gunakan hanya jika file resmi tersedia |
| Logo GerakAR | Belum tentu final |
| Tiga gambar target | Menunggu validasi |
| Model Squat | Belum final |
| Model Dynamic Stretching | Belum final |
| Model Ladder Drill | Belum final |
| Audio guide | Belum final |

Jika Unity hanya membaca asset di bawah `Assets`, gunakan salah satu strategi yang terdokumentasi:

- Copy/import terkontrol ke `app/Assets/App/...` dengan sumber tetap di `components`.
- Script Editor import yang idempotent.
- Symlink hanya jika terbukti portable untuk semua collaborator; jangan menjadikannya default.

Jangan membuat duplikasi tanpa mencatat sumber dan proses update.

---

## 15. Struktur Project Unity

```text
app/Assets/
├── App/
│   ├── Animations/
│   │   ├── Clips/
│   │   └── Controllers/
│   ├── AR/
│   │   ├── ReferenceImages/
│   │   └── Tracking/
│   ├── Audio/
│   │   └── Guides/
│   ├── Content/
│   │   ├── MovementData/
│   │   └── RelatedMovements/
│   ├── Models/
│   │   ├── Squat/
│   │   ├── DynamicStretching/
│   │   └── LadderDrill/
│   ├── Prefabs/
│   │   ├── AR/
│   │   ├── Models/
│   │   └── UI/
│   ├── Scenes/
│   │   ├── Bootstrap.unity
│   │   ├── MainAR.unity
│   │   └── NonAR.unity        # Opsional jika fallback tidak berada di Bootstrap
│   ├── Scripts/
│   │   ├── AR/
│   │   ├── Animation/
│   │   ├── Audio/
│   │   ├── Content/
│   │   ├── Core/
│   │   └── UI/
│   ├── UI/
│   │   ├── Fonts/
│   │   ├── Icons/
│   │   ├── Sprites/
│   │   └── Themes/
│   └── Tests/
│       ├── EditMode/
│       └── PlayMode/
└── Settings/
```

Scene `Bootstrap`:

- Bootstrapper.
- Intro G01.
- Onboarding G02.
- AR availability.
- Permission flow.
- Non-AR fallback.
- Scene transition.

Scene `NonAR` bila dipisahkan:

- Unsupported notice transient.
- Catalog tiga movement.
- Stage preview model/placeholder 3D dengan Camera Unity biasa, bukan AR Camera.
- Shared loop, pose timeline, audio guide, full material sheet, dan related content.
- Tidak ada component AR pada hierarchy.

Scene `MainAR`:

- AR Session.
- XR Origin.
- AR Camera.
- AR Tracked Image Manager.
- Reference Image Library.
- Model root/pool.
- G03–G07 UI.
- G09 permission overlay bila dibutuhkan setelah resume.

`MainAR` tidak boleh menjadi scene pertama pada Build Settings.

---

## 16. Gambar Target

Target harus:

- Memiliki fitur visual tersebar.
- Tidak didominasi area polos.
- Memiliki kontras cukup.
- Tidak memakai pola berulang berlebihan.
- Berbeda jelas satu sama lain.
- Memiliki ukuran fisik yang benar di reference library.
- Diuji dalam bentuk cetak dan tampilan layar.

Test target:

- Cahaya terang dan redup.
- Sudut 0°, 20°, 45°.
- Jarak dekat, sedang, dan batas nyaman.
- Target tertutup sebagian.
- Kamera bergerak pelan dan cepat.
- Pergantian antar target minimal 20 siklus.

Kualitas tracking lebih penting daripada memaksakan desain target yang terlalu polos.

---

## 17. Pipeline Model 3D

Untuk setiap model:

1. Validasi skala, pivot, orientasi.
2. Validasi rig dan avatar.
3. Validasi clip loop.
4. Hapus mesh/material/bone yang tidak dipakai.
5. Periksa foot sliding.
6. Nonaktifkan root motion jika keluar target.
7. Buat prefab.
8. Hubungkan lewat `MovementData`.

Panduan awal:

- Hanya satu `SkinnedMeshRenderer` aktif jika memungkinkan.
- Triangle 30k–60k per model boleh diprototipe karena satu model aktif; optimasi berdasarkan profiler.
- Ideal satu material utama.
- Hindari texture 4K.
- Albedo maksimum 2048 bila benar-benar perlu; map lain 1024 atau lebih kecil.
- Kompres animation setelah QA visual.
- Jangan decimate agresif sebelum membandingkan wajah, tangan, pakaian, dan performa.

---

## 18. Optimasi dan Build Android

### 18.1 Diagnosis layar kamera hitam

Permission kamera saja tidak membuktikan bahwa AR dapat berjalan. Diagnosis wajib mengikuti gate berikut:

| Kondisi | Makna | Tindakan aplikasi |
| --- | --- | --- |
| `ARSession.state == Unsupported` | Model/firmware tidak mendukung provider AR | Jangan load MainAR; notice -> Non-AR |
| `NeedsInstall` | Perangkat mendukung, layanan AR belum siap | `Install()`, periksa ulang, lalu route |
| Camera permission denied | Hardware AR mungkin didukung, tetapi kamera tidak diizinkan | G09, buka settings/coba lagi |
| Ready tetapi hitam pada device supported | Kemungkinan scene, loader, camera stack, graphics API, lifecycle, atau runtime error | Kumpulkan logcat dan jalankan checklist |
| Hitam hanya di Unity Editor | Live camera perangkat tidak tersedia di editor biasa | Gunakan XR Simulation atau device supported |

Checklist device resmi supported:

1. Verifikasi model persis berada pada daftar perangkat ARCore supported.
2. Pastikan availability berakhir `Ready`, `SessionInitializing`, atau `SessionTracking`.
3. Pastikan `android.permission.CAMERA` benar-benar granted melalui runtime/logcat.
4. Pastikan ARCore loader aktif untuk Android di XR Plug-in Management.
5. Pastikan hierarchy memiliki satu `ARSession`, satu `XROrigin`, dan satu AR Camera aktif.
6. Pastikan AR Camera memiliki `Camera`, `ARCameraManager`, `ARCameraBackground`, dan pose driver yang disyaratkan template/package.
7. Pastikan tidak ada Camera lain dengan depth lebih tinggi yang merender hitam di atas AR Camera.
8. Pastikan URP renderer/camera stack dan background rendering mengikuti baseline AR Mobile Unity 6.5.
9. Periksa Console dan `adb logcat` untuk `Unity`, `ARCore`, permission, OpenGL/Vulkan, serta subsystem initialization error.
10. Uji clean install setelah menghapus build lama dan data aplikasi.

Pada perangkat non-ARCore, berhenti setelah availability check. Memasang Google Play Services for AR secara manual tidak mengubah hardware/firmware menjadi supported dan bukan langkah production.

Konfigurasi awal:

- Scripting Backend: IL2CPP.
- Target Architecture: ARM64.
- Graphics API: OpenGL ES 3 terlebih dahulu.
- Color Space mengikuti template; uji material camera background.
- Managed Stripping: Medium.
- Strip Engine Code: aktif setelah QA.
- Optimize Mesh Data: aktif setelah shader/material diverifikasi.
- Development Build: hanya untuk debug.
- Release: signed APK untuk pilot; AAB jika distribusi Play Store.

Nonaktifkan jika tidak dipakai:

- Plane detection.
- Depth.
- Occlusion.
- Meshing.
- Face tracking.
- GPS/location.
- ARCore Extensions.
- Physics kompleks.
- Real-time shadow berat.
- HDR/post-processing mahal.

Target performa:

| Metrik | Target awal |
| --- | --- |
| Frame rate | Minimum 30 FPS stabil pada perangkat sasaran |
| Model aktif | Maksimum satu |
| Deteksi normal | Ideal 1–2 detik |
| UI | Tidak freeze saat sheet dibuka |
| Memory | Tidak meningkat terus setelah ganti target berulang |
| Thermal | Tidak cepat panas dalam sesi uji normal |

Jangan menjanjikan ukuran APK sebelum membuat build report. Ukuran 50–90 MB hanya hipotesis awal, bukan acceptance mutlak.

---

## 19. Privasi dan Keamanan Anak

- Permission kamera saja untuk fitur awal.
- Jangan meminta microphone.
- Jangan menyimpan, merekam, atau mengunggah camera frame.
- Jangan meminta nama, umur, sekolah, email, lokasi, atau akun.
- Jangan memasang iklan.
- Jangan memasang analytics tanpa persetujuan eksplisit.
- Jangan menambahkan permission internet jika aplikasi tidak memerlukannya.
- Materi olahraga dan safety copy harus ditinjau guru/pihak kompeten sebelum pilot.
- Error harus ramah anak dan menyarankan bantuan orang dewasa.

---

## 20. Tahapan Implementasi

### Tahap 0 — Audit

- Baca AGENTS.
- Periksa Git.
- Periksa project Unity dan versi editor.
- Periksa `components`.
- Daftar asset tersedia/hilang.
- Pastikan `.gitignore` Unity.

### Tahap 1 — Baseline project

- Validasi AR Mobile template.
- Pin package.
- Pastikan Android module.
- Buat/rapikan Bootstrap dan MainAR.
- Validasi compile bersih.
- Buat smoke build bila environment memungkinkan.

### Tahap 2 — Content model

- Implement `MovementData` dan repository.
- Buat tiga data asset placeholder.
- Implement `ThemeConfig` dan `AppAssetConfig`.
- Tambahkan EditMode tests untuk resolver.

### Tahap 3 — Tracking prototype

- Reference image library test.
- ARTrackedImageManager.
- Primitive berbeda per target.
- Grace period.
- Satu content aktif.
- Test device.

### Tahap 4 — State machine

- Implement seluruh state/transisi.
- Cancellation dan lifecycle.
- Permission dan unsupported flow.
- Non-AR fallback.
- Gate availability harus selesai sebelum load `MainAR` atau request kamera.
- Pisahkan `CameraDenied`, `UnsupportedNotice`, `ARInstallFailed`, dan `NonARCatalog`.
- Unit/EditMode tests state transition.

### Tahap 5 — UI G01–G04

- Intro poster/loading.
- Onboarding.
- Scanner.
- Confirmation feedback.
- Safe area dan responsive layout.

### Tahap 6 — Model, G05, timeline

- Placeholder/model import.
- Loop animation.
- Timeline scrub.
- Key pose marker.
- Audio button state.
- Floating controls.

### Tahap 7 — G06–G08 content

- Full sheet.
- Scroll content.
- Related detail.
- Non-AR catalog.
- Non-AR movement player dengan loop, timeline, audio, sheet, dan related content.
- Pastikan scene/jalur Non-AR sama sekali tidak bergantung pada komponen AR.
- Data-driven content tiga movement.

### Tahap 8 — G09 dan lifecycle

- Permission denied.
- Open settings.
- Retry.
- Pause/resume.
- AR session recovery.

### Tahap 9 — Optimasi dan QA

- Profiling.
- Texture/animation compression.
- 20-cycle target switching.
- Screen size/notch tests.
- Pilot signed APK.

Agent boleh menyempurnakan urutan jika dependency teknis membutuhkan, tetapi tidak boleh mengerjakan polish final sebelum tracking dan state baseline stabil.

---

## 21. Testing Wajib

### EditMode

- `MovementResolver` memetakan tiga reference name dengan benar.
- Reference name tidak dikenal tidak menyebabkan exception.
- AppState transition valid/invalid.
- Key pose sorted dan berada 0–1.
- Movement content lengkap minimum.
- Theme/category mapping benar.

### PlayMode

- Onboarding hanya sekali.
- G01 -> G02/CheckingAR.
- Permission denied -> G09.
- Unsupported -> notice singkat -> G08.
- Install gagal -> notice singkat -> G08.
- Unsupported tidak pernah memuat `MainAR` atau membuat AR subsystem.
- Mode Non-AR tidak meminta camera permission.
- G08 -> pilih movement -> loop/timeline/audio/materi/related content berjalan.
- Target -> G04 -> G05.
- Slider -> Inspect -> Loop.
- Material -> G06 -> G07 -> G06 -> G05.
- Target lost menutup sheet dan membatalkan pose.
- Audio state mengikuti lifecycle.

### Device

- Fresh install.
- Permission allow/deny/permanently denied.
- Buka settings lalu kembali.
- AR service tersedia/tidak tersedia.
- Minimal satu perangkat resmi ARCore-supported dan satu perangkat non-ARCore.
- Perangkat non-ARCore tidak menampilkan kamera hitam dan langsung diarahkan ke fallback.
- Perangkat supported tanpa layanan AR terbaru menjalankan alur install/update lalu melakukan pemeriksaan ulang.
- Tiga target cetak dan layar.
- Cahaya/sudut/jarak.
- Pause/resume dan background/foreground.
- Ganti target minimal 20 kali.
- Buka/tutup sheet berulang.
- Slider saat tracking limited.
- Perangkat notch dan rasio berbeda.
- Perangkat rendah-menengah minimal satu.

### UX anak

Amati:

- Anak memahami cara scan tanpa penjelasan panjang.
- Feedback G04 dipahami.
- Timeline ditemukan dan dapat digeser.
- Audio tidak dikira tombol animasi.
- Materi dan tombol kembali dipahami.
- Non-AR mode tidak terasa sebagai kegagalan total.
- Teks terbaca.

Jika beberapa anak gagal pada tindakan sama, perbaiki UI; jangan hanya menambah paragraf.

---

## 22. Workflow Git dan GitHub

Sebelum perubahan pertama:

```bash
git remote -v
git branch --show-current
git status --short
```

Setiap perubahan logis:

```bash
git diff
git add <file-terkait>
git commit -m "type(scope): deskripsi singkat"
git push origin <branch-aktif>
```

Format Conventional Commits:

```text
chore(project): configure Unity 6.5 AR baseline
feat(core): add application state machine
feat(ar): add tracked image movement resolver
feat(ui): implement green forest scanning view
feat(animation): add pose timeline scrubbing
feat(audio): add movement audio guide state
feat(content): add non-AR movement catalog
test(ar): cover movement reference mapping
docs: consolidate agent development instructions
```

Aturan:

- Satu commit per perubahan logis.
- Test/build sebelum commit sesuai risiko.
- Periksa diff dan file staged.
- Push segera setelah commit.
- Lanjutkan pekerjaan setelah push berhasil.
- Jangan membuat branch, PR, tag, atau release kecuali diminta.
- Jangan commit `Library/`, `Temp/`, `Logs/`, `Obj/`, `Build/`, credential, keystore, token, atau cache IDE.
- Jangan memasukkan token ke remote URL.
- Jika push gagal, jangan mengarang keberhasilan. Laporkan atau perbaiki autentikasi.

Agent yang tidak memiliki izin push tetap boleh menyelesaikan perubahan lokal dan commit jika aman, lalu melaporkan blocker push dengan jelas.

---

## 23. Acceptance Criteria Versi Awal

Agent hanya boleh menyatakan implementasi awal selesai jika:

1. Project terbuka pada Unity 6.5 tanpa compile error yang diketahui.
2. Package AR Foundation/ARCore selaras dan build target Android aktif.
3. G01–G09 tersedia sesuai fungsi, termasuk Non-AR dan permission denied.
4. Safe area dan rasio layar diuji.
5. Tiga `MovementData` utama tersedia.
6. Tiga reference image dapat dipetakan dengan benar, minimal menggunakan target test yang ditandai jelas.
7. Hanya satu model aktif.
8. G04 tidak berulang pada tracking update yang sama.
9. Animasi loop berjalan.
10. Timeline scrub menggunakan clip utuh dan kembali otomatis ke loop.
11. Audio button memiliki state benar dan tidak memutar asset palsu.
12. G06 full sheet terbuka langsung sekitar 94%.
13. G07 tidak mengganti model AR utama.
14. G08 menampilkan Squat, Dynamic Stretching, dan Ladder Drill.
15. G09 benar-benar memeriksa permission setelah kembali dari settings.
16. Target lost dan pause/resume tidak meninggalkan UI/model/audio salah state.
17. Tidak ada data pribadi atau camera frame yang disimpan/dikirim.
18. Minimum 30 FPS pada perangkat sasaran atau hasil profiling dan blocker terdokumentasi.
19. EditMode/PlayMode tests yang relevan lulus.
20. Build Android terakhir berhasil, atau keterbatasan environment dilaporkan secara akurat.
21. Tidak ada asset internet/AI yang diklaim sebagai asset resmi.
22. Perubahan sudah dibagi menjadi commit logis dan push berhasil atau blocker credential dilaporkan.
23. Build dikonfigurasi AR Optional dan dapat dibuka pada perangkat non-ARCore.
24. Perangkat unsupported menerima notice ramah lalu masuk G08 tanpa melihat kamera hitam.
25. G08 menyediakan loop, timeline pose, audio, materi, dan gerakan terkait untuk ketiga movement; hanya fitur AR yang hilang.
26. `MainAR` tidak dimuat sebelum availability Ready dan permission granted.
27. Camera denied tidak pernah salah diklasifikasikan sebagai AR unsupported.

Laporan akhir harus mencantumkan:

- Fitur selesai.
- Test/build yang dijalankan beserta hasil.
- Perangkat uji.
- Commit terakhir.
- Asset placeholder.
- Asset final yang masih ditunggu.
- Known issue dan langkah berikutnya.

---

## 24. Definition of Done per Perubahan

Sebuah perubahan baru dianggap selesai jika:

- Scope sesuai satu tujuan.
- Tidak merusak state lain.
- Tidak menambah warning/error yang tidak dijelaskan.
- Responsive dan safe area bila menyentuh UI.
- Data-driven bila menyentuh movement/content.
- Lifecycle/cancellation ditangani bila async/coroutine.
- Test ditambah atau alasan tidak menambah test dicatat.
- Diff telah direview.
- Commit message menjelaskan hasil, bukan aktivitas.
- Push berhasil atau blocker dicatat.

---

## 25. Referensi Resmi

Gunakan dokumentasi resmi dan cocokkan dengan versi package yang terpasang:

- Unity 6.5 manual: `https://docs.unity3d.com/6000.5/Documentation/Manual/`
- AR development Unity 6.5: `https://docs.unity3d.com/6000.5/Documentation/Manual/AROverview.html`
- AR Foundation package: `https://docs.unity3d.com/Packages/com.unity.xr.arfoundation@6.5/`
- ARCore XR Plugin: `https://docs.unity3d.com/Packages/com.unity.xr.arcore@6.5/`
- Google ARCore + AR Foundation: `https://developers.google.com/ar/develop/unity-arf/getting-started-ar-foundation`
- ARCore supported devices: `https://developers.google.com/ar/devices`
- AR Required vs AR Optional: `https://developers.google.com/ar/develop/unity-arf/enable-arcore`
- AR Foundation session availability: `https://docs.unity3d.com/Packages/com.unity.xr.arfoundation@6.5/manual/features/session/platform-support.html`
- Logo Universitas Negeri Padang: `https://www.unp.ac.id/logo-universitas/`

Jangan menyalin contoh API dari versi package berbeda tanpa memeriksa signature API pada package yang benar-benar terpasang.
