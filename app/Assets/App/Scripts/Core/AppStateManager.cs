// ============================================================
// GerakAR – AppStateManager.cs
// Singleton that owns the current AppState and broadcasts
// state-change events to all listening systems.
// ============================================================
using System;
using UnityEngine;

namespace GerakAR.Core
{
    /// <summary>
    /// Central state machine. All state transitions go through
    /// <see cref="TransitionTo"/>. Only one instance exists per session.
    /// </summary>
    public class AppStateManager : MonoBehaviour
    {
        // ── Singleton ────────────────────────────────────────────────

        public static AppStateManager Instance { get; private set; }

        // ── State ────────────────────────────────────────────────────

        /// <summary>Current application state.</summary>
        public AppState CurrentState { get; private set; } = AppState.Intro;

        /// <summary>
        /// Fired whenever the state changes.
        /// Args: (previousState, newState).
        /// </summary>
        public static event Action<AppState, AppState> OnStateChanged;

        // ── Unity lifecycle ──────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        // ── Public API ───────────────────────────────────────────────

        /// <summary>
        /// Attempt a state transition. No-op if already in the target state.
        /// </summary>
        public void TransitionTo(AppState newState)
        {
            if (CurrentState == newState) return;

            AppState previous = CurrentState;
            CurrentState = newState;

            Debug.Log($"[AppStateManager] {previous} → {newState}");
            OnStateChanged?.Invoke(previous, newState);
        }

        // ── Convenience helpers ──────────────────────────────────────

        public bool Is(AppState state) => CurrentState == state;

        public bool IsAny(params AppState[] states)
        {
            foreach (var s in states)
                if (CurrentState == s) return true;
            return false;
        }
    }
}
