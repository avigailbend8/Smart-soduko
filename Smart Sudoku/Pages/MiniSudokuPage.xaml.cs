using System;
using Windows.UI;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;

namespace Smart_Sudoku.Pages
{
    /// <summary>A small board (4×4 or 6×6). Same rules, fewer numbers, no hearts or timer.</summary>
    public sealed partial class MiniSudokuPage : BasePage
    {
        private static readonly SolidColorBrush WrongBrush = new SolidColorBrush(Color.FromArgb(255, 217, 58, 74));

        private int size = 4;
        private int[,] puzzle = new int[4, 4];
        private int[,] solution = new int[4, 4];
        private int[,] board = new int[4, 4];
        private Border[,] cells = new Border[4, 4];
        private TextBlock[,] texts = new TextBlock[4, 4];
        private int selRow = -1, selCol = -1;
        private bool solved;

        // Color mode: colored circles instead of numbers, for little kids
        private static readonly string[] ColorSymbols = { "🔴", "🟡", "🔵", "🟢", "🟣", "🟠" };
        private static readonly SolidColorBrush WrongCellBrush = new SolidColorBrush(Color.FromArgb(255, 255, 214, 220));
        private bool colorMode;

        // Speed mode: 3 minutes to solve as many 4×4 boards as possible
        public const int SpeedSeconds = 180;
        private const int SpeedBoardCoins = 10;
        private DispatcherTimer? speedTimer;
        private int speedLeft, speedBoards;
        private bool SpeedRunning => speedTimer != null;

        public MiniSudokuPage()
        {
            this.InitializeComponent();
            NewBoard();
        }

        private string Symbol(int n) => colorMode ? ColorSymbols[n - 1] : SymbolSets.Current.Symbols[n - 1];

        private void NewBoard()
        {
            (puzzle, solution) = MiniSudoku.Create(size);
            board = (int[,])puzzle.Clone();
            selRow = selCol = -1;
            solved = false;
            MiniStatus.Text = RulesText();
            BuildBoard();
            BuildPad();
            UpdateSizeButtons();
            Refresh();
        }

        private void BuildBoard()
        {
            MiniBoard.Children.Clear();
            MiniBoard.RowDefinitions.Clear();
            MiniBoard.ColumnDefinitions.Clear();
            double cellSize = size == 4 ? 90 : 70;
            cells = new Border[size, size];
            texts = new TextBlock[size, size];
            int boxRows = MiniSudoku.BoxRows(size), boxCols = MiniSudoku.BoxCols(size);

            for (int i = 0; i < size; i++)
            {
                MiniBoard.RowDefinitions.Add(new RowDefinition { Height = new GridLength(cellSize) });
                MiniBoard.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(cellSize) });
            }

