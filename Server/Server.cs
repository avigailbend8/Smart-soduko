using System.Text;
using System.Text.Json;
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
 *      1) choose a difficulty and click Start
 *      2) send StartGame(difficulty)   ----------->  1) set 0 score and change status to "playing"
 *                                                       for all players in the room
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
 *
 * ********************************************************************************************************************
 *
 *  DM (deathmatch): everyone in a room plays the same board against everyone else. Two players = a 1-on-1 duel,
 *  more players = a group duel. The highest score wins. Rooms (a code the players choose) keep groups apart.
 */

namespace Server
{
    public class Server
    {
        private const string DefaultRoom = "main";

        /// <summary>One group of players that plays together.</summary>
        private sealed class Room
        {
            public string Code = DefaultRoom;
            public readonly Dictionary<Guid, Player> _players = new();
            public bool GameRunning;
        }

        private readonly WatsonTcpServer _server;
        private readonly Database _database;
        private readonly object _lock = new();

        // Connected clients (Guid -> IpPort), and which room each registered client is in
        private readonly Dictionary<Guid, string> _clients = new();
        private readonly Dictionary<Guid, Room> _roomOf = new();
        private readonly Dictionary<string, Room> _rooms = new();

        public Server(int port, Database database)
        {
            _database = database;
            _server = new WatsonTcpServer("0.0.0.0", port);
            _server.Events.ClientConnected += (s, e) => Log($"Client connected: {e.Client.IpPort}");
            _server.Events.ClientDisconnected += OnClientDisconnected;
            _server.Events.MessageReceived += OnMessageReceived;
        }

        public void Start() => _server.Start();

        public void Stop() => _server.Stop();

        private void OnMessageReceived(object? sender, MessageReceivedEventArgs e)
        {
            var message = DuelJson.Unpack(Encoding.UTF8.GetString(e.Data));
            if (message == null)
                return;

            Guid client = e.Client.Guid;
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
                        OnStartGame(client, JsonSerializer.Deserialize(message.Data, DuelJson.Default.Int32));
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
        {
            lock (_lock)
                _clients[client] = ipPort;
            Log($"Added client {ipPort}");
        }

        // 2. Register: add the player to their room and tell everyone in it
        private void OnRegister(Guid client, RegisterInfo info)
        {
            Room room;
            lock (_lock)
            {
                string code = string.IsNullOrWhiteSpace(info.Room) ? DefaultRoom : info.Room.Trim().ToLowerInvariant();
                if (!_rooms.TryGetValue(code, out room!))
                    _rooms[code] = room = new Room { Code = code };

                string name = UniqueName(room, string.IsNullOrWhiteSpace(info.Name) ? "שחקן" : info.Name.Trim());
                room._players[client] = new Player { Name = name, Status = room.GameRunning ? PlayerStatus.Finish : PlayerStatus.Waiting };
                _roomOf[client] = room;
                Log($"Registered player \"{name}\" in room \"{code}\"");
            }
            SendUpdatePlayers(room);
        }

        private void OnGetLeaderboard(Guid client)
        {
            var top = _database.TopPlayers(10);
            _ = _server.SendAsync(client, DuelJson.Pack(Commands.Leaderboard, top, DuelJson.Default.ListLeaderboardEntry));
        }

        // Start button: reset everyone in the room, then send the same new board to all of them
        private void OnStartGame(Guid client, int difficulty)
        {
            DuelGame game;
            Room? room;
            lock (_lock)
            {
                if (!_roomOf.TryGetValue(client, out room) || room.GameRunning || room._players.Count == 0)
                    return;

                foreach (var player in room._players.Values)
                {
                    player.Score = 0;
                    player.Solved = false;
                    player.Status = PlayerStatus.Playing;
                }
                room.GameRunning = true;
                game = MakeBoard(Math.Clamp(difficulty, 0, Levels.Difficulties.Count - 1));
            }

            Log($"Game started in room \"{room.Code}\" ({Levels.Difficulties[game.Difficulty].Name})");
            SendUpdatePlayers(room);
            Broadcast(room, DuelJson.Pack(Commands.StartGame, game, DuelJson.Default.DuelGame));
        }

        // A player's score or status changed
        private void OnChangePlayer(Guid client, Player changed)
        {
            bool gameOver;
            Room? room;
            lock (_lock)
            {
                if (!_roomOf.TryGetValue(client, out room) || !room._players.TryGetValue(client, out var player))
                    return;

                bool justFinished = player.Status != PlayerStatus.Finish && changed.Status == PlayerStatus.Finish;
                player.Score = Math.Max(0, changed.Score);
                player.Status = changed.Status;
                player.Solved = changed.Solved;

                if (justFinished && room.GameRunning)
                {
                    if (player.Solved)
                    {
                        int place = room._players.Values.Count(p => p.Solved);
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
            Broadcast(room, DuelJson.Pack(Commands.Emoji, safe, DuelJson.Default.EmojiMessage));
        }

        private void OnClientDisconnected(object? sender, DisconnectionEventArgs e)
        {
            bool gameOver = false;
            Room? room;
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
        {
            GameResult result;
            lock (_lock)
            {
                room.GameRunning = false;
                var ranking = room._players.Values.OrderByDescending(p => p.Score)
                    .Select(p => new Player { Name = p.Name, Score = p.Score, Status = PlayerStatus.Finish, Solved = p.Solved })
                    .ToList();
                result = new GameResult { Winner = ranking.FirstOrDefault()?.Name ?? "", Players = ranking };
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
        {
            List<Player> players;
            lock (_lock)
                players = room._players.Values.ToList();
            Broadcast(room, DuelJson.Pack(Commands.UpdatePlayers, players, DuelJson.Default.ListPlayer));
        }

        private void Broadcast(Room room, string text)
        {
            List<Guid> targets;
            lock (_lock)
                targets = room._players.Keys.Where(_clients.ContainsKey).ToList();
            foreach (Guid client in targets)
                _ = _server.SendAsync(client, text);
        }

        private static string UniqueName(Room room, string name)
        {
            string unique = name;
            for (int i = 2; room._players.Values.Any(p => p.Name == unique); i++)
                unique = $"{name} {i}";
            return unique;
        }

        private static DuelGame MakeBoard(int difficulty)
        {
            var d = Levels.Difficulties[difficulty];
            var (puzzle, solution) = SudokuGenerator.Create((d.FirstEmpty + d.LastEmpty) / 2);
            var game = new DuelGame { Difficulty = difficulty };
            for (int i = 0; i < 81; i++)
            {
                game.Puzzle[i] = puzzle[i / 9, i % 9];
                game.Solution[i] = solution[i / 9, i % 9];
            }
            return game;
        }

        private static void Log(string text) => Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {text}");
    }
}
