// ============================================================
// MotionLearn – AppState.cs
// Defines all application states used by the state machine.
// ============================================================
namespace MotionLearn.Core
{
    /// <summary>
    /// All mutually-exclusive states of the MotionLearn application.
    /// Transitions are managed by <see cref="AppStateManager"/>.
    /// </summary>
    public enum AppState
    {
        /// <summary>Initial bootstrap phase; loading dependencies.</summary>
        Bootstrapping,

        /// <summary>Splash / intro screen is showing (G01).</summary>
        Intro,

        /// <summary>Safety onboarding and instructions (G02).</summary>
        Onboarding,

        /// <summary>Checking whether ARCore is available on this device.</summary>
        CheckingAR,

        /// <summary>Waiting for the user to grant camera permission.</summary>
        RequestingPermission,

        /// <summary>Loading MainAR scene after permission and AR availability confirmed.</summary>
        LoadingARScene,

        /// <summary>Camera is live; waiting for an image target to appear (G03).</summary>
        Scanning,

        /// <summary>Target found; showing brief confirmation feedback (G04).</summary>
        TargetConfirmed,

        /// <summary>A target is tracked and the animation is looping normally (G05).</summary>
        TrackingLoop,

        /// <summary>User is dragging the pose timeline; animation is scrubbed manually.</summary>
        InspectingPose,

        /// <summary>Bottom-sheet material panel is open; animation is paused (G06).</summary>
        ShowingMaterial,

        /// <summary>Related movement detail is showing in bottom sheet (G07).</summary>
        ShowingRelatedMaterial,

        /// <summary>Tracking was lost; grace timer is counting down before hiding the model.</summary>
        TrackingLost,

        /// <summary>Transient notice that AR is unsupported; routes to NonARCatalog.</summary>
        UnsupportedNotice,

        /// <summary>Non-AR catalog showing three movements (G08).</summary>
        NonARCatalog,

        /// <summary>Non-AR movement player with loop, timeline, audio, and materials.</summary>
        NonARMovementPlayer,

        /// <summary>Camera permission denied by user (G09).</summary>
        CameraDenied,

        /// <summary>AR service installation failed after NeedsInstall.</summary>
        ARInstallFailed
    }
}
