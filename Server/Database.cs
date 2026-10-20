using Microsoft.Data.Sqlite;
using Smart_Sudoku.Duel;

namespace Server
{
    /// <summary>
    /// The server's SQLite database (database.db): every finished duel, and each player's record
    /// (games played, wins, best score).
    /// </summary>
    public class Database
    {
        private readonly string _connectionString;

        public Database(string path)
        {
            _connectionString = new SqliteConnectionStringBuilder { DataSource = path }.ToString();

            using var connection = Open();
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
        {
            using var connection = Open();
            using var transaction = connection.BeginTransaction();

            using (var game = connection.CreateCommand())
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
        {
            var top = new List<LeaderboardEntry>();
            using var connection = Open();
            using var query = connection.CreateCommand();
            query.CommandText = "SELECT Name, Wins, GamesPlayed, BestScore FROM Players ORDER BY Wins DESC, BestScore DESC LIMIT $count";
            query.Parameters.AddWithValue("$count", count);
            using var reader = query.ExecuteReader();
            while (reader.Read())
            {
                top.Add(new LeaderboardEntry
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
        {
            var connection = new SqliteConnection(_connectionString);
            connection.Open();
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
