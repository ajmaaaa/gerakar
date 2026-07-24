using TMPro;

namespace MotionLearn.Core
{
    public sealed class FontSet
    {
        public TMP_FontAsset Body { get; }
        public TMP_FontAsset Medium { get; }
        public TMP_FontAsset Heading { get; }
        public TMP_FontAsset Display { get; }

        public FontSet(TMP_FontAsset body, TMP_FontAsset medium, TMP_FontAsset heading, TMP_FontAsset display)
        {
            Body = body;
            Medium = medium;
            Heading = heading;
            Display = display;
        }

        public TMP_FontAsset this[int i] => i switch
        {
            0 => Body,
            1 => Medium,
            2 => Heading,
            3 => Display,
            _ => Body
        };

        public static implicit operator TMP_FontAsset(FontSet fs) => fs.Body;
    }
}
