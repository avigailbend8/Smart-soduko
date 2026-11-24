using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using Microsoft.Data.Sqlite;
using Windows.Storage;

namespace Smart_Sudoku
{
    /// <summary>One user's line in the leaderboard. UserId 0 is the guest on this computer.</summary>
    public sealed record LeaderboardRow(int UserId, string Name, int Coins, int Solved, int Stars, int FastestSeconds, bool HasAvatar);
    // אובייקט: LeaderboardRow — שורה אחת בטבלת השיאים של המחשב: מי המשתמש, כמה מטבעות, כמה שלבים, כמה כוכבים, הזמן הכי מהיר, והאם יש לו תמונה.

    /// <summary>
    /// SQLite database (smartsudoku.db in the app's local folder) with the registered
    /// users and their progress. Passwords are stored only as salted PBKDF2 hashes.
    /// </summary>
    public static partial class Database
    // אובייקט: Database — מסד הנתונים של המשחק (הקובץ smartsudoku.db): המשתמשים הרשומים וכל ההתקדמות שלהם.
    // הסיסמאות לא נשמרות כמו שהן, אלא רק כ"קוד מעורבל" שאי אפשר להחזיר לסיסמה.
    {
        private const int HashIterations = 100_000;
        // אובייקט: HashIterations — כמה פעמים מערבבים את הסיסמה (100,000), כדי שיהיה מאוד קשה לנחש אותה.

        private static string? connectionString;
        // אובייקט: connectionString — ה"כתובת" של קובץ מסד הנתונים, שבעזרתה פותחים אליו חיבור.

        public static bool IsReady => connectionString != null;
        // אובייקט: IsReady — אמת אם מסד הנתונים כבר הוכן ואפשר להשתמש בו.

        public static void Init()
        // פעולה: Init — מכינה את מסד הנתונים כשהאפליקציה עולה: קובעת איפה הקובץ נמצא, ויוצרת את כל הטבלאות אם הן לא קיימות
        // (משתמשים, תוצאות שלבים, שלבים פתוחים, קניות, הגדרות, תמונות, חברים ומילת שחזור).
        {
            SQLitePCL.Batteries_V2.Init();

            string path = Path.Combine(ApplicationData.Current.LocalFolder.Path, "smartsudoku.db");
            // אובייקט: path — המיקום של קובץ מסד הנתונים בתיקייה של האפליקציה.
            connectionString = new SqliteConnectionStringBuilder { DataSource = path }.ToString();

            using var connection = Open();
            // אובייקט: connection — חיבור פתוח למסד הנתונים.
            Execute(connection, @"
                CREATE TABLE IF NOT EXISTS Users (
                    Id           INTEGER PRIMARY KEY AUTOINCREMENT,
                    UserName     TEXT    NOT NULL UNIQUE COLLATE NOCASE,
                    PasswordHash TEXT    NOT NULL,
                    Salt         TEXT    NOT NULL,
                    Coins        INTEGER NOT NULL DEFAULT 100,
                    BonusHints   INTEGER NOT NULL DEFAULT 0,
                    BonusLives   INTEGER NOT NULL DEFAULT 0,
                    Theme        TEXT    NOT NULL DEFAULT 'pink',
                    Symbols      TEXT    NOT NULL DEFAULT 'numbers',
                    SoundOn      INTEGER NOT NULL DEFAULT 1,
                    CreatedAt    TEXT    NOT NULL
                );
                CREATE TABLE IF NOT EXISTS LevelResults (
                    UserId   INTEGER NOT NULL REFERENCES Users(Id) ON DELETE CASCADE,
                    LevelId  INTEGER NOT NULL,
                    Stars    INTEGER NOT NULL,
                    BestTime INTEGER NOT NULL,
                    PRIMARY KEY (UserId, LevelId)
                );
                CREATE TABLE IF NOT EXISTS UnlockedLevels (
                    UserId     INTEGER NOT NULL REFERENCES Users(Id) ON DELETE CASCADE,
                    Difficulty INTEGER NOT NULL,
                    Level      INTEGER NOT NULL,
                    PRIMARY KEY (UserId, Difficulty)
                );
                CREATE TABLE IF NOT EXISTS Avatars (
                    UserId    INTEGER PRIMARY KEY REFERENCES Users(Id) ON DELETE CASCADE,
                    Image     BLOB    NOT NULL,
                    UpdatedAt TEXT    NOT NULL
                );
                CREATE TABLE IF NOT EXISTS Friends (
                    UserId     INTEGER NOT NULL REFERENCES Users(Id) ON DELETE CASCADE,
                    FriendName TEXT    NOT NULL COLLATE NOCASE,
                    AddedAt    TEXT    NOT NULL,
                    PRIMARY KEY (UserId, FriendName)
                );
                CREATE TABLE IF NOT EXISTS Recovery (
                    UserId INTEGER PRIMARY KEY REFERENCES Users(Id) ON DELETE CASCADE,
                    Hash   TEXT    NOT NULL,
                    Salt   TEXT    NOT NULL
                );");

            // Statistics, preferences, missions, adventure, saved game, store items, purchases and achievements
            CreateProgressTables(connection);
        }

        /// <summary>Creates a user. Returns the new user's id, or null with an error message.</summary>
        public static int? Register(string userName, string password, PlayerProgress startingProgress, out string error)
        // פעולה: Register — יוצרת משתמש חדש. בודקת שהשם פנוי, מערבלת את הסיסמה ושומרת. מחזירה את המספר של המשתמש, או null עם הודעת שגיאה.
        {
            using var connection = Open();
            // אובייקט: connection — חיבור למסד הנתונים.

            using (var check = connection.CreateCommand())
            // אובייקט: check — פקודה שבודקת אם שם המשתמש כבר תפוס.
            {
                check.CommandText = "SELECT 1 FROM Users WHERE UserName = $name";
                check.Parameters.AddWithValue("$name", userName);
                if (check.ExecuteScalar() != null)
                {
                    error = "שם המשתמש הזה כבר תפוס — נסו שם אחר";
                    return null;
                }
            }

            byte[] salt = RandomNumberGenerator.GetBytes(16);
            // אובייקט: salt — "מלח": מספר אקראי שמערבבים עם הסיסמה, כדי שלשני אנשים עם אותה סיסמה יהיה קוד שונה.
            byte[] hash = HashPassword(password, salt);
            // אובייקט: hash — הסיסמה אחרי הערבול.

            int userId;
            // אובייקט: userId — המספר שמסד הנתונים נתן למשתמש החדש.
            using (var insert = connection.CreateCommand())
            // אובייקט: insert — פקודה שמוסיפה את המשתמש החדש לטבלה.
            {
                insert.CommandText = @"
                    INSERT INTO Users (UserName, PasswordHash, Salt, CreatedAt)
                    VALUES ($name, $hash, $salt, $created);
                    SELECT last_insert_rowid();";
                insert.Parameters.AddWithValue("$name", userName);
                insert.Parameters.AddWithValue("$hash", Convert.ToBase64String(hash));
                insert.Parameters.AddWithValue("$salt", Convert.ToBase64String(salt));
                insert.Parameters.AddWithValue("$created", DateTime.UtcNow.ToString("o"));
                userId = Convert.ToInt32(insert.ExecuteScalar());
            }

            SaveProgress(userId, startingProgress);
            error = "";
            return userId;
        }

        /// <summary>Checks the user name and password. Returns the user's id, or null.</summary>
        public static int? Login(string userName, string password)
        // פעולה: Login — בודקת שם משתמש וסיסמה. אם הם נכונים — מחזירה את המספר של המשתמש, ואחרת null.
        {
            using var connection = Open();
            // אובייקט: connection — חיבור למסד הנתונים.
            using var command = connection.CreateCommand();
            // אובייקט: command — פקודה שמחפשת את המשתמש לפי השם.
            command.CommandText = "SELECT Id, PasswordHash, Salt FROM Users WHERE UserName = $name AND IsGuest = 0";
            command.Parameters.AddWithValue("$name", userName);

            using var reader = command.ExecuteReader();
            // אובייקט: reader — קורא את התוצאה (המספר, הסיסמה המעורבלת והמלח).
            if (!reader.Read())
                return null;   // no such user

            if (!PasswordMatches(password, reader.GetString(1), reader.GetString(2)))
                return null;   // wrong password
            return reader.GetInt32(0);
        }

        /// <summary>The user name as it was registered (keeps the original spelling).</summary>
        public static string GetUserName(int userId)
        // פעולה: GetUserName — מחזירה את שם המשתמש כפי שנרשם (עם האותיות הגדולות והקטנות המקוריות).
        {
            using var connection = Open();
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT UserName FROM Users WHERE Id = $id";
            command.Parameters.AddWithValue("$id", userId);
            return (string?)command.ExecuteScalar() ?? "";
        }

        // ---------- Passwords ----------

        private static bool CheckPassword(SqliteConnection connection, int userId, string password)
        // פעולה: CheckPassword — בודקת אם הסיסמה של משתמש נכונה.
        {
            using var command = connection.CreateCommand();
            // אובייקט: command — פקודה שקוראת את הסיסמה המעורבלת והמלח של המשתמש.
            command.CommandText = "SELECT PasswordHash, Salt FROM Users WHERE Id = $id";
            command.Parameters.AddWithValue("$id", userId);
            using var reader = command.ExecuteReader();
            if (!reader.Read())
                return false;
            return PasswordMatches(password, reader.GetString(0), reader.GetString(1));
        }

        private static void SetPassword(SqliteConnection connection, int userId, string newPassword)
        // פעולה: SetPassword — שומרת סיסמה חדשה (מעורבלת, עם מלח חדש).
        {
            byte[] salt = RandomNumberGenerator.GetBytes(16);
            // אובייקט: salt — מלח חדש לסיסמה החדשה.
            using var command = connection.CreateCommand();
            command.CommandText = "UPDATE Users SET PasswordHash = $hash, Salt = $salt WHERE Id = $id";
            command.Parameters.AddWithValue("$hash", Convert.ToBase64String(HashPassword(newPassword, salt)));
            command.Parameters.AddWithValue("$salt", Convert.ToBase64String(salt));
            command.Parameters.AddWithValue("$id", userId);
            command.ExecuteNonQuery();
        }

        /// <summary>Changes the password, if the old one is right.</summary>
        public static bool ChangePassword(int userId, string oldPassword, string newPassword)
        // פעולה: ChangePassword — משנה סיסמה, אבל רק אם הסיסמה הישנה נכונה.
        {
            using var connection = Open();
            if (!CheckPassword(connection, userId, oldPassword))
                return false;
            SetPassword(connection, userId, newPassword);
            return true;
        }

        /// <summary>Saves a secret recovery word (hashed, like a password) for "forgot password".</summary>
        public static void SetRecoveryWord(int userId, string word)
        // פעולה: SetRecoveryWord — שומרת "מילה סודית" (מעורבלת כמו סיסמה), כדי שאפשר יהיה לאפס סיסמה אם שוכחים אותה.
        {
            byte[] salt = RandomNumberGenerator.GetBytes(16);
            // אובייקט: salt — מלח למילה הסודית.
            using var connection = Open();
            using var command = connection.CreateCommand();
            command.CommandText = "INSERT OR REPLACE INTO Recovery (UserId, Hash, Salt) VALUES ($id, $hash, $salt)";
            command.Parameters.AddWithValue("$id", userId);
            command.Parameters.AddWithValue("$hash", Convert.ToBase64String(HashPassword(word.Trim(), salt)));
            command.Parameters.AddWithValue("$salt", Convert.ToBase64String(salt));
            command.ExecuteNonQuery();
        }

        public static bool HasRecoveryWord(int userId)
        // פעולה: HasRecoveryWord — בודקת אם למשתמש יש מילה סודית.
        {
            using var connection = Open();
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT 1 FROM Recovery WHERE UserId = $id";
            command.Parameters.AddWithValue("$id", userId);
            return command.ExecuteScalar() != null;
        }

        /// <summary>"Forgot password": sets a new password if the recovery word is right.</summary>
        public static bool ResetPassword(string userName, string recoveryWord, string newPassword)
        // פעולה: ResetPassword — "שכחתי סיסמה": קובעת סיסמה חדשה, אם המילה הסודית נכונה.
        {
            using var connection = Open();
            int? userId = FindUser(connection, userName);
            // אובייקט: userId — המספר של המשתמש (או null אם אין משתמש כזה).
            if (userId == null)
                return false;
            int id = userId.Value;
            // אובייקט: id — המספר של המשתמש.

            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT Hash, Salt FROM Recovery WHERE UserId = $id";
                command.Parameters.AddWithValue("$id", id);
                using var reader = command.ExecuteReader();
                if (!reader.Read())
                    return false;   // the user never chose a secret word
                if (!PasswordMatches(recoveryWord.Trim(), reader.GetString(0), reader.GetString(1)))
                    return false;
            }

            SetPassword(connection, id, newPassword);
            return true;
        }

        // ---------- Friends ----------

        private static int? FindUser(SqliteConnection connection, string userName)
        // פעולה: FindUser (הפנימית) — מחפשת משתמש לפי השם ומחזירה את המספר שלו (או null).
        {
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT Id FROM Users WHERE UserName = $name AND IsGuest = 0";
            command.Parameters.AddWithValue("$name", userName.Trim());
            object? result = command.ExecuteScalar();
            // אובייקט: result — מה שמסד הנתונים החזיר.
            if (result == null)
                return null;   // no user with that name
            return Convert.ToInt32(result);
        }

        /// <summary>The id of a user registered on this computer, or null.</summary>
        public static int? FindUser(string userName)
        // פעולה: FindUser — מחפשת משתמש שרשום במחשב הזה לפי השם, ומחזירה את המספר שלו (או null).
        {
            using var connection = Open();
            return FindUser(connection, userName);
        }

        public static List<string> GetFriends(int userId)
        // פעולה: GetFriends — מחזירה את רשימת החברים של המשתמש, לפי הסדר שבו הוסיף אותם.
        {
            var friends = new List<string>();
            // אובייקט: friends — רשימת שמות החברים.
            using var connection = Open();
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT FriendName FROM Friends WHERE UserId = $id ORDER BY AddedAt";
            command.Parameters.AddWithValue("$id", userId);
            using var reader = command.ExecuteReader();
            while (reader.Read())
                friends.Add(reader.GetString(0));
            return friends;
        }

        public static void AddFriend(int userId, string friendName)
        // פעולה: AddFriend — מוסיפה חבר לרשימה (אם הוא כבר שם — לא עושה כלום).
        {
            using var connection = Open();
            using var command = connection.CreateCommand();
            command.CommandText = "INSERT OR IGNORE INTO Friends (UserId, FriendName, AddedAt) VALUES ($id, $name, $at)";
            command.Parameters.AddWithValue("$id", userId);
            command.Parameters.AddWithValue("$name", friendName.Trim());
            command.Parameters.AddWithValue("$at", DateTime.UtcNow.ToString("o"));
            command.ExecuteNonQuery();
        }

        public static void RemoveFriend(int userId, string friendName)
        // פעולה: RemoveFriend — מוחקת חבר מהרשימה.
        {
            using var connection = Open();
            using var command = connection.CreateCommand();
            command.CommandText = "DELETE FROM Friends WHERE UserId = $id AND FriendName = $name";
            command.Parameters.AddWithValue("$id", userId);
            command.Parameters.AddWithValue("$name", friendName);
            command.ExecuteNonQuery();
        }

        /// <summary>Adds coins to another user on this computer (a gift). Returns false if there's no such user.</summary>
        public static bool GiveCoins(string userName, int amount)
        // פעולה: GiveCoins — נותנת מטבעות במתנה למשתמש אחר במחשב. מחזירה false אם אין משתמש כזה.
        {
            using var connection = Open();
            using var command = connection.CreateCommand();
            command.CommandText = "UPDATE Users SET Coins = Coins + $amount WHERE UserName = $name AND IsGuest = 0";
            command.Parameters.AddWithValue("$amount", amount);
            command.Parameters.AddWithValue("$name", userName.Trim());
            return command.ExecuteNonQuery() == 1;
        }

        /// <summary>One preference of any user (for example their profile picture frame), or null.</summary>
        public static string? GetSetting(int userId, string key)
        // פעולה: GetSetting — קוראת העדפה אחת של משתמש כלשהו מהטבלה Preferences (למשל מסגרת התמונה שלו), או null.
        {
            string? column = null;
            // אובייקט: column — העמודה בטבלה Preferences שבה נשמרת ההעדפה הזאת.
            foreach (var preference in PreferenceColumns)
            {
                if (preference.Key == key)
                    column = preference.Column;
            }
            if (column == null)
                return null;

            using var connection = Open();
            using var command = connection.CreateCommand();
            command.CommandText = $"SELECT {column} FROM Preferences WHERE UserId = $id";
            command.Parameters.AddWithValue("$id", userId);
            object? value = command.ExecuteScalar();
            // אובייקט: value — הערך שנמצא בעמודה (או כלום, אם עוד לא נשמר).
            return value == null || value is DBNull ? null : Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture);
        }

