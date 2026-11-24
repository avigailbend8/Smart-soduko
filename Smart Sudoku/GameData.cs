using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Storage;

namespace Smart_Sudoku
{
    /// <summary>
    /// The current player's progress. Registered users are saved in the database;
    /// the guest is saved in this computer's app settings.
    /// </summary>
    public static class GameData
    // אובייקט: GameData — ההתקדמות של השחקן הנוכחי, והמקום היחיד שבו משנים מטבעות, קניות, כוכבים והגדרות.
    // משתמש רשום נשמר במסד הנתונים, ואורח נשמר בהגדרות של האפליקציה במחשב.
    {
        private static PlayerProgress progress = new PlayerProgress();
        // אובייקט: progress — כל המידע על השחקן שמשחק עכשיו (מטבעות, כוכבים, קניות...).
        private static int? userId;
        // אובייקט: userId — המספר של המשתמש המחובר, או null כשמשחקים כאורח.

        public static int Coins => progress.Coins;
        // אובייקט: Coins — כמה מטבעות יש לשחקן.
        public static int BonusHints => progress.BonusHints;
        // אובייקט: BonusHints — כמה רמזים נוספים יש לשחקן.
        public static int BonusLives => progress.BonusLives;
        // אובייקט: BonusLives — כמה לבבות נוספים יש לשחקן.
        public static string ThemeId => progress.Theme;
        // אובייקט: ThemeId — ערכת העיצוב שנבחרה.
        public static string SymbolsId => progress.Symbols;
        // אובייקט: SymbolsId — ערכת הסמלים שנבחרה ללוח.
        public static bool SoundOn => progress.SoundOn;
        // אובייקט: SoundOn — אמת אם הצלילים פועלים.

        public static void LoadUser(int id)
        // פעולה: LoadUser — טוענת את ההתקדמות של משתמש רשום ממסד הנתונים.
        {
            userId = id;
            progress = Database.LoadProgress(id);
        }

        public static void LoadGuest()
        // פעולה: LoadGuest — טוענת את ההתקדמות של האורח ממסד הנתונים (האורח הוא שורה בטבלת המשתמשים).
        {
            userId = Database.GuestUserId();
            progress = Database.LoadProgress(userId.Value);
        }

        public static int GetStars(int levelId) => progress.Stars.TryGetValue(levelId, out int s) ? s : 0;
        // פעולה: GetStars — מחזירה כמה כוכבים השחקן השיג בשלב (0 אם עוד לא פתר אותו).

        /// <summary>Best time in seconds, or 0 if the level was never solved.</summary>
        public static int GetBestTime(int levelId) => progress.BestTimes.TryGetValue(levelId, out int t) ? t : 0;
        // פעולה: GetBestTime — מחזירה את הזמן הכי טוב בשלב, בשניות (0 אם עוד לא פתר אותו).

        /// <summary>Highest unlocked level number in a difficulty (level 1 is always open).</summary>
        public static int UnlockedLevel(int difficulty) =>
        // פעולה: UnlockedLevel — מחזירה את השלב הכי גבוה שפתוח ברמת קושי מסוימת (שלב 1 תמיד פתוח).
            progress.Unlocked.TryGetValue(difficulty, out int level) ? Math.Max(1, level) : 1;

        // ---------- Coin rewards ----------

        /// <summary>Every reward is doubled.</summary>
        public const int CoinMultiplier = 2;
        // אובייקט: CoinMultiplier — כל פרס במטבעות מוכפל פי 2.

        /// <summary>Higher levels and harder difficulties pay more.</summary>
        public static int LevelCoins(int levelId) =>
        // פעולה: LevelCoins — כמה מטבעות מקבלים על השלב עצמו. שלבים גבוהים ורמות קשות נותנים יותר.
            10 + Levels.NumberOf(levelId) * 2 + Levels.DifficultyOf(levelId) * 20;

        public static int StarCoins(int stars) => stars * 5;
        // פעולה: StarCoins — כמה מטבעות מקבלים על הכוכבים (5 לכל כוכב).

        /// <summary>Time to beat for the speed bonus. Harder levels get more time.</summary>
        public static int TargetSeconds(int levelId) =>
        // פעולה: TargetSeconds — הזמן שצריך לנצח כדי לקבל בונוס מהירות. בשלבים קשים יש יותר זמן.
            240 + Levels.NumberOf(levelId) * 15 + Levels.DifficultyOf(levelId) * 120;

        /// <summary>1 coin for every 5 seconds faster than the target time.</summary>
        public static int SpeedCoins(int levelId, int seconds) => Math.Max(0, TargetSeconds(levelId) - seconds) / 5;
        // פעולה: SpeedCoins — בונוס מהירות: מטבע אחד על כל 5 שניות שהשחקן היה מהיר יותר מהזמן היעד.

        /// <summary>Coins for solving a board: level + stars + speed, doubled. Used by levels and duels.</summary>
        public static int RewardCoins(int levelId, int earnedStars, int seconds) =>
        // פעולה: RewardCoins — סך כל המטבעות על פתרון לוח: שלב + כוכבים + מהירות, והכול כפול 2.
            (LevelCoins(levelId) + StarCoins(earnedStars) + SpeedCoins(levelId, seconds)) * CoinMultiplier;

        /// <summary>Saves a solved level and returns how many coins it earned.</summary>
        public static int RecordWin(int levelId, int earnedStars, int seconds)
        // פעולה: RecordWin — שומרת ניצחון בשלב: כוכבים, זמן שיא, פותחת את השלב הבא ומוסיפה מטבעות. מחזירה כמה מטבעות הרוויח.
        {
            int earnedCoins = RewardCoins(levelId, earnedStars, seconds);
            // אובייקט: earnedCoins — כמה מטבעות השחקן הרוויח בשלב הזה.

            progress.Stars[levelId] = Math.Max(GetStars(levelId), earnedStars);
            int best = GetBestTime(levelId);
            // אובייקט: best — הזמן הכי טוב הקודם בשלב.
            if (best == 0 || seconds < best)
                progress.BestTimes[levelId] = seconds;

            int difficulty = Levels.DifficultyOf(levelId);
            // אובייקט: difficulty — רמת הקושי של השלב.
            int next = Math.Min(Levels.PerDifficulty, Levels.NumberOf(levelId) + 1);
            // אובייקט: next — השלב הבא שצריך להיפתח (לא יותר מ-20).
            progress.Unlocked[difficulty] = Math.Max(UnlockedLevel(difficulty), next);

            progress.Coins += earnedCoins;
            Save();
            return earnedCoins;
        }

        // ---------- Store ----------

        /// <summary>Owned items, counting a theme's symbol set as owned once the theme is.</summary>
        public static bool Owns(string itemId)
        // פעולה: Owns — בודקת אם השחקן כבר קנה פריט. ערכת סמלים של עיצוב נחשבת שלו אם יש לו את העיצוב.
        {
            if (progress.Owned.Contains(itemId))
                return true;

            // A symbol set that comes with a theme is owned when its theme is owned
            SymbolSet set = SymbolSets.Get(itemId);
            // אובייקט: set — ערכת הסמלים שמתאימה לשם הפריט.
            return set.Id == itemId && set.ThemeId != null && progress.Owned.Contains(set.ThemeId);
        }

        public static bool Buy(string itemId, int price)
        // פעולה: Buy — קונה פריט מהחנות: מורידה מטבעות ומוסיפה אותו לפריטים של השחקן. עיצוב מגיע גם עם הסמלים שלו.
        {
            if (Owns(itemId))
                return true;
            if (!Spend(price))
                return false;
            progress.Owned.Add(itemId);

            // A theme comes with its matching board symbols
            if (SymbolSets.ForTheme(itemId) is SymbolSet matching)
                progress.Owned.Add(matching.Id);

            Save();
            return true;
        }

        /// <summary>Coins won outside the levels (for example in a duel).</summary>
        public static void AddCoins(int amount)
        // פעולה: AddCoins — מוסיפה מטבעות שהרוויחו מחוץ לשלבים (למשל בדו-קרב או בהישג).
        {
            progress.Coins += amount;
            Save();
        }

        /// <summary>Gives a store item for free (a lucky wheel prize). A theme's board symbols count as owned through Owns().</summary>
        public static void GiveItem(string itemId)
        // פעולה: GiveItem — נותנת פריט מהחנות במתנה (פרס מגלגל המזל).
        {
            progress.Owned.Add(itemId);
            Save();
        }

        public static bool BuyHints(int price, int count)
        // פעולה: BuyHints — קונה רמזים: מורידה מטבעות ומוסיפה רמזים.
        {
            if (!Spend(price))
                return false;
            progress.BonusHints += count;
            Save();
            return true;
        }

        public static bool BuyLives(int price, int count)
        // פעולה: BuyLives — קונה לבבות: מורידה מטבעות ומוסיפה לבבות.
        {
            if (!Spend(price))
                return false;
            progress.BonusLives += count;
            Save();
            return true;
        }

        /// <summary>Pays for something. Returns false if there are not enough coins.</summary>
        public static bool Spend(int price)
        // פעולה: Spend — משלמת מטבעות. מחזירה false אם אין מספיק.
        {
            if (progress.Coins < price)
                return false;
            progress.Coins -= price;
            Save();
            return true;
        }

        public static bool UseBonusHint()
        // פעולה: UseBonusHint — משתמשת ברמז נוסף אחד (אם יש).
        {
            if (progress.BonusHints == 0)
                return false;
            progress.BonusHints--;
            Save();
            return true;
        }

        public static bool UseBonusLife()
        // פעולה: UseBonusLife — משתמשת בלב נוסף אחד (אם יש).
        {
            if (progress.BonusLives == 0)
                return false;
            progress.BonusLives--;
            Save();
            return true;
        }

        // ---------- Secret gift code ----------

        /// <summary>The secret codes for the "White Twix" button, and how many coins each one gives.</summary>
        private static readonly Dictionary<string, int> GiftCodes = new Dictionary<string, int>
        // אובייקט: GiftCodes — הקודים הסודיים לכפתור "טוויקס לבן", וכמה מטבעות כל קוד נותן.
        {
            ["0109"] = 1000,
            ["6767"] = 500,
            ["נומי וגולי"] = 100,
        };

        /// <summary>Gives the code's coins every time a right code is entered. Returns the coins given (0 = wrong code).</summary>
        public static int RedeemGiftCode(string code)
        // פעולה: RedeemGiftCode — בודקת קוד סודי. אם הוא נכון — נותנת את המטבעות שלו. מחזירה כמה מטבעות ניתנו (0 = קוד שגוי).
        {
            if (!GiftCodes.TryGetValue(code.Trim(), out int coins))
                return 0;

            progress.Coins += coins;
            Save();
            return coins;
        }

        // ---------- Extra settings ----------

        public static string GetExtra(string key, string fallback = "") =>
        // פעולה: GetExtra — קוראת הגדרה נוספת לפי השם שלה. אם אין כזאת — מחזירה את ברירת המחדל.
            progress.Extras.TryGetValue(key, out string? value) ? value : fallback;

        public static void SetExtra(string key, string? value)
        // פעולה: SetExtra — שומרת הגדרה נוספת לפי שם. אם הערך null — מוחקת אותה.
        {
            if (value == null)
                progress.Extras.Remove(key);
            else
                progress.Extras[key] = value;
            Save();
        }

        /// <summary>Show wrong numbers in red and take a heart for each (on by default).</summary>
        public static bool ShowMistakes
        // אובייקט: ShowMistakes — האם להראות מספרים שגויים באדום ולהוריד לב על כל טעות (פועל מההתחלה).
        {
            get => GetExtra("showMistakes", "1") == "1";
            set => SetExtra("showMistakes", value ? "1" : "0");
        }

        /// <summary>Background music (off by default).</summary>
        public static bool MusicOn
        // אובייקט: MusicOn — האם מוזיקת הרקע פועלת (כבויה מההתחלה).
        {
            get => GetExtra("music") == "1";
            set => SetExtra("music", value ? "1" : "0");
        }

        public static bool NightMode
        // אובייקט: NightMode — האם מצב לילה (צבעים כהים) פועל.
        {
            get => GetExtra("night") == "1";
            set => SetExtra("night", value ? "1" : "0");
        }

        public static string FrameId
        // אובייקט: FrameId — המסגרת שנבחרה ללוח.
        {
            get => GetExtra("frame", "classic");
            set => SetExtra("frame", value);
        }

        /// <summary>A level left in the middle, or null.</summary>
        public static SavedGame? SavedGame
        // אובייקט: SavedGame — שלב שעזבו באמצע (או null). נשמר כטקסט JSON בתוך ההגדרות הנוספות.
        {
            get
            {
                string json = GetExtra("savedGame");
                // אובייקט: json — הטקסט השמור של המשחק.
                if (json.Length == 0)
                    return null;
                try
                {
                    return System.Text.Json.JsonSerializer.Deserialize(json, AppJson.Default.SavedGame);
                }
                catch (System.Text.Json.JsonException)
                {
                    return null;
                }
            }
            set => SetExtra("savedGame", value == null ? null : System.Text.Json.JsonSerializer.Serialize(value, AppJson.Default.SavedGame));
        }

        // ---------- Achievements ----------

        /// <summary>Unlocks an achievement once. Returns true the first time.</summary>
        public static bool Unlock(string achievementId)
        // פעולה: Unlock — פותחת הישג פעם אחת בלבד. מחזירה true רק בפעם הראשונה.
        {
            string marker = "ach-" + achievementId;
            // אובייקט: marker — "סימון" שנשמר ברשימת הפריטים כדי לזכור שההישג כבר נפתח.
            if (progress.Owned.Contains(marker))
                return false;
            progress.Owned.Add(marker);
            Save();
            return true;
        }

        public static bool HasAchievement(string achievementId) => progress.Owned.Contains("ach-" + achievementId);
        // פעולה: HasAchievement — בודקת אם השחקן כבר השיג הישג מסוים.

        /// <summary>A counter kept in the extras (for achievements like "win 5 duels").</summary>
        public static int AddToCounter(string name, int amount = 1)
        // פעולה: AddToCounter — מוסיפה למונה (למשל "ניצחונות בדו-קרב") ומחזירה את הערך החדש.
        {
            int value = int.TryParse(GetExtra("count." + name, "0"), out int v) ? v : 0;
            // אובייקט: value — הערך של המונה.
            value += amount;
            SetExtra("count." + name, value.ToString());
            return value;
        }

        public static int Counter(string name) => int.TryParse(GetExtra("count." + name, "0"), out int v) ? v : 0;
        // פעולה: Counter — מחזירה את הערך של מונה.

        public static int SolvedLevelsCount => progress.Stars.Count;
        // אובייקט: SolvedLevelsCount — כמה שלבים השחקן כבר פתר.

        public static int ThreeStarLevelsCount => progress.Stars.Values.Count(s => s == 3);
        // אובייקט: ThreeStarLevelsCount — בכמה שלבים השחקן קיבל 3 כוכבים.

        // ---------- Settings ----------

        public static void SelectTheme(string id)
        // פעולה: SelectTheme — שומרת את ערכת העיצוב שהשחקן בחר.
        {
            progress.Theme = id;
            Save();
        }

        public static void SelectSymbols(string id)
        // פעולה: SelectSymbols — שומרת את ערכת הסמלים שהשחקן בחר.
        {
            progress.Symbols = id;
            Save();
        }

        public static void SetSound(bool on)
        // פעולה: SetSound — מפעילה או מכבה את הצלילים.
        {
            progress.SoundOn = on;
            Save();
        }

        private static void Save()
        // פעולה: Save — שומרת את ההתקדמות במסד הנתונים (גם של משתמש רשום וגם של האורח).
        {
            if (userId is int id)
                Database.SaveProgress(id, progress);
        }
    }

