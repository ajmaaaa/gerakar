using UnityEngine;

namespace GerakAR.UI
{
    public static class GerakARTheme
    {
        // ── Green-dominant palette ────────────────────────────────────
        public static readonly Color DeepForest     = new Color(0.07f, 0.216f, 0.165f, 1f);  // #12372A
        public static readonly Color ForestGreen    = new Color(0.12f, 0.365f, 0.259f, 1f);  // #1F5D42
        public static readonly Color WarmCream      = new Color(0.957f, 0.941f, 0.902f, 1f); // #F4F0E6
        public static readonly Color WarmWhite      = new Color(1f, 1f, 1f, 1f);             // #FFFFFE
        public static readonly Color SoftSand       = new Color(0.918f, 0.867f, 0.812f, 1f); // #EADDCF
        public static readonly Color SecondaryText  = new Color(0.443f, 0.376f, 0.251f, 1f); // #716040
        public static readonly Color Error          = new Color(0.949f, 0.314f, 0.259f, 1f); // #F25042

        // ── Categories ────────────────────────────────────────────────
        public static Color CategoryColor(string category) => category switch
        {
            "Squat"              => ForestGreen,
            "DynamicStretching"  => ForestGreen,
            "LadderDrill"        => ForestGreen,
            _                    => ForestGreen
        };

        // ── Layout ────────────────────────────────────────────────────
        public const float ScreenWidth  = 360f;
        public const float ScreenHeight = 800f;
        public const float Margin       = 20f;
        public const float RadiusCard   = 14f;
        public const float RadiusSheet  = 28f;
        public const float ButtonHeight = 48f;
        public const float FabSize      = 48f;
        public const float IconSize     = 22f;
    }
}