        /// <summary>Lets go of open database files (needed before restoring a backup over them).</summary>
        public static void CloseAll() => SqliteConnection.ClearAllPools();
        // פעולה: CloseAll — משחררת את קבצי מסד הנתונים הפתוחים (צריך לפני שמשחזרים גיבוי).

        public static string FilePath => Path.Combine(ApplicationData.Current.LocalFolder.Path, "smartsudoku.db");
        // אובייקט: FilePath — המיקום המלא של קובץ מסד הנתונים.

        // ---------- Profile pictures ----------

        /// <summary>Saves (or replaces) a user's profile picture. The image is already small (see Avatars).</summary>
        public static void SetAvatar(int userId, byte[] image)
        // פעולה: SetAvatar — שומרת (או מחליפה) את תמונת הפרופיל של משתמש.
        {
            using var connection = Open();
            using var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT OR REPLACE INTO Avatars (UserId, Image, UpdatedAt)
                VALUES ($id, $image, $updated)";
            command.Parameters.AddWithValue("$id", userId);
            command.Parameters.AddWithValue("$image", image);
            command.Parameters.AddWithValue("$updated", DateTime.UtcNow.ToString("o"));
            command.ExecuteNonQuery();
        }

        /// <summary>A user's profile picture, or null if they haven't chosen one.</summary>
        public static byte[]? GetAvatar(int userId)
        // פעולה: GetAvatar — מחזירה את תמונת הפרופיל של משתמש, או null אם אין לו.
        {
            using var connection = Open();
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT Image FROM Avatars WHERE UserId = $id";
            command.Parameters.AddWithValue("$id", userId);
            return command.ExecuteScalar() as byte[];
        }

