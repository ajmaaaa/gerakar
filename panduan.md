# Panduan Pengisian Materi & Aset — GerakAR

Dokumen ini menjelaskan struktur data dan cara mengisi materi, model 3D, audio, ikon, serta gambar target ke dalam project Unity **GerakAR**, baik untuk mode **AR** maupun **Non-AR**.

---

## 1. Lokasi Penyimpanan Materi (ScriptableObjects)

Seluruh data materi gerakan disimpan menggunakan aset `MovementData` (ScriptableObject) di dalam folder:
📁 `app/Assets/App/Content/MovementData/`

Saat ini ada tiga file materi utama:
1. **Squat** → `MovementData_Squat.asset`
2. **Dynamic Stretching** → `MovementData_DynamicStretch.asset`
3. **Ladder Drill** → `MovementData_LadderDrill.asset`

### Cara Mengedit Data Materi:
Anda bisa membuka file-file di atas langsung lewat **Inspector** di Unity Editor untuk mengedit properti berikut secara visual:
* **Movement Id**: ID unik gerakan (misal: `squat`).
* **Display Name**: Nama gerakan yang tampil di UI (misal: `Squat`).
* **Category Color**: Warna aksen kategori gerakan (misal: Terracotta untuk Squat).
* **Model Prefab**: Drag-and-drop prefab model 3D karakter yang memperagakan gerakan.
* **Animation Clip**: Drag-and-drop klip animasi gerakan loop terkait.
* **Key Poses**: Daftar pose utama (timeline) yang berisi `normalizedTime` (0-1) dan deskripsi pose.
* **Short Description**: Deskripsi singkat gerakan.
* **Steps**: Daftar cara melakukan gerakan (maksimal 3 langkah).
* **Safety Tips**: Tips keselamatan gerakan (Ingat, Ya!).
* **Trained Areas**: Otot-otot yang dilatih.
* **Common Mistakes**: Kesalahan gerakan umum yang harus dihindari.
* **Related Movements**: Daftar variasi gerakan serupa (Thumbnails, Title, Steps, dll.).
* **Audio Guide**: Aset file audio (.mp3 / .wav) panduan suara untuk gerakan ini.

---

## 2. Struktur Pengisian Aset

### A. Model 3D & Animasi (FBX)
* Letakkan file FBX model 3D karakter dan klip animasi di folder:
  📁 `app/Assets/App/Content/Models/`
* Pastikan Rig model diatur sebagai **Humanoid** di Inspector Unity (pada tab `Rig` model FBX).
* Kaitkan prefab model tersebut ke kolom **Model Prefab** di dalam aset `MovementData` terkait.

### B. Audio Panduan (.mp3 / .wav)
* Letakkan rekaman suara penjelasan instruktur/guru di folder:
  📁 `app/Assets/App/Content/Audio/`
* Drag file audio tersebut ke kolom **Audio Guide** di dalam aset `MovementData` terkait.

### C. Ikon & Grafis (.png / .svg)
* Ikon antarmuka aplikasi diletakkan di folder:
  📁 `app/Assets/App/UI/Icons/Lucide/`
* Project menggunakan paket pendukung vektor untuk membaca format `.svg` secara native sebagai sprite.
* Gambar thumbnail gerakan serupa dari pemilik project secara otomatis di-import dan disinkronkan dari folder root `components/` ke:
  📁 `app/Assets/App/UI/Sprites/Related/`

### D. Gambar Target Pindai AR (Image Tracking)
* Gambar target cetak diletakkan di folder:
  📁 `app/Assets/App/Content/Targets/`
* Masukkan gambar-gambar tersebut ke dalam **XR Reference Image Library** yang bernama `ReferenceImageLibrary.asset` di folder tersebut.
* Pastikan nama gambar di Reference Library sama persis dengan kolom **Reference Image Name** di `MovementData` terkait agar sistem pendeteksi kamera dapat mencocokkan model 3D yang tepat saat pemindaian berhasil.

---

## 3. Perbedaan Mode AR dan Non-AR

Aplikasi dirancang sebagai **AR Optional** agar tetap berjalan di perangkat yang tidak mendukung ARCore.

### A. Alur Mode AR (Scene `MainAR`)
1. Pengguna membuka scene kamera AR.
2. Kamera aktif menggunakan subsystem AR Foundation (`ARTrackedImageManager`).
3. Ketika target gambar terdeteksi, model 3D dimunculkan persis di atas target gambar secara spasial.
4. UI Bottom Sheet dapat ditarik untuk membaca materi detail.

### B. Alur Mode Non-AR (Scene `NonAR` / Fallback)
1. Perangkat yang tidak mendukung ARCore akan dialihkan otomatis ke mode Non-AR.
2. Mode Non-AR **tidak meminta izin kamera** dan tidak menyalakan sistem AR.
3. Sebagai gantinya, model 3D karakter ditampilkan di dalam viewport 3D terisolasi (menggunakan kamera Unity standar berlatar belakang studio netral).
4. Pengguna tetap bisa memutar animasi gerakan, menggeser timeline pose, mendengarkan audio panduan, dan membaca materi bottom sheet secara penuh.
