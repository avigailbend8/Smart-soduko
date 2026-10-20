using System;
using System.Collections.Generic;
using System.Linq;
using Smart_Sudoku.Duel;
using Windows.Storage;
using Windows.UI.Core;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace Smart_Sudoku.Pages
{
    /// <summary>
    /// Connect to the duel server and a room, see who is in it, see the champions table,
    /// and start a game for everyone in the room.
    /// </summary>
    public sealed partial class DuelLobbyPage : Page
    {
        private const string ServerSettingKey = "duel.server";
        private const string RoomSettingKey = "duel.room";

        private static int selectedDifficulty;

        public DuelLobbyPage()
        {
            this.InitializeComponent();
        }

        private static Brush Res(string key) => (Brush)Application.Current.Resources[key];

        private string PlayerName => Session.PlayerName ?? "אורח";

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            var settings = ApplicationData.Current.LocalSettings.Values;
            ServerBox.Text = settings[ServerSettingKey] as string ?? "127.0.0.1";
            RoomBox.Text = settings[RoomSettingKey] as string ?? "";
            BuildDifficultyButtons();

            if (Client.Current is Client client)
            {
                Subscribe(client);
                ShowRoom();
            }
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
            if (Client.Current is Client client)
                Unsubscribe(client);
        }

        private void Subscribe(Client client)
        {
            client.PlayersUpdated += OnPlayersUpdated;
            client.GameStarted += OnGameStarted;
            client.Disconnected += OnDisconnected;
            client.LeaderboardReceived += OnLeaderboardReceived;
        }

        private void Unsubscribe(Client client)
        {
            client.PlayersUpdated -= OnPlayersUpdated;
            client.GameStarted -= OnGameStarted;
            client.Disconnected -= OnDisconnected;
            client.LeaderboardReceived -= OnLeaderboardReceived;
        }

        private async void Connect_Click(object sender, RoutedEventArgs e)
        {
            string host = ServerBox.Text.Trim();
            string room = RoomBox.Text.Trim();
            if (host.Length == 0)
            {
                ShowError("צריך להקליד את כתובת השרת");
                return;
            }

            ConnectButton.IsEnabled = false;
            ConnectButton.Content = "מתחברים...";
            ConnectError.Visibility = Visibility.Collapsed;
            try
            {
                var client = await Client.ConnectAsync(host, Client.DefaultPort, PlayerName, room);
                var settings = ApplicationData.Current.LocalSettings.Values;
                settings[ServerSettingKey] = host;
                settings[RoomSettingKey] = room;
                Subscribe(client);
                ShowRoom();
            }
            catch (Exception)
            {
                ShowError($"לא הצלחנו להתחבר לשרת {host}. בדקו שתוכנת Server פתוחה ושהכתובת נכונה.");
            }
            finally
            {
                ConnectButton.IsEnabled = true;
                ConnectButton.Content = "התחברות";
            }
        }

        private void ShowRoom()
        {
            ConnectCard.Visibility = Visibility.Collapsed;
            RoomCard.Visibility = Visibility.Visible;
            string room = Client.Current?.Room ?? "";
            RoomTitle.Text = room.Length > 0 ? $"השחקנים בחדר \"{room}\"" : "השחקנים בחדר הראשי";
            RefreshPlayers();
        }

        private void BuildDifficultyButtons()
        {
            DifficultyPanel.Children.Clear();
            foreach (var difficulty in Levels.Difficulties)
            {
                bool selected = difficulty.Index == selectedDifficulty;
                var button = new Button
                {
                    Content = $"{difficulty.Emoji} {difficulty.Name}",
                    FontSize = 16,
                    Padding = new Thickness(16, 6, 16, 6),
                    Tag = difficulty.Index,
                    Background = Res(selected ? "SelectedCellBrush" : "ButtonHoverBrush"),
                    BorderBrush = Res(selected ? "RoseBrush" : "SoftPinkBrush"),
                    BorderThickness = new Thickness(selected ? 2.5 : 1.5),
                };
                button.Click += (s, e) =>
                {
                    selectedDifficulty = (int)((Button)s).Tag;
                    BuildDifficultyButtons();
                };
                DifficultyPanel.Children.Add(button);
            }
        }

        private void RefreshPlayers()
        {
            var players = Client.Current?.Players ?? new List<Player>();
            PlayersList.Children.Clear();

            foreach (var player in players)
            {
                bool me = player.Name == PlayerName;
                var row = new Grid
                {
                    Background = Res(me ? "SelectedCellBrush" : "RelatedCellBrush"),
                    CornerRadius = new CornerRadius(12),
                    Padding = new Thickness(16, 10, 16, 10),
                };
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                row.Children.Add(new TextBlock
                {
                    Text = (me ? "💖 " : "🙂 ") + player.Name + (me ? " (אני)" : ""),
                    FontSize = 18,
                    FontWeight = me ? FontWeights.Bold : FontWeights.SemiBold,
                    Foreground = Res("BerryBrush"),
                });
                var status = new TextBlock
                {
                    Text = StatusText(player.Status),
                    FontSize = 16,
                    Foreground = Res("MutedTextBrush"),
                };
                Grid.SetColumn(status, 1);
                row.Children.Add(status);

                PlayersList.Children.Add(row);
            }

            bool someonePlaying = players.Any(p => p.Status == PlayerStatus.Playing);
            StartButton.IsEnabled = players.Count >= 2 && !someonePlaying;
            RoomHint.Text = someonePlaying
                ? "משחק כבר רץ בחדר — חכו שייגמר ואז תוכלו להצטרף לסיבוב הבא."
                : players.Count < 2
                    ? "מחכים לעוד שחקן... (2 שחקנים = אחד על אחד, יותר = דו-קרב קבוצתי)"
                    : $"{players.Count} שחקנים מוכנים! כל אחד יכול ללחוץ Start.";
        }

        private static string StatusText(string status) => status switch
        {
            PlayerStatus.Playing => "משחק/ת 🎮",
            PlayerStatus.Finish => "סיים/ה ✓",
            _ => "מחכה ⏳",
        };

        private void ShowLeaderboard(List<LeaderboardEntry> top)
        {
            LeaderboardList.Children.Clear();
            if (top.Count == 0)
            {
                LeaderboardList.Children.Add(new TextBlock
                {
                    Text = "עוד לא שוחקו דו-קרבות. בואו נהיה הראשונים!",
                    FontSize = 16,
                    Foreground = Res("MutedTextBrush"),
                });
            }

            int place = 1;
            foreach (var entry in top)
            {
                string medal = place switch { 1 => "🥇", 2 => "🥈", 3 => "🥉", _ => $"{place}." };
                LeaderboardList.Children.Add(new TextBlock
                {
                    Text = $"{medal}  {entry.Name} — {entry.Wins} ניצחונות מתוך {entry.GamesPlayed} · שיא {entry.BestScore}",
                    FontSize = 17,
                    FontWeight = place <= 3 ? FontWeights.SemiBold : FontWeights.Normal,
                    Foreground = Res("BerryBrush"),
                    TextWrapping = TextWrapping.Wrap,
                });
                place++;
            }
            LeaderboardCard.Visibility = Visibility.Visible;
        }

        private async void Start_Click(object sender, RoutedEventArgs e)
        {
            if (Client.Current is Client client)
            {
                StartButton.IsEnabled = false;
                await client.StartGameAsync(selectedDifficulty);
            }
        }

        private async void Leaderboard_Click(object sender, RoutedEventArgs e)
        {
            if (Client.Current is Client client)
                await client.GetLeaderboardAsync();
        }

        private void Leave_Click(object sender, RoutedEventArgs e)
        {
            if (Client.Current is Client client)
            {
                Unsubscribe(client);
                client.Disconnect();
            }
            RoomCard.Visibility = Visibility.Collapsed;
            LeaderboardCard.Visibility = Visibility.Collapsed;
            ConnectCard.Visibility = Visibility.Visible;
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            Leave_Click(sender, e);
            if (Frame.CanGoBack)
                Frame.GoBack();
        }

        // ---------- Server events (background thread -> UI thread) ----------

        private async void OnPlayersUpdated() =>
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, RefreshPlayers);

        private async void OnLeaderboardReceived(List<LeaderboardEntry> top) =>
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => ShowLeaderboard(top));

        private async void OnGameStarted(DuelGame game) =>
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => Frame.Navigate(typeof(GamePage), game));

        private async void OnDisconnected() =>
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                RoomCard.Visibility = Visibility.Collapsed;
                ConnectCard.Visibility = Visibility.Visible;
                ShowError("החיבור לשרת נותק.");
            });

        private void ShowError(string message)
        {
            ConnectError.Text = message;
            ConnectError.Visibility = Visibility.Visible;
        }
    }
}
