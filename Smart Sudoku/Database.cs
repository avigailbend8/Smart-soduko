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

    /// <summary>
    /// SQLite database (smartsudoku.db in the app's local folder) with the registered
    /// users and their progress. Passwords are stored only as salted PBKDF2 hashes.
    /// </summary>
    public static class Database
    {
        private const int HashIterations = 100_000;

        private static string? connectionString;

        public static bool IsReady => connectionString != null;

        public static void Init()
        {
            SQLitePCL.Batteries_V2.Init();

            string path = Path.Combine(ApplicationData.Current.LocalFolder.Path, "smartsudoku.db");
            connectionString = new SqliteConnectionStringBuilder { DataSource = path }.ToString();

            using var connection = Open();
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
                CREATE TABLE IF NOT EXISTS Purchases (
                    UserId INTEGER NOT NULL REFERENCES Users(Id) ON DELETE CASCADE,
                    ItemId TEXT    NOT NULL,
                    PRIMARY KEY (UserId, ItemId)
                );
                CREATE TABLE IF NOT EXISTS UserSettings (
                    UserId INTEGER NOT NULL REFERENCES Users(Id) ON DELETE CASCADE,
                    Key    TEXT    NOT NULL,
                    Value  TEXT    NOT NULL,
                    PRIMARY KEY (UserId, Key)
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
        }

        /// <summary>Creates a user. Returns the new user's id, or null with an error message.</summary>
        public static int? Register(string userName, string password, PlayerProgress startingProgress, out string error)
        {
            using var connection = Open();

            using (var check = connection.CreateCommand())
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
            byte[] hash = HashPassword(password, salt);

            int userId;
            using (var insert = connection.CreateCommand())
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
        {
            using var connection = Open();
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT Id, PasswordHash, Salt FROM Users WHERE UserName = $name";
            command.Parameters.AddWithValue("$name", userName);

            using var reader = command.ExecuteReader();
            if (!reader.Read())
                return null;   // no such user

            if (!PasswordMatches(password, reader.GetString(1), reader.GetString(2)))
                return null;   // wrong password
            return reader.GetInt32(0);
        }

        /// <summary>The user name as it was registered (keeps the original spelling).</summary>
        public static string GetUserName(int userId)
        {
            using var connection = Open();
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT UserName FROM Users WHERE Id = $id";
            command.Parameters.AddWithValue("$id", userId);
            return (string?)command.ExecuteScalar() ?? "";
        }

        public static PlayerProgress LoadProgress(int userId)
        {
            var progress = new PlayerProgress();
            using var connection = Open();

            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT Coins, BonusHints, BonusLives, Theme, Symbols, SoundOn FROM Users WHERE Id = $id";
                command.Parameters.AddWithValue("$id", userId);
                using var reader = command.ExecuteReader();
                if (reader.Read())
                {
                    progress.Coins = reader.GetInt32(0);
                    progress.BonusHints = reader.GetInt32(1);
                    progress.BonusLives = reader.GetInt32(2);
                    progress.Theme = reader.GetString(3);
                    progress.Symbols = reader.GetString(4);
                    progress.SoundOn = reader.GetInt32(5) != 0;
                }
            }

            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT LevelId, Stars, BestTime FROM LevelResults WHERE UserId = $id";
                command.Parameters.AddWithValue("$id", userId);
                using var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    progress.Stars[reader.GetInt32(0)] = reader.GetInt32(1);
                    progress.BestTimes[reader.GetInt32(0)] = reader.GetInt32(2);
                }
            }

            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT Difficulty, Level FROM UnlockedLevels WHERE UserId = $id";
                command.Parameters.AddWithValue("$id", userId);
                using var reader = command.ExecuteReader();
                while (reader.Read())
                    progress.Unlocked[reader.GetInt32(0)] = reader.GetInt32(1);
            }

            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT ItemId FROM Purchases WHERE UserId = $id";
                command.Parameters.AddWithValue("$id", userId);
                using var reader = command.ExecuteReader();
                while (reader.Read())
                    progress.Owned.Add(reader.GetString(0));
            }

            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT Key, Value FROM UserSettings WHERE UserId = $id";
                command.Parameters.AddWithValue("$id", userId);
                using var reader = command.ExecuteReader();
                while (reader.Read())
                    progress.Extras[reader.GetString(0)] = reader.GetString(1);
            }

            return progress;
        }

        public static void SaveProgress(int userId, PlayerProgress progress)
        {
            using var connection = Open();
            using var transaction = connection.BeginTransaction();

            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"
                    UPDATE Users SET Coins = $coins, BonusHints = $hints, BonusLives = $lives,
                                     Theme = $theme, Symbols = $symbols, SoundOn = $sound
                    WHERE Id = $id";
                command.Parameters.AddWithValue("$coins", progress.Coins);
                command.Parameters.AddWithValue("$hints", progress.BonusHints);
                command.Parameters.AddWithValue("$lives", progress.BonusLives);
                command.Parameters.AddWithValue("$theme", progress.Theme);
                command.Parameters.AddWithValue("$symbols", progress.Symbols);
                command.Parameters.AddWithValue("$sound", progress.SoundOn ? 1 : 0);
                command.Parameters.AddWithValue("$id", userId);
                command.ExecuteNonQuery();
            }

            foreach (var level in progress.Stars)
            {
                using var command = connection.CreateCommand();
                command.CommandText = @"
                    INSERT OR REPLACE INTO LevelResults (UserId, LevelId, Stars, BestTime)
                    VALUES ($id, $level, $stars, $time)";
                command.Parameters.AddWithValue("$id", userId);
                command.Parameters.AddWithValue("$level", level.Key);
                command.Parameters.AddWithValue("$stars", level.Value);
                command.Parameters.AddWithValue("$time", progress.BestTimes.TryGetValue(level.Key, out int t) ? t : 0);
                command.ExecuteNonQuery();
            }

            foreach (var unlocked in progress.Unlocked)
            {
                using var command = connection.CreateCommand();
                command.CommandText = @"
                    INSERT OR REPLACE INTO UnlockedLevels (UserId, Difficulty, Level)
                    VALUES ($id, $difficulty, $level)";
                command.Parameters.AddWithValue("$id", userId);
                command.Parameters.AddWithValue("$difficulty", unlocked.Key);
                command.Parameters.AddWithValue("$level", unlocked.Value);
                command.ExecuteNonQuery();
            }

            foreach (string item in progress.Owned)
            {
                using var command = connection.CreateCommand();
                command.CommandText = "INSERT OR IGNORE INTO Purchases (UserId, ItemId) VALUES ($id, $item)";
                command.Parameters.AddWithValue("$id", userId);
                command.Parameters.AddWithValue("$item", item);
                command.ExecuteNonQuery();
            }

            using (var clear = connection.CreateCommand())
            {
                clear.CommandText = "DELETE FROM UserSettings WHERE UserId = $id";
                clear.Parameters.AddWithValue("$id", userId);
                clear.ExecuteNonQuery();
            }
            foreach (var setting in progress.Extras)
            {
                using var command = connection.CreateCommand();
                command.CommandText = "INSERT INTO UserSettings (UserId, Key, Value) VALUES ($id, $key, $value)";
                command.Parameters.AddWithValue("$id", userId);
                command.Parameters.AddWithValue("$key", setting.Key);
                command.Parameters.AddWithValue("$value", setting.Value);
                command.ExecuteNonQuery();
            }

            transaction.Commit();
        }

        // ---------- Passwords ----------

        private static bool CheckPassword(SqliteConnection connection, int userId, string password)
        {
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT PasswordHash, Salt FROM Users WHERE Id = $id";
            command.Parameters.AddWithValue("$id", userId);
            using var reader = command.ExecuteReader();
            if (!reader.Read())
                return false;
            return PasswordMatches(password, reader.GetString(0), reader.GetString(1));
        }

        private static void SetPassword(SqliteConnection connection, int userId, string newPassword)
        {
            byte[] salt = RandomNumberGenerator.GetBytes(16);
            using var command = connection.CreateCommand();
            command.CommandText = "UPDATE Users SET PasswordHash = $hash, Salt = $salt WHERE Id = $id";
            command.Parameters.AddWithValue("$hash", Convert.ToBase64String(HashPassword(newPassword, salt)));
            command.Parameters.AddWithValue("$salt", Convert.ToBase64String(salt));
            command.Parameters.AddWithValue("$id", userId);
            command.ExecuteNonQuery();
        }

        /// <summary>Changes the password, if the old one is right.</summary>
        public static bool ChangePassword(int userId, string oldPassword, string newPassword)
        {
            using var connection = Open();
            if (!CheckPassword(connection, userId, oldPassword))
                return false;
            SetPassword(connection, userId, newPassword);
            return true;
        }

        /// <summary>Saves a secret recovery word (hashed, like a password) for "forgot password".</summary>
        public static void SetRecoveryWord(int userId, string word)
        {
            byte[] salt = RandomNumberGenerator.GetBytes(16);
            using var connection = Open();
            using var command = connection.CreateCommand();
            command.CommandText = "INSERT OR REPLACE INTO Recovery (UserId, Hash, Salt) VALUES ($id, $hash, $salt)";
            command.Parameters.AddWithValue("$id", userId);
            command.Parameters.AddWithValue("$hash", Convert.ToBase64String(HashPassword(word.Trim(), salt)));
            command.Parameters.AddWithValue("$salt", Convert.ToBase64String(salt));
            command.ExecuteNonQuery();
        }

        public static bool HasRecoveryWord(int userId)
        {
            using var connection = Open();
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT 1 FROM Recovery WHERE UserId = $id";
            command.Parameters.AddWithValue("$id", userId);
            return command.ExecuteScalar() != null;
        }

        /// <summary>"Forgot password": sets a new password if the recovery word is right.</summary>
        public static bool ResetPassword(string userName, string recoveryWord, string newPassword)
        {
            using var connection = Open();
            int? userId = FindUser(connection, userName);
            if (userId == null)
                return false;
            int id = userId.Value;

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
        {
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT Id FROM Users WHERE UserName = $name";
            command.Parameters.AddWithValue("$name", userName.Trim());
            object? result = command.ExecuteScalar();
            if (result == null)
                return null;   // no user with that name
            return Convert.ToInt32(result);
        }

        /// <summary>The id of a user registered on this computer, or null.</summary>
        public static int? FindUser(string userName)
        {
            using var connection = Open();
            return FindUser(connection, userName);
        }

        public static List<string> GetFriends(int userId)
        {
            var friends = new List<string>();
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
        {
            using var connection = Open();
            using var command = connection.CreateCommand();
            command.CommandText = "UPDATE Users SET Coins = Coins + $amount WHERE UserName = $name";
            command.Parameters.AddWithValue("$amount", amount);
            command.Parameters.AddWithValue("$name", userName.Trim());
            return command.ExecuteNonQuery() == 1;
        }

        /// <summary>One saved setting of any user (for example their profile picture frame), or null.</summary>
        public static string? GetSetting(int userId, string key)
        {
            using var connection = Open();
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT Value FROM UserSettings WHERE UserId = $id AND Key = $key";
            command.Parameters.AddWithValue("$id", userId);
            command.Parameters.AddWithValue("$key", key);
            return command.ExecuteScalar() as string;
        }

        /// <summary>Lets go of open database files (needed before restoring a backup over them).</summary>
        public static void CloseAll() => SqliteConnection.ClearAllPools();

        public static string FilePath => Path.Combine(ApplicationData.Current.LocalFolder.Path, "smartsudoku.db");

        // ---------- Profile pictures ----------

        /// <summary>Saves (or replaces) a user's profile picture. The image is already small (see Avatars).</summary>
        public static void SetAvatar(int userId, byte[] image)
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
        {
            using var connection = Open();
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT Image FROM Avatars WHERE UserId = $id";
            command.Parameters.AddWithValue("$id", userId);
            return command.ExecuteScalar() as byte[];
        }

        public static void RemoveAvatar(int userId)
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
        {
            var rows = new List<LeaderboardRow>();
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
        {
            return Rfc2898DeriveBytes.Pbkdf2(password, salt, HashIterations, HashAlgorithmName.SHA256, 32);
        }

        /// <summary>
        /// Checks a typed password against the saved hash and salt (both saved as Base64 text).
        /// Used for signing in, changing the password and the secret recovery word.
        /// </summary>
        private static bool PasswordMatches(string password, string savedHash, string savedSalt)
        {
            byte[] expected = Convert.FromBase64String(savedHash);
            byte[] actual = HashPassword(password, Convert.FromBase64String(savedSalt));
            // Compares every byte even after a difference, so the time taken gives nothing away
            return CryptographicOperations.FixedTimeEquals(expected, actual);
        }

        private static SqliteConnection Open()
        {
            if (connectionString == null)
                throw new InvalidOperationException("Database.Init was not called");

            var connection = new SqliteConnection(connectionString);
            connection.Open();
            Execute(connection, "PRAGMA foreign_keys = ON;");
            return connection;
        }

        private static void Execute(SqliteConnection connection, string sql)
        {
            using var command = connection.CreateCommand();
            command.CommandText = sql;
            command.ExecuteNonQuery();
        }
    }
}
