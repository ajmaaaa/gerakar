// ============================================================
// GerakAR – ModelPool.cs
// Simple cache that instantiates one GameObject per MovementData
// and reuses it on subsequent detections to avoid GC spikes.
// ============================================================
using System.Collections.Generic;
using UnityEngine;
using GerakAR.Content;

namespace GerakAR.AR
{
    /// <summary>
    /// Manages a pool of model GameObjects (one per MovementData).
    /// Models are instantiated on first use, then hidden/shown on
    /// subsequent tracking events. Root is set as the transform parent.
    /// </summary>
    public class ModelPool : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────

        [Tooltip("Parent transform under which all pooled models are placed.")]
        [SerializeField] private Transform modelRoot;

        [Tooltip("Camera used to keep the detected model centered after scanning.")]
        [SerializeField] private Camera presentationCamera;

        // ── Private state ─────────────────────────────────────────────

        private readonly Dictionary<string, GameObject> _pool = new();
        private GameObject _activeModel;
        private string _activeMovementId;
        private Transform _screenSpaceRoot;
        private ScreenSpaceModelController _screenSpaceController;

        // ── Public API ────────────────────────────────────────────────

        /// <summary>ID of the currently visible movement, or null.</summary>
        public string ActiveMovementId => _activeMovementId;

        /// <summary>Currently visible model GameObject, or null.</summary>
        public GameObject ActiveModel => _activeModel;

        public void SetRootActive(bool active)
        {
            if (modelRoot != null)
                modelRoot.gameObject.SetActive(active);
        }

        /// <summary>
        /// Activate the model for <paramref name="data"/> and deactivate
        /// any previously active model. The model is instantiated from the
        /// prefab on first call; subsequent calls reuse the instance.
        /// </summary>
        /// <returns>The activated (or already active) GameObject.</returns>
        public GameObject Activate(MovementData data)
        {
            if (data == null) return null;

            // Hide previous
            if (_activeModel != null)
                _activeModel.SetActive(false);

            // Retrieve or create
            if (!_pool.TryGetValue(data.movementId, out GameObject model) || model == null)
            {
                model = CreateModel(data);
                _pool[data.movementId] = model;
            }

            model.SetActive(true);
            MoveToScreenSpace(model);
            _activeModel = model;
            _activeMovementId = data.movementId;
            return model;
        }

        /// <summary>Hide whichever model is currently active.</summary>
        public void HideActive()
        {
            if (_activeModel != null)
                _activeModel.SetActive(false);

            _activeModel = null;
            _activeMovementId = null;
            if (_screenSpaceController != null)
                _screenSpaceController.SetInteractionEnabled(false);
        }

        /// <summary>
        /// Reposition the active model so its root aligns with
        /// <paramref name="anchor"/> (the tracked image transform).
        /// </summary>
        public void UpdateAnchor(Transform anchor)
        {
            if (_activeModel == null || anchor == null) return;
            _activeModel.transform.SetPositionAndRotation(anchor.position, anchor.rotation);
        }

        // ── Private helpers ───────────────────────────────────────────

        private GameObject CreateModel(MovementData data)
        {
            GameObject go;

            if (data.modelPrefab != null)
            {
                go = Instantiate(data.modelPrefab, modelRoot);
            }
            else
            {
                // ── Placeholder primitive ─────────────────────────────
                // A coloured cube verifies pose and scale before final models arrive.
                // Replace by assigning data.modelPrefab in the Inspector.
                go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.name = $"Placeholder_{data.movementId}";
                go.transform.SetParent(modelRoot, false);
                go.transform.localPosition = new Vector3(0f, 0.05f, 0f);
                go.transform.localScale = Vector3.one * 0.1f;

                // Apply category color
                var renderer = go.GetComponent<Renderer>();
                if (renderer != null)
                {
                    Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                    if (shader != null)
                    {
                        var mat = new Material(shader) { color = data.categoryColor };
                        renderer.sharedMaterial = mat;
                    }
                }
            }

            go.SetActive(false);
            return go;
        }

        private void MoveToScreenSpace(GameObject model)
        {
            EnsureScreenSpaceRoot();
            if (_screenSpaceRoot == null)
                return;

            model.transform.SetParent(_screenSpaceRoot, false);
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.identity;
            CenterModel(model);
            _screenSpaceController.ResetView();
            _screenSpaceController.SetInteractionEnabled(true);
        }

        private void EnsureScreenSpaceRoot()
        {
            if (_screenSpaceRoot != null)
                return;

            if (presentationCamera == null)
                presentationCamera = Camera.main;
            if (presentationCamera == null)
            {
                Debug.LogError("[ModelPool] Main presentation camera was not found.");
                return;
            }

            var root = new GameObject("ScreenSpaceModelRoot");
            _screenSpaceRoot = root.transform;
            _screenSpaceRoot.SetParent(presentationCamera.transform, false);
            _screenSpaceController = root.AddComponent<ScreenSpaceModelController>();
        }

        private void CenterModel(GameObject model)
        {
            Renderer[] renderers = model.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
                return;

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);

            Vector3 localCenter = _screenSpaceRoot.InverseTransformPoint(bounds.center);
            model.transform.localPosition -= localCenter;
        }
    }
}
