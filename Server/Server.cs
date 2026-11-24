using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Smart_Sudoku;
using Smart_Sudoku.Duel;
using WatsonTcp;

/* ****************************************  Client - Server commands protocol  ****************************************
 *
 *      Client                                        Server                                          All Clients (in the room)
 *
 *                                                    START
 *
 *  1.  send Connected                  ----------->  1) add client IpPort
 *
 *  2.  send Register(playerName, room) ----------->  1) add player to the room's _players list
 *                                                    2) send UpdatePlayers(_players)  ---------->   1) refresh players table
 *
 *      send GetLeaderboard             ----------->  1) read the top players from database.db
 *                                                    2) send Leaderboard(top)         ---------->   (only to the asking client)
 *
 *      send GetOnline                  ----------->  1) list everyone connected       ---------->   (only to the asking client)
 *      send Invite(friend)             ----------->  1) find the friend               ---------->   1) the friend sees the invite
 *      send Chat(text)                 ----------->  1) clean rude words, pass it on  ---------->   1) show the chat line
 *
 *      1) choose a difficulty (and teams) and click Start
 *      2) send StartGame(options)      ----------->  1) set 0 score and change status to "playing"
 *                                                       for all players in the room (in teams: red / blue)
 *                                                    2) send UpdatePlayers(_players)  ---------->   1) refresh players table
 *                                                    3) send StartGame(board)         ---------->   2) open Start dialog (5-4-3-2-1)
 *                                                                                                       and play the same board
 *
 *  ++++++++++++++++++++++++++++++++++++++++++++++++++++  Game begin  ++++++++++++++++++++++++++++++++++++++++++++++++++++
 *
 *      1) change player's score or status
 *      2) send ChangePlayer(Player player) ------->  1) update _players list
 *                                                    2) send UpdatePlayers(_players)  ---------->   1) refresh players table
 *
 *      send Emoji(emoji)               ----------->  1) pass it on                    ---------->   1) show the emoji bubble
 *
 *      1) player ends game and changes
 *         status to "finish"
 *      2) send ChangePlayer(Player player) ------->  1) update _players list with changed player
 *                                                       (a solved board gets a finish bonus by place)
 *                                                    2) send UpdatePlayers(_players)  ---------->   1) refresh players table
 *                                                    3) if all players are finished,
 *                                                       save the game in database.db and
 *                                                       send GameOver(winner)         ---------->   1) open GameOver dialog with winner name
 *                                                       (in teams: the team with the higher total wins)
 *
 * ********************************************************************************************************************
 *
 *  DM (deathmatch): everyone in a room plays the same board against everyone else. Two players = a 1-on-1 duel,
 *  more players = a group duel. The highest score wins. Rooms (a code the players choose) keep groups apart.
 */

