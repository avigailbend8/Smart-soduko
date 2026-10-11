using System;
using System.Collections.Generic;
using Windows.UI;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;

namespace Smart_Sudoku.Pages
{
    /// <summary>
    /// The Sudoku board. Receives the level number from LevelsPage.
    /// </summary>
    public sealed partial class GamePage : Page
    {
        private const double CellSize = 50;
        private const int MaxLives = 5;
        public const int HeartPrice = 30;
        private const int FreeHints = 3;

        private static readonly SolidColorBrush WrongBrush = new SolidColorBrush(Color.FromArgb(255, 217, 58, 74));
        private static readonly SolidColorBrush PlainBg = new SolidColorBrush(Colors.White);
        private static readonly Random Rnd = new Random();

        private readonly Border[,] cells = new Border[9, 9];
        private readonly TextBlock[,] cellTexts = new TextBlock[9, 9];
        private readonly TextBlock[,,] noteTexts = new TextBlock[9, 9, 9];
        private readonly DispatcherTimer timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        private readonly Stack<(int[,] Board, int[,] Notes, bool[,] Hinted)> history = new Stack<(int[,], int[,], bool[,])>();

        private int level = 1;  // level id: difficulty * 100 + level number (see Levels)
        private int[,] puzzle = new int[9, 9];
        private int[,] solution = new int[9, 9];
        private int[,] board = new int[9, 9];
        private int[,] notes = new int[9, 9];      // bit n set = note for number n
        private bool[,] hinted = new bool[9, 9];
        private bool hasGame, finished, notesMode, paused;
        private int selRow = -1, selCol = -1;
        private int lives, mistakes, hintsUsed, seconds;

        public GamePage()
        {
            this.InitializeComponent();
            BuildBoard();
            AddBoxLines();
            BuildNumberPad();
            timer.Tick += (s, e) =>
            {
                seconds++;
                UpdateStats();
            };
        }

        private static Brush Res(string key) => (Brush)Application.Current.Resources[key];

        private static string Symbol(int n) => SymbolSets.Current.Symbols[n - 1];

        private int HintsLeft => Math.Max(0, FreeHints - hintsUsed) + GameData.BonusHints;

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            // Coming back from the help page keeps the current game
            if (e.NavigationMode == NavigationMode.Back && hasGame)
            {
                if (!finished && !paused)
                    timer.Start();
                Refresh();
                return;
            }

            if (e.Parameter is int chosenLevel)
                level = chosenLevel;

            NewGame();
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
            timer.Stop();
        }

        // ---------- Building the UI ----------

