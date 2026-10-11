using System;
using System.IO;
using System.Security.Cryptography;
using Microsoft.Data.Sqlite;
using Windows.Storage;

namespace Smart_Sudoku
{
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
                return null;

            byte[] expected = Convert.FromBase64String(reader.GetString(1));
            byte[] actual = HashPassword(password, Convert.FromBase64String(reader.GetString(2)));
            return CryptographicOperations.FixedTimeEquals(expected, actual) ? reader.GetInt32(0) : null;
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

            transaction.Commit();
        }

        private static byte[] HashPassword(string password, byte[] salt) =>
            Rfc2898DeriveBytes.Pbkdf2(password, salt, HashIterations, HashAlgorithmName.SHA256, 32);

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
