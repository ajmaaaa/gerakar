# MotionLearn – Environment & Setup Guide

## Unity Editor Required

**Unity version**: `6000.5.3f1` (Unity 6.5.3)

All C# scripts and asset files are written for this version. Do NOT open with an older Unity version as the URP and AR Foundation APIs may differ.

---

## Opening the Project

1. Open **Unity Hub**.
2. Click **Add** → **Add project from disk**.
3. Select the `app/` folder (not the repository root).
4. Unity Hub will detect the version automatically.

---

## Initial Setup Checklist

After first opening, Unity will import all packages. This may take 5–10 minutes.

### Verify packages (Window → Package Manager)
| Package | Version |
|---------|---------|
| AR Foundation | 6.5.0 |
| ARCore XR Plugin | 6.5.0 |
| Universal RP | 17.5.0 |
| TextMeshPro | (included) |

### Platform settings (File → Build Settings)
- Switch platform to **Android**
- Minimum API Level: **API 24** (Android 7.0)
- Scripting Backend: **IL2CPP**
- Target Architecture: **ARM64**

### XR Plugin Management (Edit → Project Settings → XR Plug-in Management)
- Enable **ARCore** for Android tab
- Do NOT enable ARKit unless iOS is needed

---

## Importing Assets from `components/`

The `components/` folder at the repository root contains source images from the project owner.

### How to import illustrations for Related Movements:
1. In Unity Project window, navigate to `Assets/App/UI/Sprites/`
2. Drag-and-drop relevant PNG files from `components/` into this folder
3. In the Inspector, set **Texture Type** to **Sprite (2D and UI)**
4. Apply
5. Assign sprites to `RelatedMovementData.thumbnail` fields in each `MovementData_*.asset`

### How to import the final AR target images:
1. Navigate to `Assets/App/AR/ReferenceImages/`
2. Import the three target PNG files (one per movement)
3. Create an `XRReferenceImageLibrary` asset in that folder
4. Add each image with these exact **Reference Names**:
   - `squat_target`
   - `dynamic_stretch_target`
   - `ladder_drill_target`
5. Set the physical size for each image (measure the actual flipbook page in meters)
6. Assign the library to `ARTrackedImageManager.referenceLibrary` in the MainAR scene

---

## Scene Setup Instructions

### Bootstrap Scene (`Assets/App/Scenes/Bootstrap.unity`)

Create a new scene with:

```
GameObject: BootstrapManager
  Components:
    - AppStateManager
    - PermissionController
    - ARAvailabilityChecker

Canvas (Screen Space - Overlay)
  - IntroPanel (Image + CanvasGroup)
      - MotionLearnLogo (Image)
      - CoverImage (Image)  ← placeholder slot for final cover
      - IntroController (on BootstrapManager or separate GO)

  - OnboardingPanel (initially inactive)
      - Title: "Sebelum Mulai"
      - BulletList:
          - "Gunakan di tempat yang cukup luas."
          - "Minta guru atau orang tua mendampingi."
          - "Izinkan kamera untuk melihat gerakan."
      - MulaiButton (Button) → OnboardingController.OnMulaiPressed()

  - UnsupportedPanel (initially inactive)
      - MessageText (TMP)
      - ARAvailabilityChecker.unsupportedPanel = this
```

Add `Bootstrap` scene to Build Settings at index 0.

---

### MainAR Scene (`Assets/App/Scenes/MainAR.unity`)

