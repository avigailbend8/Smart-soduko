using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using Microsoft.Data.Sqlite;

namespace Smart_Sudoku
{
    /// <summary>
    /// The player's progress in the database: every kind of data has its own table
    /// (statistics, preferences, daily missions, adventure, saved game, store items, purchases, achievements).
    /// The guest is a row in the Users table too (IsGuest = 1).
    /// </summary>
    public static partial class Database
    // אובייקט: Database (חלק ההתקדמות) — כל סוג מידע של השחקן נשמר בטבלה משלו: סטטיסטיקות, העדפות, משימות יומיות,
    // מסע, משחק שמור, מוצרי החנות, קניות והישגים. גם האורח הוא שורה בטבלת המשתמשים (IsGuest = 1).
    {
        public const string GuestName = "אורח";
        // אובייקט: GuestName — השם של משתמש האורח בטבלת המשתמשים.

        // The game keeps these values by name (PlayerProgress.Extras); each name has its own column
        private static readonly (string Key, string Column, bool IsText)[] StatColumns =
        // אובייקט: StatColumns — לכל נתון של השחקן: השם שלו במשחק, העמודה שלו בטבלה PlayerStats, והאם הוא טקסט או מספר.
        {
            ("xp", "Xp", false),
            ("stats.games", "Games", false),
            ("stats.seconds", "PlaySeconds", false),
            ("stats.mistakes", "Mistakes", false),
            ("stats.fastest", "FastestSeconds", false),
            ("stats.zen", "ZenSolved", false),
            ("stats.minis", "MiniSolved", false),
            ("stats.duels", "Duels", false),
            ("stats.duelWins", "DuelWins", false),
            ("count.duelWins", "DuelWinCount", false),
            ("stats.aiDuels", "AiDuels", false),
            ("stats.aiWins", "AiWins", false),
            ("speed.best", "SpeedBest", false),
            ("wheel.spins", "WheelSpins", false),
            ("wheel.golden", "GoldenSpins", false),
            ("wheel.last", "WheelLastDay", true),
            ("daily.streak", "DailyStreak", false),
            ("daily.last", "DailyLastDay", true),
            ("league.week", "LeagueWeek", true),
            ("league.points", "LeaguePoints", false),
            ("league.lastWeek", "LastWeekLeague", true),
        };

        private static readonly (string Key, string Column, bool IsText)[] PreferenceColumns =
        // אובייקט: PreferenceColumns — ההעדפות של השחקן ועמודה לכל אחת בטבלה Preferences.
        {
            ("showMistakes", "ShowMistakes", false),
            ("music", "Music", false),
            ("night", "Night", false),
            ("frame", "BoardFrame", true),
            ("mascot", "Mascot", true),
            ("avatarFrame", "AvatarFrame", true),
        };

        // ---------- Tables ----------

        /// <summary>Creates the progress tables, fills the store and achievements tables, and moves old data into them.</summary>
        private static void CreateProgressTables(SqliteConnection connection)
        // פעולה: CreateProgressTables — יוצרת את טבלאות ההתקדמות, ממלאת את טבלת מוצרי החנות ואת טבלת ההישגים,
        // ומעבירה לטבלאות החדשות מידע שנשמר בגרסאות קודמות.
        {
            if (!HasColumn(connection, "Users", "IsGuest"))
                Execute(connection, "ALTER TABLE Users ADD COLUMN IsGuest INTEGER NOT NULL DEFAULT 0");

            Execute(connection, $@"
                CREATE TABLE IF NOT EXISTS PlayerStats (
                    UserId INTEGER PRIMARY KEY REFERENCES Users(Id) ON DELETE CASCADE,
                    {string.Join(",\n                    ", StatColumns.Select(c => c.Column + (c.IsText ? " TEXT" : " INTEGER")))}
                );
                CREATE TABLE IF NOT EXISTS Preferences (
                    UserId INTEGER PRIMARY KEY REFERENCES Users(Id) ON DELETE CASCADE,
                    {string.Join(",\n                    ", PreferenceColumns.Select(c => c.Column + (c.IsText ? " TEXT" : " INTEGER")))}
                );
                CREATE TABLE IF NOT EXISTS DailySolves (
                    UserId INTEGER NOT NULL REFERENCES Users(Id) ON DELETE CASCADE,
                    Day    TEXT    NOT NULL,
                    Count  INTEGER NOT NULL,
                    PRIMARY KEY (UserId, Day)
                );
                CREATE TABLE IF NOT EXISTS DailyMissions (
                    UserId    INTEGER NOT NULL REFERENCES Users(Id) ON DELETE CASCADE,
                    Day       TEXT    NOT NULL,
                    MissionId TEXT    NOT NULL,
                    Progress  INTEGER NOT NULL DEFAULT 0,
                    Claimed   INTEGER NOT NULL DEFAULT 0,
                    PRIMARY KEY (UserId, Day, MissionId)
                );
                CREATE TABLE IF NOT EXISTS AdventureProgress (
                    UserId INTEGER NOT NULL REFERENCES Users(Id) ON DELETE CASCADE,
                    World  INTEGER NOT NULL,
                    Stage  INTEGER NOT NULL,
                    Stars  INTEGER NOT NULL,
                    PRIMARY KEY (UserId, World, Stage)
                );
                CREATE TABLE IF NOT EXISTS SavedGames (
                    UserId    INTEGER PRIMARY KEY REFERENCES Users(Id) ON DELETE CASCADE,
                    LevelId   INTEGER NOT NULL,
                    IsDaily   INTEGER NOT NULL,
                    Puzzle    TEXT    NOT NULL,
                    Solution  TEXT    NOT NULL,
                    Board     TEXT    NOT NULL,
                    Notes     TEXT    NOT NULL,
                    Hinted    TEXT    NOT NULL,
                    Lives     INTEGER NOT NULL,
                    Mistakes  INTEGER NOT NULL,
                    HintsUsed INTEGER NOT NULL,
                    Seconds   INTEGER NOT NULL
                );
                CREATE TABLE IF NOT EXISTS StoreItems (
                    Id       TEXT    PRIMARY KEY,
                    Category TEXT    NOT NULL,
                    Name     TEXT    NOT NULL,
                    Price    INTEGER NOT NULL
                );
                CREATE TABLE IF NOT EXISTS Achievements (
                    Id          TEXT    PRIMARY KEY,
                    Emoji       TEXT    NOT NULL,
                    Name        TEXT    NOT NULL,
                    Description TEXT    NOT NULL,
                    RewardCoins INTEGER NOT NULL
                );
                CREATE TABLE IF NOT EXISTS UserAchievements (
                    UserId        INTEGER NOT NULL REFERENCES Users(Id) ON DELETE CASCADE,
                    AchievementId TEXT    NOT NULL REFERENCES Achievements(Id),
                    UnlockedAt    TEXT    NOT NULL,
                    PRIMARY KEY (UserId, AchievementId)
                );");

            FillCatalog(connection);
            MovePurchasesTable(connection);
            MoveOldSettings(connection);
        }

        /// <summary>Writes every store item and achievement into their tables, so the tables always match the game.</summary>
        private static void FillCatalog(SqliteConnection connection)
        // פעולה: FillCatalog — כותבת את כל מוצרי החנות ואת כל ההישגים לטבלאות שלהם, כך שהטבלאות תמיד מתאימות למשחק.
        {
            var items = new List<(string Id, string Category, string Name, int Price)>
            // אובייקט: items — כל המוצרים בחנות: מזהה, קטגוריה, שם ומחיר.
            {
                ("hint-pack", "consumable", "חבילת 3 רמזים", Pages.StorePage.HintPackPrice),
                ("heart", "consumable", "לב נוסף", Pages.GamePage.HeartPrice),
            };
            foreach (var theme in Themes.All)
                items.Add((theme.Id, "theme", theme.Name, theme.Price));
            foreach (var set in SymbolSets.All)
                items.Add((set.Id, "symbols", set.Name, set.Price));
            foreach (var frame in BoardFrames.All)
                items.Add(("frame-" + frame.Id, "boardFrame", frame.Name, frame.Price));
            foreach (var mascot in Cosmetics.Mascots)
                items.Add(("mascot-" + mascot.Id, "mascot", mascot.Name, mascot.Price));
            foreach (var frame in Cosmetics.AvatarFrames)
                items.Add(("aframe-" + frame.Id, "avatarFrame", frame.Name, frame.Price));

            using var transaction = connection.BeginTransaction();
            foreach (var item in items)
            {
                using var command = connection.CreateCommand();
                command.CommandText = @"
                    INSERT INTO StoreItems (Id, Category, Name, Price) VALUES ($id, $category, $name, $price)
                    ON CONFLICT(Id) DO UPDATE SET Category = excluded.Category, Name = excluded.Name, Price = excluded.Price";
                command.Parameters.AddWithValue("$id", item.Id);
                command.Parameters.AddWithValue("$category", item.Category);
                command.Parameters.AddWithValue("$name", item.Name);
                command.Parameters.AddWithValue("$price", item.Price);
                command.ExecuteNonQuery();
            }
            foreach (var achievement in Smart_Sudoku.Achievements.All)
            {
                using var command = connection.CreateCommand();
                command.CommandText = @"
                    INSERT INTO Achievements (Id, Emoji, Name, Description, RewardCoins) VALUES ($id, $emoji, $name, $description, $coins)
                    ON CONFLICT(Id) DO UPDATE SET Emoji = excluded.Emoji, Name = excluded.Name,
                                                  Description = excluded.Description, RewardCoins = excluded.RewardCoins";
                command.Parameters.AddWithValue("$id", achievement.Id);
                command.Parameters.AddWithValue("$emoji", achievement.Emoji);
                command.Parameters.AddWithValue("$name", achievement.Name);
                command.Parameters.AddWithValue("$description", achievement.Description);
                command.Parameters.AddWithValue("$coins", achievement.RewardCoins);
                command.ExecuteNonQuery();
            }
            transaction.Commit();
        }

        /// <summary>
        /// Older versions kept achievements inside Purchases, with no link to the store.
        /// Rebuilds Purchases with a foreign key to StoreItems and moves achievements to UserAchievements.
        /// </summary>
        private static void MovePurchasesTable(SqliteConnection connection)
        // פעולה: MovePurchasesTable — בגרסאות קודמות ההישגים נשמרו בתוך טבלת הקניות, בלי קשר לטבלת המוצרים.
        // בונה מחדש את טבלת הקניות עם מפתח זר (FK) לטבלת המוצרים, ומעבירה את ההישגים לטבלה UserAchievements.
        {
            using (var check = connection.CreateCommand())
            {
                check.CommandText = "SELECT sql FROM sqlite_master WHERE type = 'table' AND name = 'Purchases'";
                if (check.ExecuteScalar() is string sql && sql.Contains("StoreItems"))
                    return;   // already the new table
            }

            bool hadOld = HasTable(connection, "Purchases");
            // אובייקט: hadOld — אמת אם יש טבלת קניות ישנה שצריך להעביר ממנה.
            using var transaction = connection.BeginTransaction();
            if (hadOld)
                Execute(connection, "ALTER TABLE Purchases RENAME TO OldPurchases");
            Execute(connection, @"
                CREATE TABLE Purchases (
                    UserId INTEGER NOT NULL REFERENCES Users(Id) ON DELETE CASCADE,
                    ItemId TEXT    NOT NULL REFERENCES StoreItems(Id),
                    PRIMARY KEY (UserId, ItemId)
                );");
            if (hadOld)
            {
                Execute(connection, $@"
                    INSERT OR IGNORE INTO UserAchievements (UserId, AchievementId, UnlockedAt)
                    SELECT UserId, substr(ItemId, 5), '{DateTime.UtcNow:o}' FROM OldPurchases
                    WHERE ItemId LIKE 'ach-%' AND substr(ItemId, 5) IN (SELECT Id FROM Achievements);
                    INSERT OR IGNORE INTO Purchases (UserId, ItemId)
                    SELECT UserId, ItemId FROM OldPurchases WHERE ItemId IN (SELECT Id FROM StoreItems);
                    DROP TABLE OldPurchases;");
            }
            transaction.Commit();
        }

        /// <summary>Older versions kept everything else as name/value text in UserSettings. Moves it to the new tables.</summary>
        private static void MoveOldSettings(SqliteConnection connection)
        // פעולה: MoveOldSettings — בגרסאות קודמות כל השאר נשמר כ"שם = ערך" בטבלה UserSettings. מעבירה אותו לטבלאות החדשות ומוחקת אותה.
        {
            if (!HasTable(connection, "UserSettings"))
                return;

            var oldSettings = new Dictionary<int, Dictionary<string, string>>();
            // אובייקט: oldSettings — לכל משתמש: ההגדרות הישנות שלו.
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT UserId, Key, Value FROM UserSettings";
                using var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    int userId = reader.GetInt32(0);
                    // אובייקט: userId — המספר של המשתמש שההגדרה הזאת שלו.
                    if (!oldSettings.TryGetValue(userId, out var settings))
                        oldSettings[userId] = settings = new Dictionary<string, string>();
                    settings[reader.GetString(1)] = reader.GetString(2);
                }
            }

            foreach (var user in oldSettings)
            {
                var progress = LoadProgress(connection, user.Key);
                // אובייקט: progress — ההתקדמות של המשתמש מהטבלאות החדשות, שאליה מוסיפים את ההגדרות הישנות.
                foreach (var setting in user.Value)
                    progress.Extras[setting.Key] = setting.Value;
                SaveProgress(connection, user.Key, progress);
            }
            Execute(connection, "DROP TABLE UserSettings");
        }

        // ---------- The guest ----------

        /// <summary>The guest's row in Users (created the first time). Progress the guest had in older versions moves in.</summary>
        public static int GuestUserId()
        // פעולה: GuestUserId — מחזירה את המספר של האורח בטבלת המשתמשים (ויוצרת אותו בפעם הראשונה).
        // התקדמות שהייתה לאורח בגרסאות קודמות (בהגדרות של Windows) עוברת למסד הנתונים.
        {
            using var connection = Open();
            using (var find = connection.CreateCommand())
            {
                find.CommandText = "SELECT Id FROM Users WHERE IsGuest = 1";
                if (find.ExecuteScalar() is long id)
                    return (int)id;
            }

            int guestId;
            // אובייקט: guestId — המספר שקיבל האורח החדש.
            using (var insert = connection.CreateCommand())
            {
                // No password: the guest can never be signed in to from the sign-in page
                insert.CommandText = @"
                    INSERT INTO Users (UserName, PasswordHash, Salt, CreatedAt, IsGuest)
                    VALUES ($name, '', '', $created, 1);
                    SELECT last_insert_rowid();";
                insert.Parameters.AddWithValue("$name", GuestName);
                insert.Parameters.AddWithValue("$created", DateTime.UtcNow.ToString("o"));
                guestId = Convert.ToInt32(insert.ExecuteScalar());
            }

            SaveProgress(connection, guestId, GuestStore.Load());
            GuestStore.Clear();
            return guestId;
        }

        // ---------- Loading ----------

        public static PlayerProgress LoadProgress(int userId)
        // פעולה: LoadProgress — טוענת את כל ההתקדמות של משתמש מכל הטבלאות.
        {
            using var connection = Open();
            return LoadProgress(connection, userId);
        }

        private static PlayerProgress LoadProgress(SqliteConnection connection, int userId)
        // פעולה: LoadProgress (הפנימית) — קוראת כל טבלה ובונה ממנה את ההתקדמות של המשתמש.
        {
            var progress = new PlayerProgress();
            // אובייקט: progress — ההתקדמות שנבנית מתוך הטבלאות.

            ReadRows(connection, userId, "SELECT Coins, BonusHints, BonusLives, Theme, Symbols, SoundOn FROM Users WHERE Id = $id", reader =>
            {
                progress.Coins = reader.GetInt32(0);
                progress.BonusHints = reader.GetInt32(1);
                progress.BonusLives = reader.GetInt32(2);
                progress.Theme = reader.GetString(3);
                progress.Symbols = reader.GetString(4);
                progress.SoundOn = reader.GetInt32(5) != 0;
            });
            ReadRows(connection, userId, "SELECT LevelId, Stars, BestTime FROM LevelResults WHERE UserId = $id", reader =>
            {
                progress.Stars[reader.GetInt32(0)] = reader.GetInt32(1);
                progress.BestTimes[reader.GetInt32(0)] = reader.GetInt32(2);
            });
            ReadRows(connection, userId, "SELECT Difficulty, Level FROM UnlockedLevels WHERE UserId = $id",
                reader => progress.Unlocked[reader.GetInt32(0)] = reader.GetInt32(1));
            ReadRows(connection, userId, "SELECT ItemId FROM Purchases WHERE UserId = $id",
                reader => progress.Owned.Add(reader.GetString(0)));
            ReadRows(connection, userId, "SELECT AchievementId FROM UserAchievements WHERE UserId = $id",
                reader => progress.Owned.Add("ach-" + reader.GetString(0)));

            ReadColumns(connection, userId, "PlayerStats", StatColumns, progress.Extras);
            ReadColumns(connection, userId, "Preferences", PreferenceColumns, progress.Extras);

            ReadRows(connection, userId, "SELECT Day, Count FROM DailySolves WHERE UserId = $id",
                reader => progress.Extras["stats.day." + reader.GetString(0)] = Text(reader.GetInt32(1)));
            ReadRows(connection, userId, "SELECT Day, MissionId, Progress, Claimed FROM DailyMissions WHERE UserId = $id", reader =>
            {
                string day = reader.GetString(0), mission = reader.GetString(1);
                // אובייקט: day, mission — היום של המשימה, ואיזו משימה זאת.
                bool claimed = reader.GetInt32(3) != 0;
                // אובייקט: claimed — אמת אם השחקן כבר לקח את הפרס על המשימה.
                if (mission == "chest")
                {
                    if (claimed)
                        progress.Extras[$"m.{day}.chest"] = "1";
                    return;
                }
                progress.Extras[$"m.{day}.{mission}"] = Text(reader.GetInt32(2));
                if (claimed)
                    progress.Extras[$"m.{day}.{mission}.claimed"] = "1";
            });
            ReadRows(connection, userId, "SELECT World, Stage, Stars FROM AdventureProgress WHERE UserId = $id",
                reader => progress.Extras[$"adv.{reader.GetInt32(0)}.{reader.GetInt32(1)}"] = Text(reader.GetInt32(2)));
            ReadRows(connection, userId, @"
                SELECT LevelId, IsDaily, Puzzle, Solution, Board, Notes, Hinted, Lives, Mistakes, HintsUsed, Seconds
                FROM SavedGames WHERE UserId = $id", reader =>
            {
                var saved = new SavedGame
                // אובייקט: saved — המשחק השמור, כפי שנבנה מהעמודות של הטבלה.
                {
                    LevelId = reader.GetInt32(0),
                    Daily = reader.GetInt32(1) != 0,
                    Puzzle = Numbers(reader.GetString(2)),
                    Solution = Numbers(reader.GetString(3)),
                    Board = Numbers(reader.GetString(4)),
                    Notes = Numbers(reader.GetString(5)),
                    Hinted = Numbers(reader.GetString(6)).Select(n => n != 0).ToArray(),
                    Lives = reader.GetInt32(7),
                    Mistakes = reader.GetInt32(8),
                    HintsUsed = reader.GetInt32(9),
                    Seconds = reader.GetInt32(10),
                };
                progress.Extras["savedGame"] = JsonSerializer.Serialize(saved, AppJson.Default.SavedGame);
            });

            return progress;
        }

        // ---------- Saving ----------

        public static void SaveProgress(int userId, PlayerProgress progress)
        // פעולה: SaveProgress — שומרת את כל ההתקדמות של משתמש בכל הטבלאות, הכול ביחד (או שהכול נשמר, או ששום דבר).
        {
            using var connection = Open();
            SaveProgress(connection, userId, progress);
        }

        private static void SaveProgress(SqliteConnection connection, int userId, PlayerProgress progress)
        // פעולה: SaveProgress (הפנימית) — כותבת כל חלק של ההתקדמות לטבלה שלו, בתוך טרנזקציה אחת.
        {
            using var transaction = connection.BeginTransaction();
            // אובייקט: transaction — "חבילה" של שינויים שנשמרים כולם יחד בסוף.

            Run(connection, @"
                UPDATE Users SET Coins = $coins, BonusHints = $hints, BonusLives = $lives,
                                 Theme = $theme, Symbols = $symbols, SoundOn = $sound
                WHERE Id = $id",
                ("$id", userId), ("$coins", progress.Coins), ("$hints", progress.BonusHints), ("$lives", progress.BonusLives),
                ("$theme", progress.Theme), ("$symbols", progress.Symbols), ("$sound", progress.SoundOn ? 1 : 0));

            foreach (var level in progress.Stars)
            {
                Run(connection, "INSERT OR REPLACE INTO LevelResults (UserId, LevelId, Stars, BestTime) VALUES ($id, $level, $stars, $time)",
                    ("$id", userId), ("$level", level.Key), ("$stars", level.Value),
                    ("$time", progress.BestTimes.TryGetValue(level.Key, out int time) ? time : 0));
            }
            foreach (var unlocked in progress.Unlocked)
            {
                Run(connection, "INSERT OR REPLACE INTO UnlockedLevels (UserId, Difficulty, Level) VALUES ($id, $difficulty, $level)",
                    ("$id", userId), ("$difficulty", unlocked.Key), ("$level", unlocked.Value));
            }

            foreach (string item in progress.Owned)
            {
                if (item.StartsWith("ach-"))
                {
                    Run(connection, @"
                        INSERT OR IGNORE INTO UserAchievements (UserId, AchievementId, UnlockedAt)
                        SELECT $id, $achievement, $at WHERE EXISTS (SELECT 1 FROM Achievements WHERE Id = $achievement)",
                        ("$id", userId), ("$achievement", item.Substring(4)), ("$at", DateTime.UtcNow.ToString("o")));
                }
                else
                {
                    Run(connection, @"
                        INSERT OR IGNORE INTO Purchases (UserId, ItemId)
                        SELECT $id, $item WHERE EXISTS (SELECT 1 FROM StoreItems WHERE Id = $item)",
                        ("$id", userId), ("$item", item));
                }
            }

            WriteColumns(connection, userId, "PlayerStats", StatColumns, progress.Extras);
            WriteColumns(connection, userId, "Preferences", PreferenceColumns, progress.Extras);

            foreach (string table in new[] { "DailySolves", "DailyMissions", "AdventureProgress", "SavedGames" })
                Run(connection, $"DELETE FROM {table} WHERE UserId = $id", ("$id", userId));

            var missions = new Dictionary<(string Day, string Mission), (int Progress, bool Claimed)>();
            // אובייקט: missions — לכל יום ומשימה: כמה התקדמו והאם הפרס נלקח.
            foreach (var extra in progress.Extras)
            {
                string key = extra.Key;
                // אובייקט: key — השם של הנתון (למשל "stats.day.2026-11-24" או "adv.0.3").
                if (key.StartsWith("stats.day."))
                {
                    Run(connection, "INSERT INTO DailySolves (UserId, Day, Count) VALUES ($id, $day, $count)",
                        ("$id", userId), ("$day", key.Substring("stats.day.".Length)), ("$count", Number(extra.Value)));
                }
                else if (key.StartsWith("m.") && key.Length > 13)
                {
                    // m.<yyyy-MM-dd>.<mission>  /  m.<day>.<mission>.claimed  /  m.<day>.chest
                    string day = key.Substring(2, 10);
                    // אובייקט: day — התאריך של המשימה (10 התווים שאחרי "m.").
                    string rest = key.Substring(13);
                    // אובייקט: rest — מה שבא אחרי התאריך: שם המשימה, ואולי ".claimed".
                    bool claimedKey = rest.EndsWith(".claimed");
                    // אובייקט: claimedKey — אמת אם זה הנתון "הפרס נלקח" ולא ההתקדמות.
                    string mission = claimedKey ? rest.Substring(0, rest.Length - ".claimed".Length) : rest;
                    // אובייקט: mission — שם המשימה עצמה, בלי ".claimed".
                    missions.TryGetValue((day, mission), out var state);
                    if (claimedKey || mission == "chest")
                        state.Claimed = extra.Value == "1";
                    else
                        state.Progress = Number(extra.Value);
                    missions[(day, mission)] = state;
                }
                else if (key.StartsWith("adv."))
                {
                    string[] parts = key.Split('.');
                    if (parts.Length == 3)
                    {
                        Run(connection, "INSERT INTO AdventureProgress (UserId, World, Stage, Stars) VALUES ($id, $world, $stage, $stars)",
                            ("$id", userId), ("$world", Number(parts[1])), ("$stage", Number(parts[2])), ("$stars", Number(extra.Value)));
                    }
                }
                else if (key == "savedGame")
                {
                    SaveGameRow(connection, userId, extra.Value);
                }
            }
            foreach (var mission in missions)
            {
                Run(connection, "INSERT INTO DailyMissions (UserId, Day, MissionId, Progress, Claimed) VALUES ($id, $day, $mission, $progress, $claimed)",
                    ("$id", userId), ("$day", mission.Key.Day), ("$mission", mission.Key.Mission),
                    ("$progress", mission.Value.Progress), ("$claimed", mission.Value.Claimed ? 1 : 0));
            }

            transaction.Commit();
        }

        /// <summary>Writes the level left in the middle, one column per part (the 81 cells as "0,5,3,...").</summary>
        private static void SaveGameRow(SqliteConnection connection, int userId, string json)
        // פעולה: SaveGameRow — שומרת את השלב שנעצר באמצע: עמודה לכל חלק (81 המשבצות נשמרות כטקסט "0,5,3,...").
        {
            SavedGame? saved;
            // אובייקט: saved — המשחק השמור.
            try
            {
                saved = JsonSerializer.Deserialize(json, AppJson.Default.SavedGame);
            }
            catch (JsonException)
            {
                return;
            }
            if (saved == null)
                return;

            Run(connection, @"
                INSERT INTO SavedGames (UserId, LevelId, IsDaily, Puzzle, Solution, Board, Notes, Hinted, Lives, Mistakes, HintsUsed, Seconds)
                VALUES ($id, $level, $daily, $puzzle, $solution, $board, $notes, $hinted, $lives, $mistakes, $hints, $seconds)",
                ("$id", userId), ("$level", saved.LevelId), ("$daily", saved.Daily ? 1 : 0),
                ("$puzzle", string.Join(",", saved.Puzzle)), ("$solution", string.Join(",", saved.Solution)),
                ("$board", string.Join(",", saved.Board)), ("$notes", string.Join(",", saved.Notes)),
                ("$hinted", string.Join(",", saved.Hinted.Select(h => h ? 1 : 0))),
                ("$lives", saved.Lives), ("$mistakes", saved.Mistakes), ("$hints", saved.HintsUsed), ("$seconds", saved.Seconds));
        }

        // ---------- Small helpers ----------

        private static void ReadColumns(SqliteConnection connection, int userId, string table, (string Key, string Column, bool IsText)[] columns, Dictionary<string, string> extras)
        // פעולה: ReadColumns — קוראת שורה אחת מטבלה עם עמודה לכל נתון, ומכניסה כל ערך שקיים להתקדמות.
        {
            ReadRows(connection, userId, $"SELECT {string.Join(", ", columns.Select(c => c.Column))} FROM {table} WHERE UserId = $id", reader =>
            {
                for (int i = 0; i < columns.Length; i++)
                {
                    if (!reader.IsDBNull(i))
                        extras[columns[i].Key] = columns[i].IsText ? reader.GetString(i) : Text(reader.GetInt64(i));
                }
            });
        }

        private static void WriteColumns(SqliteConnection connection, int userId, string table, (string Key, string Column, bool IsText)[] columns, Dictionary<string, string> extras)
        // פעולה: WriteColumns — כותבת שורה אחת לטבלה עם עמודה לכל נתון (נתון שאין לו ערך נשמר כ-NULL).
        {
            using var command = connection.CreateCommand();
            command.CommandText = $@"
                INSERT OR REPLACE INTO {table} (UserId, {string.Join(", ", columns.Select(c => c.Column))})
                VALUES ($id, {string.Join(", ", columns.Select(c => "$" + c.Column))})";
            command.Parameters.AddWithValue("$id", userId);
            foreach (var column in columns)
            {
                object value = !extras.TryGetValue(column.Key, out string? text) ? DBNull.Value
                    : column.IsText ? text : Number(text);
                    // אובייקט: value — מה שנכתב בעמודה: טקסט, מספר, או NULL אם אין ערך.
                command.Parameters.AddWithValue("$" + column.Column, value);
            }
            command.ExecuteNonQuery();
        }

        private static void ReadRows(SqliteConnection connection, int userId, string sql, Action<SqliteDataReader> row)
        // פעולה: ReadRows — מריצה שאילתת SELECT על המשתמש, ומפעילה את row על כל שורה בתוצאה.
        {
            using var command = connection.CreateCommand();
            command.CommandText = sql;
            command.Parameters.AddWithValue("$id", userId);
            using var reader = command.ExecuteReader();
            while (reader.Read())
                row(reader);
        }

        private static void Run(SqliteConnection connection, string sql, params (string Name, object Value)[] parameters)
        // פעולה: Run — מריצה פקודת SQL עם הפרמטרים שלה.
        {
            using var command = connection.CreateCommand();
            command.CommandText = sql;
            foreach (var (name, value) in parameters)
                command.Parameters.AddWithValue(name, value);
            command.ExecuteNonQuery();
        }

        private static bool HasTable(SqliteConnection connection, string table)
        // פעולה: HasTable — בודקת אם טבלה קיימת במסד הנתונים.
        {
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT 1 FROM sqlite_master WHERE type = 'table' AND name = $name";
            command.Parameters.AddWithValue("$name", table);
            return command.ExecuteScalar() != null;
        }

        private static bool HasColumn(SqliteConnection connection, string table, string column)
        // פעולה: HasColumn — בודקת אם יש לטבלה עמודה בשם מסוים.
        {
            using var command = connection.CreateCommand();
            command.CommandText = $"SELECT 1 FROM pragma_table_info('{table}') WHERE name = $name";
            command.Parameters.AddWithValue("$name", column);
            return command.ExecuteScalar() != null;
        }

        private static int Number(string text) => int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int n) ? n : 0;
        // פעולה: Number — הופכת טקסט למספר (0 אם זה לא מספר).

        private static string Text(long number) => number.ToString(CultureInfo.InvariantCulture);
        // פעולה: Text — הופכת מספר לטקסט.

        private static int[] Numbers(string text) =>
        // פעולה: Numbers — הופכת טקסט כמו "0,5,3" לרשימת מספרים.
            text.Length == 0 ? new int[0] : text.Split(',').Select(Number).ToArray();
    }
}
