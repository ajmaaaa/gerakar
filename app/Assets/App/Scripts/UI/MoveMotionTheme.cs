using UnityEngine;

namespace MoveMotion.UI
{
    public static class MoveMotionTheme
    {
        // ── Modern Minimalist Palette ────────────────────────────────────
        public static readonly Color DarkText       = new Color(0.125f, 0.149f, 0.125f, 1f); // #202620 (Charcoal)
        public static readonly Color LightText      = new Color(0.663f, 0.745f, 0.635f, 1f); // #A9BEA2 (Soft Sage)
        public static readonly Color Background     = new Color(0.957f, 0.941f, 0.902f, 1f); // #F4F0E6 (Warm Cream)
        public static readonly Color Surface        = new Color(1f, 1f, 1f, 1f);             // #FFFFFF
        public static readonly Color Primary        = new Color(0.122f, 0.365f, 0.259f, 1f); // #1F5D42 (Forest Green)
        public static readonly Color PrimaryVariant = new Color(0.071f, 0.216f, 0.165f, 1f); // #12372A (Deep Forest)
        public static readonly Color Secondary      = new Color(0.376f, 0.490f, 0.310f, 1f); // #607D4F (Moss Green)
        public static readonly Color Error          = new Color(0.9f, 0.3f, 0.3f, 1f);       // #E64C4C

        // ── Categories ────────────────────────────────────────────────
        public static Color CategoryColor(string category) => category.ToLower() switch
        {
            "primary" => Primary,
            "secondary" => Secondary,
            "error" => Error,
            _ => LightText
        };

        // ── Layout ────────────────────────────────────────────────────
        public const float ScreenWidth  = 360f;
        public const float ScreenHeight = 800f;
        public const float Margin       = 24f; // Increased for more spacious feel
        public const float RadiusCard   = 16f; // Slightly larger radius for softer look
        public const float RadiusSheet  = 24f;
        public const float ButtonHeight = 56f; // Taller buttons for better touch
        public const float FabSize      = 56f;
        public const float IconSize     = 24f;
    }
}

