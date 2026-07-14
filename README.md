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

## Tech Stack

| Bagian | Teknologi |
|--------|-----------|
| Engine | Unity 6000.5.3f1 |
| Bahasa | C# |
| AR | AR Foundation 6.5.0 + ARCore XR Plugin 6.5.0 |
| Render | URP 17.5.0 (mobile minimal) |
| UI | uGUI + TextMeshPro |
| Build | IL2CPP, ARM64, Android min API 24 |

---

## Struktur Folder

```
gerakar/
├── app/                          # Unity project
│   ├── Assets/
│   │   └── App/
│   │       ├── Animations/       # Animator controller
│   │       ├── AR/               # Reference image library
│   │       ├── Audio/            # Slot audio (belum aktif)
│   │       ├── Content/          # MovementData & MovementDatabase asset
│   │       ├── Models/           # Placeholder model (slot untuk model final)
│   │       ├── Prefabs/          # Prefab model & UI
│   │       ├── Scenes/           # Bootstrap.unity & MainAR.unity
│   │       ├── Scripts/
│   │       │   ├── AR/           # ARImageTrackingController, ModelPool
│   │       │   ├── Animation/    # MovementController
│   │       │   ├── Content/      # MovementData, MovementDatabase
│   │       │   ├── Core/         # AppState, AppStateManager, GerakAREvents
│   │       │   └── UI/           # Semua controller UI
│   │       └── UI/               # Icon, sprite, font
│   ├── Packages/                 # manifest.json
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
| Gambar target AR (3 buah) | ⏳ Belum — lihat `docs/ASSET_CHECKLIST.md` |
| Model 3D + animasi (3 karakter) | ⏳ Belum — placeholder capsule aktif |
| Logo GerakAR | ⏳ Belum — placeholder teks aktif |
| Audio panduan | ⏳ Belum — slot sudah disiapkan |

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
