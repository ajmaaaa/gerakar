// ============================================================
// GerakAR – OnboardingButtonWirer.cs
// Memasang listener tombol MULAI ke OnboardingController
// secara runtime untuk menghindari masalah serialisasi
// persistent listener dari Editor script.
// ============================================================
using UnityEngine;
using UnityEngine.UI;

namespace GerakAR.UI
{
    /// <summary>
    /// Dipasang langsung di GameObject tombol MULAI.
    /// Saat Start(), mencari OnboardingController di scene
    /// dan menyambungkan onClick tombol ke OnMulaiPressed().
    /// Pendekatan runtime ini 100% reliable.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class OnboardingButtonWirer : MonoBehaviour
    {
        private void Start()
        {
            var btn = GetComponent<Button>();
            if (btn == null) return;

            var controller = FindObjectOfType<OnboardingController>();
            if (controller == null)
            {
                Debug.LogWarning("[GerakAR] OnboardingButtonWirer: OnboardingController tidak ditemukan di scene.");
                return;
            }

            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(controller.OnMulaiPressed);
            Debug.Log("[GerakAR] OnboardingButtonWirer: Tombol MULAI berhasil disambungkan.");
        }
    }
}
