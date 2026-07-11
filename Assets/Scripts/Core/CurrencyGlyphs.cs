namespace SurveHive.Core
{
    /// <summary>
    /// Inline rich-text tags for the currency icons — the TMP default sprite
    /// asset (built by CurrencyGlyphsBuilder) carries glyphs named
    /// <c>honey</c>/<c>jelly</c>, so any UI text shows a currency as its image
    /// by prepending these. Pure markup + trailing space, no localizable words.
    /// </summary>
    public static class CurrencyGlyphs
    {
        public const string Honey = "<sprite name=\"honey\"> ";
        public const string Jelly = "<sprite name=\"jelly\"> ";
    }
}