namespace Server
{
    public class Server
    // אובייקט: Server — השרת של הדו-קרבות. מקבל חיבורים מהמשחקים, מחלק את השחקנים לחדרים ומעביר ביניהם הודעות.
    {
        private const string DefaultRoom = "main";
        // אובייקט: DefaultRoom — שם החדר הרגיל, כשהשחקן לא בחר קוד חדר משלו.

        /// <summary>One group of players that plays together.</summary>
        private sealed class Room
        // אובייקט: Room — חדר אחד: קבוצת שחקנים שמשחקים יחד באותו לוח.
        {
            public string Code = DefaultRoom;
            // אובייקט: Code — הקוד (השם) של החדר.
            public readonly Dictionary<Guid, Player> _players = new();
            // אובייקט: _players — כל השחקנים שבחדר. לכל חיבור יש מספר מזהה ייחודי (Guid), ולידו השחקן שלו.
            public bool GameRunning;
            // אובייקט: GameRunning — אמת כשבחדר יש עכשיו דו-קרב פעיל.
            public bool Teams;
            // אובייקט: Teams — אמת כשהדו-קרב בחדר הוא בקבוצות.
        }

        private readonly WatsonTcpServer _server;
        // אובייקט: _server — רכיב הרשת (WatsonTcp) שמקבל חיבורים ושולח ומקבל הודעות.
        private readonly Database _database;
        // אובייקט: _database — מסד הנתונים, כדי לשמור תוצאות ולקרוא את טבלת האלופים.
        private readonly object _lock = new();
        // אובייקט: _lock — "מנעול". כמה שחקנים יכולים לשלוח הודעות באותו רגע, והמנעול דואג שרק אחד ישנה את הרשימות בכל פעם.

        // Connected clients (Guid -> IpPort), and which room each registered client is in
        private readonly Dictionary<Guid, string> _clients = new();
        // אובייקט: _clients — כל המשחקים שמחוברים עכשיו: מספר מזהה, ולידו הכתובת שלו ברשת.
        private readonly Dictionary<Guid, Room> _roomOf = new();
        // אובייקט: _roomOf — לכל שחקן רשום: באיזה חדר הוא נמצא.
        private readonly Dictionary<string, Room> _rooms = new();
        // אובייקט: _rooms — כל החדרים הפתוחים, לפי הקוד שלהם.

        public Server(int port, Database database)
        // פעולה: Server (בנאי) — יוצרת את השרת על הפורט שקיבלה, ומחברת אותו לאירועים: מישהו התחבר, מישהו התנתק, הגיעה הודעה.
        {
            _database = database;
            _server = new WatsonTcpServer("0.0.0.0", port);
            _server.Events.ClientConnected += (s, e) => Log($"Client connected: {e.Client.IpPort}");
            _server.Events.ClientDisconnected += OnClientDisconnected;
            _server.Events.MessageReceived += OnMessageReceived;
        }

        public void Start() => _server.Start();
        // פעולה: Start — מפעילה את השרת, כדי שיתחיל לקבל שחקנים.

        public void Stop() => _server.Stop();
        // פעולה: Stop — מכבה את השרת.

        private void OnMessageReceived(object? sender, MessageReceivedEventArgs e)
        // פעולה: OnMessageReceived — רצה בכל פעם שמגיעה הודעה ממשחק. היא פותחת את ההודעה, בודקת איזו פקודה זאת,
        // ושולחת אותה לפעולה המתאימה (הרשמה, התחלת משחק, צ'אט וכו').
        {
            var message = DuelJson.Unpack(Encoding.UTF8.GetString(e.Data));
            // אובייקט: message — ההודעה שהגיעה, אחרי שהומרה מטקסט לאובייקט.
            if (message == null)
                return;

            Guid client = e.Client.Guid;
            // אובייקט: client — המספר המזהה של המשחק ששלח את ההודעה.
            try
            {
                switch (message.Command)
                {
                    case Commands.Connected:
                        OnConnected(client, e.Client.IpPort);
                        break;
                    case Commands.Register:
                        if (JsonSerializer.Deserialize(message.Data, DuelJson.Default.RegisterInfo) is RegisterInfo info)
                            OnRegister(client, info);
                        break;
                    case Commands.GetLeaderboard:
                        OnGetLeaderboard(client);
                        break;
                    case Commands.StartGame:
                        if (JsonSerializer.Deserialize(message.Data, DuelJson.Default.StartOptions) is StartOptions options)
                            OnStartGame(client, options);
                        break;
                    case Commands.Chat:
                        if (JsonSerializer.Deserialize(message.Data, DuelJson.Default.ChatMessage) is ChatMessage chat)
                            OnChat(client, chat);
                        break;
                    case Commands.GetOnline:
                        OnGetOnline(client);
                        break;
                    case Commands.Invite:
                        if (JsonSerializer.Deserialize(message.Data, DuelJson.Default.InviteMessage) is InviteMessage invite)
                            OnInvite(client, invite);
                        break;
                    case Commands.ChangePlayer:
                        if (JsonSerializer.Deserialize(message.Data, DuelJson.Default.Player) is Player player)
                            OnChangePlayer(client, player);
                        break;
                    case Commands.Emoji:
                        if (JsonSerializer.Deserialize(message.Data, DuelJson.Default.EmojiMessage) is EmojiMessage emoji)
                            OnEmoji(client, emoji);
                        break;
                    default:
                        Log($"Unknown command: {message.Command}");
                        break;
                }
            }
            catch (JsonException)
            {
                Log($"Bad data for {message.Command}");
            }
        }

        // 1. Connected: remember the client's address
        private void OnConnected(Guid client, string ipPort)
        // פעולה: OnConnected — זוכרת את הכתובת של משחק שהתחבר עכשיו.
        {
            lock (_lock)
                _clients[client] = ipPort;
            Log($"Added client {ipPort}");
        }

        // 2. Register: add the player to their room and tell everyone in it
        private void OnRegister(Guid client, RegisterInfo info)
        // פעולה: OnRegister — מכניסה שחקן לחדר שבחר (או יוצרת חדר חדש), נותנת לו שם ייחודי, ומעדכנת את כל מי שבחדר.
        {
            Room room;
            // אובייקט: room — החדר שאליו השחקן נכנס.
            lock (_lock)
            {
                string code = string.IsNullOrWhiteSpace(info.Room) ? DefaultRoom : info.Room.Trim().ToLowerInvariant();
                // אובייקט: code — קוד החדר באותיות קטנות, כדי ש"Room" ו-"room" יהיו אותו חדר.
                if (!_rooms.TryGetValue(code, out room!))
                    _rooms[code] = room = new Room { Code = code };

                string name = UniqueName(room, string.IsNullOrWhiteSpace(info.Name) ? "שחקן" : info.Name.Trim());
                // אובייקט: name — שם השחקן. אם השם כבר תפוס בחדר, מוסיפים לו מספר.
                room._players[client] = new Player { Name = name, Status = room.GameRunning ? PlayerStatus.Finish : PlayerStatus.Waiting };
                _roomOf[client] = room;
                Log($"Registered player \"{name}\" in room \"{code}\"");
            }
            SendUpdatePlayers(room);
        }

        private void OnGetLeaderboard(Guid client)
        // פעולה: OnGetLeaderboard — שולחת למי שביקש את 10 השחקנים המובילים מטבלת האלופים.
        {
            var top = _database.TopPlayers(10);
            // אובייקט: top — רשימת השחקנים המובילים ממסד הנתונים.
            _ = _server.SendAsync(client, DuelJson.Pack(Commands.Leaderboard, top, DuelJson.Default.ListLeaderboardEntry));
        }

        // Start button: reset everyone in the room, then send the same new board to all of them
        private void OnStartGame(Guid client, StartOptions options)
        // פעולה: OnStartGame — מתחילה דו-קרב בחדר: מאפסת לכולם את הניקוד, מחלקת לקבוצות אם צריך,
        // יוצרת לוח חדש ושולחת לכולם בדיוק את אותו לוח.
        {
            int difficulty = options.Difficulty;
            // אובייקט: difficulty — רמת הקושי שנבחרה.
            DuelGame game;
            // אובייקט: game — הלוח החדש שכולם ישחקו בו.
            Room? room;
            // אובייקט: room — החדר שבו מתחילים את המשחק.
            lock (_lock)
            {
                if (!_roomOf.TryGetValue(client, out room) || room.GameRunning || room._players.Count == 0)
                    return;

                // Teams need at least 2 players; players are split red, blue, red, blue... in the order they joined
                room.Teams = options.Teams && room._players.Count >= 2;
                int index = 0;
                // אובייקט: index — סופר שחקנים, כדי לחלק אותם לסירוגין: אדום, כחול, אדום, כחול...
                foreach (var player in room._players.Values)
                {
                    player.Score = 0;
                    player.Solved = false;
                    player.Status = PlayerStatus.Playing;
                    player.Team = room.Teams ? (index++ % 2 == 0 ? Smart_Sudoku.Duel.Teams.Red : Smart_Sudoku.Duel.Teams.Blue) : "";
                }
                room.GameRunning = true;
                game = MakeBoard(Math.Clamp(difficulty, 0, Levels.Difficulties.Count - 1));
                game.Teams = room.Teams;
            }

            Log($"Game started in room \"{room.Code}\" ({Levels.Difficulties[game.Difficulty].Name})");
            SendUpdatePlayers(room);
            Broadcast(room, DuelJson.Pack(Commands.StartGame, game, DuelJson.Default.DuelGame));
        }

        // A player's score or status changed
        private void OnChangePlayer(Guid client, Player changed)
        // פעולה: OnChangePlayer — מעדכנת ניקוד או מצב של שחקן. מי שסיים את הלוח מקבל בונוס לפי המקום שלו.
        // כשכל השחקנים בחדר סיימו — המשחק נגמר.
        {
            bool gameOver;
            // אובייקט: gameOver — אמת אם כל השחקנים סיימו, ואז צריך להכריז על מנצח.
            Room? room;
            // אובייקט: room — החדר של השחקן ששלח את העדכון.
            lock (_lock)
            {
                if (!_roomOf.TryGetValue(client, out room) || !room._players.TryGetValue(client, out var player))
                    return;

                bool justFinished = player.Status != PlayerStatus.Finish && changed.Status == PlayerStatus.Finish;
                // אובייקט: justFinished — אמת אם השחקן סיים בדיוק עכשיו (ולא כבר קודם).
                player.Score = Math.Max(0, changed.Score);
                player.Status = changed.Status;
                player.Solved = changed.Solved;

                if (justFinished && room.GameRunning)
                {
                    if (player.Solved)
                    {
                        int place = room._players.Values.Count(p => p.Solved);
                        // אובייקט: place — באיזה מקום השחקן סיים (ראשון, שני...), לפי כמה שחקנים פתרו את הלוח עד עכשיו.
                        player.Score += DuelScoring.FinishBonus(place);
                    }
                    Log($"\"{player.Name}\" finished ({(player.Solved ? "solved" : "out of hearts")})");
                }

                gameOver = room.GameRunning && room._players.Values.All(p => p.Status == PlayerStatus.Finish);
            }

            SendUpdatePlayers(room);
            if (gameOver)
                SendGameOver(room);
        }

        // Quick reactions: passed on to everyone in the same room, with the sender's name from the server
        private void OnEmoji(Guid client, EmojiMessage emoji)
        // פעולה: OnEmoji — מעבירה אימוג'י לכל מי שבחדר, עם השם האמיתי של השולח (מהשרת, כדי שאי אפשר יהיה להתחזות).
        {
            Room? room;
            string name;
            lock (_lock)
            {
                if (!_roomOf.TryGetValue(client, out room) || !room._players.TryGetValue(client, out var player))
                    return;
                name = player.Name;
            }
            var safe = new EmojiMessage { Name = name, Emoji = emoji.Emoji.Length <= 8 ? emoji.Emoji : "" };
            // אובייקט: safe — אימוג'י "בטוח": עם השם מהשרת, ובלי טקסט ארוך מדי.
            Broadcast(room, DuelJson.Pack(Commands.Emoji, safe, DuelJson.Default.EmojiMessage));
        }

        // ---------- Chat, friends online, invites ----------

        private static readonly string[] RudeWords = { "טיפש", "טיפשה", "מטומטם", "מטומטמת", "אידיוט", "סתום", "סתמי", "דפוק", "stupid", "idiot", "dumb", "shut up" };
        // אובייקט: RudeWords — רשימת מילים לא יפות. בצ'אט הן מוחלפות בלב 💖.

        private void OnChat(Guid client, ChatMessage chat)
        // פעולה: OnChat — מקבלת הודעת צ'אט, מקצרת אותה ל-120 תווים לכל היותר, מחליפה מילים לא יפות בלבבות,
        // ושולחת אותה לכל מי שבחדר.
        {
            Room? room;
            string name;
            lock (_lock)
            {
                if (!_roomOf.TryGetValue(client, out room) || !room._players.TryGetValue(client, out var player))
                    return;
                name = player.Name;
            }

            // Short messages only, and rude words become hearts
            string text = chat.Text.Trim();
            // אובייקט: text — הטקסט של ההודעה, אחרי ניקוי רווחים.
            if (text.Length == 0)
                return;
            if (text.Length > 120)
                text = text.Substring(0, 120);
            foreach (string word in RudeWords)
                text = Regex.Replace(text, Regex.Escape(word), "💖", RegexOptions.IgnoreCase);

            Broadcast(room, DuelJson.Pack(Commands.Chat, new ChatMessage { Name = name, Text = text }, DuelJson.Default.ChatMessage));
        }

        private void OnGetOnline(Guid client)
        // פעולה: OnGetOnline — שולחת למי שביקש את רשימת כל השחקנים שמחוברים עכשיו (לרשימת החברים).
        {
            List<OnlinePlayer> online;
            // אובייקט: online — רשימת השחקנים המחוברים, עם החדר והמצב של כל אחד.
            lock (_lock)
            {
                online = _rooms.Values.SelectMany(room => room._players.Values.Select(p => new OnlinePlayer
                {
                    Name = p.Name,
                    Room = room.Code == DefaultRoom ? "" : room.Code,
                    Status = p.Status,
                })).ToList();
            }
            _ = _server.SendAsync(client, DuelJson.Pack(Commands.Online, online, DuelJson.Default.ListOnlinePlayer));
        }

        // Passes an invitation to the named player, telling them which room to join
        private void OnInvite(Guid client, InviteMessage invite)
        // פעולה: OnInvite — מעבירה הזמנה לחבר לפי השם שלו, ואומרת לו לאיזה חדר להצטרף.
        {
            Guid? target = null;
            // אובייקט: target — המספר המזהה של החבר המוזמן (אם הוא נמצא).
            InviteMessage forward;
            // אובייקט: forward — ההזמנה שתישלח לחבר: ממי, למי ולאיזה חדר.
            lock (_lock)
            {
                if (!_roomOf.TryGetValue(client, out var room) || !room._players.TryGetValue(client, out var sender))
                    return;
                forward = new InviteMessage { From = sender.Name, To = invite.To, Room = room.Code == DefaultRoom ? "" : room.Code };
                foreach (var other in _rooms.Values)
                {
                    foreach (var pair in other._players)
                    {
                        if (pair.Value.Name == invite.To)
                            target = pair.Key;
                    }
                }
            }
            if (target is Guid to)
            {
                Log($"\"{forward.From}\" invited \"{forward.To}\"");
                _ = _server.SendAsync(to, DuelJson.Pack(Commands.Invite, forward, DuelJson.Default.InviteMessage));
            }
        }

        private void OnClientDisconnected(object? sender, DisconnectionEventArgs e)
        // פעולה: OnClientDisconnected — רצה כשמשחק מתנתק. מוציאה את השחקן מהחדר, סוגרת חדר שהתרוקן,
        // ואם כל מי שנשאר כבר סיים — מכריזה על סוף המשחק.
        {
            bool gameOver = false;
            // אובייקט: gameOver — אמת אם אחרי היציאה כל מי שנשאר כבר סיים.
            Room? room;
            // אובייקט: room — החדר שממנו השחקן יצא.
            lock (_lock)
            {
                _clients.Remove(e.Client.Guid);
                if (_roomOf.Remove(e.Client.Guid, out room))
                {
                    if (room._players.Remove(e.Client.Guid, out var player))
                        Log($"\"{player.Name}\" left room \"{room.Code}\"");
                    gameOver = room.GameRunning && room._players.Count > 0 && room._players.Values.All(p => p.Status == PlayerStatus.Finish);
                    if (room._players.Count == 0)
                        _rooms.Remove(room.Code);
                }
            }

            if (room == null)
                return;
            SendUpdatePlayers(room);
            if (gameOver)
                SendGameOver(room);
        }

        // All players in the room finished: the highest score wins, and the game is saved in the database
        private void SendGameOver(Room room)
        // פעולה: SendGameOver — מסיימת דו-קרב: מסדרת את השחקנים לפי הניקוד, קובעת מנצח (או קבוצה מנצחת),
        // שולחת לכולם את התוצאה ושומרת את המשחק במסד הנתונים.
        {
            GameResult result;
            // אובייקט: result — התוצאה הסופית של הדו-קרב.
            lock (_lock)
            {
                room.GameRunning = false;
                var ranking = room._players.Values.OrderByDescending(p => p.Score)
                // אובייקט: ranking — השחקנים מסודרים מהניקוד הגבוה לנמוך.
                    .Select(p => new Player { Name = p.Name, Score = p.Score, Status = PlayerStatus.Finish, Solved = p.Solved, Team = p.Team })
                    .ToList();
                result = new GameResult { Winner = ranking.FirstOrDefault()?.Name ?? "", Players = ranking };

                // In teams, each team's points are added up and the higher total wins
                if (room.Teams)
                {
                    result.RedScore = ranking.Where(p => p.Team == Smart_Sudoku.Duel.Teams.Red).Sum(p => p.Score);
                    result.BlueScore = ranking.Where(p => p.Team == Smart_Sudoku.Duel.Teams.Blue).Sum(p => p.Score);
                    result.WinnerTeam = result.RedScore >= result.BlueScore ? Smart_Sudoku.Duel.Teams.Red : Smart_Sudoku.Duel.Teams.Blue;
                    result.Winner = Smart_Sudoku.Duel.Teams.Name(result.WinnerTeam);
                }
                foreach (var player in room._players.Values)
                    player.Status = PlayerStatus.Waiting;
            }

            Log($"Game over in room \"{room.Code}\". Winner: {result.Winner}");
            Broadcast(room, DuelJson.Pack(Commands.GameOver, result, DuelJson.Default.GameResult));
            SendUpdatePlayers(room);

            try
            {
                _database.SaveGame(result);
            }
            catch (Exception ex)
            {
                Log($"Could not save the game to the database: {ex.Message}");
            }
        }

        private void SendUpdatePlayers(Room room)
        // פעולה: SendUpdatePlayers — שולחת לכל מי שבחדר את רשימת השחקנים המעודכנת (שמות, ניקוד ומצב).
        {
            List<Player> players;
            // אובייקט: players — עותק של רשימת השחקנים בחדר.
            lock (_lock)
                players = room._players.Values.ToList();
            Broadcast(room, DuelJson.Pack(Commands.UpdatePlayers, players, DuelJson.Default.ListPlayer));
        }

        private void Broadcast(Room room, string text)
        // פעולה: Broadcast — שולחת את אותה הודעה לכל השחקנים שבחדר.
        {
            List<Guid> targets;
            // אובייקט: targets — כל השחקנים בחדר שעדיין מחוברים.
            lock (_lock)
                targets = room._players.Keys.Where(_clients.ContainsKey).ToList();
            foreach (Guid client in targets)
                _ = _server.SendAsync(client, text);
        }

        private static string UniqueName(Room room, string name)
        // פעולה: UniqueName — מחזירה שם שלא תפוס בחדר. אם "דנה" כבר קיימת, השם החדש יהיה "דנה 2".
        {
            string unique = name;
            // אובייקט: unique — השם שנבדק, עד שנמצא שם פנוי.
            for (int i = 2; room._players.Values.Any(p => p.Name == unique); i++)
                unique = $"{name} {i}";
            return unique;
        }

        private static DuelGame MakeBoard(int difficulty)
        // פעולה: MakeBoard — יוצרת לוח סודוקו חדש לפי רמת הקושי, והופכת אותו לרשימה של 81 מספרים כדי לשלוח ברשת.
        {
            var d = Levels.Difficulties[difficulty];
            // אובייקט: d — ההגדרות של רמת הקושי (כמה משבצות להשאיר ריקות).
            var (puzzle, solution) = SudokuGenerator.Create((d.FirstEmpty + d.LastEmpty) / 2);
            var game = new DuelGame { Difficulty = difficulty };
            // אובייקט: game — הלוח שיישלח לשחקנים: החידה והפתרון.
            for (int i = 0; i < 81; i++)
            {
                game.Puzzle[i] = puzzle[i / 9, i % 9];
                game.Solution[i] = solution[i / 9, i % 9];
            }
            return game;
        }

        private static void Log(string text) => Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {text}");
        // פעולה: Log — כותבת שורה למסך של השרת, עם השעה, כדי לראות מה קורה.
    }
}
