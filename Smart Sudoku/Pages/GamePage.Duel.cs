using System;
using System.Collections.Generic;
using System.Linq;
using Smart_Sudoku.Duel;
using Windows.UI.Core;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;

namespace Smart_Sudoku.Pages
{
    /// <summary>
    /// Duel mode: everyone in the room plays the same board, sending their score to the server
    /// (ChangePlayer) and seeing everyone else's score live (UpdatePlayers) until GameOver.
    /// </summary>
    public sealed partial class GamePage
    // אובייקט: GamePage (חלק הדו-קרב ברשת) — כל מי שבחדר משחק על אותו לוח. כל שחקן שולח לשרת את הניקוד שלו (ChangePlayer),
    // ורואה בזמן אמת את הניקוד של כולם (UpdatePlayers), עד שהמשחק נגמר (GameOver).
    {
        /// <summary>Extra coins for winning a duel, on top of the regular coins for solving the board.</summary>
        private const int WinnerBonus = 150;
        // אובייקט: WinnerBonus — כמה מטבעות בונוס מקבל המנצח בדו-קרב (150), מעבר למטבעות על פתרון הלוח.
        private const int CountdownFrom = 5;
        // אובייקט: CountdownFrom — מאיזה מספר מתחילה הספירה לאחור (5).

        private int duelCoins;  // coins earned by solving the board in this duel
        // אובייקט: duelCoins — כמה מטבעות השחקן הרוויח בדו-קרב הזה על פתרון הלוח.

        private Client? duelClient;
        // אובייקט: duelClient — החיבור לשרת בזמן הדו-קרב (או null כשלא בדו-קרב).
        private bool[,] duelScored = new bool[9, 9];
        // אובייקט: duelScored — אילו משבצות כבר נתנו נקודות (כדי שאי אפשר יהיה "לצבור" נקודות על אותה משבצת שוב ושוב).
        private bool duelTeams;
        // אובייקט: duelTeams — אמת אם הדו-קרב הוא בקבוצות.
        private bool IsDuel => duelClient != null;
        // אובייקט: IsDuel — אמת כשמשחקים עכשיו דו-קרב ברשת.

        private void StartDuel(DuelGame game)
        // פעולה: StartDuel — מתחילה דו-קרב ברשת: מקשיבה להודעות מהשרת, טוענת את הלוח שהשרת שלח, מאפסת ניקוד ומתחילה ספירה לאחור.
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
            duelTeams = game.Teams;
            level = Levels.Id(game.Difficulty, Levels.PerDifficulty / 2);
            // אובייקט: level — השלב שלפיו נספרים המטבעות (השלב האמצעי ברמת הקושי של הדו-קרב).
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
        // פעולה: EndDuel — מסיימת את מצב הדו-קרב: מפסיקה להקשיב לשרת ומסתירה את החלונות של הדו-קרב.
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
        // פעולה: SetDuelUi — מסתירה בדו-קרב את מה שלא מתאים לו (רמזים, לוח חדש, עצירה, שיאים), ומציגה את פס התוצאות ואת כפתורי האימוג'י.
        {
            var normalOnly = duel ? Visibility.Collapsed : Visibility.Visible;
            // אובייקט: normalOnly — "גלוי" במשחק רגיל ו"מוסתר" בדו-קרב.
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
        // אובייקט: Reactions — האימוג'ים שאפשר לשלוח בזמן דו-קרב.
        private DateTime lastEmojiSent = DateTime.MinValue;
        // אובייקט: lastEmojiSent — מתי נשלח האימוג'י האחרון (כדי לא לשלוח יותר מאחד בשנייה).

        private void BuildEmojiBar()
        // פעולה: BuildEmojiBar — בונה את שורת כפתורי האימוג'י (פעם אחת בלבד).
        {
            if (EmojiBar.Children.Count > 0)
                return;

            foreach (string emoji in Reactions)
            {
                var button = new Button
                // אובייקט: button — כפתור של אימוג'י אחד.
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
        // פעולה: OnDuelEmoji — כשמגיע אימוג'י משחקן אחר: מציגה אותו על המסך.
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => ShowEmojiBubble(message));

        // A bubble with the name and emoji that floats up and fades away
        private void ShowEmojiBubble(EmojiMessage message)
        // פעולה: ShowEmojiBubble — מציגה "בועה" עם השם והאימוג'י, שעולה למעלה ונעלמת לאט.
        {
            var transform = new TranslateTransform();
            // אובייקט: transform — ההזזה של הבועה כלפי מעלה.
            var bubble = new Border
            // אובייקט: bubble — הבועה עצמה.
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
            // אובייקט: random — מחולל אקראי למקום של הבועה.
            Canvas.SetLeft(bubble, ActualWidth * (0.1 + random.NextDouble() * 0.7));
            Canvas.SetTop(bubble, ActualHeight * 0.75);
            CelebrationCanvas.Children.Add(bubble);

            var storyboard = new Storyboard();
            // אובייקט: storyboard — ה"תסריט" של האנימציה.
            var rise = new DoubleAnimation { To = -ActualHeight * 0.45, Duration = TimeSpan.FromSeconds(2.6) };
            // אובייקט: rise — האנימציה של העלייה.
            Storyboard.SetTarget(rise, transform);
            Storyboard.SetTargetProperty(rise, "Y");
            var fade = new DoubleAnimation { To = 0, BeginTime = TimeSpan.FromSeconds(1.6), Duration = TimeSpan.FromSeconds(1) };
            // אובייקט: fade — האנימציה של ההיעלמות.
            Storyboard.SetTarget(fade, bubble);
            Storyboard.SetTargetProperty(fade, "Opacity");
            storyboard.Children.Add(rise);
            storyboard.Children.Add(fade);
            storyboard.Completed += (s, e) => CelebrationCanvas.Children.Remove(bubble);
            storyboard.Begin();
        }

        // The "Start" dialog from the protocol: 5, 4, 3, 2, 1, "Go!", then the clock starts
        private void RunCountdown()
        // פעולה: RunCountdown — חלון הספירה לאחור: 5, 4, 3, 2, 1, "יאללה!", ואז השעון מתחיל (וגם המחשב, בדו-קרב נגד המחשב).
        {
            timer.Stop();
            int count = CountdownFrom;
            // אובייקט: count — המספר הנוכחי בספירה.
            CountdownText.Text = count.ToString();
            CountdownOverlay.Visibility = Visibility.Visible;
            Sounds.Play("correct");

            var tick = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            // אובייקט: tick — שעון שמוריד מספר בכל שנייה.
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
                    StartAiTimer();   // only does something in a duel against the computer
                }
            };
            tick.Start();
        }

        /// <summary>
        /// Called after every move in a duel: updates this player's score on the server.
        /// Each cell scores only the first time it's filled correctly, so erasing or undoing can't farm points.
        /// </summary>
        private void DuelMove(bool correct, int row, int col)
        // פעולה: DuelMove — רצה אחרי כל מהלך בדו-קרב: מעדכנת את הניקוד שלי ושולחת אותו לשרת.
        // כל משבצת נותנת נקודות רק בפעם הראשונה שהיא נכונה, כדי שמחיקה ו"בטל" לא יאפשרו לצבור נקודות.
        {
            if (duelClient == null)
                return;

            var me = duelClient.Me;
            // אובייקט: me — השחקן שלי בדו-קרב.
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
        // פעולה: DuelFinish — השחקן סיים: פתר את הלוח, נגמרו לו הלבבות, או יצא. מודיעה לשרת, נותנת מטבעות על פתרון,
        // ומציגה חלון "מחכים שכולם יסיימו".
        {
            if (duelClient == null)
                return;

            finished = true;
            timer.Stop();
            var me = duelClient.Me;
            // אובייקט: me — השחקן שלי בדו-קרב.
            me.Status = PlayerStatus.Finish;
            me.Solved = solved;
            _ = duelClient.ChangePlayerAsync();

            // Solving the board earns coins just like a regular level (no level progress is saved)
            if (solved)
            {
                int stars = StarsFor(mistakes);
                // אובייקט: stars — כמה כוכבים (לפי הטעויות), כדי לחשב מטבעות.
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
        // פעולה: RefreshScoreboard — מעדכנת את פס התוצאות שמעל הלוח: בקבוצות קודם הסכום של כל קבוצה, ואז כל שחקן לפי הניקוד.
        {
            if (duelClient == null)
                return;

            DuelScoreboard.Children.Clear();

            // Team duels: the two team totals come first
            if (duelTeams)
            {
                AddTeamTotal(Teams.Red, "🔴", Windows.UI.Color.FromArgb(255, 255, 214, 214));
                AddTeamTotal(Teams.Blue, "🔵", Windows.UI.Color.FromArgb(255, 210, 228, 255));
            }

            foreach (var player in duelClient.Players.OrderByDescending(p => p.Score))
            {
                bool me = player.Name == duelClient.Me.Name;
                // אובייקט: me — אמת אם זה השחקן שלי.
                int score = ShownScore(player);
                // אובייקט: score — הניקוד שמוצג לשחקן.

                string team = "";
                // אובייקט: team — העיגול של הקבוצה (אדום או כחול), אם יש.
                if (player.Team == Teams.Red)
                    team = "🔴 ";
                else if (player.Team == Teams.Blue)
                    team = "🔵 ";
                string done = player.Status == PlayerStatus.Finish ? " ✓" : "";
                // אובייקט: done — סימן ✓ אם השחקן כבר סיים.

                DuelScoreboard.Children.Add(new Border
                {
                    Background = Res(me ? "SelectedCellBrush" : "RelatedCellBrush"),
                    BorderBrush = Res(me ? "RoseBrush" : "SoftPinkBrush"),
                    BorderThickness = new Thickness(1.5),
                    CornerRadius = new CornerRadius(14),
                    Padding = new Thickness(14, 6, 14, 6),
                    Child = new TextBlock
                    {
                        Text = $"{team}{player.Name}  {score}{done}",
                        FontSize = 18,
                        FontWeight = me ? FontWeights.Bold : FontWeights.SemiBold,
                        Foreground = Res("BerryBrush"),
                    },
                });
            }
        }

        // My own score is shown from my moves right away, before the server sends it back
        private int ShownScore(Player player)
        // פעולה: ShownScore — הניקוד שמוצג לשחקן. הניקוד שלי מוצג מיד לפי המהלכים שלי, עוד לפני שהשרת מחזיר אותו.
        {
            if (duelClient != null && player.Name == duelClient.Me.Name)
                return Math.Max(player.Score, duelClient.Me.Score);
            return player.Score;
        }

        // One team's total points, in its own color
        private void AddTeamTotal(string team, string emoji, Windows.UI.Color color)
        // פעולה: AddTeamTotal — מוסיפה לפס התוצאות את סך הנקודות של קבוצה, בצבע שלה.
        {
            if (duelClient == null)
                return;

            int total = 0;
            // אובייקט: total — סך הנקודות של הקבוצה.
            foreach (var player in duelClient.Players)
            {
                if (player.Team == team)
                    total += ShownScore(player);
            }

            DuelScoreboard.Children.Add(new Border
            {
                Background = new SolidColorBrush(color),
                CornerRadius = new CornerRadius(14),
                Padding = new Thickness(14, 6, 14, 6),
                Child = new TextBlock
                {
                    Text = $"{emoji} {total}",
                    FontSize = 20,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 60, 30, 60)),
                },
            });
        }

        private void ShowRanking(IEnumerable<Player> players)
        // פעולה: ShowRanking — מציגה את טבלת המקומות בחלון התוצאות, עם מדליות.
        {
            DuelRanking.Children.Clear();
            int place = 1;
            // אובייקט: place — המקום בטבלה.
            foreach (var player in players.OrderByDescending(p => p.Score))
            {
                string medal = Medal(place);
                // אובייקט: medal — המדליה או מספר המקום.
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
        // פעולה: OnDuelPlayersUpdated — כשמגיעה רשימת שחקנים מהשרת: מעדכנת את פס התוצאות (ואת הטבלה, אם חלון ההמתנה פתוח).
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                RefreshScoreboard();
                if (DuelOverlay.Visibility == Visibility.Visible && duelClient != null && DuelButtons.Visibility != Visibility.Visible)
                    ShowRanking(duelClient.Players);
            });

        // GameOver: show the winner, and give coins (more to the winner)
        private async void OnDuelGameOver(GameResult result) =>
        // פעולה: OnDuelGameOver — הדו-קרב נגמר: מציגה מי ניצח (או איזו קבוצה), נותנת בונוס למנצח, פותחת הישגים, ומציגה את כל המטבעות שהרווחתי.
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                if (duelClient == null)
                    return;

                finished = true;
                timer.Stop();
                // In teams, everyone on the winning team wins
                bool teamGame = result.WinnerTeam.Length > 0;
                // אובייקט: teamGame — אמת אם זה היה דו-קרב קבוצות.
                bool iWon;
                if (teamGame)
                    iWon = duelClient.Me.Team == result.WinnerTeam;
                else
                    iWon = result.Winner == duelClient.Me.Name;
                if (iWon)
                {
                    GameData.AddCoins(WinnerBonus);
                    Sounds.Play("win");
                    Celebrate();
                    Achievements.Unlock("duel-win");
                    if (GameData.AddToCounter("duelWins") >= 5)
                        Achievements.Unlock("duel-5");
                }

                Progression.DuelPlayed(iWon);
                int total = duelCoins + (iWon ? WinnerBonus : 0);
                // אובייקט: total — כל המטבעות שהרווחתי בדו-קרב (כולל הבונוס אם ניצחתי).
                DuelTitle.Text = iWon ? "ניצחת! 🏆" : "המשחק נגמר";
                string text;
                if (teamGame)
                    text = $"🏆 ניצחה {result.Winner}\n🔴 {result.RedScore}  ·  🔵 {result.BlueScore}\n";
                else
                    text = $"🏆 המנצח/ת: {result.Winner}\n";
                if (duelCoins > 0)
                    text += $"פתרון הלוח: +{duelCoins} 🪙\n";
                if (iWon)
                    text += $"בונוס ניצחון: +{WinnerBonus} 🪙\n";
                text += $"🪙 סה\"כ בדו-קרב: +{total}";
                DuelText.Text = text;
                ShowRanking(result.Players);
                DuelButtons.Visibility = Visibility.Visible;
                DuelOverlay.Visibility = Visibility.Visible;
                UpdateStats();
            });

