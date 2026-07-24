// ============================================================
// MotionLearn – MovementDatabase.cs
// ScriptableObject registry that maps referenceImageName →
// MovementData. Assign via the Inspector on the MainAR scene.
// ============================================================
using System.Collections.Generic;
using UnityEngine;

namespace MotionLearn.Content
{
    /// <summary>
    /// Container for all three (or more) MovementData assets.
    /// Create via: Assets → Create → AR Sports → Movement Database
    /// Attach to a persistent GameObject in the MainAR scene.
    /// </summary>
    [CreateAssetMenu(menuName = "AR Sports/Movement Database", fileName = "MovementDatabase")]
    public class MovementDatabase : ScriptableObject
    {
        [Tooltip("All registered movement configurations. " +
                 "Each entry must have a unique referenceImageName.")]
        public List<MovementData> movements = new();

        // ── Lookup ───────────────────────────────────────────────────

        /// <summary>
        /// Returns the <see cref="MovementData"/> whose
        /// <c>referenceImageName</c> matches <paramref name="imageName"/>.
        /// Returns null and logs a warning if not found.
        /// </summary>
        public MovementData FindByReferenceImageName(string imageName)
        {
            foreach (var m in movements)
            {
                if (m != null && m.referenceImageName == imageName)
                    return m;
            }

            Debug.LogWarning($"[MovementDatabase] No MovementData found for image name '{imageName}'.");
            return null;
        }

        /// <summary>
        /// Returns the <see cref="MovementData"/> whose
        /// <c>movementId</c> matches <paramref name="id"/>.
        /// </summary>
        public MovementData FindById(string id)
        {
            foreach (var m in movements)
            {
                if (m != null && m.movementId == id)
                    return m;
            }
            return null;
        }
    }
}
