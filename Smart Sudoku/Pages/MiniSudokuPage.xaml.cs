using System;
using Windows.UI;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;

namespace Smart_Sudoku.Pages
{
    /// <summary>A small board (4×4 or 6×6). Same rules, fewer numbers, no hearts or timer.</summary>
    public sealed partial class MiniSudokuPage : Page
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

        public MiniSudokuPage()
        {
            this.InitializeComponent();
            NewBoard();
        }

        private static Brush Res(string key) => (Brush)Application.Current.Resources[key];

        private static string Symbol(int n) => SymbolSets.Current.Symbols[n - 1];

        private void NewBoard()
        {
            (puzzle, solution) = MiniSudoku.Create(size);
            board = (int[,])puzzle.Clone();
            selRow = selCol = -1;
            solved = false;
            MiniStatus.Text = $"מלאו את המספרים 1 עד {size}: פעם אחת בכל שורה, עמודה ומלבן";
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
                    texts[r, c].Foreground = puzzle[r, c] != 0 ? Res("BerryBrush")
                        : value != 0 && value != solution[r, c] ? WrongBrush : Res("RoseBrush");

                    bool selected = r == selRow && c == selCol;
                    bool related = selRow >= 0 && (r == selRow || c == selCol);
                    cells[r, c].Background = Res(selected ? "SelectedCellBrush" : related ? "RelatedCellBrush" : "CardBrush");
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
            int coins = size == 4 ? 20 : 40;
            GameData.AddCoins(coins);
            MiniStatus.Text = $"כל הכבוד! 🎉 +{coins} 🪙";
            Sounds.Play("win");
            Achievements.Unlock("mini");
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
                var fall = new Windows.UI.Xaml.Media.Animation.DoubleAnimation
                {
                    To = ActualHeight + 80,
                    Duration = TimeSpan.FromSeconds(2 + random.NextDouble() * 2),
                    BeginTime = TimeSpan.FromSeconds(random.NextDouble()),
                };
                Windows.UI.Xaml.Media.Animation.Storyboard.SetTarget(fall, move);
                Windows.UI.Xaml.Media.Animation.Storyboard.SetTargetProperty(fall, "Y");
                var storyboard = new Windows.UI.Xaml.Media.Animation.Storyboard();
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
            size = 4;
            NewBoard();
        }

        private void Size6_Click(object sender, RoutedEventArgs e)
        {
            size = 6;
            NewBoard();
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            if (Frame.CanGoBack)
                Frame.GoBack();
        }
    }
}