        private void BuildBoard()
        {
            for (int i = 0; i < 9; i++)
            {
                BoardGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(CellSize) });
                BoardGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(CellSize) });
            }

            for (int r = 0; r < 9; r++)
            {
                for (int c = 0; c < 9; c++)
                {
                    // Small 3x3 grid for notes, with the big value on top
                    var notesGrid = new Grid { Margin = new Thickness(2) };
                    for (int i = 0; i < 3; i++)
                    {
                        notesGrid.RowDefinitions.Add(new RowDefinition());
                        notesGrid.ColumnDefinitions.Add(new ColumnDefinition());
                    }
                    for (int k = 0; k < 9; k++)
                    {
                        var note = new TextBlock
                        {
                            FontSize = 11,
                            Foreground = Res("MutedTextBrush"),
                            HorizontalAlignment = HorizontalAlignment.Center,
                            VerticalAlignment = VerticalAlignment.Center,
                        };
                        Grid.SetRow(note, k / 3);
                        Grid.SetColumn(note, k % 3);
                        notesGrid.Children.Add(note);
                        noteTexts[r, c, k] = note;
                    }

                    var text = new TextBlock
                    {
                        FontSize = 26,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                    };

                    var content = new Grid();
                    content.Children.Add(notesGrid);
                    content.Children.Add(text);

                    var cell = new Border
                    {
                        Background = PlainBg,
                        BorderBrush = Res("SoftPinkBrush"),
                        BorderThickness = new Thickness(c == 0 ? 0 : 1, r == 0 ? 0 : 1, 0, 0),
                        Child = content,
                        Tag = r * 9 + c,
                        // Round the corner cells so highlights stay inside the board's rounded frame
                        CornerRadius = new CornerRadius(
                            r == 0 && c == 0 ? 11 : 0,
                            r == 0 && c == 8 ? 11 : 0,
                            r == 8 && c == 8 ? 11 : 0,
                            r == 8 && c == 0 ? 11 : 0),
                    };
                    cell.Tapped += Cell_Tapped;

                    Grid.SetRow(cell, r);
                    Grid.SetColumn(cell, c);
                    BoardGrid.Children.Add(cell);
                    cells[r, c] = cell;
                    cellTexts[r, c] = text;
                }
            }
        }

        // Thick lines between the 3x3 boxes, drawn over the cells
        private void AddBoxLines()
        {
            foreach (int i in new[] { 3, 6 })
            {
                var vertical = new Windows.UI.Xaml.Shapes.Rectangle
                {
                    Width = 3,
                    Fill = Res("RoseBrush"),
                    HorizontalAlignment = HorizontalAlignment.Left,
                    Margin = new Thickness(-1, 0, 0, 0),
                    IsHitTestVisible = false,
                };
                Grid.SetColumn(vertical, i);
                Grid.SetRowSpan(vertical, 9);
                BoardGrid.Children.Add(vertical);

                var horizontal = new Windows.UI.Xaml.Shapes.Rectangle
                {
                    Height = 3,
                    Fill = Res("RoseBrush"),
                    VerticalAlignment = VerticalAlignment.Top,
                    Margin = new Thickness(0, -1, 0, 0),
                    IsHitTestVisible = false,
                };
                Grid.SetRow(horizontal, i);
                Grid.SetColumnSpan(horizontal, 9);
                BoardGrid.Children.Add(horizontal);
            }
        }

        private void BuildNumberPad()
        {
            for (int n = 1; n <= 9; n++)
            {
                var button = new Button
                {
                    Style = (Style)Application.Current.Resources["NumberButton"],
                    Tag = n,
                };
                Windows.UI.Xaml.Automation.AutomationProperties.SetName(button, n.ToString());
                button.Click += NumberButton_Click;
                NumberPad.Children.Add(button);
            }
        }

        // ---------- Game flow ----------

        private void NewGame()
        {
            (puzzle, solution) = SudokuGenerator.Create(Levels.EmptyCells(level));
            StartBoard();
        }

        // Starts (or restarts) the current puzzle from scratch
        private void StartBoard()
        {
            board = (int[,])puzzle.Clone();
            notes = new int[9, 9];
            hinted = new bool[9, 9];
            history.Clear();
            lives = MaxLives;
            mistakes = 0;
            hintsUsed = 0;
            seconds = 0;
            finished = false;
            notesMode = false;
            paused = false;
            hasGame = true;
            selRow = selCol = -1;

            var difficulty = Levels.DifficultyFor(level);
            LevelText.Text = $"Level {Levels.NumberOf(level)} · {difficulty.Name} {difficulty.Emoji}";
            StatusText.Text = "בחרו משבצת ריקה ואז מספר";
            WinOverlay.Visibility = Visibility.Collapsed;
            GameOverOverlay.Visibility = Visibility.Collapsed;
            PauseOverlay.Visibility = Visibility.Collapsed;
            CelebrationCanvas.Children.Clear();

            timer.Start();
            Refresh();
        }

        private void PushHistory() =>
            history.Push(((int[,])board.Clone(), (int[,])notes.Clone(), (bool[,])hinted.Clone()));

        private bool IsSolved()
        {
            for (int r = 0; r < 9; r++)
            {
                for (int c = 0; c < 9; c++)
                {
                    if (board[r, c] != solution[r, c])
                        return false;
                }
            }
            return true;
        }

        // A correct number removes the same note from its row, column and box
        private void RemoveNoteFromPeers(int row, int col, int n)
        {
            int bit = 1 << n;
            for (int i = 0; i < 9; i++)
            {
                notes[row, i] &= ~bit;
                notes[i, col] &= ~bit;
            }
            int br = row / 3 * 3, bc = col / 3 * 3;
            for (int r = br; r < br + 3; r++)
            {
                for (int c = bc; c < bc + 3; c++)
                    notes[r, c] &= ~bit;
            }
        }

        private void PlaceValue(int row, int col, int n)
        {
            board[row, col] = n;
            notes[row, col] = 0;
            if (n == solution[row, col])
                RemoveNoteFromPeers(row, col, n);
        }

        private void Win()
        {
            finished = true;
            timer.Stop();

            int earnedStars = (mistakes + hintsUsed) switch
            {
                0 => 3,
                <= 2 => 2,
                _ => 1,
            };
            int oldBest = GameData.GetBestTime(level);
            int earnedCoins = GameData.RecordWin(level, earnedStars, seconds);
            bool newBest = oldBest == 0 || seconds < oldBest;
            int? next = Levels.Next(level);
            var difficulty = Levels.DifficultyFor(level);
            bool finishedDifficulty = Levels.NumberOf(level) == Levels.PerDifficulty;

            WinStarsText.Text = new string('★', earnedStars) + new string('☆', 3 - earnedStars);
            WinText.Text = next == null
                ? "סיימת את כל השלבים בכל הדרגות! איזו אלופה 💖"
                : finishedDifficulty
                    ? $"סיימת את כל דרגת {difficulty.Name}! {difficulty.Emoji} 🎉"
                    : $"פתרת את שלב {Levels.NumberOf(level)} 🎉";
            WinDetailsText.Text =
                $"⏱ {FormatTime(seconds)}" +
                (newBest ? "   🏆 שיא חדש!" : $"   (שיא: {FormatTime(oldBest)})") +
                $"\n\nשלב: {GameData.LevelCoins(level)}+" +
                $"\nכוכבים: {GameData.StarCoins(earnedStars)}+" +
                $"\nמהירות ⚡: {GameData.SpeedCoins(level, seconds)}+" +
                $"\nבונוס כפול: ×{GameData.CoinMultiplier}" +
                $"\n🪙 סה\"כ: {earnedCoins}+ מטבעות";
            NextLevelButton.Content = next == null
                ? "חזרה לשלבים"
                : finishedDifficulty
                    ? $"לדרגה הבאה: {Levels.DifficultyFor(next.Value).Name}  ▶"
                    : "לשלב הבא  ▶";
            BackToLevelsButton.Visibility = next == null ? Visibility.Collapsed : Visibility.Visible;
            WinOverlay.Visibility = Visibility.Visible;

            Sounds.Play("win");
            Celebrate();
            Refresh();
        }

        private void GameOver()
        {
            finished = true;
            timer.Stop();
            BuyHeartButton.Content = $"❤ לב נוסף · {HeartPrice} 🪙";
            GameOverMessage.Text = "לא נורא! כל טעות היא חלק מהלמידה 💕";
            GameOverOverlay.Visibility = Visibility.Visible;
        }

        // Buy one heart and keep playing the same board
        private void BuyHeartButton_Click(object sender, RoutedEventArgs e)
        {
            if (!GameData.Spend(HeartPrice))
            {
                GameOverMessage.Text = $"אין מספיק מטבעות 😅 (צריך {HeartPrice} 🪙)";
                return;
            }

            Sounds.Play("coin");
            lives = 1;
            finished = false;
            GameOverOverlay.Visibility = Visibility.Collapsed;
            StatusText.Text = "קיבלת לב נוסף! ממשיכים 💖";
            timer.Start();
            Refresh();
        }

        private static string FormatTime(int totalSeconds) => $"{totalSeconds / 60:00}:{totalSeconds % 60:00}";

        // ---------- Drawing ----------

        private void Refresh()
        {
            int selectedValue = selRow >= 0 ? board[selRow, selCol] : 0;

            for (int r = 0; r < 9; r++)
            {
                for (int c = 0; c < 9; c++)
                {
                    int value = board[r, c];
                    var text = cellTexts[r, c];
                    text.Text = value == 0 ? "" : Symbol(value);

                    if (puzzle[r, c] != 0)
                    {
                        text.Foreground = Res("BerryBrush");
                        text.FontWeight = FontWeights.Bold;
                    }
                    else
                    {
                        text.Foreground = value != 0 && value != solution[r, c] ? WrongBrush : Res("RoseBrush");
                        text.FontWeight = hinted[r, c] ? FontWeights.Bold : FontWeights.SemiBold;
                    }

                    for (int k = 0; k < 9; k++)
                    {
                        bool show = value == 0 && (notes[r, c] & (1 << (k + 1))) != 0;
                        noteTexts[r, c, k].Text = show ? Symbol(k + 1) : "";
                    }

                    Brush bg = PlainBg;
                    if (selRow >= 0)
                    {
                        bool related = r == selRow || c == selCol || (r / 3 == selRow / 3 && c / 3 == selCol / 3);
                        if (r == selRow && c == selCol)
                            bg = Res("SelectedCellBrush");
                        else if (selectedValue != 0 && value == selectedValue)
                            bg = Res("SameNumberBrush");
                        else if (related)
                            bg = Res("RelatedCellBrush");
                    }
                    cells[r, c].Background = bg;
                }
            }

            UpdateNumberPad();
            UpdateStats();
            UpdateTools();
        }

        // Hide a number's button once all 9 of it are correctly placed.
        // The button keeps its spot so the other buttons don't jump around.
        private void UpdateNumberPad()
        {
            var placed = new int[10];
            for (int r = 0; r < 9; r++)
            {
                for (int c = 0; c < 9; c++)
                {
                    if (board[r, c] != 0 && board[r, c] == solution[r, c])
                        placed[board[r, c]]++;
                }
            }

            foreach (Button button in NumberPad.Children)
            {
                int n = (int)button.Tag;
                bool done = placed[n] == 9;
                button.Content = Symbol(n);
                button.Opacity = done ? 0 : 1;
                button.IsHitTestVisible = !done;
                button.IsTabStop = !done;
            }
        }

        private void UpdateStats()
        {
            TimerText.Text = $"⏱ {FormatTime(seconds)}";
            LivesText.Text = new string('♥', lives) + new string('♡', Math.Max(0, MaxLives - lives))
                + (GameData.BonusLives > 0 ? $"  +{GameData.BonusLives}" : "");
            CoinsText.Text = $"🪙 {GameData.Coins}";

            // Speed bonus shrinks as time passes, so the player sees it counting down
            SpeedText.Text = $"⚡ +{GameData.SpeedCoins(level, seconds) * GameData.CoinMultiplier}";
            int best = GameData.GetBestTime(level);
            BestText.Text = best > 0 ? $"🏆 {FormatTime(best)}" : "🏆 --:--";
        }

        private void UpdateTools()
        {
            HintText.Text = $"רמז ({HintsLeft})";
            NotesText.Text = notesMode ? "פתקים: פועל" : "פתקים";
            NotesButton.Background = notesMode ? Res("SelectedCellBrush") : PlainBg;
            NotesButton.BorderBrush = notesMode ? Res("RoseBrush") : Res("SoftPinkBrush");
            SoundIcon.Glyph = GameData.SoundOn ? "" : "";
        }

        private void Celebrate()
        {
            CelebrationCanvas.Children.Clear();
            double width = ActualWidth, height = ActualHeight;
            string[] shapes = { "♥", "✿", "✦", "❀", "♥", "💖", "🌸" };
            string[] brushes = { "RoseBrush", "DecoBrush", "SoftPinkBrush" };
            var storyboard = new Storyboard();

            for (int i = 0; i < 45; i++)
            {
                var transform = new CompositeTransform();
                var piece = new TextBlock
                {
                    Text = shapes[Rnd.Next(shapes.Length)],
                    FontFamily = new FontFamily("Segoe UI Symbol"),
                    FontSize = Rnd.Next(16, 36),
                    Foreground = Res(brushes[Rnd.Next(brushes.Length)]),
                    RenderTransform = transform,
                    RenderTransformOrigin = new Windows.Foundation.Point(0.5, 0.5),
                };
                Canvas.SetLeft(piece, Rnd.NextDouble() * width);
                Canvas.SetTop(piece, -50);
                CelebrationCanvas.Children.Add(piece);

                var duration = TimeSpan.FromSeconds(2.5 + Rnd.NextDouble() * 2);
                var delay = TimeSpan.FromSeconds(Rnd.NextDouble() * 1.2);

                var fall = new DoubleAnimation { To = height + 100, Duration = duration, BeginTime = delay };
                Storyboard.SetTarget(fall, transform);
                Storyboard.SetTargetProperty(fall, "TranslateY");
                storyboard.Children.Add(fall);

                var spin = new DoubleAnimation { To = Rnd.Next(-360, 360), Duration = duration, BeginTime = delay };
                Storyboard.SetTarget(spin, transform);
                Storyboard.SetTargetProperty(spin, "Rotation");
                storyboard.Children.Add(spin);
            }

            storyboard.Completed += (s, e) => CelebrationCanvas.Children.Clear();
            storyboard.Begin();
        }

        // ---------- Input ----------

        private void Cell_Tapped(object sender, TappedRoutedEventArgs e)
        {
            int index = (int)((Border)sender).Tag;
            selRow = index / 9;
            selCol = index % 9;
            Refresh();
        }

        private void NumberButton_Click(object sender, RoutedEventArgs e)
        {
            if (finished || selRow < 0 || puzzle[selRow, selCol] != 0)
                return;

            int n = (int)((Button)sender).Tag;

            if (notesMode)
            {
                if (board[selRow, selCol] != 0)
                    return;
                PushHistory();
                notes[selRow, selCol] ^= 1 << n;
                Refresh();
                return;
            }

            if (board[selRow, selCol] == n)
                return;

            PushHistory();
            hinted[selRow, selCol] = false;
            PlaceValue(selRow, selCol, n);

            if (n != solution[selRow, selCol])
            {
                mistakes++;
                lives--;
                Sounds.Play("wrong");
                StatusText.Text = "אופס, המספר הזה לא מתאים כאן";
                if (lives <= 0 && GameData.UseBonusLife())
                {
                    lives = 1;
                    StatusText.Text = "השתמשנו בלב שמור מהחנות 💖";
                }
                Refresh();
                if (lives <= 0)
                    GameOver();
                return;
            }

            Sounds.Play("correct");
            StatusText.Text = "יופי! 💕";
            Refresh();
            if (IsSolved())
                Win();
        }

        private void HintButton_Click(object sender, RoutedEventArgs e)
        {
            if (finished)
                return;

            if (HintsLeft == 0)
            {
                StatusText.Text = "נגמרו הרמזים — אפשר לקנות עוד בחנות 🛍️";
                return;
            }

            // Prefer the selected cell; otherwise pick a random cell that still needs a value
            int row = -1, col = -1;
            if (selRow >= 0 && puzzle[selRow, selCol] == 0 && board[selRow, selCol] != solution[selRow, selCol])
            {
                row = selRow;
                col = selCol;
            }
            else
            {
                var open = new List<int>();
                for (int i = 0; i < 81; i++)
                {
                    if (board[i / 9, i % 9] != solution[i / 9, i % 9])
                        open.Add(i);
                }
                if (open.Count == 0)
                    return;
                int pick = open[Rnd.Next(open.Count)];
                row = pick / 9;
                col = pick % 9;
            }

            PushHistory();
            if (hintsUsed >= FreeHints)
                GameData.UseBonusHint();
            hintsUsed++;

            PlaceValue(row, col, solution[row, col]);
            hinted[row, col] = true;
            selRow = row;
            selCol = col;

            Sounds.Play("hint");
            StatusText.Text = "הנה רמז קטן 💡";
            Refresh();
            if (IsSolved())
                Win();
        }

        private void UndoButton_Click(object sender, RoutedEventArgs e)
        {
            if (finished || history.Count == 0)
                return;

            (board, notes, hinted) = history.Pop();
            StatusText.Text = "ביטלנו את הצעד האחרון ↩";
            Refresh();
        }

        private void EraseButton_Click(object sender, RoutedEventArgs e)
        {
            if (finished || selRow < 0 || puzzle[selRow, selCol] != 0)
                return;
            if (board[selRow, selCol] == 0 && notes[selRow, selCol] == 0)
                return;

            PushHistory();
            board[selRow, selCol] = 0;
            notes[selRow, selCol] = 0;
            hinted[selRow, selCol] = false;
            StatusText.Text = "בחרו משבצת ריקה ואז מספר";
            Refresh();
        }

        private void NotesButton_Click(object sender, RoutedEventArgs e)
        {
            notesMode = !notesMode;
            StatusText.Text = notesMode
                ? "מצב פתקים: המספרים ייכתבו קטן, בתור אפשרויות ✏️"
                : "בחרו משבצת ריקה ואז מספר";
            UpdateTools();
        }

        // Same board, wipes everything the player did
        private void RestartButton_Click(object sender, RoutedEventArgs e)
        {
            StartBoard();
        }

        // A different board at the same level
        private void NewBoardButton_Click(object sender, RoutedEventArgs e)
        {
            NewGame();
        }

        private void NextLevelButton_Click(object sender, RoutedEventArgs e)
        {
            if (Levels.Next(level) is not int next)
            {
                if (Frame.CanGoBack)
                    Frame.GoBack();
                return;
            }

            level = next;
            NewGame();
        }

        // Stop: freezes the timer and hides the board until the player continues
        private void PauseButton_Click(object sender, RoutedEventArgs e)
        {
            if (finished)
                return;

            paused = true;
            timer.Stop();
            PauseTimeText.Text = $"הזמן נעצר על ⏱ {FormatTime(seconds)}";
            PauseOverlay.Visibility = Visibility.Visible;
        }

        private void ResumeButton_Click(object sender, RoutedEventArgs e)
        {
            paused = false;
            PauseOverlay.Visibility = Visibility.Collapsed;
            if (!finished)
                timer.Start();
        }

        private void SoundButton_Click(object sender, RoutedEventArgs e)
        {
            GameData.SetSound(!GameData.SoundOn);
            UpdateTools();
        }

        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(HelpPage));
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            if (Frame.CanGoBack)
                Frame.GoBack();
        }
    }
}
