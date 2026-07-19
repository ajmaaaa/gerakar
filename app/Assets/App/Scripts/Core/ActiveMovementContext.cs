using MoveMotion.Content;

namespace MoveMotion.Core
{
    /// <summary>
    /// Authoritative context for the currently active movement across the app.
    /// Replaces scattered static variables or fake extension methods.
    /// </summary>
    public static class ActiveMovementContext
    {
        public static string ActiveId { get; set; }
        public static MovementData ActiveData { get; set; }
        
        public static void Clear()
        {
            ActiveId = null;
            ActiveData = null;
        }
    }
}
