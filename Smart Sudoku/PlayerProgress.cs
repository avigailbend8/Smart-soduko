using System.Collections.Generic;

namespace Smart_Sudoku
{
    /// <summary>
    /// Everything saved about one player. Stored in the database for registered users,
    /// and in the app's local settings for the guest.
    /// </summary>
    public sealed class PlayerProgress
    // אובייקט: PlayerProgress — כל מה שנשמר על שחקן אחד: מטבעות, קניות, כוכבים, שיאים והגדרות.
    // הכול נשמר בטבלאות של מסד הנתונים (גם של האורח). איזו טבלה שומרת מה — בקובץ Database.Progress.cs.
    {
        public const int StartingCoins = 100;
        // אובייקט: StartingCoins — כמה מטבעות מקבל שחקן חדש (100).

        public int Coins { get; set; } = StartingCoins;
        // אובייקט: Coins — כמה מטבעות יש לשחקן עכשיו.
        public int BonusHints { get; set; }
        // אובייקט: BonusHints — רמזים נוספים שהשחקן קנה או זכה בהם.
        public int BonusLives { get; set; }
        // אובייקט: BonusLives — לבבות נוספים שהשחקן קנה או זכה בהם.
        public string Theme { get; set; } = "pink";
        // אובייקט: Theme — ערכת העיצוב שהשחקן בחר (למשל "pink").
        public string Symbols { get; set; } = "numbers";
        // אובייקט: Symbols — הסמלים שמופיעים על הלוח במקום מספרים (מספרים, פירות, חיות...).
        public bool SoundOn { get; set; } = true;
        // אובייקט: SoundOn — אמת אם הצלילים פועלים.

        /// <summary>Store items the player owns (themes and symbol sets).</summary>
        public HashSet<string> Owned { get; set; } = new HashSet<string> { "pink", "numbers" };
        // אובייקט: Owned — כל הפריטים מהחנות שהשחקן כבר קנה (ערכות עיצוב וסמלים).

        /// <summary>Level id -> best stars.</summary>
        public Dictionary<int, int> Stars { get; set; } = new Dictionary<int, int>();
        // אובייקט: Stars — לכל שלב: מספר הכוכבים הכי טוב שהשחקן השיג בו.

        /// <summary>Level id -> best time in seconds.</summary>
        public Dictionary<int, int> BestTimes { get; set; } = new Dictionary<int, int>();
        // אובייקט: BestTimes — לכל שלב: הזמן הכי מהיר (בשניות) שבו השחקן פתר אותו.

        /// <summary>Difficulty -> highest unlocked level number.</summary>
        public Dictionary<int, int> Unlocked { get; set; } = new Dictionary<int, int>();
        // אובייקט: Unlocked — לכל רמת קושי: מה השלב הכי גבוה שכבר נפתח.

        /// <summary>
        /// Other saved settings by name (night mode, board frame, daily challenge, lucky wheel, a game in progress...).
        /// Kept as text so new features don't need new database columns.
        /// </summary>
        public Dictionary<string, string> Extras { get; set; } = new Dictionary<string, string>();
        // אובייקט: Extras — עוד הגדרות לפי שם (מצב לילה, מסגרת, אתגר יומי, גלגל המזל, משחק שמור...).
        // כל אחת נשמרת בטבלה ובעמודה משלה במסד הנתונים (סטטיסטיקות, העדפות, משימות, מסע, משחק שמור).

        public bool HasAnyProgress => Stars.Count > 0 || Coins != StartingCoins || Owned.Count > 2;
        // אובייקט: HasAnyProgress — אמת אם השחקן כבר התקדם במשהו (כוכבים, מטבעות או קניות).
    }
}