    /// <summary>
    /// Where older versions kept the guest's progress (the app's local settings, "guest.*" keys).
    /// It is read once, moved into the database, and then cleared.
    /// </summary>
    public static class GuestStore
    // אובייקט: GuestStore — המקום שבו גרסאות קודמות שמרו את האורח (בהגדרות של האפליקציה, מפתחות "guest.").
    // קוראים אותו פעם אחת, מעבירים למסד הנתונים, ומוחקים.
    {
        private const string Prefix = "guest.";
        // אובייקט: Prefix — התחילית של שמות ההגדרות של האורח.

        private static IDictionary<string, object> Values => ApplicationData.Current.LocalSettings.Values;
        // אובייקט: Values — ההגדרות המקומיות של האפליקציה במחשב.

        public static PlayerProgress Load()
        // פעולה: Load — טוענת את כל ההתקדמות של האורח מההגדרות. אם משהו חסר — משתמשת בערך התחלתי.
        {
            var progress = new PlayerProgress
            // אובייקט: progress — ההתקדמות של האורח שנבנית מתוך ההגדרות.
            {
                Coins = Get("coins", PlayerProgress.StartingCoins),
                BonusHints = Get("bonusHints", 0),
                BonusLives = Get("bonusLives", 0),
                Theme = Get("theme", "pink"),
                Symbols = Get("symbols", "numbers"),
                SoundOn = Get("sound", true),
                Owned = new HashSet<string>(Get("owned", "pink,numbers").Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)),
                Stars = ParseMap(Get("stars", "")),
                BestTimes = ParseMap(Get("times", "")),
                Unlocked = ParseMap(Get("unlockedByDifficulty", "")),
                Extras = ParseExtras(Get("extras", "")),
            };

            // Older versions saved a single unlocked level, which belongs to the easy difficulty
            if (progress.Unlocked.Count == 0 && Values.TryGetValue(Prefix + "unlocked", out object? old) && old is int oldLevel)
                progress.Unlocked[0] = oldLevel;

            return progress;
        }

