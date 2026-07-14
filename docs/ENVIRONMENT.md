# GerakAR – Environment & Setup Guide

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
      - GerakARLogo (Image)
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

UI Canvas (Screen Space - Overlay, sort order 10)
  ├── ScanOverlay
  │     ├── ScanFrame (Image with border sprite)
  │     └── HintText (TMP): "Arahkan kamera ke gambar gerakan"
  │
  ├── ARControls (hidden initially)
  │     ├── MovementLabel (TMP): movement name
  │     └── Timeline
  │           ├── Slider (UI Slider)
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
  │     ├── MovementName (TMP)
  │     ├── CategoryAccentBar (Image)
  │     ├── ShortDescription (TMP)
  │     ├── StepsContainer (VerticalLayoutGroup)
  │     ├── SafetyTip (TMP)
  │     ├── FullStateExtras (hidden in Half state)
  │     │     ├── TrainedAreasContainer
  │     │     └── CommonMistakesContainer
  │     ├── RelatedCardsContainer (HorizontalScrollView)
  │     └── RelatedDetailPanel (initially inactive)
  │           ├── BackButton
  │           ├── LabelText (TMP): "Materi Tambahan"
  │           ├── TitleText (TMP)
  │           ├── DescText (TMP)
  │           └── StepsContainer
  │
  └── Scrim (full-screen semi-transparent Image, initially inactive)

ARUIController.cs wiring:
  scanOverlay → ScanOverlay
  arControls → ARControls
  movementNameLabel → MovementLabel
  closeButton → CloseButton
  materialButton → MaterialButton
  timelineRoot → Timeline
```

---

## Color Tokens (Green Forest)

```
Deep Forest    #12372A  – FABs, headers
Forest Green   #1F5D42  – active elements, progress
Moss Green     #607D4F  – markers, secondary
Soft Sage      #A9BEA2  – track, inactive, grab handle
Warm Cream     #F4F0E6  – bottom sheet background, cards
Charcoal       #202620  – body text
Terracotta     #B8684A  – Squat accent
Muted Teal     #3F7C78  – Dynamic Stretch accent
Muted Mustard  #C3A24B  – Ladder Drill accent
```

---

## Build (Android)

```bash
# Using Unity batch mode (requires Unity installation)
/path/to/Unity \
  -batchmode \
  -projectPath /path/to/gerakar/app \
  -buildTarget Android \
  -quit \
  -logFile /tmp/gerakar-build.log
```

Or build from **File → Build Settings → Build** inside Unity Editor.

APK for testing → signed APK  
Play Store → AAB (File → Build Settings → Build App Bundle)