            for (int r = 0; r < size; r++)
            {
                for (int c = 0; c < size; c++)
                {
                    var text = new TextBlock
                    {
                        FontSize = cellSize * 0.5,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                    };
                    // Thick lines between the boxes
                    var cell = new Border
                    {
                        BorderBrush = Res(c % boxCols == 0 && c > 0 || r % boxRows == 0 && r > 0 ? "RoseBrush" : "SoftPinkBrush"),
                        BorderThickness = new Thickness(
                            c == 0 ? 0 : c % boxCols == 0 ? 3 : 1,
                            r == 0 ? 0 : r % boxRows == 0 ? 3 : 1, 0, 0),
                        Child = text,
                        Tag = r * size + c,
                    };
                    cell.Tapped += Cell_Tapped;
                    Grid.SetRow(cell, r);
                    Grid.SetColumn(cell, c);
                    MiniBoard.Children.Add(cell);
                    cells[r, c] = cell;
                    texts[r, c] = text;
                }
            }
        }

        private void BuildPad()
        {
            MiniPad.Children.Clear();
            for (int n = 1; n <= size; n++)
            {
                var button = new Button
                {
                    Content = Symbol(n),
                    Style = (Style)Application.Current.Resources["NumberButton"],
                    Tag = n,
                };
                button.Click += Number_Click;
                MiniPad.Children.Add(button);
            }
        }

        private void UpdateSizeButtons()
        {
            Size4Button.Background = Res(size == 4 ? "SelectedCellBrush" : "CardBrush");
            Size6Button.Background = Res(size == 6 ? "SelectedCellBrush" : "CardBrush");
            ColorButton.Background = Res(colorMode ? "SelectedCellBrush" : "CardBrush");
            SpeedButton.Background = Res(SpeedRunning ? "SelectedCellBrush" : "CardBrush");
            SpeedButton.Content = SpeedRunning ? "⏹ לעצור" : "⚡ מצב מהירות";
        }

        private void Refresh()
        {
            for (int r = 0; r < size; r++)
            {
                for (int c = 0; c < size; c++)
                {
                    int value = board[r, c];
                    texts[r, c].Text = value == 0 ? "" : Symbol(value);
                    texts[r, c].FontWeight = puzzle[r, c] != 0 ? FontWeights.Bold : FontWeights.SemiBold;
                    bool given = puzzle[r, c] != 0;
                    bool wrong = !given && value != 0 && value != solution[r, c];
                    bool selected = r == selRow && c == selCol;
                    bool related = selRow >= 0 && (r == selRow || c == selCol);

                    // Number color: given numbers dark, wrong ones red, the player's own numbers in the accent color
                    if (given)
                        texts[r, c].Foreground = Res("BerryBrush");
                    else if (wrong)
                        texts[r, c].Foreground = WrongBrush;
                    else
                        texts[r, c].Foreground = Res("RoseBrush");

                    // Cell color: in color mode a wrong cell turns pink, otherwise show the selection
                    if (colorMode && wrong)
                        cells[r, c].Background = WrongCellBrush;
                    else if (selected)
                        cells[r, c].Background = Res("SelectedCellBrush");
                    else if (related)
                        cells[r, c].Background = Res("RelatedCellBrush");
                    else
                        cells[r, c].Background = Res("CardBrush");
                }
            }
        }

        private void Cell_Tapped(object sender, TappedRoutedEventArgs e)
        {
            int index = (int)((Border)sender).Tag;
            selRow = index / size;
            selCol = index % size;
            Refresh();
        }

        private void Number_Click(object sender, RoutedEventArgs e)
        {
            if (solved || selRow < 0 || puzzle[selRow, selCol] != 0)
                return;

            int n = (int)((Button)sender).Tag;
            board[selRow, selCol] = n;
            Sounds.Play(n == solution[selRow, selCol] ? "correct" : "wrong");
            Refresh();
            CheckSolved();
        }

        private void CheckSolved()
        {
            for (int r = 0; r < size; r++)
            {
                for (int c = 0; c < size; c++)
                {
                    if (board[r, c] != solution[r, c])
                        return;
                }
            }

            solved = true;
            if (SpeedRunning)
            {
                SpeedBoardSolved();
                return;
            }

            int coins = size == 4 ? 20 : 40;
            GameData.AddCoins(coins);
            MiniStatus.Text = $"כל הכבוד! 🎉 +{coins} 🪙";
            Sounds.Play("win");
            Achievements.Unlock("mini");
            Progression.MiniSolved();
            Celebrate();
        }

        private void Celebrate()
        {
            var random = new Random();
            for (int i = 0; i < 30; i++)
            {
                var piece = new TextBlock { Text = i % 2 == 0 ? "💖" : "✨", FontSize = random.Next(18, 34) };
                var move = new TranslateTransform();
                piece.RenderTransform = move;
                Canvas.SetLeft(piece, random.NextDouble() * ActualWidth);
                Canvas.SetTop(piece, -40);
                Confetti.Children.Add(piece);
                var fall = new DoubleAnimation
                {
                    To = ActualHeight + 80,
                    Duration = TimeSpan.FromSeconds(2 + random.NextDouble() * 2),
                    BeginTime = TimeSpan.FromSeconds(random.NextDouble()),
                };
                Storyboard.SetTarget(fall, move);
                Storyboard.SetTargetProperty(fall, "Y");
                var storyboard = new Storyboard();
                storyboard.Children.Add(fall);
                storyboard.Completed += (s, e) => Confetti.Children.Remove(piece);
                storyboard.Begin();
            }
        }

        private void Erase_Click(object sender, RoutedEventArgs e)
        {
            if (solved || selRow < 0 || puzzle[selRow, selCol] != 0)
                return;
            board[selRow, selCol] = 0;
            Refresh();
        }

        private void NewBoard_Click(object sender, RoutedEventArgs e) => NewBoard();

        private void Size4_Click(object sender, RoutedEventArgs e)
        {
            StopSpeed(showResult: false);
            size = 4;
            NewBoard();
        }

        private void Size6_Click(object sender, RoutedEventArgs e)
        {
            StopSpeed(showResult: false);
            size = 6;
            NewBoard();
        }

        private void Color_Click(object sender, RoutedEventArgs e)
        {
            colorMode = !colorMode;
            BuildPad();
            UpdateSizeButtons();
            if (!solved)
                MiniStatus.Text = RulesText();
            Refresh();
        }

        private string RulesText()
        {
            if (colorMode)
                return "כל צבע פעם אחת בכל שורה, עמודה ומלבן 🎨";
            return $"מלאו את המספרים 1 עד {size}: פעם אחת בכל שורה, עמודה ומלבן";
        }

        // ---------- Speed mode ----------

        private static int SpeedBest => int.TryParse(GameData.GetExtra("speed.best", "0"), out int best) ? best : 0;

        private void Speed_Click(object sender, RoutedEventArgs e)
        {
            if (SpeedRunning)
            {
                StopSpeed(showResult: true);
                return;
            }

            size = 4;
            speedLeft = SpeedSeconds;
            speedBoards = 0;
            speedTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            speedTimer.Tick += (s, args) =>
            {
                speedLeft--;
                ShowSpeed();
                if (speedLeft <= 0)
                    StopSpeed(showResult: true);
            };
            speedTimer.Start();
            NewBoard();
            ShowSpeed();
            SpeedText.Visibility = Visibility.Visible;
        }

        private void ShowSpeed() =>
            SpeedText.Text = $"⏱️ {speedLeft / 60}:{speedLeft % 60:00}   ·   🧩 {speedBoards}   ·   🏆 שיא: {SpeedBest}";

        private void SpeedBoardSolved()
        {
            speedBoards++;
            GameData.AddCoins(SpeedBoardCoins);
            Progression.MiniSolved();
            Sounds.Play("coin");
            MiniStatus.Text = $"יש! +{SpeedBoardCoins} 🪙  עוד אחד!";
            ShowSpeed();

            // The next board comes right away
            var next = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(600) };
            next.Tick += (s, e) =>
            {
                next.Stop();
                if (SpeedRunning)
                    NewBoard();
            };
            next.Start();
        }

        private void StopSpeed(bool showResult)
        {
            if (speedTimer == null)
                return;
            speedTimer.Stop();
            speedTimer = null;
            UpdateSizeButtons();
            if (!showResult)
            {
                SpeedText.Visibility = Visibility.Collapsed;
                return;
            }

            solved = true;
            bool record = speedBoards > SpeedBest;
            if (record)
                GameData.SetExtra("speed.best", speedBoards.ToString());
            if (speedBoards >= 5)
                Achievements.Unlock("speed");
            SpeedText.Text = record && speedBoards > 0
                ? $"🏆 שיא חדש! {speedBoards} לוחות ב-3 דקות!"
                : $"⏱️ נגמר הזמן! פתרת {speedBoards} לוחות (השיא: {SpeedBest})";
            MiniStatus.Text = $"קיבלת {speedBoards * SpeedBoardCoins} 🪙. לוחצים על ⚡ כדי לנסות שוב";
            if (speedBoards > 0)
            {
                Sounds.Play("win");
                Celebrate();
            }
        }

        protected override void OnNavigatedFrom(Windows.UI.Xaml.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
            StopSpeed(showResult: false);
        }
    }
}