        private async void OnDuelDisconnected() =>
        // פעולה: OnDuelDisconnected — החיבור לשרת נותק באמצע: עוצרת את המשחק ומציגה הודעה.
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
        // פעולה: DuelLobby_Click — כפתור "סיבוב נוסף": נגד המחשב — לוח חדש מול אותו יריב. ברשת — חזרה לחדר ההמתנה.
        {
            // Against the computer, "another round" means a new board with the same opponent
            if (ai != null)
            {
                StartAiDuel(new AiDuelStart(ai.Id));
                return;
            }
            EndDuel();
            if (Frame.CanGoBack)
                Frame.GoBack();
        }

        private void DuelExit_Click(object sender, RoutedEventArgs e)
        // פעולה: DuelExit_Click — כפתור "יציאה": מתנתקת וחוזרת לתפריט הראשי.
        {
            if (IsAiDuel)
                EndAiDuel();
            else
                Client.Current?.Disconnect();
            EndDuel();
            Frame.Navigate(typeof(MenuPage));
            Frame.BackStack.Clear();
        }

        /// <summary>Leaving in the middle of a duel counts as finishing (without solving).</summary>
        private void LeaveDuel()
        // פעולה: LeaveDuel — יציאה באמצע דו-קרב נחשבת כסיום (בלי פתרון), ומודיעים על זה לשרת.
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
