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
    // אובייקט: MiniSudokuPage — עמוד הסודוקו הקטן (4×4 או 6×6): אותם חוקים, פחות מספרים, בלי לבבות ובלי שעון.
    // יש בו גם מצב צבעים (לילדים קטנים) ומצב מהירות (כמה שיותר לוחות ב-3 דקות).
    {
        private static readonly SolidColorBrush WrongBrush = new SolidColorBrush(Color.FromArgb(255, 217, 58, 74));
        // אובייקט: WrongBrush — צבע אדום למספר שגוי.

        private int size = 4;
        // אובייקט: size — גודל הלוח: 4 או 6.
        private int[,] puzzle = new int[4, 4];
        // אובייקט: puzzle — החידה: המספרים שנתונים מההתחלה (0 = ריק).
        private int[,] solution = new int[4, 4];
        // אובייקט: solution — הפתרון המלא.
        private int[,] board = new int[4, 4];
        // אובייקט: board — מה שעל הלוח עכשיו (החידה ועוד מה שהשחקן מילא).
        private Border[,] cells = new Border[4, 4];
        // אובייקט: cells — המשבצות על המסך (כל אחת היא Border עם מסגרת).
        private TextBlock[,] texts = new TextBlock[4, 4];
        // אובייקט: texts — הטקסט שבתוך כל משבצת.
        private int selRow = -1, selCol = -1;
        // אובייקט: selRow, selCol — המשבצת שנבחרה (-1 = אף משבצת).
        private bool solved;
        // אובייקט: solved — אמת כשהלוח נפתר.

        // Color mode: colored circles instead of numbers, for little kids
        private static readonly string[] ColorSymbols = { "🔴", "🟡", "🔵", "🟢", "🟣", "🟠" };
        // אובייקט: ColorSymbols — העיגולים הצבעוניים שמופיעים במצב צבעים במקום מספרים.
        private static readonly SolidColorBrush WrongCellBrush = new SolidColorBrush(Color.FromArgb(255, 255, 214, 220));
        // אובייקט: WrongCellBrush — צבע ורוד למשבצת שגויה במצב צבעים.
        private bool colorMode;
        // אובייקט: colorMode — אמת כשמצב הצבעים פועל.

        // Speed mode: 3 minutes to solve as many 4×4 boards as possible
        public const int SpeedSeconds = 180;
        // אובייקט: SpeedSeconds — כמה זמן יש במצב מהירות (180 שניות = 3 דקות).
        private const int SpeedBoardCoins = 10;
        // אובייקט: SpeedBoardCoins — כמה מטבעות מקבלים על כל לוח במצב מהירות.
        private DispatcherTimer? speedTimer;
        // אובייקט: speedTimer — השעון של מצב המהירות (null כשהמצב לא פועל).
        private int speedLeft, speedBoards;
        // אובייקט: speedLeft, speedBoards — כמה שניות נשארו, וכמה לוחות נפתרו במצב מהירות.
        private bool SpeedRunning => speedTimer != null;
        // אובייקט: SpeedRunning — אמת כשמצב המהירות פועל.

        public MiniSudokuPage()
        // פעולה: MiniSudokuPage (בנאי) — טוענת את העיצוב ויוצרת לוח ראשון.
        {
            this.InitializeComponent();
            NewBoard();
        }

        private string Symbol(int n) => colorMode ? ColorSymbols[n - 1] : SymbolSets.Current.Symbols[n - 1];
        // פעולה: Symbol — מחזירה מה להציג במקום המספר n: עיגול צבעוני במצב צבעים, או הסמל מערכת הסמלים שנבחרה.

        private void NewBoard()
        // פעולה: NewBoard — יוצרת לוח חדש בגודל שנבחר, ובונה מחדש את הלוח, את כפתורי המספרים ואת כפתורי הגודל.
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
        // פעולה: BuildBoard — בונה את משבצות הלוח על המסך, עם קווים עבים בין המלבנים.
        {
            MiniBoard.Children.Clear();
            MiniBoard.RowDefinitions.Clear();
            MiniBoard.ColumnDefinitions.Clear();
            double cellSize = size == 4 ? 90 : 70;
            // אובייקט: cellSize — הגודל של כל משבצת (גדול יותר בלוח 4×4).
            cells = new Border[size, size];
            texts = new TextBlock[size, size];
            int boxRows = MiniSudoku.BoxRows(size), boxCols = MiniSudoku.BoxCols(size);
            // אובייקט: boxRows, boxCols — הגודל של כל מלבן קטן בלוח.

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
                    // אובייקט: text — הטקסט שבתוך המשבצת.
                    {
                        FontSize = cellSize * 0.5,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                    };
                    // Thick lines between the boxes
                    var cell = new Border
                    // אובייקט: cell — המשבצת עצמה. ה-Tag שומר את המספר שלה, כדי לדעת על איזו לחצו.
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
        // פעולה: BuildPad — בונה את כפתורי המספרים (או הצבעים) שמתחת ללוח.
        {
            MiniPad.Children.Clear();
            for (int n = 1; n <= size; n++)
            {
                var button = new Button
                // אובייקט: button — כפתור מספר אחד.
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
        // פעולה: UpdateSizeButtons — מדגישה את הכפתורים שפעילים עכשיו (גודל, מצב צבעים, מצב מהירות).
        {
            Size4Button.Background = Res(size == 4 ? "SelectedCellBrush" : "CardBrush");
            Size6Button.Background = Res(size == 6 ? "SelectedCellBrush" : "CardBrush");
            ColorButton.Background = Res(colorMode ? "SelectedCellBrush" : "CardBrush");
            SpeedButton.Background = Res(SpeedRunning ? "SelectedCellBrush" : "CardBrush");
            SpeedButton.Content = SpeedRunning ? "⏹ לעצור" : "⚡ מצב מהירות";
        }

        private void Refresh()
        // פעולה: Refresh — צובעת מחדש את כל הלוח: מספרים נתונים בצבע כהה, שגויים באדום, של השחקן בצבע הערכה,
        // והמשבצת שנבחרה עם השורה והעמודה שלה מודגשות.
        {
            for (int r = 0; r < size; r++)
            {
                for (int c = 0; c < size; c++)
                {
                    int value = board[r, c];
                    // אובייקט: value — המספר שבמשבצת.
                    texts[r, c].Text = value == 0 ? "" : Symbol(value);
                    texts[r, c].FontWeight = puzzle[r, c] != 0 ? FontWeights.Bold : FontWeights.SemiBold;
                    bool given = puzzle[r, c] != 0;
                    // אובייקט: given — אמת אם המספר היה נתון מההתחלה.
                    bool wrong = !given && value != 0 && value != solution[r, c];
                    // אובייקט: wrong — אמת אם השחקן כתב כאן מספר שגוי.
                    bool selected = r == selRow && c == selCol;
                    // אובייקט: selected — אמת אם זו המשבצת שנבחרה.
                    bool related = selRow >= 0 && (r == selRow || c == selCol);
                    // אובייקט: related — אמת אם המשבצת באותה שורה או עמודה של המשבצת שנבחרה.

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
        // פעולה: Cell_Tapped — לחיצה על משבצת: בוחרת אותה.
        {
            int index = (int)((Border)sender).Tag;
            // אובייקט: index — המספר של המשבצת שנלחצה.
            selRow = index / size;
            selCol = index % size;
            Refresh();
        }

        private void Number_Click(object sender, RoutedEventArgs e)
        // פעולה: Number_Click — לחיצה על כפתור מספר: כותבת אותו במשבצת שנבחרה, משמיעה צליל נכון או שגוי, ובודקת אם הלוח נפתר.
        {
            if (solved || selRow < 0 || puzzle[selRow, selCol] != 0)
                return;

            int n = (int)((Button)sender).Tag;
            // אובייקט: n — המספר שנלחץ.
            board[selRow, selCol] = n;
            Sounds.Play(n == solution[selRow, selCol] ? "correct" : "wrong");
            Refresh();
            CheckSolved();
        }

        private void CheckSolved()
        // פעולה: CheckSolved — בודקת אם כל הלוח נכון. אם כן — נותנת מטבעות, הישג וחגיגה (או עוברת ללוח הבא במצב מהירות).
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
            // אובייקט: coins — כמה מטבעות מקבלים (20 בלוח 4×4, 40 בלוח 6×6).
            GameData.AddCoins(coins);
            MiniStatus.Text = $"כל הכבוד! 🎉 +{coins} 🪙";
            Sounds.Play("win");
            Achievements.Unlock("mini");
            Progression.MiniSolved();
            Celebrate();
        }

        private void Celebrate()
        // פעולה: Celebrate — חגיגה: 30 לבבות ונצנוצים נופלים מלמעלה.
        {
            var random = new Random();
            // אובייקט: random — מחולל אקראי למיקום, לגודל ולמהירות של כל קישוט.
            for (int i = 0; i < 30; i++)
            {
                var piece = new TextBlock { Text = i % 2 == 0 ? "💖" : "✨", FontSize = random.Next(18, 34) };
                // אובייקט: piece — קישוט אחד שנופל.
                var move = new TranslateTransform();
                // אובייקט: move — ההזזה של הקישוט כלפי מטה.
                piece.RenderTransform = move;
                Canvas.SetLeft(piece, random.NextDouble() * ActualWidth);
                Canvas.SetTop(piece, -40);
                Confetti.Children.Add(piece);
                var fall = new DoubleAnimation
                // אובייקט: fall — האנימציה של הנפילה.
                {
                    To = ActualHeight + 80,
                    Duration = TimeSpan.FromSeconds(2 + random.NextDouble() * 2),
                    BeginTime = TimeSpan.FromSeconds(random.NextDouble()),
                };
                Storyboard.SetTarget(fall, move);
                Storyboard.SetTargetProperty(fall, "Y");
                var storyboard = new Storyboard();
                // אובייקט: storyboard — ה"תסריט" שמריץ את הנפילה, ומוחק את הקישוט בסוף.
                storyboard.Children.Add(fall);
                storyboard.Completed += (s, e) => Confetti.Children.Remove(piece);
                storyboard.Begin();
            }
        }

        private void Erase_Click(object sender, RoutedEventArgs e)
        // פעולה: Erase_Click — כפתור "מחק": מוחקת את המספר מהמשבצת שנבחרה.
        {
            if (solved || selRow < 0 || puzzle[selRow, selCol] != 0)
                return;
            board[selRow, selCol] = 0;
            Refresh();
        }

        private void NewBoard_Click(object sender, RoutedEventArgs e) => NewBoard();
        // פעולה: NewBoard_Click — כפתור "לוח חדש".

        private void Size4_Click(object sender, RoutedEventArgs e)
        // פעולה: Size4_Click — כפתור "4×4": עוברת ללוח קטן.
        {
            StopSpeed(showResult: false);
            size = 4;
            NewBoard();
        }

        private void Size6_Click(object sender, RoutedEventArgs e)
        // פעולה: Size6_Click — כפתור "6×6": עוברת ללוח בינוני.
        {
            StopSpeed(showResult: false);
            size = 6;
            NewBoard();
        }

        private void Color_Click(object sender, RoutedEventArgs e)
        // פעולה: Color_Click — כפתור "מצב צבעים": מחליפה בין מספרים לעיגולים צבעוניים.
        {
            colorMode = !colorMode;
            BuildPad();
            UpdateSizeButtons();
            if (!solved)
                MiniStatus.Text = RulesText();
            Refresh();
        }

        private string RulesText()
        // פעולה: RulesText — מחזירה את ההסבר הקצר של החוקים (בצבעים או במספרים).
        {
            if (colorMode)
                return "כל צבע פעם אחת בכל שורה, עמודה ומלבן 🎨";
            return $"מלאו את המספרים 1 עד {size}: פעם אחת בכל שורה, עמודה ומלבן";
        }

        // ---------- Speed mode ----------

        private static int SpeedBest => int.TryParse(GameData.GetExtra("speed.best", "0"), out int best) ? best : 0;
        // אובייקט: SpeedBest — השיא במצב מהירות (כמה לוחות ב-3 דקות).

        private void Speed_Click(object sender, RoutedEventArgs e)
        // פעולה: Speed_Click — כפתור "מצב מהירות": מתחילה 3 דקות של לוחות 4×4 (או עוצרת אם המצב כבר פועל).
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
        // פעולה: ShowSpeed — מציגה את הזמן שנשאר, כמה לוחות נפתרו ומה השיא.
            SpeedText.Text = $"⏱️ {speedLeft / 60}:{speedLeft % 60:00}   ·   🧩 {speedBoards}   ·   🏆 שיא: {SpeedBest}";

        private void SpeedBoardSolved()
        // פעולה: SpeedBoardSolved — לוח נפתר במצב מהירות: מטבעות, צליל, ואחרי רגע קצר — לוח חדש.
        {
            speedBoards++;
            GameData.AddCoins(SpeedBoardCoins);
            Progression.MiniSolved();
            Sounds.Play("coin");
            MiniStatus.Text = $"יש! +{SpeedBoardCoins} 🪙  עוד אחד!";
            ShowSpeed();

            // The next board comes right away
            var next = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(600) };
            // אובייקט: next — שעון שמחכה חצי שנייה לפני הלוח הבא.
            next.Tick += (s, e) =>
            {
                next.Stop();
                if (SpeedRunning)
                    NewBoard();
            };
            next.Start();
        }

        private void StopSpeed(bool showResult)
        // פעולה: StopSpeed — עוצרת את מצב המהירות. אם showResult אמת — מציגה את התוצאה, שומרת שיא ונותנת הישג.
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
            // אובייקט: record — אמת אם זה שיא חדש.
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
        // פעולה: OnNavigatedFrom — רצה כשיוצאים מהעמוד: עוצרת את מצב המהירות.
        {
            base.OnNavigatedFrom(e);
            StopSpeed(showResult: false);
        }
    }
}
