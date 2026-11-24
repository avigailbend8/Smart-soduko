using Microsoft.Data.Sqlite;
using Smart_Sudoku.Duel;

namespace Server
{
    /// <summary>
    /// The server's SQLite database (database.db): every finished duel, and each player's record
    /// (games played, wins, best score).
    /// </summary>
    public class Database
    // אובייקט: Database — מסד הנתונים של השרת. שומר כל דו-קרב שהסתיים ואת ההישגים של כל שחקן (משחקים, ניצחונות, שיא).
    {
        private readonly string _connectionString;
        // אובייקט: _connectionString — "הכתובת" של קובץ מסד הנתונים, שבעזרתה פותחים אליו חיבור.

        public Database(string path)
        // פעולה: Database (בנאי) — נקראת כשיוצרים את מסד הנתונים. שומרת איפה הקובץ נמצא,
        // ויוצרת את שתי הטבלאות (שחקנים ומשחקים) אם הן עוד לא קיימות.
        {
            _connectionString = new SqliteConnectionStringBuilder { DataSource = path }.ToString();

            using var connection = Open();
            // אובייקט: connection — חיבור פתוח לקובץ מסד הנתונים. בזכות using הוא נסגר לבד בסוף.
            Execute(connection, @"
                CREATE TABLE IF NOT EXISTS Players (
                    Name        TEXT    PRIMARY KEY,
                    GamesPlayed INTEGER NOT NULL DEFAULT 0,
                    Wins        INTEGER NOT NULL DEFAULT 0,
                    BestScore   INTEGER NOT NULL DEFAULT 0
                );
                CREATE TABLE IF NOT EXISTS Games (
                    Id          INTEGER PRIMARY KEY AUTOINCREMENT,
                    PlayedAt    TEXT    NOT NULL,
                    Winner      TEXT    NOT NULL,
                    PlayerCount INTEGER NOT NULL
                );");
        }

        /// <summary>Saves a finished duel and updates every player's record.</summary>
        public void SaveGame(GameResult result)
        // פעולה: SaveGame — שומרת דו-קרב שהסתיים: מוסיפה שורה לטבלת המשחקים,
        // ומעדכנת לכל שחקן כמה משחקים שיחק, כמה ניצח ומה השיא שלו.
        {
            using var connection = Open();
            // אובייקט: connection — חיבור למסד הנתונים לצורך השמירה.
            using var transaction = connection.BeginTransaction();
            // אובייקט: transaction — "חבילה" של שינויים: או שכולם נשמרים ביחד, או שאף אחד לא נשמר (כדי שלא יישאר מידע חצי שמור).

            using (var game = connection.CreateCommand())
            // אובייקט: game — פקודת SQL שמוסיפה את המשחק לטבלת המשחקים.
            {
                game.CommandText = "INSERT INTO Games (PlayedAt, Winner, PlayerCount) VALUES ($at, $winner, $count)";
                game.Parameters.AddWithValue("$at", DateTime.Now.ToString("yyyy-MM-dd HH:mm"));
                game.Parameters.AddWithValue("$winner", result.Winner);
                game.Parameters.AddWithValue("$count", result.Players.Count);
                game.ExecuteNonQuery();
            }

            foreach (var player in result.Players)
            {
                using var update = connection.CreateCommand();
                // אובייקט: update — פקודת SQL שמוסיפה שחקן חדש, או מעדכנת שחקן שכבר קיים.
                update.CommandText = @"
                    INSERT INTO Players (Name, GamesPlayed, Wins, BestScore) VALUES ($name, 1, $win, $score)
                    ON CONFLICT(Name) DO UPDATE SET
                        GamesPlayed = GamesPlayed + 1,
                        Wins        = Wins + $win,
                        BestScore   = MAX(BestScore, $score)";
                update.Parameters.AddWithValue("$name", player.Name);
                update.Parameters.AddWithValue("$win", player.Name == result.Winner ? 1 : 0);
                update.Parameters.AddWithValue("$score", player.Score);
                update.ExecuteNonQuery();
            }

            transaction.Commit();
        }

        /// <summary>The players with the most wins: the champions table.</summary>
        public List<LeaderboardEntry> TopPlayers(int count)
        // פעולה: TopPlayers — מחזירה את השחקנים עם הכי הרבה ניצחונות (טבלת האלופים). count = כמה שחקנים להחזיר.
        {
            var top = new List<LeaderboardEntry>();
            // אובייקט: top — הרשימה שתתמלא בשחקנים המובילים ותוחזר בסוף.
            using var connection = Open();
            using var query = connection.CreateCommand();
            // אובייקט: query — פקודת SQL שבוחרת את השחקנים לפי מספר הניצחונות, מהגדול לקטן.
            query.CommandText = "SELECT Name, Wins, GamesPlayed, BestScore FROM Players ORDER BY Wins DESC, BestScore DESC LIMIT $count";
            query.Parameters.AddWithValue("$count", count);
            using var reader = query.ExecuteReader();
            // אובייקט: reader — "קורא" שעובר על התוצאות שורה אחרי שורה.
            while (reader.Read())
            {
                top.Add(new LeaderboardEntry
                // אובייקט: LeaderboardEntry — שורה אחת בטבלת האלופים: שם, ניצחונות, משחקים ושיא.
                {
                    Name = reader.GetString(0),
                    Wins = reader.GetInt32(1),
                    GamesPlayed = reader.GetInt32(2),
                    BestScore = reader.GetInt32(3),
                });
            }
            return top;
        }

        private SqliteConnection Open()
        // פעולה: Open — פותחת חיבור חדש למסד הנתונים ומחזירה אותו.
        {
            var connection = new SqliteConnection(_connectionString);
            // אובייקט: connection — החיבור החדש למסד הנתונים.
            connection.Open();
            return connection;
        }

        private static void Execute(SqliteConnection connection, string sql)
        // פעולה: Execute — מריצה פקודת SQL שלא מחזירה תוצאות (למשל יצירת טבלה).
        {
            using var command = connection.CreateCommand();
            // אובייקט: command — הפקודה שנשלחת למסד הנתונים.
            command.CommandText = sql;
            command.ExecuteNonQuery();
        }
    }
}
