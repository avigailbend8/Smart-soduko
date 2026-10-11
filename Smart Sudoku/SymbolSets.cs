using System.Collections.Generic;
using System.Linq;

namespace Smart_Sudoku
{
    /// <param name="ThemeId">The theme this set matches. Buying that theme also gives the set,
    /// and choosing the theme switches the board to it.</param>
    public sealed record SymbolSet(string Id, string Name, int Price, string[] Symbols, string? ThemeId = null);

    /// <summary>
    /// What is shown on the board instead of the digits 1-9.
    /// </summary>
    public static class SymbolSets
    {
        public static readonly IReadOnlyList<SymbolSet> All = new[]
        {
            new SymbolSet("numbers", "מספרים", 0, new[] { "1", "2", "3", "4", "5", "6", "7", "8", "9" }),
            new SymbolSet("fruits", "פירות", 80, new[] { "🍓", "🍒", "🍑", "🍋", "🍏", "🍇", "🍉", "🥝", "🍊" }),
            new SymbolSet("animals", "חיות", 80, new[] { "🐱", "🐶", "🐰", "🦊", "🐼", "🐨", "🐸", "🐷", "🐵" }),
            new SymbolSet("sweets", "ממתקים", 80, new[] { "🍩", "🍪", "🧁", "🍭", "🍬", "🍫", "🍰", "🍦", "🎂" }, "candy"),

            // Sets that come with a theme
            new SymbolSet("rainbow-set", "עיגולי קשת", 0, new[] { "🔴", "🟠", "🟡", "🟢", "🔵", "🟣", "🟤", "⚫", "⚪" }, "rainbow"),
            new SymbolSet("sunset-set", "חוף בשקיעה", 0, new[] { "🌅", "☀️", "🌴", "🦩", "🐚", "🍹", "⛱️", "🌊", "🌙" }, "sunset"),
            new SymbolSet("galaxy-set", "כוכבים", 0, new[] { "🪐", "🌙", "⭐", "🌟", "💫", "☄️", "🌌", "✨", "🔭" }, "galaxy"),
            new SymbolSet("hero-set", "גיבורי-על", 0, new[] { "⚡", "💥", "🦸", "🦹", "🛡️", "🕸️", "💪", "⭐", "🏆" }, "hero"),
            new SymbolSet("princess-set", "ארמון הנסיכות", 0, new[] { "👑", "👸", "🏰", "💎", "🎀", "🦢", "🪞", "💍", "🌹" }, "princess"),
            new SymbolSet("unicorn-set", "ארץ החד-קרן", 0, new[] { "🦄", "🌈", "☁️", "⭐", "💜", "✨", "🌙", "🍭", "🦋" }, "unicorn"),
            new SymbolSet("ocean-set", "חיות ים", 0, new[] { "🐠", "🐚", "🐬", "🐙", "🦀", "🐳", "🦈", "🐢", "🐡" }, "ocean"),
            new SymbolSet("space-set", "מסע בחלל", 0, new[] { "🚀", "🪐", "🌙", "🛸", "☄️", "⭐", "👽", "🌍", "☀️" }, "space"),
            new SymbolSet("garden-set", "גן פורח", 0, new[] { "🌸", "🌷", "🌼", "🦋", "🐝", "🌿", "🌻", "🐞", "🍄" }, "garden"),
            new SymbolSet("kittens-set", "עולם החתולים", 0, new[] { "🐱", "🐾", "🧶", "🐟", "🥛", "🐭", "🎀", "🧸", "🛏️" }, "kittens"),
            new SymbolSet("winter-set", "יום חורף", 0, new[] { "❄️", "⛄", "🧣", "🧤", "☕", "🎿", "🌨️", "🦌", "🧊" }, "winter"),
            new SymbolSet("peter-set", "הגינה של פיטר", 0, new[] { "🐰", "🥕", "🥬", "🌿", "🧺", "🦔", "🐦", "🌼", "🍓" }, "peter"),
            new SymbolSet("alice-set", "ארץ הפלאות", 0, new[] { "🎩", "🐇", "🐛", "🍄", "🫖", "⏰", "🃏", "🌹", "🐱" }, "alice"),
            new SymbolSet("fairytale-set", "אגדות", 0, new[] { "👑", "🏰", "👠", "🎃", "🐭", "🪄", "💍", "🍎", "🐉" }, "fairytale"),
        };

        public static SymbolSet Get(string id) => All.FirstOrDefault(s => s.Id == id) ?? All[0];

        public static SymbolSet Current => Get(GameData.SymbolsId);

        /// <summary>The set that matches a theme, if it has one.</summary>
        public static SymbolSet? ForTheme(string themeId) => All.FirstOrDefault(s => s.ThemeId == themeId);
    }
}
