// ============================================================
// MotionLearn – AudioGuideController.cs
// Manages playback of the voice guide audio clip for each movement.
// Playback starts only after the user presses the audio control.
// Pauses when the user scrubs the timeline or target is lost.
// Provides public methods for the Play/Pause UI buttons.
// ============================================================
using UnityEngine;
using System;
using MotionLearn.Core;
using MotionLearn.Content;

namespace MotionLearn.Audio
{
    [RequireComponent(typeof(AudioSource))]
    public class AudioGuideController : MonoBehaviour
    {
        // ── Singleton / Reference ─────────────────────────────────────

        public static AudioGuideController Instance { get; private set; }
        public static event Action<bool> OnAudioAvailabilityChanged;

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
            MotionLearnEvents.OnMovementDetected += PrepareAudioForMovement;
            MotionLearnEvents.OnTrackingLost += StopAudio;
            MotionLearnEvents.OnPoseInspectionStarted += PauseAudioOnScrub;
            MotionLearnEvents.OnPoseInspectionEnded += ResumeAudioAfterScrub;
            AppStateManager.OnStateChanged += OnAppStateChanged;
        }

        private void OnDestroy()
        {
            MotionLearnEvents.OnMovementDetected -= PrepareAudioForMovement;
            MotionLearnEvents.OnTrackingLost -= StopAudio;
            MotionLearnEvents.OnPoseInspectionStarted -= PauseAudioOnScrub;
            MotionLearnEvents.OnPoseInspectionEnded -= ResumeAudioAfterScrub;
            AppStateManager.OnStateChanged -= OnAppStateChanged;
        }

        // ── Event Handlers ────────────────────────────────────────────

        private void PrepareAudioForMovement(string movementId)
        {
            _audioSource.Stop();
            _audioSource.clip = null;
            _currentData = null;
            _isManualPaused = false;

            if (movementDatabase == null)
            {
                OnAudioAvailabilityChanged?.Invoke(false);
                return;
            }
            MovementData data = movementDatabase.FindById(movementId);
            if (data == null || data.audioGuide == null)
            {
                OnAudioAvailabilityChanged?.Invoke(false);
                return;
            }

            _currentData = data;
            _isManualPaused = false;

            _audioSource.clip = data.audioGuide;
            _audioSource.Stop();
            OnAudioAvailabilityChanged?.Invoke(true);
        }

        private void StopAudio(string movementId)
        {
            _audioSource.Stop();
            _audioSource.clip = null;
            _currentData = null;
            OnAudioAvailabilityChanged?.Invoke(false);
        }

        private void OnAppStateChanged(AppState previous, AppState next)
        {
            if (next is AppState.ShowingMaterial or AppState.ShowingRelatedMaterial)
            {
                if (_audioSource.isPlaying)
                    _audioSource.Pause();
                return;
            }

            if (next is AppState.Scanning or AppState.TrackingLost or AppState.NonARCatalog or AppState.CameraDenied)
                StopAudio(ActiveMovementContext.ActiveId ?? string.Empty);
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
        public bool HasAudio => _currentData != null && _audioSource.clip != null;

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
