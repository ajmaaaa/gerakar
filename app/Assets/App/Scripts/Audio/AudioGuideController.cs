// ============================================================
// GerakAR – AudioGuideController.cs
// Manages playback of the voice guide audio clip for each movement.
// Playback starts automatically when the movement animation starts.
// Pauses when the user scrubs the timeline or target is lost.
// Provides public methods for the Play/Pause UI buttons.
// ============================================================
using UnityEngine;
using GerakAR.Core;
using GerakAR.Content;

namespace GerakAR.Audio
{
    [RequireComponent(typeof(AudioSource))]
    public class AudioGuideController : MonoBehaviour
    {
        // ── Singleton / Reference ─────────────────────────────────────

        public static AudioGuideController Instance { get; private set; }

        // ── Inspector ─────────────────────────────────────────────────

        [Header("References")]
        [SerializeField] private MovementDatabase movementDatabase;

        // ── Private state ─────────────────────────────────────────────

        private AudioSource _audioSource;
        private AppStateManager _stateMgr;
        private MovementData _currentData;
        private bool _isManualPaused;

        // ── Unity lifecycle ───────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            _audioSource = GetComponent<AudioSource>();
            _audioSource.playOnAwake = false;
            _audioSource.loop = false;
        }

        private void Start()
        {
            _stateMgr = AppStateManager.Instance;

            // Subscribe to event bus
            GerakAREvents.OnLoopStarted += PlayAudioForMovement;
            GerakAREvents.OnTrackingLost += StopAudio;
            GerakAREvents.OnPoseInspectionStarted += PauseAudioOnScrub;
            GerakAREvents.OnPoseInspectionEnded += ResumeAudioAfterScrub;
        }

        private void OnDestroy()
        {
            GerakAREvents.OnLoopStarted -= PlayAudioForMovement;
            GerakAREvents.OnTrackingLost -= StopAudio;
            GerakAREvents.OnPoseInspectionStarted -= PauseAudioOnScrub;
            GerakAREvents.OnPoseInspectionEnded -= ResumeAudioAfterScrub;
        }

        // ── Event Handlers ────────────────────────────────────────────

        private void PlayAudioForMovement(string movementId)
        {
            if (movementDatabase == null) return;
            MovementData data = movementDatabase.FindById(movementId);
            if (data == null || data.audioGuide == null) return;

            _currentData = data;
            _isManualPaused = false;

            _audioSource.clip = data.audioGuide;
            _audioSource.Play();
        }

        private void StopAudio(string movementId)
        {
            _audioSource.Stop();
            _audioSource.clip = null;
            _currentData = null;
        }

        private void PauseAudioOnScrub(float normTime)
        {
            if (_audioSource.isPlaying)
                _audioSource.Pause();
        }

        private void ResumeAudioAfterScrub()
        {
            // Resume only if not manually paused by UI button
            if (!_isManualPaused && _currentData != null && _audioSource.clip != null)
                _audioSource.UnPause();
        }

        // ── Public Play/Pause Control API ─────────────────────────────

        public bool IsPlaying => _audioSource.isPlaying;
        public bool IsPaused => !_audioSource.isPlaying && _audioSource.time > 0;

        /// <summary>Toggle play/pause manually from UI button.</summary>
        public void TogglePlayPause()
        {
            if (_audioSource.isPlaying)
            {
                PauseAudioManually();
            }
            else
            {
                PlayAudioManually();
            }
        }

        public void PlayAudioManually()
        {
            if (_currentData == null) return;
            _isManualPaused = false;
            _audioSource.UnPause();
            if (!_audioSource.isPlaying && _audioSource.clip != null)
                _audioSource.Play();
        }

        public void PauseAudioManually()
        {
            _isManualPaused = true;
            if (_audioSource.isPlaying)
                _audioSource.Pause();
        }
    }
}
