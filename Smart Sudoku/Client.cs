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
    {
        public const int DefaultPort = 9000;

        private readonly WatsonTcpClient _client;

        public static Client? Current { get; private set; }

        /// <summary>This player's own score and status during a duel.</summary>
        public Player Me { get; }

        /// <summary>Everyone in the room, as the server last sent it.</summary>
        public List<Player> Players { get; private set; } = new List<Player>();

        public event Action? PlayersUpdated;
        public event Action<DuelGame>? GameStarted;
        public event Action<GameResult>? GameOver;
        public event Action? Disconnected;
        public event Action<List<LeaderboardEntry>>? LeaderboardReceived;
        public event Action<EmojiMessage>? EmojiReceived;

        /// <summary>The room code this client joined.</summary>
        public string Room { get; private set; } = "";

        private Client(string host, int port, string playerName)
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

        /// <summary>Connects, then sends Connected and Register(playerName, room). Throws if the server can't be reached.</summary>
        public static async Task<Client> ConnectAsync(string host, int port, string playerName, string room)
        {
            Current?.Disconnect();

            var client = new Client(host, port, playerName);
            await Task.Run(() => client._client.Connect());
            Current = client;

            await client.SendAsync(Commands.Connected, "", DuelJson.Default.String);
            client.Room = room;
            await client.SendAsync(Commands.Register, new RegisterInfo { Name = playerName, Room = room }, DuelJson.Default.RegisterInfo);
            return client;
        }

        public void Disconnect()
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
        public Task StartGameAsync(int difficulty) => SendAsync(Commands.StartGame, difficulty, DuelJson.Default.Int32);

        /// <summary>Asks for the champions table; the answer arrives through LeaderboardReceived.</summary>
        public Task GetLeaderboardAsync() => SendAsync(Commands.GetLeaderboard, "", DuelJson.Default.String);

        /// <summary>Sends a quick reaction to everyone in the room.</summary>
        public Task SendEmojiAsync(string emoji) => SendAsync(Commands.Emoji, new EmojiMessage { Name = Me.Name, Emoji = emoji }, DuelJson.Default.EmojiMessage);

        /// <summary>Sends this player's new score or status.</summary>
        public Task ChangePlayerAsync() => SendAsync(Commands.ChangePlayer, Me, DuelJson.Default.Player);

        private async Task SendAsync<T>(string command, T data, System.Text.Json.Serialization.Metadata.JsonTypeInfo<T> type)
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
        {
            var message = DuelJson.Unpack(Encoding.UTF8.GetString(e.Data));
            if (message == null)
                return;

            switch (message.Command)
            {
                case Commands.UpdatePlayers:
                    Players = JsonSerializer.Deserialize(message.Data, DuelJson.Default.ListPlayer) ?? new List<Player>();
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
