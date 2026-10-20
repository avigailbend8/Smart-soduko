using System.Collections.Generic;
using System.Linq;

namespace Smart_Sudoku
{
    public sealed record Achievement(string Id, string Emoji, string Name, string Description, int RewardCoins);

    /// <summary>
    /// Badges for milestones. Unlocking one gives coins and shows a small notification.
    /// </summary>
    public static class Achievements
    {
        public static readonly IReadOnlyList<Achievement> All = new[]
        {
            new Achievement("first-win", "🌟", "צעד ראשון", "לפתור שלב ראשון", 50),
            new Achievement("perfect", "💎", "בלי טעויות", "לפתור שלב בלי טעויות ובלי רמזים", 50),
            new Achievement("perfect10", "👑", "מושלמת ×10", "לקבל 3 כוכבים ב-10 שלבים", 150),
            new Achievement("speedy", "⚡", "ברק", "לפתור שלב בפחות מ-2 דקות", 100),
            new Achievement("easy-done", "🌱", "אלופת הקל", "לסיים את כל 20 השלבים בדרגה קלה", 200),
            new Achievement("expert", "🔥", "מומחית", "לפתור שלב בדרגת מומחה", 150),
            new Achievement("daily", "📅", "אתגר ראשון", "לפתור אתגר יומי", 50),
            new Achievement("streak7", "🗓️", "שבוע ברצף", "לפתור אתגר יומי 7 ימים ברצף", 300),
            new Achievement("duel-win", "⚔️", "ניצחון בדו-קרב", "לנצח בדו-קרב ברשת", 100),
            new Achievement("duel-5", "🏆", "אלופת הזירה", "לנצח 5 דו-קרבות", 250),
            new Achievement("shopper", "🛍️", "קנייה ראשונה", "לקנות משהו בחנות", 30),
            new Achievement("wheel", "🎡", "בר מזל", "לסובב את גלגל המזל", 20),
            new Achievement("mini", "🧩", "סודוקו קטן", "לפתור לוח 4×4 או 6×6", 50),
            new Achievement("night", "🌙", "ינשוף לילה", "להפעיל מצב לילה", 20),
        };

        /// <summary>Unlocks an achievement (once), pays its coins and shows a notification.</summary>
        public static void Unlock(string id)
        {
            var achievement = All.FirstOrDefault(a => a.Id == id);
            if (achievement == null || !GameData.Unlock(id))
                return;

            GameData.AddCoins(achievement.RewardCoins);
            Sounds.Play("coin");
            App.ShowToast($"{achievement.Emoji} הישג חדש: {achievement.Name}!  +{achievement.RewardCoins} 🪙");
        }
    }
}
