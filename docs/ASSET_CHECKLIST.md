# MotionLearn – Asset Checklist

Status terakhir: **Implementasi awal selesai (semua placeholder aktif)**

---

## ✅ Asset yang Sudah Ada

| File | Lokasi | Keterangan |
|------|--------|------------|
| Ilustrasi squat (9 variasi) | `components/1-*.png` … `9-*.png` | Siap digunakan sebagai thumbnail |
| Ilustrasi dynamic stretch (8 variasi) | `components/10-*.png` … `19-*.png` | Siap digunakan sebagai thumbnail |
| Ilustrasi ladder drill (15+ variasi) | `components/20-*.png` … `34-*.png` | Siap digunakan sebagai thumbnail |
| Gambar C1–C5 | `components/C1.png` … `C5.png` | Kandidat cover/intro – konfirmasi pemilik |
| Gambar I1–I2 | `components/I1.png`, `I2.png` | Kandidat ilustrasi besar – konfirmasi pemilik |
| Gambar T1–T9 | `components/T1.png` … `T9.png` | Kandidat gambar target AR – konfirmasi pemilik |

---

## ❌ Asset Final yang Masih Ditunggu

### 1. Gambar Target AR (WAJIB untuk tracking)

| Slot | Reference Name di Library | Keterangan |
|------|--------------------------|------------|
| Target Squat | `squat_target` | Gambar flipbook halaman Squat – harus dicetak, kaya fitur, tidak polos |
| Target Dynamic Stretching | `dynamic_stretch_target` | Gambar flipbook halaman Dynamic Stretching |
| Target Ladder Drill | `ladder_drill_target` | Gambar flipbook halaman Ladder Drill |

**Cara import**: Lihat `docs/ENVIRONMENT.md` → *Importing the final AR target images*.

**Persyaratan gambar target yang baik**:
- Memiliki detail visual tersebar di seluruh gambar
- Tidak didominasi area kosong atau warna polos
- Setiap target jelas berbeda dari yang lain
- Ukuran fisik di flipbook harus dicatat (dalam meter) untuk diisi di library

---

### 2. Model 3D + Animasi (WAJIB untuk model final)

| Slot | Folder tujuan | Format | Keterangan |
|------|--------------|--------|------------|
| Model Squat | `Assets/App/Models/Squat/` | FBX atau GLB | Rig + animation clip looping |
| Model Dynamic Stretching | `Assets/App/Models/DynamicStretch/` | FBX atau GLB | Rig + animation clip looping |
| Model Ladder Drill | `Assets/App/Models/LadderDrill/` | FBX atau GLB | Rig + animation clip looping (root motion: OFF) |

**Batas awal**: ~30.000–60.000 triangle per model, 1 material utama, texture max 2048px.

**Setelah import**:
1. Buat prefab dari model di `Assets/App/Prefabs/Models/`
2. Assign prefab ke field `modelPrefab` di masing-masing `MovementData_*.asset`
3. Assign animation clip ke field `animationClip`
4. Pastikan Animator Controller = `MovementAnimatorController`
5. Sesuaikan `keyPoses[].normalizedTime` dengan melihat Animation Window

---

### 3. Logo MotionLearn (opsional untuk intro)

| Slot | Format | Keterangan |
|------|--------|------------|
| Logo MotionLearn | PNG/SVG (transparent) | Ditampilkan di intro screen, tidak di halaman kamera |

Assign ke field `introImage` di `IntroController` component pada Bootstrap scene.

---

### 4. Cover Final (opsional untuk intro)

| Slot | Format | Keterangan |
|------|--------|------------|
| Cover intro | PNG (2:3 portrait recommended) | Background illustration untuk intro screen |

Kandidat: `C1.png` – `C5.png` atau file baru. Konfirmasi dengan pemilik project.

---

### 5. Audio Panduan (pengembangan berikutnya)

| Slot | Format | Keterangan |
|------|--------|------------|
| Audio Squat | WAV/MP3 | Field `audioGuide` di `MovementData_Squat.asset` |
| Audio Dynamic Stretching | WAV/MP3 | Field `audioGuide` di `MovementData_DynamicStretch.asset` |
| Audio Ladder Drill | WAV/MP3 | Field `audioGuide` di `MovementData_LadderDrill.asset` |

Audio TIDAK ditampilkan di UI versi awal. Slot sudah tersedia untuk pengembangan berikutnya.

---

## Thumbnail Gerakan Terkait

Thumbnail ilustrasi untuk kartu gerakan terkait sudah tersedia di `components/`. 

Pemetaan yang direkomendasikan:

### Squat Related
| Kartu | File ilustrasi |
|-------|---------------|
| Bodyweight Squat | `components/4-Deep Squat.png` |
| Squat Jump | `components/5-Jumping Squat.png` |
| Pistol Squat | `components/6-Single-Leg Squat.png` |
| Front Squat | `components/3-Front Squat.png` |

### Dynamic Stretching Related
| Kartu | File ilustrasi |
|-------|---------------|
| Standing Toe Touch | `components/13-Standing Toe Touch.png` |
| Diagonal Reach | `components/14-Stepping Trunk Turn.png` |
| Trunk Rotation | `components/14-Stepping Trunk Turn.png` |
| High Knee March | `components/11-High-Knee March.png` |

### Ladder Drill Related
| Kartu | File ilustrasi |
|-------|---------------|
| One-In | `components/20-1-in-the-Hole.png` |
| Two-In | `components/21-2-in-the-Hole.png` |
| Side Shuffle | `components/27-Ickey Shuffle.png` |
| Two-Foot Hop | `components/24-2-Foot Hops.png` |

**Cara import**: Drag PNG dari `components/` ke `Assets/App/UI/Sprites/`, set Texture Type → Sprite, assign ke `RelatedMovementData.thumbnail`.
