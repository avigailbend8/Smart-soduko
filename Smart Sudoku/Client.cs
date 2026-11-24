using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Smart_Sudoku.Duel;
using WatsonTcp;

namespace Smart_Sudoku
{
    /// <summary>
    /// The game's side of the duel protocol (see the table in Server/Server.cs).
    /// One client is kept for the whole app while the player is connected.
    /// Events arrive on a background thread; pages move them to the UI thread.
    /// </summary>
    public sealed class Client
    // אובייקט: Client — הצד של המשחק בתקשורת עם שרת הדו-קרבות: מתחבר לשרת, שולח את הפעולות של השחקן ומקבל עדכונים.
    // כל עוד השחקן מחובר, יש לקוח אחד לכל האפליקציה.
    {
        public const int DefaultPort = 9000;
        // אובייקט: DefaultPort — מספר הפורט הרגיל של השרת (9000).

        private readonly WatsonTcpClient _client;
        // אובייקט: _client — רכיב הרשת (WatsonTcp) שמתחבר לשרת ושולח ומקבל הודעות.

        public static Client? Current { get; private set; }
        // אובייקט: Current — הלקוח המחובר עכשיו (או null אם לא מחוברים), כדי שכל המסכים ישתמשו באותו חיבור.

        /// <summary>This player's own score and status during a duel.</summary>
        public Player Me { get; }
        // אובייקט: Me — השחקן שלי: השם, הניקוד והמצב שלי בדו-קרב.

        /// <summary>Everyone in the room, as the server last sent it.</summary>
        public List<Player> Players { get; private set; } = new List<Player>();
        // אובייקט: Players — כל השחקנים בחדר, כפי שהשרת שלח בפעם האחרונה.

        public event Action? PlayersUpdated;
        // אובייקט: PlayersUpdated — "פעמון" שמצלצל כשרשימת השחקנים משתנה, כדי שהמסך יתעדכן.
        public event Action<DuelGame>? GameStarted;
        // אובייקט: GameStarted — "פעמון" שמצלצל כשהדו-קרב מתחיל, עם הלוח.
        public event Action<GameResult>? GameOver;
        // אובייקט: GameOver — "פעמון" שמצלצל כשהדו-קרב נגמר, עם התוצאה.
        public event Action? Disconnected;
        // אובייקט: Disconnected — "פעמון" שמצלצל כשהחיבור לשרת נותק.
        public event Action<List<LeaderboardEntry>>? LeaderboardReceived;
        // אובייקט: LeaderboardReceived — "פעמון" שמצלצל כשמגיעה טבלת האלופים.
        public event Action<EmojiMessage>? EmojiReceived;
        // אובייקט: EmojiReceived — "פעמון" שמצלצל כשמישהו שולח אימוג'י.
        public event Action<ChatMessage>? ChatReceived;
        // אובייקט: ChatReceived — "פעמון" שמצלצל כשמגיעה הודעת צ'אט.
        public event Action<List<OnlinePlayer>>? OnlineReceived;
        // אובייקט: OnlineReceived — "פעמון" שמצלצל כשמגיעה רשימת המחוברים.
        public event Action<InviteMessage>? InviteReceived;
        // אובייקט: InviteReceived — "פעמון" שמצלצל כשמישהו מזמין אותי לחדר.

        /// <summary>Recent chat lines in the room, so a page opened later can still show them.</summary>
        public List<ChatMessage> ChatHistory { get; } = new List<ChatMessage>();
        // אובייקט: ChatHistory — הודעות הצ'אט האחרונות בחדר, כדי שמסך שנפתח מאוחר יותר יוכל להציג אותן.

        /// <summary>The room code this client joined.</summary>
        public string Room { get; private set; } = "";
        // אובייקט: Room — קוד החדר שהלקוח הצטרף אליו.

        private Client(string host, int port, string playerName)
        // פעולה: Client (בנאי) — יוצרת לקוח חדש: יוצרת את השחקן שלי, מכינה את החיבור לשרת, ומקשיבה להודעות ולניתוק.
        {
            Me = new Player { Name = playerName };
            _client = new WatsonTcpClient(host, port);
            _client.Settings.ConnectTimeoutSeconds = 5;
            _client.Events.MessageReceived += OnMessageReceived;
            _client.Events.ServerDisconnected += (s, e) =>
            {
                if (Current == this)
                    Current = null;
                Disconnected?.Invoke();
            };
        }

        public bool IsConnected => _client.Connected;
        // אובייקט: IsConnected — אמת אם יש עכשיו חיבור לשרת.

        /// <summary>Connects, then sends Connected and Register(playerName, room). Throws if the server can't be reached.</summary>
        public static async Task<Client> ConnectAsync(string host, int port, string playerName, string room)
        // פעולה: ConnectAsync — מתחברת לשרת, ואז שולחת "התחברתי" ו"אני נכנס לחדר" עם השם והחדר. אם השרת לא זמין — זורקת שגיאה.
        {
            Current?.Disconnect();

            var client = new Client(host, port, playerName);
            // אובייקט: client — הלקוח החדש שמתחבר.
            await Task.Run(() => client._client.Connect());
            Current = client;

            await client.SendAsync(Commands.Connected, "", DuelJson.Default.String);
            client.Room = room;
            await client.SendAsync(Commands.Register, new RegisterInfo { Name = playerName, Room = room }, DuelJson.Default.RegisterInfo);
            return client;
        }

        public void Disconnect()
        // פעולה: Disconnect — מתנתקת מהשרת.
        {
            if (Current == this)
                Current = null;
            try
            {
                _client.Disconnect();
            }
            catch (Exception)
            {
                // Already gone
            }
        }

        /// <summary>The Start button: asks the server to start a game for everyone in the room.</summary>
        public Task StartGameAsync(int difficulty, bool teams) =>
        // פעולה: StartGameAsync — כפתור "התחל": מבקשת מהשרת להתחיל דו-קרב לכל מי שבחדר, ברמת הקושי שנבחרה.
            SendAsync(Commands.StartGame, new StartOptions { Difficulty = difficulty, Teams = teams }, DuelJson.Default.StartOptions);

        public Task SendChatAsync(string text) => SendAsync(Commands.Chat, new ChatMessage { Name = Me.Name, Text = text }, DuelJson.Default.ChatMessage);
        // פעולה: SendChatAsync — שולחת הודעת צ'אט לחדר.

        /// <summary>Asks who is connected; the answer arrives through OnlineReceived.</summary>
        public Task GetOnlineAsync() => SendAsync(Commands.GetOnline, "", DuelJson.Default.String);
        // פעולה: GetOnlineAsync — שואלת את השרת מי מחובר. התשובה מגיעה דרך הפעמון OnlineReceived.

        /// <summary>Invites a player (by name) to this room.</summary>
        public Task InviteAsync(string friendName) => SendAsync(Commands.Invite, new InviteMessage { To = friendName }, DuelJson.Default.InviteMessage);
        // פעולה: InviteAsync — מזמינה חבר (לפי השם שלו) לחדר שלי.

        /// <summary>Asks for the champions table; the answer arrives through LeaderboardReceived.</summary>
        public Task GetLeaderboardAsync() => SendAsync(Commands.GetLeaderboard, "", DuelJson.Default.String);
        // פעולה: GetLeaderboardAsync — מבקשת את טבלת האלופים. התשובה מגיעה דרך הפעמון LeaderboardReceived.

        /// <summary>Sends a quick reaction to everyone in the room.</summary>
        public Task SendEmojiAsync(string emoji) => SendAsync(Commands.Emoji, new EmojiMessage { Name = Me.Name, Emoji = emoji }, DuelJson.Default.EmojiMessage);
        // פעולה: SendEmojiAsync — שולחת אימוג'י מהיר לכל מי שבחדר.

        /// <summary>Sends this player's new score or status.</summary>
        public Task ChangePlayerAsync() => SendAsync(Commands.ChangePlayer, Me, DuelJson.Default.Player);
        // פעולה: ChangePlayerAsync — שולחת לשרת את הניקוד או המצב החדש שלי.

        private async Task SendAsync<T>(string command, T data, System.Text.Json.Serialization.Metadata.JsonTypeInfo<T> type)
        // פעולה: SendAsync — "אורזת" פקודה ומידע להודעה ושולחת לשרת. אם החיבור נפל — לא קורסת.
        {
            try
            {
                await _client.SendAsync(DuelJson.Pack(command, data, type));
            }
            catch (Exception)
            {
                // A lost connection is reported through Disconnected
            }
        }

        private void OnMessageReceived(object? sender, MessageReceivedEventArgs e)
        // פעולה: OnMessageReceived — רצה כשמגיעה הודעה מהשרת: בודקת איזו פקודה זאת, ומצלצלת בפעמון המתאים.
        {
            var message = DuelJson.Unpack(Encoding.UTF8.GetString(e.Data));
            // אובייקט: message — ההודעה שהגיעה מהשרת.
            if (message == null)
                return;

            switch (message.Command)
            {
                case Commands.UpdatePlayers:
                    Players = JsonSerializer.Deserialize(message.Data, DuelJson.Default.ListPlayer) ?? new List<Player>();
                    // Learn which team this player is on (team duels)
                    foreach (var player in Players)
                    {
                        if (player.Name == Me.Name)
                            Me.Team = player.Team;
                    }
                    PlayersUpdated?.Invoke();
                    break;

                case Commands.StartGame:
                    if (JsonSerializer.Deserialize(message.Data, DuelJson.Default.DuelGame) is DuelGame game)
                    {
                        Me.Score = 0;
                        Me.Solved = false;
                        Me.Status = PlayerStatus.Playing;
                        GameStarted?.Invoke(game);
                    }
                    break;

                case Commands.Leaderboard:
                    LeaderboardReceived?.Invoke(JsonSerializer.Deserialize(message.Data, DuelJson.Default.ListLeaderboardEntry) ?? new List<LeaderboardEntry>());
                    break;

                case Commands.Chat:
                    if (JsonSerializer.Deserialize(message.Data, DuelJson.Default.ChatMessage) is ChatMessage chat)
                    {
                        ChatHistory.Add(chat);
                        if (ChatHistory.Count > 50)
                            ChatHistory.RemoveAt(0);
                        ChatReceived?.Invoke(chat);
                    }
                    break;

                case Commands.Online:
                    OnlineReceived?.Invoke(JsonSerializer.Deserialize(message.Data, DuelJson.Default.ListOnlinePlayer) ?? new List<OnlinePlayer>());
                    break;

                case Commands.Invite:
                    if (JsonSerializer.Deserialize(message.Data, DuelJson.Default.InviteMessage) is InviteMessage invite)
                        InviteReceived?.Invoke(invite);
                    break;

                case Commands.Emoji:
                    if (JsonSerializer.Deserialize(message.Data, DuelJson.Default.EmojiMessage) is EmojiMessage emoji)
                        EmojiReceived?.Invoke(emoji);
                    break;

                case Commands.GameOver:
                    if (JsonSerializer.Deserialize(message.Data, DuelJson.Default.GameResult) is GameResult result)
                    {
                        Me.Status = PlayerStatus.Waiting;
                        GameOver?.Invoke(result);
                    }
                    break;
            }
        }
    }
}