```
AR Session
XR Origin
  AR Camera
    ARCameraBackground
    ARCameraManager
AR Tracked Image Manager
  referenceLibrary: [assign XRReferenceImageLibrary]
  maxNumberOfMovingImages: 1

ModelRoot (empty Transform, used by ModelPool)

Managers (empty GameObject)
  - AppStateManager (should auto-persist from Bootstrap; add here as fallback)
  - ARImageTrackingController
      movementDatabase: [assign MovementDatabase.asset]
      modelPool: [assign ModelPool]
  - ModelPool
      modelRoot: [assign ModelRoot transform]
  - MovementController
  - AudioGuideController (RequireComponent: AudioSource)
      movementDatabase: [assign MovementDatabase.asset]

UI Canvas (Screen Space - Overlay, sort order 10)
  ├── ScanOverlay
  │     ├── ScanFrame (Image with border sprite, corners only)
  │     └── HintText (TMP): "Arahkan kamera ke gambar gerakan"
  │
  ├── DetectionToast (hidden initially)
  │     └── Panel (Warm Cream, green checkmark icon + Text: "Gambar terdeteksi")
  │
  ├── ARControls (hidden initially)
  │     ├── MovementLabel (TMP): movement name
  │     └── Timeline
  │           ├── Slider (UI Slider)
  │           ├── PlayPauseButton (Button, next to slider)
  │           │     └── Icon (Image)
  │           ├── MarkerContainer (RectTransform)
  │           ├── HintText (TMP): "Lepaskan untuk melanjutkan gerakan"
  │           └── PoseTimelineController
  │
  ├── FloatingButtons (hidden initially)
  │     ├── CloseButton (Button, circle 52–56dp, Deep Forest #12372A)
  │     └── MaterialButton (Button, circle 52–56dp, Deep Forest #12372A)
  │
  ├── BottomSheet
  │     ├── Grab Handle
  │     ├── CloseButton (X)
  │     ├── CategoryTypeLabel (TMP) ← "Gerakan Utama" / "Materi Tambahan"
  │     ├── BackToPrimaryButton (Button) ← hidden initially
  │     ├── MovementName (TMP)
  │     ├── CategoryAccentBar (Image)
  │     ├── ScrollView (Vertical)
  │     │     └── Content
  │     │           ├── ShortDescription (TMP)
  │     │           ├── StepsContainer (VerticalLayoutGroup)
  │     │           └── SafetyTip (TMP)
  │     ├── FullStateExtras (hidden in Half state, inside ScrollView)
  │     │     ├── TrainedAreasContainer
  │     │     └── CommonMistakesContainer
  │     └── RelatedCardsContainer (HorizontalScrollView)
  │
  └── Scrim (full-screen semi-transparent Image, initially inactive)

ARUIController.cs wiring:
  scanOverlay → ScanOverlay
  detectionToast → DetectionToast
  arControls → ARControls
  movementNameLabel → MovementLabel
  closeButton → CloseButton
  materialButton → MaterialButton
  timelineRoot → Timeline
  playPauseButton → PlayPauseButton
  playPauseIcon → PlayPauseButton/Icon
  playSprite → [Play Icon Sprite]
  pauseSprite → [Pause Icon Sprite]
```

---

## Color Tokens (Green Forest)

```
Clean Off-White #FAF9F6  – Background bottom sheet, kartu, panel (lebih bersih, tidak kusam)
Deep Forest     #12372A  – FAB, teks heading
Forest Green    #1F5D42  – Progress, nomor langkah
Soft Sage       #A9BEA2  – Track timeline, grab handle
Charcoal        #202620  – Teks isi
Terracotta      #B8684A  – Aksen Squat
Muted Teal      #3F7C78  – Aksen Dynamic Stretch
Muted Mustard   #C3A24B  – Aksen Ladder Drill
```

---

## Aturan Desain & Interaksi Penting

### 1. Bingkai Scan (Fokus)
- Bingkai scan hanya berupa **garis sudut tepi saja** (bracket corners).
- **TIDAK boleh** menggunakan background semi-transparent putih/abu-abu di dalam atau di luar bingkai.
- Bingkai scan langsung **hilang** ketika gambar terdeteksi (masuk state `Detecting`).

### 2. Efek Transisi Deteksi
- Ketika gambar target terdeteksi, muncul panel toast di tengah layar dengan pesan **"Gambar terdeteksi"** dan **ikon centang hijau** selama 1.2 detik.
- Selama toast ini tampil, model AR utama **tetap disembunyikan**.
- Setelah 1.2 detik (toast selesai), model AR utama baru dimunculkan dan animasi loop berjalan.

### 3. Interaksi Bottom Sheet
- Konten materi (deskripsi, langkah, tips) berada dalam ScrollView vertikal (bisa di-scroll atas-bawah).
- Daftar gerakan serupa berada di bagian bawah, dapat digeser horizontal (kiri-kanan).
- Ketika kartu gerakan serupa ditekan, konten materi di atasnya langsung diperbarui di tempat (*in-place update*) sesuai gerakan yang dipilih, lengkap dengan label **"Materi Tambahan"** dan tombol **"Kembali ke Gerakan Utama"**. Model AR di kamera tetap mempertahankan gerakan utama.

---

## Build (Android)

```bash
# Using Unity batch mode (requires Unity installation)
/path/to/Unity \
  -batchmode \
  -projectPath /path/to/motionlearn/app \
  -buildTarget Android \
  -quit \
  -logFile /tmp/motionlearn-build.log
```

Or build from **File → Build Settings → Build** inside Unity Editor.

APK for testing → signed APK  
Play Store → AAB (File → Build Settings → Build App Bundle)
