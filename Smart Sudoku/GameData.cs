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
    {
        private static PlayerProgress progress = new PlayerProgress();
        private static int? userId;

        public static int Coins => progress.Coins;
        public static int BonusHints => progress.BonusHints;
        public static int BonusLives => progress.BonusLives;
        public static string ThemeId => progress.Theme;
        public static string SymbolsId => progress.Symbols;
        public static bool SoundOn => progress.SoundOn;

        public static void LoadUser(int id)
        {
            userId = id;
            progress = Database.LoadProgress(id);
        }

        public static void LoadGuest()
        {
            userId = null;
            progress = GuestStore.Load();
        }

        public static int GetStars(int levelId) => progress.Stars.TryGetValue(levelId, out int s) ? s : 0;

        /// <summary>Best time in seconds, or 0 if the level was never solved.</summary>
        public static int GetBestTime(int levelId) => progress.BestTimes.TryGetValue(levelId, out int t) ? t : 0;

        /// <summary>Highest unlocked level number in a difficulty (level 1 is always open).</summary>
        public static int UnlockedLevel(int difficulty) =>
            progress.Unlocked.TryGetValue(difficulty, out int level) ? Math.Max(1, level) : 1;

        // ---------- Coin rewards ----------

        /// <summary>Every reward is doubled.</summary>
        public const int CoinMultiplier = 2;

        /// <summary>Higher levels and harder difficulties pay more.</summary>
        public static int LevelCoins(int levelId) =>
            10 + Levels.NumberOf(levelId) * 2 + Levels.DifficultyOf(levelId) * 20;

        public static int StarCoins(int stars) => stars * 5;

        /// <summary>Time to beat for the speed bonus. Harder levels get more time.</summary>
        public static int TargetSeconds(int levelId) =>
            240 + Levels.NumberOf(levelId) * 15 + Levels.DifficultyOf(levelId) * 120;

        /// <summary>1 coin for every 5 seconds faster than the target time.</summary>
        public static int SpeedCoins(int levelId, int seconds) => Math.Max(0, TargetSeconds(levelId) - seconds) / 5;

        /// <summary>Coins for solving a board: level + stars + speed, doubled. Used by levels and duels.</summary>
        public static int RewardCoins(int levelId, int earnedStars, int seconds) =>
            (LevelCoins(levelId) + StarCoins(earnedStars) + SpeedCoins(levelId, seconds)) * CoinMultiplier;

        /// <summary>Saves a solved level and returns how many coins it earned.</summary>
        public static int RecordWin(int levelId, int earnedStars, int seconds)
        {
            int earnedCoins = RewardCoins(levelId, earnedStars, seconds);

            progress.Stars[levelId] = Math.Max(GetStars(levelId), earnedStars);
            int best = GetBestTime(levelId);
            if (best == 0 || seconds < best)
                progress.BestTimes[levelId] = seconds;

            int difficulty = Levels.DifficultyOf(levelId);
            int next = Math.Min(Levels.PerDifficulty, Levels.NumberOf(levelId) + 1);
            progress.Unlocked[difficulty] = Math.Max(UnlockedLevel(difficulty), next);

            progress.Coins += earnedCoins;
            Save();
            return earnedCoins;
        }

        // ---------- Store ----------

        /// <summary>Owned items, counting a theme's symbol set as owned once the theme is.</summary>
        public static bool Owns(string itemId) =>
            progress.Owned.Contains(itemId)
            || SymbolSets.All.Any(s => s.Id == itemId && s.ThemeId != null && progress.Owned.Contains(s.ThemeId));

        public static bool Buy(string itemId, int price)
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
        {
            progress.Coins += amount;
            Save();
        }

        /// <summary>Gives a store item for free (a lucky wheel prize). A theme also brings its board symbols.</summary>
        public static void GiveItem(string itemId)
        {
            progress.Owned.Add(itemId);
            Save();
        }

        public static bool BuyHints(int price, int count)
        {
            if (!Spend(price))
                return false;
            progress.BonusHints += count;
            Save();
            return true;
        }

        public static bool BuyLives(int price, int count)
        {
            if (!Spend(price))
                return false;
            progress.BonusLives += count;
            Save();
            return true;
        }

        /// <summary>Pays for something. Returns false if there are not enough coins.</summary>
        public static bool Spend(int price)
        {
            if (progress.Coins < price)
                return false;
            progress.Coins -= price;
            Save();
            return true;
        }

        public static bool UseBonusHint()
        {
            if (progress.BonusHints == 0)
                return false;
            progress.BonusHints--;
            Save();
            return true;
        }

        public static bool UseBonusLife()
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
        {
            ["0109"] = 1000,
            ["6767"] = 500,
            ["נומי וגולי"] = 100,
        };

        /// <summary>Gives the code's coins every time a right code is entered. Returns the coins given (0 = wrong code).</summary>
        public static int RedeemGiftCode(string code)
        {
            if (!GiftCodes.TryGetValue(code.Trim(), out int coins))
                return 0;

            progress.Coins += coins;
            Save();
            return coins;
        }

        // ---------- Extra settings ----------

        public static string GetExtra(string key, string fallback = "") =>
            progress.Extras.TryGetValue(key, out string? value) ? value : fallback;

        public static void SetExtra(string key, string? value)
        {
            if (value == null)
                progress.Extras.Remove(key);
            else
                progress.Extras[key] = value;
            Save();
        }

        /// <summary>Show wrong numbers in red and take a heart for each (on by default).</summary>
        public static bool ShowMistakes
        {
            get => GetExtra("showMistakes", "1") == "1";
            set => SetExtra("showMistakes", value ? "1" : "0");
        }

        public static bool NightMode
        {
            get => GetExtra("night") == "1";
            set => SetExtra("night", value ? "1" : "0");
        }

        public static string FrameId
        {
            get => GetExtra("frame", "classic");
            set => SetExtra("frame", value);
        }

        /// <summary>A level left in the middle, or null.</summary>
        public static SavedGame? SavedGame
        {
            get
            {
                string json = GetExtra("savedGame");
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
        {
            string marker = "ach-" + achievementId;
            if (progress.Owned.Contains(marker))
                return false;
            progress.Owned.Add(marker);
            Save();
            return true;
        }

        public static bool HasAchievement(string achievementId) => progress.Owned.Contains("ach-" + achievementId);

        /// <summary>A counter kept in the extras (for achievements like "win 5 duels").</summary>
        public static int AddToCounter(string name, int amount = 1)
        {
            int value = int.TryParse(GetExtra("count." + name, "0"), out int v) ? v : 0;
            value += amount;
            SetExtra("count." + name, value.ToString());
            return value;
        }

        public static int Counter(string name) => int.TryParse(GetExtra("count." + name, "0"), out int v) ? v : 0;

        public static int SolvedLevelsCount => progress.Stars.Count;

        public static int ThreeStarLevelsCount => progress.Stars.Values.Count(s => s == 3);

        // ---------- Settings ----------

        public static void SelectTheme(string id)
        {
            progress.Theme = id;
            Save();
        }

        public static void SelectSymbols(string id)
        {
            progress.Symbols = id;
            Save();
        }

        public static void SetSound(bool on)
        {
            progress.SoundOn = on;
            Save();
        }

        private static void Save()
        {
            if (userId is int id)
                Database.SaveProgress(id, progress);
            else
                GuestStore.Save(progress);
        }
    }

    /// <summary>
    /// The guest's progress, kept in the app's local settings ("guest.*" keys).
    /// </summary>
    public static class GuestStore
    {
        private const string Prefix = "guest.";

        private static IDictionary<string, object> Values => ApplicationData.Current.LocalSettings.Values;

        public static PlayerProgress Load()
        {
            var progress = new PlayerProgress
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

        public static void Save(PlayerProgress progress)
        {
            Values[Prefix + "coins"] = progress.Coins;
            Values[Prefix + "bonusHints"] = progress.BonusHints;
            Values[Prefix + "bonusLives"] = progress.BonusLives;
            Values[Prefix + "theme"] = progress.Theme;
            Values[Prefix + "symbols"] = progress.Symbols;
            Values[Prefix + "sound"] = progress.SoundOn;
            Values[Prefix + "owned"] = string.Join(",", progress.Owned);
            Values[Prefix + "stars"] = FormatMap(progress.Stars);
            Values[Prefix + "times"] = FormatMap(progress.BestTimes);
            Values[Prefix + "unlockedByDifficulty"] = FormatMap(progress.Unlocked);
            Values[Prefix + "extras"] = System.Text.Json.JsonSerializer.Serialize(progress.Extras, AppJson.Default.DictionaryStringString);
        }

        private static Dictionary<string, string> ParseExtras(string json)
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
            Values.TryGetValue(Prefix + key, out object? value) && value is T typed ? typed : fallback;

        // "1:3;2:2" <-> {1:3, 2:2}
        private static Dictionary<int, int> ParseMap(string text) =>
            text.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(pair => pair.Split(':'))
                .Where(parts => parts.Length == 2)
                .ToDictionary(parts => int.Parse(parts[0]), parts => int.Parse(parts[1]));

        private static string FormatMap(Dictionary<int, int> map) =>
            string.Join(";", map.Select(kv => $"{kv.Key}:{kv.Value}"));
    }
}
