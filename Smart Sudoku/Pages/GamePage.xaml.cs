using Windows.UI;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace Smart_Sudoku.Pages
{
    /// <summary>
    /// The Sudoku board. Receives the level number from LevelsPage.
    /// </summary>
    public sealed partial class GamePage : Page
    {
        private const double CellSize = 50;

        private static readonly SolidColorBrush GivenBrush = new SolidColorBrush(Color.FromArgb(255, 138, 42, 85));
        private static readonly SolidColorBrush EnteredBrush = new SolidColorBrush(Color.FromArgb(255, 231, 90, 151));
        private static readonly SolidColorBrush WrongBrush = new SolidColorBrush(Color.FromArgb(255, 217, 58, 74));
        private static readonly SolidColorBrush LineBrush = new SolidColorBrush(Color.FromArgb(255, 246, 198, 218));
        private static readonly SolidColorBrush BoxLineBrush = new SolidColorBrush(Color.FromArgb(255, 231, 90, 151));
        private static readonly SolidColorBrush SelectedBg = new SolidColorBrush(Color.FromArgb(255, 249, 197, 218));
        private static readonly SolidColorBrush SameNumberBg = new SolidColorBrush(Color.FromArgb(255, 251, 216, 230));
        private static readonly SolidColorBrush RelatedBg = new SolidColorBrush(Color.FromArgb(255, 254, 240, 246));
        private static readonly SolidColorBrush PlainBg = new SolidColorBrush(Colors.White);

        private readonly Border[,] cells = new Border[9, 9];
        private readonly TextBlock[,] cellTexts = new TextBlock[9, 9];

        private int level = 1;
        private int[,] puzzle = new int[9, 9];
        private int[,] solution = new int[9, 9];
        private int[,] board = new int[9, 9];
        private bool hasGame;
        private int selRow = -1, selCol = -1;

        public GamePage()
        {
            this.InitializeComponent();
            BuildBoard();
            AddBoxLines();
            BuildNumberPad();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            // Coming back from the help page keeps the current game
            if (e.NavigationMode == NavigationMode.Back && hasGame)
                return;

            if (e.Parameter is int chosenLevel)
                level = chosenLevel;

            NewGame();
        }

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
                    var text = new TextBlock
                    {
                        FontSize = 26,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                    };

                    var cell = new Border
                    {
                        Background = PlainBg,
                        BorderBrush = LineBrush,
                        BorderThickness = new Thickness(c == 0 ? 0 : 1, r == 0 ? 0 : 1, 0, 0),
                        Child = text,
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

        // Thick pink lines between the 3x3 boxes, drawn over the cells
        private void AddBoxLines()
        {
            foreach (int i in new[] { 3, 6 })
            {
                var vertical = new Windows.UI.Xaml.Shapes.Rectangle
                {
                    Width = 3,
                    Fill = BoxLineBrush,
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
                    Fill = BoxLineBrush,
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
                    Content = n.ToString(),
                    Style = (Style)Application.Current.Resources["NumberButton"],
                    Tag = n,
                };
                button.Click += NumberButton_Click;
                NumberPad.Children.Add(button);
            }
        }

        private void NewGame()
        {
            (puzzle, solution) = SudokuGenerator.Create(level);
            board = (int[,])puzzle.Clone();
            selRow = selCol = -1;
            hasGame = true;
            LevelText.Text = $"Level {level}";
            StatusText.Text = "בחרו משבצת ריקה ואז מספר";
            WinOverlay.Visibility = Visibility.Collapsed;
            Refresh();
        }

        private void ShowWin()
        {
            bool isLastLevel = level >= LevelsPage.LevelCount;
            WinText.Text = isLastLevel
                ? "סיימת את כל השלבים! איזו אלופה 💖"
                : $"פתרת את שלב {level} 🎉";
            NextLevelButton.Content = isLastLevel ? "חזרה לשלבים" : "לשלב הבא  ▶";
            BackToLevelsButton.Visibility = isLastLevel ? Visibility.Collapsed : Visibility.Visible;
            WinOverlay.Visibility = Visibility.Visible;
        }

        private void NextLevelButton_Click(object sender, RoutedEventArgs e)
        {
            if (level >= LevelsPage.LevelCount)
            {
                if (Frame.CanGoBack)
                    Frame.GoBack();
                return;
            }

            level++;
            NewGame();
        }

        private void Refresh()
        {
            int selectedValue = selRow >= 0 ? board[selRow, selCol] : 0;

            for (int r = 0; r < 9; r++)
            {
                for (int c = 0; c < 9; c++)
                {
                    int value = board[r, c];
                    var text = cellTexts[r, c];
                    text.Text = value == 0 ? "" : value.ToString();

                    if (puzzle[r, c] != 0)
                    {
                        text.Foreground = GivenBrush;
                        text.FontWeight = FontWeights.Bold;
                    }
                    else
                    {
                        text.Foreground = value != 0 && value != solution[r, c] ? WrongBrush : EnteredBrush;
                        text.FontWeight = FontWeights.SemiBold;
                    }

                    Brush bg = PlainBg;
                    if (selRow >= 0)
                    {
                        bool related = r == selRow || c == selCol || (r / 3 == selRow / 3 && c / 3 == selCol / 3);
                        if (r == selRow && c == selCol)
                            bg = SelectedBg;
                        else if (selectedValue != 0 && value == selectedValue)
                            bg = SameNumberBg;
                        else if (related)
                            bg = RelatedBg;
                    }
                    cells[r, c].Background = bg;
                }
            }

            UpdateNumberPad();
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
                bool done = placed[(int)button.Tag] == 9;
                button.Opacity = done ? 0 : 1;
                button.IsHitTestVisible = !done;
                button.IsTabStop = !done;
            }
        }

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

        private void Cell_Tapped(object sender, TappedRoutedEventArgs e)
        {
            int index = (int)((Border)sender).Tag;
            selRow = index / 9;
            selCol = index % 9;
            Refresh();
        }

        private void NumberButton_Click(object sender, RoutedEventArgs e)
        {
            if (selRow < 0 || puzzle[selRow, selCol] != 0)
                return;

            int n = (int)((Button)sender).Tag;
            board[selRow, selCol] = n;
            Refresh();

            if (IsSolved())
                ShowWin();
            else if (n != solution[selRow, selCol])
                StatusText.Text = "אופס, המספר הזה לא מתאים כאן";
            else
                StatusText.Text = "יופי! 💕";
        }

        private void EraseButton_Click(object sender, RoutedEventArgs e)
        {
            if (selRow < 0 || puzzle[selRow, selCol] != 0)
                return;

            board[selRow, selCol] = 0;
            StatusText.Text = "בחרו משבצת ריקה ואז מספר";
            Refresh();
        }

        // Same board, wipes everything the player entered
        private void RestartButton_Click(object sender, RoutedEventArgs e)
        {
            board = (int[,])puzzle.Clone();
            selRow = selCol = -1;
            StatusText.Text = "בחרו משבצת ריקה ואז מספר";
            Refresh();
        }

        // A different board at the same level
        private void NewBoardButton_Click(object sender, RoutedEventArgs e)
        {
            NewGame();
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