        public static void RemoveAvatar(int userId)
        // פעולה: RemoveAvatar — מוחקת את תמונת הפרופיל של משתמש.
        {
            using var connection = Open();
            using var command = connection.CreateCommand();
            command.CommandText = "DELETE FROM Avatars WHERE UserId = $id";
            command.Parameters.AddWithValue("$id", userId);
            command.ExecuteNonQuery();
        }

        // ---------- Leaderboard ----------

        /// <summary>
        /// Every registered user with their totals: levels solved, stars, coins and their fastest level.
        /// Sorted by stars, then levels, then coins.
        /// </summary>
        public static List<LeaderboardRow> GetLeaderboard()
        // פעולה: GetLeaderboard — מחזירה את כל המשתמשים הרשומים עם הסיכומים שלהם (שלבים, כוכבים, מטבעות, זמן הכי מהיר),
        // מסודרים לפי כוכבים, אחר כך לפי שלבים, ואחר כך לפי מטבעות.
        {
            var rows = new List<LeaderboardRow>();
            // אובייקט: rows — השורות של טבלת השיאים.
            using var connection = Open();
            using var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT u.Id, u.UserName, u.Coins,
                       COUNT(r.LevelId)                       AS Solved,
                       COALESCE(SUM(r.Stars), 0)               AS Stars,
                       COALESCE(MIN(NULLIF(r.BestTime, 0)), 0) AS Fastest,
                       EXISTS (SELECT 1 FROM Avatars a WHERE a.UserId = u.Id) AS HasAvatar
                FROM Users u
                LEFT JOIN LevelResults r ON r.UserId = u.Id
                WHERE u.IsGuest = 0
                GROUP BY u.Id
                ORDER BY Stars DESC, Solved DESC, u.Coins DESC";
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                rows.Add(new LeaderboardRow(
                    UserId: reader.GetInt32(0),
                    Name: reader.GetString(1),
                    Coins: reader.GetInt32(2),
                    Solved: reader.GetInt32(3),
                    Stars: reader.GetInt32(4),
                    FastestSeconds: reader.GetInt32(5),
                    HasAvatar: reader.GetInt32(6) == 1));
            }
            return rows;
        }

        /// <summary>
        /// Turns a password into a "hash": a code that can't be turned back into the password.
        /// The salt is a random value per user, so two users with the same password get different hashes.
        /// </summary>
        private static byte[] HashPassword(string password, byte[] salt)
        // פעולה: HashPassword — מערבלת סיסמה עם המלח (בשיטת PBKDF2), כך שאי אפשר להחזיר אותה לסיסמה המקורית.
        {
            return Rfc2898DeriveBytes.Pbkdf2(password, salt, HashIterations, HashAlgorithmName.SHA256, 32);
        }

        /// <summary>
        /// Checks a typed password against the saved hash and salt (both saved as Base64 text).
        /// Used for signing in, changing the password and the secret recovery word.
        /// </summary>
        private static bool PasswordMatches(string password, string savedHash, string savedSalt)
        // פעולה: PasswordMatches — בודקת אם הסיסמה שהוקלדה מתאימה לסיסמה המעורבלת השמורה.
        {
            byte[] expected = Convert.FromBase64String(savedHash);
            // אובייקט: expected — הסיסמה המעורבלת ששמורה במסד הנתונים.
            byte[] actual = HashPassword(password, Convert.FromBase64String(savedSalt));
            // אובייקט: actual — הסיסמה שהוקלדה, אחרי ערבול עם אותו מלח.
            // Compares every byte even after a difference, so the time taken gives nothing away
            return CryptographicOperations.FixedTimeEquals(expected, actual);
        }

        private static SqliteConnection Open()
        // פעולה: Open — פותחת חיבור חדש למסד הנתונים (ומפעילה את הבדיקה של קשרים בין טבלאות).
        {
            if (connectionString == null)
                throw new InvalidOperationException("Database.Init was not called");

            var connection = new SqliteConnection(connectionString);
            // אובייקט: connection — החיבור החדש.
            connection.Open();
            Execute(connection, "PRAGMA foreign_keys = ON;");
            return connection;
        }

        private static void Execute(SqliteConnection connection, string sql)
        // פעולה: Execute — מריצה פקודת SQL שלא מחזירה תוצאות.
        {
            using var command = connection.CreateCommand();
            // אובייקט: command — הפקודה שנשלחת למסד הנתונים.
            command.CommandText = sql;
            command.ExecuteNonQuery();
        }
    }
}
