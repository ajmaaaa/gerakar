// ============================================================
// MotionLearn – MovementData.cs
// ScriptableObject that holds all data for one movement target.
// Assign prefab, clip, content, and audio from the Inspector.
// Final assets (model, clip, audio) are assigned here when ready.
// ============================================================
using System.Collections.Generic;
using UnityEngine;

namespace MotionLearn.Content
{
    // ── Supporting types ─────────────────────────────────────────────

    /// <summary>
    /// One annotated key pose on the animation timeline.
    /// </summary>
    [System.Serializable]
    public class KeyPoseData
    {
        [Range(0f, 1f)]
        [Tooltip("Position on the 0-1 normalised animation timeline.")]
        public float normalizedTime;

        [Tooltip("Short label shown above the marker dot (e.g. 'Turun penuh').")]
        public string label;
    }

    /// <summary>
    /// A related movement card shown in the bottom sheet.
    /// Does NOT require a 3D model – rendered as an illustration card.
    /// </summary>
    [System.Serializable]
    public class RelatedMovementData
    {
        public string title;

        [Tooltip("Illustration thumbnail from the components/ folder (imported as Sprite).")]
        public Sprite thumbnail;

        [TextArea(1, 3)]
        public string shortDescription;

        [Tooltip("Up to 3 step sentences for SD-level readers.")]
        public List<string> steps = new();

        [Tooltip("One safety tip per related movement.")]
        public List<string> safetyTips = new();
    }

    // ── Main ScriptableObject ────────────────────────────────────────

    /// <summary>
    /// All data required to drive one AR movement experience.
    /// Create via: Assets → Create → AR Sports → Movement Data
    /// </summary>
    [CreateAssetMenu(menuName = "AR Sports/Movement Data", fileName = "MovementData_New")]
    public class MovementData : ScriptableObject
    {
        // ── Identity ─────────────────────────────────────────────────

        [Header("Identity")]
        [Tooltip("Internal ID used by the event system. Must be unique (e.g. 'squat').")]
        public string movementId;

        [Tooltip("Exact name of this image in the XRReferenceImageLibrary " +
                 "(e.g. 'squat_target'). Case-sensitive.")]
        public string referenceImageName;

        [Tooltip("Display name shown in the AR label and bottom sheet heading.")]
        public string displayName;

        [Tooltip("Primary illustration used by the Non-AR catalog and detail preview.")]
        public Sprite thumbnail;

        // ── Category color ───────────────────────────────────────────

        [Header("Category")]
        [Tooltip("Accent color for this movement (Terracotta / Muted Teal / Muted Mustard).")]
        public Color categoryColor = new Color(0.72f, 0.41f, 0.29f); // Default Terracotta

        // ── 3D Model ─────────────────────────────────────────────────

        [Header("3D Model (assign final prefab when ready)")]
        [Tooltip("Root prefab to instantiate/activate for this target. " +
                 "Leave null to use the placeholder created by ModelPool.")]
        public GameObject modelPrefab;

        [Tooltip("Main animation clip for looping. Must match the Animator Controller state.")]
        public AnimationClip animationClip;

        // ── Key poses ────────────────────────────────────────────────

        [Header("Key Poses (5-8 for the timeline)")]
        public List<KeyPoseData> keyPoses = new();

        // ── Bottom-sheet content ─────────────────────────────────────

        [Header("Bottom Sheet – Half state")]
        [TextArea(1, 3)]
        public string shortDescription;

        [Tooltip("Up to 3 step sentences for SD-level readers.")]
        public List<string> steps = new();

        [Tooltip("One primary safety tip.")]
        public List<string> safetyTips = new();

        [Header("Bottom Sheet – Full state")]
        [Tooltip("Body parts trained by this movement.")]
        public List<string> trainedAreas = new();

        [Tooltip("Common mistakes to highlight.")]
        public List<string> commonMistakes = new();

        [Header("Related Movements")]
        public List<RelatedMovementData> relatedMovements = new();

        // ── Future audio ─────────────────────────────────────────────

        [Header("Audio (future – do not display in UI v1)")]
        [Tooltip("Narration clip for this movement. Assign when audio is ready.")]
        public AudioClip audioGuide;
    }
}
