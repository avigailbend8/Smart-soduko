using System;
using System.Collections.Generic;
using System.Linq;
using Smart_Sudoku.Duel;
using Windows.UI.Core;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Smart_Sudoku.Pages
{
    /// <summary>
    /// Duel mode: everyone in the room plays the same board, sending their score to the server
    /// (ChangePlayer) and seeing everyone else's score live (UpdatePlayers) until GameOver.
    /// </summary>
    public sealed partial class GamePage
    {
        /// <summary>Extra coins for winning a duel, on top of the regular coins for solving the board.</summary>
        private const int WinnerBonus = 150;
        private const int CountdownFrom = 5;

        private int duelCoins;  // coins earned by solving the board in this duel

        private Client? duelClient;
        private bool[,] duelScored = new bool[9, 9];
        private bool IsDuel => duelClient != null;

        private void StartDuel(DuelGame game)
        {
            duelClient = Client.Current;
            if (duelClient == null)
                return;

            duelClient.PlayersUpdated += OnDuelPlayersUpdated;
            duelClient.GameOver += OnDuelGameOver;
            duelClient.Disconnected += OnDuelDisconnected;
            duelClient.EmojiReceived += OnDuelEmoji;
            BuildEmojiBar();

            // Duel boards are mid-difficulty, so coins are counted like the middle level of that difficulty
            level = Levels.Id(game.Difficulty, Levels.PerDifficulty / 2);
            duelCoins = 0;
            for (int i = 0; i < 81; i++)
            {
                puzzle[i / 9, i % 9] = game.Puzzle[i];
                solution[i / 9, i % 9] = game.Solution[i];
            }
            StartBoard();
            duelScored = new bool[9, 9];

            LevelText.Text = "⚔️ דו-קרב";
            SetDuelUi(true);
            RefreshScoreboard();
            RunCountdown();
        }

        private void EndDuel()
        {
            if (duelClient != null)
            {
                duelClient.PlayersUpdated -= OnDuelPlayersUpdated;
                duelClient.GameOver -= OnDuelGameOver;
                duelClient.Disconnected -= OnDuelDisconnected;
                duelClient.EmojiReceived -= OnDuelEmoji;
            }
            duelClient = null;
            SetDuelUi(false);
            DuelOverlay.Visibility = Visibility.Collapsed;
            CountdownOverlay.Visibility = Visibility.Collapsed;
        }

        /// <summary>Hides what doesn't fit a duel (hints, a new board, pausing, level records).</summary>
        private void SetDuelUi(bool duel)
        {
            var normalOnly = duel ? Visibility.Collapsed : Visibility.Visible;
            HintButton.Visibility = normalOnly;
            RestartButton.Visibility = normalOnly;
            NewBoardButton.Visibility = normalOnly;
            PauseButton.Visibility = normalOnly;
            MistakesButton.Visibility = normalOnly;
            SpeedText.Visibility = normalOnly;
            BestText.Visibility = normalOnly;
            DuelScoreboard.Visibility = duel ? Visibility.Visible : Visibility.Collapsed;
            EmojiBar.Visibility = duel ? Visibility.Visible : Visibility.Collapsed;
        }

        // ---------- Quick reactions ----------

        private static readonly string[] Reactions = { "👏", "😮", "🔥", "😂", "💖", "😅" };
        private DateTime lastEmojiSent = DateTime.MinValue;

        private void BuildEmojiBar()
        {
            if (EmojiBar.Children.Count > 0)
                return;

            foreach (string emoji in Reactions)
            {
                var button = new Button
                {
                    Content = emoji,
                    Style = (Style)Application.Current.Resources["CircleButton"],
                    FontSize = 22,
                };
                Windows.UI.Xaml.Automation.AutomationProperties.SetName(button, "תגובה " + emoji);
                button.Click += async (s, e) =>
                {
                    // At most one reaction a second, so nobody floods the room
                    if (duelClient == null || DateTime.Now - lastEmojiSent < TimeSpan.FromSeconds(1))
                        return;
                    lastEmojiSent = DateTime.Now;
                    await duelClient.SendEmojiAsync(emoji);
                };
                EmojiBar.Children.Add(button);
            }
        }

        private async void OnDuelEmoji(EmojiMessage message) =>
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => ShowEmojiBubble(message));

        // A bubble with the name and emoji that floats up and fades away
        private void ShowEmojiBubble(EmojiMessage message)
        {
            var transform = new Windows.UI.Xaml.Media.TranslateTransform();
            var bubble = new Border
            {
                Background = Res("ButtonHoverBrush"),
                BorderBrush = Res("RoseBrush"),
                BorderThickness = new Thickness(2),
                CornerRadius = new CornerRadius(20),
                Padding = new Thickness(14, 6, 14, 6),
                RenderTransform = transform,
                Child = new TextBlock
                {
                    Text = $"{message.Name} {message.Emoji}",
                    FontSize = 22,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = Res("BerryBrush"),
                },
            };
            var random = new Random();
            Canvas.SetLeft(bubble, ActualWidth * (0.1 + random.NextDouble() * 0.7));
            Canvas.SetTop(bubble, ActualHeight * 0.75);
            CelebrationCanvas.Children.Add(bubble);

            var storyboard = new Windows.UI.Xaml.Media.Animation.Storyboard();
            var rise = new Windows.UI.Xaml.Media.Animation.DoubleAnimation { To = -ActualHeight * 0.45, Duration = TimeSpan.FromSeconds(2.6) };
            Windows.UI.Xaml.Media.Animation.Storyboard.SetTarget(rise, transform);
            Windows.UI.Xaml.Media.Animation.Storyboard.SetTargetProperty(rise, "Y");
            var fade = new Windows.UI.Xaml.Media.Animation.DoubleAnimation { To = 0, BeginTime = TimeSpan.FromSeconds(1.6), Duration = TimeSpan.FromSeconds(1) };
            Windows.UI.Xaml.Media.Animation.Storyboard.SetTarget(fade, bubble);
            Windows.UI.Xaml.Media.Animation.Storyboard.SetTargetProperty(fade, "Opacity");
            storyboard.Children.Add(rise);
            storyboard.Children.Add(fade);
            storyboard.Completed += (s, e) => CelebrationCanvas.Children.Remove(bubble);
            storyboard.Begin();
        }

        // The "Start" dialog from the protocol: 5, 4, 3, 2, 1, "Go!", then the clock starts
        private void RunCountdown()
        {
            timer.Stop();
            int count = CountdownFrom;
            CountdownText.Text = count.ToString();
            CountdownOverlay.Visibility = Visibility.Visible;
            Sounds.Play("correct");

            var tick = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            tick.Tick += (s, e) =>
            {
                count--;
                if (count > 0)
                {
                    CountdownText.Text = count.ToString();
                    Sounds.Play("correct");
                }
                else if (count == 0)
                {
                    CountdownText.Text = "יאללה! 🚀";
                    Sounds.Play("coin");
                }
                else
                {
                    tick.Stop();
                    CountdownOverlay.Visibility = Visibility.Collapsed;
                    seconds = 0;
                    timer.Start();
                }
            };
            tick.Start();
        }

        /// <summary>
        /// Called after every move in a duel: updates this player's score on the server.
        /// Each cell scores only the first time it's filled correctly, so erasing or undoing can't farm points.
        /// </summary>
        private void DuelMove(bool correct, int row, int col)
        {
            if (duelClient == null)
                return;

            var me = duelClient.Me;
            if (correct)
            {
                if (duelScored[row, col])
                    return;
                duelScored[row, col] = true;
                me.Score += DuelScoring.CorrectCell;
            }
            else
            {
                me.Score = Math.Max(0, me.Score - DuelScoring.Mistake);
            }
            _ = duelClient.ChangePlayerAsync();
            RefreshScoreboard();
        }

        /// <summary>This player is done: solved the board, ran out of hearts, or left.</summary>
        private void DuelFinish(bool solved)
        {
            if (duelClient == null)
                return;

            finished = true;
            timer.Stop();
            var me = duelClient.Me;
            me.Status = PlayerStatus.Finish;
            me.Solved = solved;
            _ = duelClient.ChangePlayerAsync();

            // Solving the board earns coins just like a regular level (no level progress is saved)
            if (solved)
            {
                int stars = mistakes == 0 ? 3 : mistakes <= 2 ? 2 : 1;
                duelCoins = GameData.RewardCoins(level, stars, seconds);
                GameData.AddCoins(duelCoins);
                UpdateStats();
                Sounds.Play("win");
                Celebrate();
            }

            DuelTitle.Text = solved ? "סיימת! 🎉" : "נגמרו הלבבות 💔";
            DuelText.Text = (solved ? $"🪙 +{duelCoins} מטבעות · ⏱ {FormatTime(seconds)}\n" : "")
                + $"מחכים שכל השחקנים יסיימו... (המנצח/ת מקבל/ת עוד {WinnerBonus} 🪙)";
            DuelButtons.Visibility = Visibility.Collapsed;
            DuelOverlay.Visibility = Visibility.Visible;
            ShowRanking(duelClient.Players);
        }

        private void RefreshScoreboard()
        {
            if (duelClient == null)
                return;

            DuelScoreboard.Children.Clear();
            foreach (var player in duelClient.Players.OrderByDescending(p => p.Score))
            {
                bool me = player.Name == duelClient.Me.Name;
                // My own score is shown from my moves right away, before the server echoes it back
                int score = me ? Math.Max(player.Score, duelClient.Me.Score) : player.Score;
                DuelScoreboard.Children.Add(new Border
                {
                    Background = Res(me ? "SelectedCellBrush" : "RelatedCellBrush"),
                    BorderBrush = Res(me ? "RoseBrush" : "SoftPinkBrush"),
                    BorderThickness = new Thickness(1.5),
                    CornerRadius = new CornerRadius(14),
                    Padding = new Thickness(14, 6, 14, 6),
                    Child = new TextBlock
                    {
                        Text = $"{player.Name}  {score}{(player.Status == PlayerStatus.Finish ? " ✓" : "")}",
                        FontSize = 18,
                        FontWeight = me ? FontWeights.Bold : FontWeights.SemiBold,
                        Foreground = Res("BerryBrush"),
                    },
                });
            }
        }

        private void ShowRanking(IEnumerable<Player> players)
        {
            DuelRanking.Children.Clear();
            int place = 1;
            foreach (var player in players.OrderByDescending(p => p.Score))
            {
                string medal = place switch { 1 => "🥇", 2 => "🥈", 3 => "🥉", _ => $"{place}." };
                DuelRanking.Children.Add(new TextBlock
                {
                    Text = $"{medal}  {player.Name} — {player.Score} נקודות",
                    FontSize = 19,
                    FontWeight = place == 1 ? FontWeights.Bold : FontWeights.Normal,
                    Foreground = Res("BerryBrush"),
                    HorizontalAlignment = HorizontalAlignment.Center,
                });
                place++;
            }
        }

        // ---------- Server events (background thread -> UI thread) ----------

        private async void OnDuelPlayersUpdated() =>
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                RefreshScoreboard();
                if (DuelOverlay.Visibility == Visibility.Visible && duelClient != null && DuelButtons.Visibility != Visibility.Visible)
                    ShowRanking(duelClient.Players);
            });

        // GameOver: show the winner, and give coins (more to the winner)
        private async void OnDuelGameOver(GameResult result) =>
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                if (duelClient == null)
                    return;

                finished = true;
                timer.Stop();
                bool iWon = result.Winner == duelClient.Me.Name;
                if (iWon)
                {
                    GameData.AddCoins(WinnerBonus);
                    Sounds.Play("win");
                    Celebrate();
                    Achievements.Unlock("duel-win");
                    if (GameData.AddToCounter("duelWins") >= 5)
                        Achievements.Unlock("duel-5");
                }

                int total = duelCoins + (iWon ? WinnerBonus : 0);
                DuelTitle.Text = iWon ? "ניצחת! 🏆" : "המשחק נגמר";
                DuelText.Text = $"🏆 המנצח/ת: {result.Winner}\n"
                    + (duelCoins > 0 ? $"פתרון הלוח: +{duelCoins} 🪙\n" : "")
                    + (iWon ? $"בונוס ניצחון: +{WinnerBonus} 🪙\n" : "")
                    + $"🪙 סה\"כ בדו-קרב: +{total}";
                ShowRanking(result.Players);
                DuelButtons.Visibility = Visibility.Visible;
                DuelOverlay.Visibility = Visibility.Visible;
                UpdateStats();
            });

        private async void OnDuelDisconnected() =>
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                finished = true;
                timer.Stop();
                DuelTitle.Text = "החיבור נותק";
                DuelText.Text = "השרת לא זמין יותר.";
                DuelRanking.Children.Clear();
                DuelButtons.Visibility = Visibility.Visible;
                DuelOverlay.Visibility = Visibility.Visible;
            });

        private void DuelLobby_Click(object sender, RoutedEventArgs e)
        {
            EndDuel();
            if (Frame.CanGoBack)
                Frame.GoBack();
        }

        private void DuelExit_Click(object sender, RoutedEventArgs e)
        {
            Client.Current?.Disconnect();
            EndDuel();
            Frame.Navigate(typeof(MenuPage));
            Frame.BackStack.Clear();
        }

        /// <summary>Leaving in the middle of a duel counts as finishing (without solving).</summary>
        private void LeaveDuel()
        {
            if (duelClient != null && !finished)
            {
                duelClient.Me.Status = PlayerStatus.Finish;
                duelClient.Me.Solved = false;
                _ = duelClient.ChangePlayerAsync();
            }
            EndDuel();
        }
    }
}
