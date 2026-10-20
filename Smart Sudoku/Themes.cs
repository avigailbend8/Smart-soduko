using System;
using System.Collections.Generic;
using System.Linq;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;

namespace Smart_Sudoku
{
    /// <param name="Background">Gradient colors for the page background, top-left to bottom-right.</param>
    /// <param name="Pattern">Optional picture drawn over the background (file in Assets/Patterns).</param>
    /// <param name="Decorations">Optional emoji scattered softly over the background.</param>
    /// <param name="Pictures">Optional illustrations (files in Assets/Characters), shown as framed pictures.</param>
    /// <param name="Credit">Who drew the pictures, shown in the store.</param>
    public sealed record ThemePalette(
        string Id, string Name, int Price, uint[] Background, string? Pattern,
        uint Dark, uint Accent, uint Soft, uint Muted,
        uint Deco, uint Selected, uint SameNumber, uint Related, uint Hover, uint Pressed,
        string[]? Decorations = null, string[]? Pictures = null, string? Credit = null);

    /// <summary>
    /// Color themes. Applying one recolors the shared brushes in App.xaml,
    /// so every page updates at once.
    /// </summary>
    public static class Themes
    {
        public static readonly IReadOnlyList<ThemePalette> All = new[]
        {
            new ThemePalette("pink", "ורוד", 0, new[] { 0xFFFFF4F8u, 0xFFFCE1ECu }, null,
                0xFF8A2A55, 0xFFE75A97, 0xFFF6C6DA, 0xFFA0607E,
                0xFFF48FB1, 0xFFF9C5DA, 0xFFFBD8E6, 0xFFFEF0F6, 0xFFFFF6FA, 0xFFFBD6E5),
            new ThemePalette("purple", "סגול", 60, new[] { 0xFFF8F3FFu, 0xFFEADFFBu }, null,
                0xFF5B2A86, 0xFF9B5DE5, 0xFFDCCAF6, 0xFF7E6A9E,
                0xFFC3A6F0, 0xFFDCC8F7, 0xFFE8DBFA, 0xFFF6F0FE, 0xFFFAF6FF, 0xFFE6D9FA),
            new ThemePalette("blue", "תכלת", 60, new[] { 0xFFF2F8FFu, 0xFFDCEBFBu }, null,
                0xFF1F4E79, 0xFF4A90D9, 0xFFC2DBF3, 0xFF5F7FA0,
                0xFF9CC3EC, 0xFFC7DDF5, 0xFFD9E8F8, 0xFFEEF5FD, 0xFFF5FAFF, 0xFFD6E6F7),
            new ThemePalette("mint", "מנטה", 60, new[] { 0xFFF1FBF7u, 0xFFD6F2E6u }, null,
                0xFF1E6B52, 0xFF2FB386, 0xFFBBE7D5, 0xFF5E9683,
                0xFF8FD8BC, 0xFFC2EBDA, 0xFFD5F2E6, 0xFFEDF9F4, 0xFFF4FCF8, 0xFFD3F0E4),

            // Special themes: mixed colors and pictures
            new ThemePalette("rainbow", "קשת 🌈", 120,
                new[] { 0xFFFFE3EBu, 0xFFFFF0D4u, 0xFFE4F6D8u, 0xFFDCEFFFu, 0xFFEDE0FFu }, null,
                0xFF5E2A72, 0xFFD6409F, 0xFFF0CBE6, 0xFF8A6A93,
                0xFFF7A8D0, 0xFFF6CBE6, 0xFFFADCEF, 0xFFFEF1F8, 0xFFFFF7FC, 0xFFF6D3EA),
            new ThemePalette("sunset", "שקיעה 🌅", 120,
                new[] { 0xFFFFE6D5u, 0xFFFFD3E2u, 0xFFE6D6FFu }, null,
                0xFF7A2E4D, 0xFFF2667A, 0xFFF8C9CF, 0xFF9C6872,
                0xFFF9A3A8, 0xFFFBCBD0, 0xFFFCDDE0, 0xFFFFF1F2, 0xFFFFF8F8, 0xFFFAD5D9),
            new ThemePalette("galaxy", "גלקסיה ✨", 150,
                new[] { 0xFFE3DBFFu, 0xFFD6E4FFu, 0xFFF6DBF2u }, "stars.png",
                0xFF2E1A5E, 0xFF6C4AB6, 0xFFD3C8F2, 0xFF6E6396,
                0xFFA996E3, 0xFFD6CCF5, 0xFFE3DCF8, 0xFFF4F1FD, 0xFFF9F7FF, 0xFFE1D9F7,
                new[] { "🪐", "🌙", "⭐" }),
            new ThemePalette("hero", "גיבור-על 🕸️", 150,
                new[] { 0xFFFFE5E5u, 0xFFF3ECF5u, 0xFFDDE8FFu }, "web.png",
                0xFF1D3557, 0xFFD62828, 0xFFF2C1C1, 0xFF5A6B85,
                0xFF6C8CD5, 0xFFF6C9C9, 0xFFDCE6FA, 0xFFF4F7FD, 0xFFFFF7F7, 0xFFF3D0D0,
                new[] { "⚡", "💥", "🦸", "🛡️", "⭐" }),

            // Themed worlds, decorated with emoji
            new ThemePalette("princess", "נסיכות 👑", 200,
                new[] { 0xFFFFE6F2u, 0xFFF6E1FFu, 0xFFFFF4D9u }, null,
                0xFF7B2D6B, 0xFFD94F9F, 0xFFF3C7E3, 0xFF9A6A8E,
                0xFFF0B8DA, 0xFFF7CBE6, 0xFFFADDEF, 0xFFFFF1F9, 0xFFFFF8FC, 0xFFF6D2E8,
                new[] { "👑", "🏰", "💎", "👸", "🎀", "✨" }),
            new ThemePalette("unicorn", "חד-קרן 🦄", 200,
                new[] { 0xFFFFE3F1u, 0xFFE8E1FFu, 0xFFD9F4FFu }, null,
                0xFF5B3A8C, 0xFFB565D9, 0xFFE3CCF3, 0xFF85709E,
                0xFFD4A9EC, 0xFFE6D0F6, 0xFFEFE0FA, 0xFFF9F3FE, 0xFFFCF8FF, 0xFFEBDCF8,
                new[] { "🦄", "🌈", "☁️", "⭐", "💜", "✨" }),
            new ThemePalette("ocean", "ים 🌊", 200,
                new[] { 0xFFDDF4FFu, 0xFFCDEFF5u, 0xFFE3FBF5u }, null,
                0xFF0E4C6B, 0xFF1E9BC5, 0xFFB9E3F0, 0xFF55819A,
                0xFF8CCFE6, 0xFFC4E8F4, 0xFFD8F0F8, 0xFFEFF9FC, 0xFFF5FCFE, 0xFFD2ECF5,
                new[] { "🐠", "🐚", "🐬", "🐙", "🦀", "🐳" }),
            new ThemePalette("space", "חלל 🚀", 200,
                new[] { 0xFFE3E6FFu, 0xFFD8E9FFu, 0xFFF0E0FFu }, "stars.png",
                0xFF23295E, 0xFF4F5BD5, 0xFFCDD1F5, 0xFF61668F,
                0xFF9FA7EC, 0xFFD3D7F7, 0xFFE2E5FA, 0xFFF3F4FD, 0xFFF8F9FF, 0xFFDDE0F8,
                new[] { "🚀", "🪐", "🌙", "🛸", "☄️", "⭐" }),
            new ThemePalette("candy", "ארץ הממתקים 🍭", 200,
                new[] { 0xFFFFE6F0u, 0xFFFFF2DAu, 0xFFE6F7FFu }, null,
                0xFF8A3B5C, 0xFFF0599A, 0xFFF8C8DC, 0xFFA2667F,
                0xFFF7A6C8, 0xFFFACDE0, 0xFFFCDEEA, 0xFFFFF2F7, 0xFFFFF8FB, 0xFFF9D6E5,
                new[] { "🍭", "🍬", "🧁", "🍩", "🍓", "🍦" }),
            new ThemePalette("garden", "גן פרחים 🌸", 200,
                new[] { 0xFFF2FBEAu, 0xFFFFE9F1u, 0xFFFFF7DDu }, null,
                0xFF3D6B2F, 0xFFE96A9A, 0xFFD4EBC6, 0xFF6F8A64,
                0xFFF2A9C4, 0xFFF8CFDD, 0xFFFBE1EA, 0xFFFDF3F6, 0xFFFFF9FB, 0xFFF6D6E1,
                new[] { "🌸", "🌷", "🌼", "🦋", "🐝", "🌿" }),
            new ThemePalette("kittens", "חתלתולים 🐱", 200,
                new[] { 0xFFFFF1E6u, 0xFFFFE6EEu, 0xFFF1E9FFu }, null,
                0xFF7A4B3A, 0xFFF08A6B, 0xFFF6D3C6, 0xFF9A7568,
                0xFFF5B6A2, 0xFFF9D8CD, 0xFFFBE5DD, 0xFFFFF4EF, 0xFFFFF9F6, 0xFFF8DCD2,
                new[] { "🐱", "🐾", "🧶", "🐟", "💕" }),
            new ThemePalette("winter", "חורף ❄️", 200,
                new[] { 0xFFEEF6FFu, 0xFFE3EEFFu, 0xFFF5F0FFu }, null,
                0xFF2B4C7E, 0xFF5C9DE0, 0xFFCADDF3, 0xFF64809F,
                0xFFA6C8EE, 0xFFD2E3F7, 0xFFE1ECFA, 0xFFF3F8FE, 0xFFF8FBFF, 0xFFDCE8F8,
                new[] { "❄️", "⛄", "🌨️", "🧣", "✨" }),

            // Classic storybook characters. All illustrations are public domain (Wikimedia Commons).
            new ThemePalette("peter", "פיטר הארנב 🐰", 250,
                new[] { 0xFFF4F8EEu, 0xFFFCEFE3u, 0xFFE9F1F8u }, null,
                0xFF4E5B3C, 0xFF7FA05A, 0xFFDCE6CC, 0xFF7C8668,
                0xFFB9CF9E, 0xFFDCEACB, 0xFFE8F1DD, 0xFFF5F9EF, 0xFFFAFCF6, 0xFFE2ECD4,
                new[] { "🥕", "🌿", "🐰" }, new[] { "peter1.jpg", "peter2.jpg", "peter3.jpg" },
                "איורים: Beatrix Potter, 1902 · נחלת הכלל"),
            new ThemePalette("alice", "אליס בארץ הפלאות 🎩", 250,
                new[] { 0xFFEEF3FFu, 0xFFFFF1E8u, 0xFFF3EAFFu }, null,
                0xFF2F3E6E, 0xFF5470C8, 0xFFCFD9F2, 0xFF6C7799,
                0xFF9DB0E6, 0xFFD3DCF5, 0xFFE2E8F8, 0xFFF3F6FD, 0xFFF8FAFF, 0xFFDCE3F6,
                new[] { "🎩", "🫖", "⏰", "🃏" }, new[] { "alice1.jpg", "alice2.jpg" },
                "איורים: John Tenniel, 1890 · נחלת הכלל"),
            new ThemePalette("fairytale", "אגדות קלאסיות 🏰", 250,
                new[] { 0xFFFFF3E3u, 0xFFFFE8EEu, 0xFFF1EAFFu }, null,
                0xFF6B3A2A, 0xFFE07B39, 0xFFF4D5BE, 0xFF8E6B5C,
                0xFFF2B48A, 0xFFF7D9C3, 0xFFFAE6D8, 0xFFFDF4EE, 0xFFFFFAF6, 0xFFF5DCCB,
                new[] { "👑", "🏰", "👠", "✨" }, new[] { "fairy1.jpg", "fairy3.jpg" },
                "איורים: Walter Crane ו-Arthur Rackham · נחלת הכלל"),
        };

