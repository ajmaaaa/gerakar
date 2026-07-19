# GerakAR

Aplikasi mobile Augmented Reality untuk anak Sekolah Dasar. Arahkan kamera ke gambar di flipbook, model 3D langsung muncul dan memperagakan gerakan olahraga.

---

## Gerakan yang Tersedia

| # | Gerakan | Warna Aksen |
|---|---------|-------------|
| 1 | Squat | Terracotta `#B8684A` |
| 2 | Dynamic Stretching | Muted Teal `#3F7C78` |
| 3 | Ladder Drill | Muted Mustard `#C3A24B` |

---

## Cara Kerja

1. Buka aplikasi → intro singkat → petunjuk pertama kali (sekali saja)
2. Arahkan kamera ke salah satu gambar di flipbook
3. Model 3D muncul dan animasi langsung berjalan berulang
4. Geser timeline bawah untuk memeriksa pose per frame
5. Ketuk tombol buku untuk membuka materi dan gerakan terkait

---

## Alur Interaksi & Desain Utama

- **Bingkai Scan (Fokus)**: Hanya berupa garis siku sudut tepi (bracket corners) tanpa background transparan. Bingkai ini akan langsung hilang ketika target terdeteksi.
- **Toast Transisi Deteksi**: Saat target terdeteksi, layar menampilkan toast bercentang hijau **"Gambar terdeteksi"** selama 1.2 detik sebelum memunculkan model AR.
- **Bottom Sheet Dinamis**:
  - Konten materi dapat di-scroll secara vertikal (atas-bawah).
  - Kartu gerakan serupa berada di bagian bawah dan dapat di-scroll secara horizontal (kiri-kanan).
  - Memilih gerakan serupa akan langsung memperbarui materi di atasnya secara langsung (*in-place*) dengan label **"Materi Tambahan"** dan tombol untuk kembali ke gerakan utama.
- **Mode Pembelajaran Mandiri (Non-AR)**: Tersedia untuk perangkat yang tidak mendukung AR — katalog gerakan, pratinjau model, timeline pose, dan materi tetap bisa diakses tanpa kamera.

---

## Tech Stack

| Bagian | Teknologi |
|--------|-----------|
| Engine | Unity 6000.5.3f1 |
| Bahasa | C# (namespace `MoveMotion.*`) |
| AR Image Tracking | ARToolkitX (ARUnityX) 1.3.1 via OpenUPM |
| AR Abstraction | AR Foundation 6.5.0 + ARCore XR Plugin 6.5.0 (AR Optional) |
| Render | URP 17.5.0 (mobile minimal) |
| UI | uGUI + TextMeshPro |
| Build | IL2CPP, ARM64 + ARMv7, Android min API 26 |
| Bundle ID | `id.ac.unp.gerakar` |

---

## Struktur Folder

```
gerakar/
├── app/                          # Unity project
│   ├── Assets/
│   │   └── App/
│   │       ├── Animations/       # Animator controller (AirSquat.controller)
│   │       ├── AR/               # Slot library referensi gambar target (belum diisi)
│   │       ├── Audio/            # Slot audio (belum aktif)
│   │       ├── Content/          # MovementData & MovementDatabase asset
│   │       ├── Models/
│   │       │   ├── Squat/        # Air Squat.fbx + material + texture (prototype)
│   │       │   ├── DynamicStretch/  # Kosong — menunggu model final
│   │       │   └── LadderDrill/     # Kosong — menunggu model final
│   │       ├── Prefabs/          # Prefab model & UI (AirSquat.prefab, RelatedCard, dll.)
│   │       ├── Scenes/           # Bootstrap.unity & MainAR.unity
│   │       ├── Scripts/
│   │       │   ├── AR/           # ARImageTrackingController, ARUnityXSessionController,
│   │       │   │                 # ScreenSpaceModelController, ModelPool
│   │       │   ├── Animation/    # MovementController
│   │       │   ├── Audio/        # AudioGuideController
│   │       │   ├── Content/      # MovementData, MovementDatabase
│   │       │   ├── Core/         # AppState, AppStateManager, MoveMotionEvents,
│   │       │   │                 # ARAvailabilityChecker, PermissionController
│   │       │   └── UI/           # Semua controller UI (BootstrapUIController,
│   │       │                     # ARUIController, BottomSheetController, dll.)
│   │       └── UI/
│   │           ├── Icons/        # Lucide SVG icons
│   │           └── Sprites/
│   │               ├── Branding/ # background.png, unp.png, icon.png (trial)
│   │               ├── Primary/  # Thumbnail Squat, DynamicStretching, LadderDrill
│   │               └── Shapes/   # RoundedRect, Circle, dll.
│   ├── Packages/                 # manifest.json (ARToolkitX via OpenUPM)
│   └── ProjectSettings/
├── components/                   # Sumber gambar dari pemilik project
└── docs/
    ├── ENVIRONMENT.md            # Cara setup Unity & build
    └── ASSET_CHECKLIST.md        # Daftar asset yang masih dibutuhkan
```

---

## Status Asset

| Asset | Status |
|-------|--------|
| Gambar ilustrasi gerakan terkait | ✅ Ada di `components/` |
| Background intro (G01) | ⚠️ Trial — `components/background.png` (belum final) |
| Logo UNP | ⚠️ Trial — `components/unp.png` (belum final, bukan logo resmi) |
| App icon | ⚠️ Trial — `components/icon.png` (belum final) |
| Logo GerakAR | ❌ Belum — masih placeholder teks |
| Model 3D Squat + animasi | ⚠️ Trial — `Air Squat.fbx` prototype aktif, bukan karakter final |
| Model 3D Dynamic Stretching + animasi | ❌ Belum — placeholder capsule |
| Model 3D Ladder Drill + animasi | ❌ Belum — placeholder capsule |
| Gambar target AR (3 buah) | ❌ Belum — lihat `docs/ASSET_CHECKLIST.md` |
| Audio panduan | ❌ Belum — slot sudah disiapkan |

---

## Setup

Butuh **Unity 6000.5.3f1**. Buka project dari folder `app/`, bukan dari root repository.

Panduan lengkap → [`docs/ENVIRONMENT.md`](docs/ENVIRONMENT.md)

---

## Privasi

- Tidak ada login, akun, atau profil
- Tidak menyimpan atau mengunggah gambar kamera
- Tidak ada iklan atau analytics
- Hanya minta izin kamera