        /// <summary>Removes the old guest settings, after they were moved into the database.</summary>
        public static void Clear()
        // פעולה: Clear — מוחקת את ההגדרות הישנות של האורח, אחרי שהן הועברו למסד הנתונים.
        {
            foreach (string key in new List<string>(Values.Keys))
            {
                if (key.StartsWith(Prefix))
                    Values.Remove(key);
            }
        }

        private static Dictionary<string, string> ParseExtras(string json)
        // פעולה: ParseExtras — הופכת את הטקסט השמור של ההגדרות הנוספות בחזרה ל"מילון". אם הטקסט שבור — מחזירה מילון ריק.
        {
            if (json.Length == 0)
                return new Dictionary<string, string>();
            try
            {
                return System.Text.Json.JsonSerializer.Deserialize(json, AppJson.Default.DictionaryStringString) ?? new Dictionary<string, string>();
            }
            catch (System.Text.Json.JsonException)
            {
                return new Dictionary<string, string>();
            }
        }

        private static T Get<T>(string key, T fallback) =>
        // פעולה: Get — קוראת הגדרה אחת של האורח. אם היא לא קיימת — מחזירה את ברירת המחדל.
            Values.TryGetValue(Prefix + key, out object? value) && value is T typed ? typed : fallback;

        // A dictionary is saved as text: {1:3, 2:2} <-> "1:3;2:2"
        private static Dictionary<int, int> ParseMap(string text)
        // פעולה: ParseMap — הופכת טקסט כמו "1:3;2:2" ל"מילון" של מספרים (שלב → כוכבים).
        {
            var map = new Dictionary<int, int>();
            // אובייקט: map — המילון שנבנה מהטקסט.
            foreach (string pair in text.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
            {
                string[] parts = pair.Split(':');
                // אובייקט: parts — שני החלקים של זוג אחד (לפני ואחרי הנקודתיים).
                if (parts.Length == 2)
                    map[int.Parse(parts[0])] = int.Parse(parts[1]);
            }
            return map;
        }

        private static string FormatMap(Dictionary<int, int> map)
        // פעולה: FormatMap — הופכת "מילון" של מספרים לטקסט כמו "1:3;2:2", כדי לשמור אותו.
        {
            var pairs = new List<string>();
            // אובייקט: pairs — רשימת הזוגות כטקסט.
            foreach (var item in map)
                pairs.Add(item.Key + ":" + item.Value);
            return string.Join(";", pairs);
        }
    }
}
