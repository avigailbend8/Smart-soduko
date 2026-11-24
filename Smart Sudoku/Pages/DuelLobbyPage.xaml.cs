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
    public sealed partial class DuelLobbyPage : BasePage
    // אובייקט: DuelLobbyPage — חדר ההמתנה של הדו-קרב ברשת: מתחברים לשרת ולחדר, רואים מי בחדר, מדברים בצ'אט,
    // רואים את טבלת האלופים, ומתחילים משחק לכל מי שבחדר.
    {
        private const string ServerSettingKey = "duel.server";
        // אובייקט: ServerSettingKey — השם שתחתיו נשמרת כתובת השרת האחרונה.
        private const string RoomSettingKey = "duel.room";
        // אובייקט: RoomSettingKey — השם שתחתיו נשמר קוד החדר האחרון.

        private static int selectedDifficulty;
        // אובייקט: selectedDifficulty — רמת הקושי שנבחרה לדו-קרב.

        public DuelLobbyPage()
        // פעולה: DuelLobbyPage (בנאי) — טוענת את העיצוב של העמוד מקובץ ה-XAML.
        {
            this.InitializeComponent();
        }

        private string PlayerName => Session.PlayerName ?? "אורח";
        // אובייקט: PlayerName — השם שבו השחקן מופיע בדו-קרב ("אורח" אם אין חשבון).

        protected override void OnNavigatedTo(NavigationEventArgs e)
        // פעולה: OnNavigatedTo — רצה כשנכנסים לעמוד: ממלאת את הכתובת והחדר האחרונים, ואם כבר מחוברים — מציגה את החדר.
        {
            base.OnNavigatedTo(e);
            var settings = ApplicationData.Current.LocalSettings.Values;
            // אובייקט: settings — ההגדרות המקומיות של האפליקציה.
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
        // פעולה: OnNavigatedFrom — רצה כשיוצאים מהעמוד: מפסיקה להקשיב להודעות מהשרת.
        {
            base.OnNavigatedFrom(e);
            if (Client.Current is Client client)
                Unsubscribe(client);
        }

        private void Subscribe(Client client)
        // פעולה: Subscribe — מתחילה להקשיב ל"פעמונים" של הלקוח: שחקנים, תחילת משחק, ניתוק, טבלת אלופים, צ'אט והזמנות.
        {
            client.PlayersUpdated += OnPlayersUpdated;
            client.GameStarted += OnGameStarted;
            client.Disconnected += OnDisconnected;
            client.LeaderboardReceived += OnLeaderboardReceived;
            client.ChatReceived += OnChatReceived;
            client.InviteReceived += OnInviteReceived;
        }

        private void Unsubscribe(Client client)
        // פעולה: Unsubscribe — מפסיקה להקשיב ל"פעמונים" של הלקוח.
        {
            client.PlayersUpdated -= OnPlayersUpdated;
            client.GameStarted -= OnGameStarted;
            client.Disconnected -= OnDisconnected;
            client.LeaderboardReceived -= OnLeaderboardReceived;
            client.ChatReceived -= OnChatReceived;
            client.InviteReceived -= OnInviteReceived;
        }

        private async void Connect_Click(object sender, RoutedEventArgs e)
        // פעולה: Connect_Click — כפתור "התחברות": מתחברת לשרת ולחדר, שומרת את הכתובת והחדר לפעם הבאה, ומציגה את החדר.
        // אם החיבור נכשל — מציגה הודעת שגיאה.
        {
            string host = ServerBox.Text.Trim();
            // אובייקט: host — כתובת השרת שהוקלדה.
            string room = RoomBox.Text.Trim();
            // אובייקט: room — קוד החדר שהוקלד (ריק = החדר הראשי).
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
                // אובייקט: client — החיבור החדש לשרת.
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
        // פעולה: ShowRoom — מסתירה את כרטיס ההתחברות ומציגה את החדר: השחקנים, הצ'אט וההודעות הקודמות.
        {
            ConnectCard.Visibility = Visibility.Collapsed;
            RoomCard.Visibility = Visibility.Visible;
            string room = Client.Current?.Room ?? "";
            // אובייקט: room — קוד החדר.
            RoomTitle.Text = room.Length > 0 ? $"השחקנים בחדר \"{room}\"" : "השחקנים בחדר הראשי";
            ChatCard.Visibility = Visibility.Visible;
            ChatList.Children.Clear();
            foreach (var line in Client.Current?.ChatHistory ?? new List<ChatMessage>())
                AddChatLine(line);
            RefreshPlayers();
        }

        private void BuildDifficultyButtons()
        // פעולה: BuildDifficultyButtons — בונה כפתור לכל רמת קושי של הדו-קרב. הכפתור שנבחר מודגש.
        {
            DifficultyPanel.Children.Clear();
            foreach (var difficulty in Levels.Difficulties)
            {
                bool selected = difficulty.Index == selectedDifficulty;
                // אובייקט: selected — אמת אם זו הרמה שנבחרה.
                var button = new Button
                // אובייקט: button — כפתור רמת קושי.
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
        // פעולה: RefreshPlayers — בונה מחדש את רשימת השחקנים בחדר (מה כל אחד עושה), וקובעת אם אפשר ללחוץ Start.
        {
            var players = Client.Current?.Players ?? new List<Player>();
            // אובייקט: players — השחקנים בחדר.
            PlayersList.Children.Clear();

            foreach (var player in players)
            {
                bool me = player.Name == PlayerName;
                // אובייקט: me — אמת אם השורה היא שלי.
                var row = new Grid
                // אובייקט: row — שורה של שחקן אחד: השם והמצב.
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
                // אובייקט: status — הטקסט של מצב השחקן.
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
            // אובייקט: someonePlaying — אמת אם כבר רץ משחק בחדר.
            StartButton.IsEnabled = players.Count >= 2 && !someonePlaying;
            if (someonePlaying)
                RoomHint.Text = "משחק כבר רץ בחדר — חכו שייגמר ואז תוכלו להצטרף לסיבוב הבא.";
            else if (players.Count < 2)
                RoomHint.Text = "מחכים לעוד שחקן... (2 שחקנים = אחד על אחד, יותר = דו-קרב קבוצתי)";
            else
                RoomHint.Text = $"{players.Count} שחקנים מוכנים! כל אחד יכול ללחוץ Start.";
        }

        private static string StatusText(string status)
        // פעולה: StatusText — הופכת מצב של שחקן לטקסט עם אימוג'י: משחק, סיים או מחכה.
        {
            if (status == PlayerStatus.Playing)
                return "משחק/ת 🎮";
            if (status == PlayerStatus.Finish)
                return "סיים/ה ✓";
            return "מחכה ⏳";
        }

        private void ShowLeaderboard(List<LeaderboardEntry> top)
        // פעולה: ShowLeaderboard — מציגה את טבלת האלופים של השרת, עם מדליות לשלושת הראשונים.
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
            // אובייקט: place — המקום בטבלה.
            foreach (var entry in top)
            {
                string medal = Medal(place);
                // אובייקט: medal — המדליה או מספר המקום.
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
        // פעולה: Start_Click — כפתור Start: מבקשת מהשרת להתחיל דו-קרב לכל מי שבחדר (ובקבוצות אם סימנו).
        {
            if (Client.Current is Client client)
            {
                StartButton.IsEnabled = false;
                await client.StartGameAsync(selectedDifficulty, TeamsBox.IsChecked == true);
            }
        }

        private async void Leaderboard_Click(object sender, RoutedEventArgs e)
        // פעולה: Leaderboard_Click — כפתור "טבלת אלופים": מבקשת אותה מהשרת.
        {
            if (Client.Current is Client client)
                await client.GetLeaderboardAsync();
        }

        private void Leave_Click(object sender, RoutedEventArgs e)
        // פעולה: Leave_Click — כפתור "יציאה": מתנתקת מהשרת וחוזרת לכרטיס ההתחברות.
        {
            if (Client.Current is Client client)
            {
                Unsubscribe(client);
                client.Disconnect();
            }
            RoomCard.Visibility = Visibility.Collapsed;
            LeaderboardCard.Visibility = Visibility.Collapsed;
            ChatCard.Visibility = Visibility.Collapsed;
            ConnectCard.Visibility = Visibility.Visible;
        }

        // ---------- Chat ----------

        private void AddChatLine(ChatMessage line)
        // פעולה: AddChatLine — מוסיפה שורת צ'אט: השם מודגש (שלי בצבע אחר), ואחריו ההודעה. גוללת למטה.
        {
            bool me = line.Name == PlayerName;
            // אובייקט: me — אמת אם אני כתבתי את ההודעה.
            var text = new TextBlock { FontSize = 16, TextWrapping = TextWrapping.Wrap, Foreground = Res("BerryBrush") };
            // אובייקט: text — שורת הטקסט של ההודעה.
            text.Inlines.Add(new Windows.UI.Xaml.Documents.Run { Text = line.Name + ": ", FontWeight = FontWeights.Bold, Foreground = Res(me ? "RoseBrush" : "BerryBrush") });
            text.Inlines.Add(new Windows.UI.Xaml.Documents.Run { Text = line.Text });
            ChatList.Children.Add(text);
            ChatScroll.ChangeView(null, double.MaxValue, null);
        }

        private async void SendChat_Click(object sender, RoutedEventArgs e)
        // פעולה: SendChat_Click — כפתור "שליחה": שולחת את מה שהוקלד לצ'אט.
        {
            string text = ChatBox.Text.Trim();
            // אובייקט: text — מה שהוקלד, בלי רווחים מיותרים.
            if (text.Length == 0 || Client.Current is not Client client)
                return;
            ChatBox.Text = "";
            await client.SendChatAsync(text);
        }

        private void ChatBox_KeyDown(object sender, Windows.UI.Xaml.Input.KeyRoutedEventArgs e)
        // פעולה: ChatBox_KeyDown — כשלוחצים Enter בתיבת הצ'אט — שולחים.
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
                SendChat_Click(sender, e);
        }

        // ---------- Invitations ----------

        private InviteMessage? pendingInvite;
        // אובייקט: pendingInvite — הזמנה שהגיעה ועוד לא ענו עליה.

        private void JoinInvite_Click(object sender, RoutedEventArgs e)
        // פעולה: JoinInvite_Click — כפתור "להצטרף": עוברת לחדר של החבר שהזמין.
        {
            if (pendingInvite == null)
                return;
            // Move to the friend's room on the same server
            RoomBox.Text = pendingInvite.Room;
            InviteBanner.Visibility = Visibility.Collapsed;
            pendingInvite = null;
            if (Client.Current is Client client)
            {
                Unsubscribe(client);
                client.Disconnect();
            }
            Connect_Click(sender, e);
        }

        private void DismissInvite_Click(object sender, RoutedEventArgs e)
        // פעולה: DismissInvite_Click — כפתור "לא עכשיו": סוגרת את ההזמנה.
        {
            InviteBanner.Visibility = Visibility.Collapsed;
            pendingInvite = null;
        }

        // Overrides the back button of BasePage: leave the room first, then go back
        protected override void BackButton_Click(object sender, RoutedEventArgs e)
        // פעולה: BackButton_Click — משנה את כפתור "חזרה" של BasePage: קודם יוצאים מהחדר, ואז חוזרים.
        {
            Leave_Click(sender, e);
            base.BackButton_Click(sender, e);
        }

        // ---------- Server events (background thread -> UI thread) ----------

        private async void OnPlayersUpdated() =>
        // פעולה: OnPlayersUpdated — כשמגיעה רשימת שחקנים מהשרת: מעדכנת את הרשימה במסך.
        // (הודעות מהשרת מגיעות "ברקע", ולכן מעבירים אותן למסך בעזרת Dispatcher.)
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, RefreshPlayers);

        private async void OnChatReceived(ChatMessage line) =>
        // פעולה: OnChatReceived — כשמגיעה הודעת צ'אט: מוסיפה אותה למסך.
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => AddChatLine(line));

        private async void OnInviteReceived(InviteMessage invite) =>
        // פעולה: OnInviteReceived — כשמגיעה הזמנה: מציגה פס הזמנה עם צליל.
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                pendingInvite = invite;
                InviteText.Text = $"💌 {invite.From} מזמינ/ה אותך לדו-קרב" + (invite.Room.Length > 0 ? $" בחדר \"{invite.Room}\"" : " בחדר הראשי");
                InviteBanner.Visibility = Visibility.Visible;
                Sounds.Play("coin");
            });

        private async void OnLeaderboardReceived(List<LeaderboardEntry> top) =>
        // פעולה: OnLeaderboardReceived — כשמגיעה טבלת האלופים: מציגה אותה.
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => ShowLeaderboard(top));

        private async void OnGameStarted(DuelGame game) =>
        // פעולה: OnGameStarted — כשהדו-קרב מתחיל: עוברת למסך המשחק עם הלוח.
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => Frame.Navigate(typeof(GamePage), game));

        private async void OnDisconnected() =>
        // פעולה: OnDisconnected — כשהחיבור לשרת נותק: חוזרת לכרטיס ההתחברות ומציגה הודעה.
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                RoomCard.Visibility = Visibility.Collapsed;
                ConnectCard.Visibility = Visibility.Visible;
                ShowError("החיבור לשרת נותק.");
            });

        private void ShowError(string message)
        // פעולה: ShowError — מציגה הודעת שגיאה בכרטיס ההתחברות.
        {
            ConnectError.Text = message;
            ConnectError.Visibility = Visibility.Visible;
        }
    }
}
