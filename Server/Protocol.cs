using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

// Also used by the Smart Sudoku app (linked into it), so both sides always agree on the messages.
namespace Smart_Sudoku.Duel
{
    /// <summary>Names of the commands in the client-server protocol (see the table in Server.cs).</summary>
    public static class Commands
    // אובייקט: Commands — רשימת ה"מילים" (הפקודות) שהמשחק והשרת שולחים זה לזה. שני הצדדים חייבים להשתמש באותן מילים.
    {
        // Client -> Server
        public const string Connected = "Connected";
        // אובייקט: Connected — "התחברתי": נשלח כשהחיבור לשרת מצליח.
        public const string Register = "Register";
        // אובייקט: Register — "אני נכנס/ת לחדר": שם השחקן והחדר שאליו הוא מצטרף.
        public const string StartGame = "StartGame";
        // אובייקט: StartGame — "בואו נתחיל": מבקש מהשרת להתחיל דו-קרב. השרת שולח אותו בחזרה עם הלוח.
        public const string ChangePlayer = "ChangePlayer";
        // אובייקט: ChangePlayer — "המצב שלי השתנה": ניקוד או סטטוס חדש של שחקן.

        public const string GetLeaderboard = "GetLeaderboard";
        // אובייקט: GetLeaderboard — בקשה לקבל את טבלת האלופים.
        public const string Emoji = "Emoji";
        // אובייקט: Emoji — שליחת אימוג'י מהיר לשאר השחקנים בחדר.
        public const string Chat = "Chat";
        // אובייקט: Chat — שליחת הודעת צ'אט לחדר.
        public const string GetOnline = "GetOnline";
        // אובייקט: GetOnline — בקשה לדעת מי מחובר עכשיו (לרשימת החברים).
        public const string Invite = "Invite";
        // אובייקט: Invite — הזמנה של חבר להצטרף לחדר.

        // Server -> clients
        public const string UpdatePlayers = "UpdatePlayers";
        // אובייקט: UpdatePlayers — השרת שולח לכולם את רשימת השחקנים המעודכנת.
        public const string GameOver = "GameOver";
        // אובייקט: GameOver — השרת מודיע שהדו-קרב נגמר ומי ניצח.
        public const string Leaderboard = "Leaderboard";
        // אובייקט: Leaderboard — השרת שולח את טבלת האלופים.
        public const string Online = "Online";
        // אובייקט: Online — השרת שולח את רשימת המחוברים.
        // StartGame is also sent from the server to everyone in the room, with the board they play;
        // Emoji and Chat are passed on to everyone in the room; Invite is passed on to the invited player
    }

    /// <summary>StartGame's data from a client: the difficulty, and whether to play in two teams.</summary>
    public sealed class StartOptions
    // אובייקט: StartOptions — ההגדרות של דו-קרב חדש: רמת הקושי, והאם משחקים בקבוצות.
    {
        public int Difficulty { get; set; }
        // אובייקט: Difficulty — רמת הקושי של הלוח (כמה משבצות ריקות).
        public bool Teams { get; set; }
        // אובייקט: Teams — אמת אם משחקים שתי קבוצות (אדומה נגד כחולה).
    }

    /// <summary>A chat line in the duel room.</summary>
    public sealed class ChatMessage
    // אובייקט: ChatMessage — הודעת צ'אט אחת: מי כתב ומה כתב.
    {
        public string Name { get; set; } = "";
        // אובייקט: Name — השם של מי ששלח את ההודעה.
        public string Text { get; set; } = "";
        // אובייקט: Text — תוכן ההודעה.
    }

    /// <summary>Someone connected to the server right now (for the friends list).</summary>
    public sealed class OnlinePlayer
    // אובייקט: OnlinePlayer — שחקן שמחובר לשרת עכשיו: השם שלו, החדר שלו והמצב שלו.
    {
        public string Name { get; set; } = "";
        // אובייקט: Name — שם השחקן המחובר.
        public string Room { get; set; } = "";
        // אובייקט: Room — שם החדר שבו הוא נמצא.
        public string Status { get; set; } = "";
        // אובייקט: Status — מה הוא עושה עכשיו (מחכה או משחק).
    }

    /// <summary>An invitation to join someone's room.</summary>
    public sealed class InviteMessage
    // אובייקט: InviteMessage — הזמנה לחדר: ממי, למי, ולאיזה חדר.
    {
        public string From { get; set; } = "";
        // אובייקט: From — מי שולח את ההזמנה.
        public string To { get; set; } = "";
        // אובייקט: To — מי מוזמן.
        public string Room { get; set; } = "";
        // אובייקט: Room — החדר שאליו מזמינים.
    }

    public static class Teams
    // אובייקט: Teams — השמות של שתי הקבוצות בדו-קרב קבוצתי.
    {
        public const string Red = "red";
        // אובייקט: Red — הקבוצה האדומה.
        public const string Blue = "blue";
        // אובייקט: Blue — הקבוצה הכחולה.

        public static string Name(string team) => team == Red ? "הקבוצה האדומה 🔴" : team == Blue ? "הקבוצה הכחולה 🔵" : "";
        // פעולה: Name — מקבלת קבוצה ומחזירה את השם שלה בעברית (עם עיגול צבעוני), כדי להציג על המסך.
    }