        public static ThemePalette Get(string id) => All.FirstOrDefault(t => t.Id == id) ?? All[0];

        public static void Apply(string id)
        {
            var theme = GameData.NightMode ? Night(Get(id)) : Get(id);
            var resources = Application.Current.Resources;

            var background = (LinearGradientBrush)resources["PageBackgroundBrush"];
            background.GradientStops.Clear();
            foreach (var stop in MakeStops(theme.Background))
                background.GradientStops.Add(stop);

            void Set(string key, uint argb) => ((SolidColorBrush)resources[key]).Color = ToColor(argb);

            Set("BerryBrush", theme.Dark);
            Set("RoseBrush", theme.Accent);
            Set("SoftPinkBrush", theme.Soft);
            Set("MutedTextBrush", theme.Muted);
            Set("DecoBrush", theme.Deco);
            Set("SelectedCellBrush", theme.Selected);
            Set("SameNumberBrush", theme.SameNumber);
            Set("RelatedCellBrush", theme.Related);
            Set("ButtonHoverBrush", theme.Hover);
            Set("ButtonPressedBrush", theme.Pressed);
            Set("ShadowBrush", (theme.Accent & 0x00FFFFFF) | 0x26000000);
            Set("OverlayBrush", (theme.Background[theme.Background.Length - 1] & 0x00FFFFFF) | 0xB3000000);
            Set("CardBrush", GameData.NightMode ? Blend(theme.Accent, NightBase, 0.86) : 0xFFFFFFFF);

            App.SetThemeBackground(theme.Pattern, theme.Decorations, theme.Pictures, MotionFor(theme.Id));
        }

