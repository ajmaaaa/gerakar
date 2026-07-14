// ============================================================
// GerakAR – AppState.cs
// Defines all application states used by the state machine.
// ============================================================
namespace GerakAR.Core
{
    /// <summary>
    /// All mutually-exclusive states of the GerakAR application.
    /// Transitions are managed by <see cref="AppStateManager"/>.
    /// </summary>
    public enum AppState
    {
        /// <summary>Splash / intro screen is showing.</summary>
        Intro,

        /// <summary>Waiting for the user to grant camera permission.</summary>
        RequestingPermission,

        /// <summary>Checking whether ARCore is available on this device.</summary>
        CheckingAR,

        /// <summary>Camera is live; waiting for an image target to appear.</summary>
        Scanning,

        /// <summary>A target is tracked and the animation is looping normally.</summary>
        TrackingLoop,

        /// <summary>User is dragging the pose timeline; animation is scrubbed manually.</summary>
        InspectingPose,

        /// <summary>Bottom-sheet material panel is open; animation is paused.</summary>
        ShowingMaterial,

        /// <summary>Tracking was lost; grace timer is counting down before hiding the model.</summary>
        TrackingLost,

        /// <summary>ARCore is not supported on this device.</summary>
        Unsupported
    }
}