    /// <summary>Register's data: the player's name and the room they join (players in different rooms never meet).</summary>
    public sealed class RegisterInfo
    // אובייקט: RegisterInfo — המידע שנשלח כשנכנסים לחדר: שם השחקן ושם החדר. שחקנים בחדרים שונים לא נפגשים.
    {
        public string Name { get; set; } = "";
        // אובייקט: Name — שם השחקן.
        public string Room { get; set; } = "";
        // אובייקט: Room — שם החדר.
    }

    /// <summary>A quick reaction sent during a duel.</summary>
    public sealed class EmojiMessage
    // אובייקט: EmojiMessage — אימוג'י שנשלח בזמן דו-קרב: מי שלח ואיזה אימוג'י.
    {
        public string Name { get; set; } = "";
        // אובייקט: Name — מי שלח.
        public string Emoji { get; set; } = "";
        // אובייקט: Emoji — האימוג'י עצמו.
    }

    /// <summary>One row of the champions table, from the server's database.</summary>
    public sealed class LeaderboardEntry
    // אובייקט: LeaderboardEntry — שורה אחת בטבלת האלופים של השרת.
    {
        public string Name { get; set; } = "";
        // אובייקט: Name — שם השחקן.
        public int Wins { get; set; }
        // אובייקט: Wins — כמה דו-קרבות הוא ניצח.
        public int GamesPlayed { get; set; }
        // אובייקט: GamesPlayed — בכמה דו-קרבות הוא שיחק בסך הכול.
        public int BestScore { get; set; }
        // אובייקט: BestScore — הניקוד הכי גבוה שהשיג בדו-קרב אחד.
    }

    /// <summary>One message on the wire: a command name and its data as JSON text.</summary>
    public sealed class Message
    // אובייקט: Message — "מעטפה" של הודעה אחת ברשת: שם הפקודה, והמידע שלה כטקסט JSON.
    {
        public string Command { get; set; } = "";
        // אובייקט: Command — איזו פקודה זאת (אחת מהמילים שב-Commands).
        public string Data { get; set; } = "";
        // אובייקט: Data — המידע שנשלח עם הפקודה, כטקסט JSON.
    }

    /// <summary>The board for a duel. Everyone gets the same one. Cells are row by row (81 numbers, 0 = empty).</summary>
    public sealed class DuelGame
    // אובייקט: DuelGame — הלוח של הדו-קרב. כולם מקבלים בדיוק אותו לוח.
    {
        public int Difficulty { get; set; }
        // אובייקט: Difficulty — רמת הקושי של הלוח.
        public bool Teams { get; set; }
        // אובייקט: Teams — אמת אם זה דו-קרב קבוצתי.
        public int[] Puzzle { get; set; } = new int[81];
        // אובייקט: Puzzle — הלוח להתחלה: 81 מספרים, שורה אחרי שורה. 0 = משבצת ריקה.
        public int[] Solution { get; set; } = new int[81];
        // אובייקט: Solution — הפתרון המלא של הלוח, כדי לבדוק אם תשובה נכונה.
    }

    public sealed class GameResult
    // אובייקט: GameResult — התוצאה של דו-קרב שהסתיים.
    {
        public string Winner { get; set; } = "";
        // אובייקט: Winner — שם המנצח.
        public List<Player> Players { get; set; } = new List<Player>();
        // אובייקט: Players — כל השחקנים שהשתתפו, עם הניקוד שלהם.

        /// <summary>In a team duel: the winning team (Teams.Red or Teams.Blue) and each team's total.</summary>
        public string WinnerTeam { get; set; } = "";
        // אובייקט: WinnerTeam — בדו-קרב קבוצתי: איזו קבוצה ניצחה.
        public int RedScore { get; set; }
        // אובייקט: RedScore — סך הנקודות של הקבוצה האדומה.
        public int BlueScore { get; set; }
        // אובייקט: BlueScore — סך הנקודות של הקבוצה הכחולה.
    }

    /// <summary>Points in a duel.</summary>
    public static class DuelScoring
    // אובייקט: DuelScoring — כללי הניקוד בדו-קרב.
    {
        public const int CorrectCell = 10;
        // אובייקט: CorrectCell — כמה נקודות מקבלים על כל משבצת שממלאים נכון.
        public const int Mistake = 5;
        // אובייקט: Mistake — כמה נקודות יורדות על טעות.

        /// <summary>Bonus for finishing the board, by finishing place (1st, 2nd, 3rd, then the rest).</summary>
        public static int FinishBonus(int place) => place switch
        // פעולה: FinishBonus — מחזירה את הבונוס על סיום הלוח לפי המקום: ראשון 100, שני 60, שלישי 30, והשאר 10.
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
    // אובייקט: DuelJson — עוזר שממיר הודעות לטקסט JSON ובחזרה, כדי לשלוח אותן ברשת.
    {
        public static string Pack<T>(string command, T data, System.Text.Json.Serialization.Metadata.JsonTypeInfo<T> type) =>
        // פעולה: Pack — "אורזת" הודעה: לוקחת פקודה ומידע, והופכת אותם לטקסט אחד שאפשר לשלוח.
            JsonSerializer.Serialize(new Message { Command = command, Data = JsonSerializer.Serialize(data, type) }, Default.Message);

        public static Message? Unpack(string text)
        // פעולה: Unpack — "פותחת" הודעה שהגיעה: הופכת טקסט בחזרה להודעה. אם הטקסט שבור, מחזירה null.
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