        /// <summary>How a theme's background decorations move.</summary>
        private static string MotionFor(string themeId) => themeId switch
        {
            "winter" => "snow",
            "ocean" => "bubbles",
            "galaxy" or "space" => "twinkle",
            _ => "float",
        };

        // ---------- Night mode: a dark version of any theme, made from its own colors ----------

        private const uint NightBase = 0xFF17141F;

        private static ThemePalette Night(ThemePalette day)
        {
            uint card = Blend(day.Accent, NightBase, 0.86);
            return day with
            {
                Background = day.Background.Select(c => Blend(c, NightBase, 0.88)).ToArray(),
                Dark = Blend(day.Soft, 0xFFFFFFFF, 0.45),
                Accent = Blend(day.Accent, 0xFFFFFFFF, 0.15),
                Soft = Blend(day.Accent, NightBase, 0.6),
                Muted = Blend(day.Muted, 0xFFFFFFFF, 0.4),
                Selected = Blend(day.Accent, card, 0.55),
                SameNumber = Blend(day.Accent, card, 0.7),
                Related = Blend(day.Accent, card, 0.85),
                Hover = Blend(day.Accent, card, 0.8),
                Pressed = Blend(day.Accent, card, 0.6),
            };
        }

        /// <summary>Mixes two colors: amount 0 = the first, 1 = the second.</summary>
        private static uint Blend(uint a, uint b, double amount)
        {
            uint Mix(int shift) => (uint)Math.Round(((a >> shift) & 0xFF) * (1 - amount) + ((b >> shift) & 0xFF) * amount);
            return 0xFF000000 | (Mix(16) << 16) | (Mix(8) << 8) | Mix(0);
        }

        /// <summary>Evenly spaced gradient stops, also used for previews in the store.</summary>
        public static IEnumerable<GradientStop> MakeStops(uint[] colors)
        {
            for (int i = 0; i < colors.Length; i++)
                yield return new GradientStop { Color = ToColor(colors[i]), Offset = (double)i / (colors.Length - 1) };
        }

        public static Color ToColor(uint argb) =>
            Color.FromArgb((byte)(argb >> 24), (byte)(argb >> 16), (byte)(argb >> 8), (byte)argb);
    }
}
