using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

// Also used by the Smart Sudoku app (linked into it), so both sides always agree on the messages.
namespace Smart_Sudoku.Duel
{
    /// <summary>Names of the commands in the client-server protocol (see the table in Server.cs).</summary>
    public static class Commands
    {
        // Client -> Server
        public const string Connected = "Connected";
        public const string Register = "Register";
        public const string StartGame = "StartGame";
        public const string ChangePlayer = "ChangePlayer";

        public const string GetLeaderboard = "GetLeaderboard";
        public const string Emoji = "Emoji";
        public const string Chat = "Chat";
        public const string GetOnline = "GetOnline";
        public const string Invite = "Invite";

        // Server -> clients
        public const string UpdatePlayers = "UpdatePlayers";
        public const string GameOver = "GameOver";
        public const string Leaderboard = "Leaderboard";
        public const string Online = "Online";
        // StartGame is also sent from the server to everyone in the room, with the board they play;
        // Emoji and Chat are passed on to everyone in the room; Invite is passed on to the invited player
    }

    /// <summary>StartGame's data from a client: the difficulty, and whether to play in two teams.</summary>
    public sealed class StartOptions
    {
        public int Difficulty { get; set; }
        public bool Teams { get; set; }
    }

    /// <summary>A chat line in the duel room.</summary>
    public sealed class ChatMessage
    {
        public string Name { get; set; } = "";
        public string Text { get; set; } = "";
    }

    /// <summary>Someone connected to the server right now (for the friends list).</summary>
    public sealed class OnlinePlayer
    {
        public string Name { get; set; } = "";
        public string Room { get; set; } = "";
        public string Status { get; set; } = "";
    }

    /// <summary>An invitation to join someone's room.</summary>
    public sealed class InviteMessage
    {
        public string From { get; set; } = "";
        public string To { get; set; } = "";
        public string Room { get; set; } = "";
    }

    public static class Teams
    {
        public const string Red = "red";
        public const string Blue = "blue";

        public static string Name(string team) => team == Red ? "הקבוצה האדומה 🔴" : team == Blue ? "הקבוצה הכחולה 🔵" : "";
    }

    /// <summary>Register's data: the player's name and the room they join (players in different rooms never meet).</summary>
    public sealed class RegisterInfo
    {
        public string Name { get; set; } = "";
        public string Room { get; set; } = "";
    }

    /// <summary>A quick reaction sent during a duel.</summary>
    public sealed class EmojiMessage
    {
        public string Name { get; set; } = "";
        public string Emoji { get; set; } = "";
    }

    /// <summary>One row of the champions table, from the server's database.</summary>
    public sealed class LeaderboardEntry
    {
        public string Name { get; set; } = "";
        public int Wins { get; set; }
        public int GamesPlayed { get; set; }
        public int BestScore { get; set; }
    }

    /// <summary>One message on the wire: a command name and its data as JSON text.</summary>
    public sealed class Message
    {
        public string Command { get; set; } = "";
        public string Data { get; set; } = "";
    }

    /// <summary>The board for a duel. Everyone gets the same one. Cells are row by row (81 numbers, 0 = empty).</summary>
    public sealed class DuelGame
    {
        public int Difficulty { get; set; }
        public bool Teams { get; set; }
        public int[] Puzzle { get; set; } = new int[81];
        public int[] Solution { get; set; } = new int[81];
    }

    public sealed class GameResult
    {
        public string Winner { get; set; } = "";
        public List<Player> Players { get; set; } = new List<Player>();

        /// <summary>In a team duel: the winning team (Teams.Red or Teams.Blue) and each team's total.</summary>
        public string WinnerTeam { get; set; } = "";
        public int RedScore { get; set; }
        public int BlueScore { get; set; }
    }

    /// <summary>Points in a duel.</summary>
    public static class DuelScoring
    {
        public const int CorrectCell = 10;
        public const int Mistake = 5;

        /// <summary>Bonus for finishing the board, by finishing place (1st, 2nd, 3rd, then the rest).</summary>
        public static int FinishBonus(int place) => place switch
        {
            1 => 100,
            2 => 60,
            3 => 30,
            _ => 10,
        };
    }

    /// <summary>JSON for the protocol. Source-generated, so it also works in the app's trimmed builds.</summary>
    [JsonSerializable(typeof(Message))]
    [JsonSerializable(typeof(Player))]
    [JsonSerializable(typeof(List<Player>))]
    [JsonSerializable(typeof(DuelGame))]
    [JsonSerializable(typeof(GameResult))]
    [JsonSerializable(typeof(string))]
    [JsonSerializable(typeof(int))]
    [JsonSerializable(typeof(RegisterInfo))]
    [JsonSerializable(typeof(EmojiMessage))]
    [JsonSerializable(typeof(List<LeaderboardEntry>))]
    [JsonSerializable(typeof(StartOptions))]
    [JsonSerializable(typeof(ChatMessage))]
    [JsonSerializable(typeof(List<OnlinePlayer>))]
    [JsonSerializable(typeof(InviteMessage))]
    public partial class DuelJson : JsonSerializerContext
    {
        public static string Pack<T>(string command, T data, System.Text.Json.Serialization.Metadata.JsonTypeInfo<T> type) =>
            JsonSerializer.Serialize(new Message { Command = command, Data = JsonSerializer.Serialize(data, type) }, Default.Message);

        public static Message? Unpack(string text)
        {
            try
            {
                return JsonSerializer.Deserialize(text, Default.Message);
            }
            catch (JsonException)
            {
                return null;
            }
        }
    }
}
