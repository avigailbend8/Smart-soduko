using System.Collections.Generic;
using System.Linq;

namespace Smart_Sudoku
{
    /// <param name="ThemeId">The theme this set matches. Buying that theme also gives the set,
    /// and choosing the theme switches the board to it.</param>
    public sealed record SymbolSet(string Id, string Name, int Price, string[] Symbols, string? ThemeId = null);
    // אובייקט: SymbolSet — ערכת סמלים: 9 סמלים שמופיעים על הלוח במקום הספרות 1–9, עם שם ומחיר.
    // ThemeId = ערכת העיצוב שהסמלים שייכים לה (קנייה של העיצוב נותנת גם את הסמלים).

    /// <summary>
    /// What is shown on the board instead of the digits 1-9.
    /// </summary>
    public static class SymbolSets
    // אובייקט: SymbolSets — כל ערכות הסמלים במשחק.
    {
        public static readonly IReadOnlyList<SymbolSet> All = new[]
        // אובייקט: All — הרשימה של כל ערכות הסמלים: מספרים, פירות, חיות, ממתקים, ועוד ערכות שמגיעות עם עיצובים.
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
        // פעולה: Get — מחזירה ערכת סמלים לפי השם שלה. אם לא נמצאה — מחזירה את המספרים הרגילים.

        public static SymbolSet Current => Get(GameData.SymbolsId);
        // אובייקט: Current — ערכת הסמלים שהשחקן בחר עכשיו.

        /// <summary>The set that matches a theme, if it has one.</summary>
        public static SymbolSet? ForTheme(string themeId) => All.FirstOrDefault(s => s.ThemeId == themeId);
        // פעולה: ForTheme — מחזירה את ערכת הסמלים שמתאימה לעיצוב מסוים (אם יש כזאת).
    }
}
