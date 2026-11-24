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
    public sealed partial class GamePage : BasePage
    // אובייקט: GamePage — מסך המשחק: לוח הסודוקו עצמו. מקבל מאיפה לשחק (שלב, אתגר יומי, זן, מסע, דו-קרב או משחק שמור).
    // הקוד שלו מחולק לכמה קבצים (partial): הלוח הרגיל כאן, ועוד קבצים לדו-קרב, לדו-קרב נגד המחשב ולתוספות.
    {
        private const double CellSize = 50;
        // אובייקט: CellSize — הגודל של כל משבצת בלוח (50 נקודות).
        private const int MaxLives = 5;
        // אובייקט: MaxLives — כמה לבבות יש בתחילת שלב (5).
        public const int HeartPrice = 30;
        // אובייקט: HeartPrice — המחיר של לב נוסף בחנות ובסוף משחק (30 מטבעות).
        private const int FreeHints = 3;
        // אובייקט: FreeHints — כמה רמזים חינמיים יש בכל שלב (3).

        private static readonly SolidColorBrush WrongBrush = new SolidColorBrush(Color.FromArgb(255, 217, 58, 74));
        // אובייקט: WrongBrush — צבע אדום למספר שגוי.
        // The board's cell color follows the theme (white by day, dark at night)
        private static Brush PlainBg => Res("CardBrush");
        // אובייקט: PlainBg — צבע הרקע הרגיל של משבצת (לבן ביום, כהה בלילה).
        private static readonly Random Rnd = new Random();
        // אובייקט: Rnd — מחולל אקראי (לרמזים ולחגיגות).

        private readonly Border[,] cells = new Border[9, 9];
        // אובייקט: cells — 81 המשבצות על המסך.
        private readonly TextBlock[,] cellTexts = new TextBlock[9, 9];
        // אובייקט: cellTexts — המספר הגדול שבתוך כל משבצת.
        private readonly TextBlock[,,] noteTexts = new TextBlock[9, 9, 9];
        // אובייקט: noteTexts — 9 הפתקים הקטנים שבכל משבצת (81 × 9).
        private readonly DispatcherTimer timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        // אובייקט: timer — השעון שמוסיף שנייה בכל שנייה.
        private readonly Stack<BoardSnapshot> history = new Stack<BoardSnapshot>();   // for the undo button
        // אובייקט: history — "ערימה" של צילומי לוח, בשביל כפתור "בטל": האחרון שנכנס הוא הראשון שיוצא.

        private int level = 1;  // level id: difficulty * 100 + level number (see Levels)
        // אובייקט: level — מספר השלב שמשחקים (רמת קושי × 100 + מספר השלב).
        private int[,] puzzle = new int[9, 9];
        // אובייקט: puzzle — החידה: המספרים הנתונים מההתחלה (0 = ריק).
        private int[,] solution = new int[9, 9];
        // אובייקט: solution — הפתרון המלא.
        private int[,] board = new int[9, 9];
        // אובייקט: board — מה שעל הלוח עכשיו.
        private int[,] notes = new int[9, 9];      // bit n set = note for number n
        // אובייקט: notes — הפתקים של כל משבצת, שמורים כ"ביטים" במספר אחד (ביט n דלוק = יש פתק של n).
        private bool[,] hinted = new bool[9, 9];
        // אובייקט: hinted — אילו משבצות התגלו ברמז.
        private bool hasGame, finished, notesMode, paused;
        // אובייקט: hasGame, finished, notesMode, paused — יש משחק? הוא נגמר? מצב פתקים פועל? המשחק בהפסקה?
        private int selRow = -1, selCol = -1;
        // אובייקט: selRow, selCol — המשבצת שנבחרה (-1 = אף משבצת).
        private int lives, mistakes, hintsUsed, seconds;
        // אובייקט: lives, mistakes, hintsUsed, seconds — לבבות שנשארו, טעויות, רמזים שהשתמשו בהם, ושניות שעברו.

        public GamePage()
        // פעולה: GamePage (בנאי) — טוענת את העיצוב, בונה את 81 המשבצות, את הקווים העבים ואת כפתורי המספרים, ומפעילה את השעון.
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

        private static string Symbol(int n) => SymbolSets.Current.Symbols[n - 1];
        // פעולה: Symbol — מחזירה את הסמל שמוצג במקום המספר n (לפי ערכת הסמלים שנבחרה).

        private int HintsLeft => Math.Max(0, FreeHints - hintsUsed) + GameData.BonusHints;
        // אובייקט: HintsLeft — כמה רמזים נשארו: החינמיים שנשארו בשלב, ועוד הרמזים שנקנו.

        protected override void OnNavigatedTo(NavigationEventArgs e)
        // פעולה: OnNavigatedTo — רצה כשנכנסים למסך: בודקת מה קיבלה (דו-קרב, דו-קרב נגד המחשב, משחק שמור, אתגר יומי,
        // זן, מסע או שלב רגיל) ומתחילה את המשחק המתאים. חזרה ממסך העזרה ממשיכה את אותו משחק.
        {
            base.OnNavigatedTo(e);
            HookKeyboard();
            ApplyFrame();

            // Coming back from the help page keeps the current game
            if (e.NavigationMode == NavigationMode.Back && hasGame)
            {
                if (!finished && !paused)
                {
                    timer.Start();
                    aiTimer?.Start();
                }
                Refresh();
                return;
            }

            if (e.Parameter is Duel.DuelGame duelGame)
            {
                isDaily = false;
                EndAiDuel();
                StartDuel(duelGame);
                return;
            }

            if (e.Parameter is AiDuelStart aiStart)
            {
                isDaily = false;
                isZen = false;
                adventure = null;
                if (IsDuel)
                    EndDuel();
                StartAiDuel(aiStart);
                return;
            }

            if (IsDuel)
                EndDuel();
            EndAiDuel();

            switch (e.Parameter)
            {
                case SavedGame saved:
                    ResumeGame(saved);
                    break;
                case DailyChallengeStart:
                    StartDaily();
                    break;
                case ZenStart:
                    StartZen();
                    break;
                case AdventureStart stage:
                    StartAdventure(stage);
                    break;
                case int chosenLevel:
                    level = chosenLevel;
                    NewGame();
                    break;
                default:
                    NewGame();
                    break;
            }
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        // פעולה: OnNavigatedFrom — רצה כשיוצאים מהמסך: עוצרת שעונים, מפסיקה להקשיב למקלדת, ושומרת את המשחק.
        {
            base.OnNavigatedFrom(e);
            timer.Stop();
            aiTimer?.Stop();
            UnhookKeyboard();
            SaveCurrentGame();
            Sounds.ApplyMusicSetting();
        }

        // ---------- Building the UI ----------

        private void BuildBoard()
        // פעולה: BuildBoard — בונה את 81 המשבצות. בכל משבצת יש טבלה קטנה של 3×3 לפתקים, ומעליה המספר הגדול.
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
                    // אובייקט: notesGrid — הטבלה הקטנה של הפתקים בתוך המשבצת.
                    for (int i = 0; i < 3; i++)
                    {
                        notesGrid.RowDefinitions.Add(new RowDefinition());
                        notesGrid.ColumnDefinitions.Add(new ColumnDefinition());
                    }
                    for (int k = 0; k < 9; k++)
                    {
                        var note = new TextBlock
                        // אובייקט: note — פתק קטן אחד (מספר אחד מתוך 9).
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
                    // אובייקט: text — המספר הגדול של המשבצת.
                    {
                        FontSize = 26,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                    };

                    var content = new Grid();
                    // אובייקט: content — שכבה שמחזיקה את הפתקים ואת המספר הגדול זה מעל זה.
                    content.Children.Add(notesGrid);
                    content.Children.Add(text);

                    var cell = new Border
                    // אובייקט: cell — המשבצת עצמה. בפינות של הלוח היא מעוגלת. ה-Tag שומר את המספר שלה.
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
        // פעולה: AddBoxLines — מציירת קווים עבים בין ריבועי ה-3×3, מעל המשבצות.
        {
            foreach (int i in new[] { 3, 6 })
            {
                var vertical = new Windows.UI.Xaml.Shapes.Rectangle
                // אובייקט: vertical — קו עבה מאונך.
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
                // אובייקט: horizontal — קו עבה מאוזן.
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
        // פעולה: BuildNumberPad — בונה את 9 כפתורי המספרים שמתחת ללוח.
        {
            for (int n = 1; n <= 9; n++)
            {
                var button = new Button
                // אובייקט: button — כפתור של מספר אחד.
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
        // פעולה: NewGame — יוצרת לוח חדש לשלב הנוכחי, מתחילה אותו ושומרת אותו.
        {
            if (isZen)
            {
                StartZen();
                return;
            }
            isDaily = false;
            adventure = null;
            (puzzle, solution) = SudokuGenerator.Create(Levels.EmptyCells(level));
            StartBoard();
            SaveCurrentGame();
        }

        // Starts (or restarts) the current puzzle from scratch
        private void StartBoard()
        // פעולה: StartBoard — מתחילה (או מתחילה מחדש) את הלוח הנוכחי מאפס: לבבות, טעויות, רמזים, שעון, ומסתירה חלונות שהיו פתוחים.
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

            SetTitle();
            MascotFace.Text = Cosmetics.Face("😊");
            NewBoardButton.Visibility = isDaily || IsDuel || adventure != null ? Visibility.Collapsed : Visibility.Visible;
            StatusText.Text = "בחרו משבצת ריקה ואז מספר";
            WinOverlay.Visibility = Visibility.Collapsed;
            GameOverOverlay.Visibility = Visibility.Collapsed;
            PauseOverlay.Visibility = Visibility.Collapsed;
            CelebrationCanvas.Children.Clear();

            timer.Start();
            Refresh();
        }

        // Saves a copy of the board before a move, so Undo can bring it back
        private void PushHistory()
        // פעולה: PushHistory — שומרת צילום של הלוח לפני מהלך, כדי שכפתור "בטל" יוכל להחזיר אותו.
        {
            history.Push(new BoardSnapshot((int[,])board.Clone(), (int[,])notes.Clone(), (bool[,])hinted.Clone()));
        }

        /// <summary>3 stars with no mistakes or hints, 2 stars with up to 2, otherwise 1.</summary>
        private static int StarsFor(int mistakesAndHints)
        // פעולה: StarsFor — קובעת כמה כוכבים מקבלים: 3 בלי טעויות ורמזים, 2 עם עד 2, ואחרת 1.
        {
            if (mistakesAndHints == 0)
                return 3;
            if (mistakesAndHints <= 2)
                return 2;
            return 1;
        }

        private bool BoardFull()
        // פעולה: BoardFull — בודקת אם אין יותר משבצות ריקות בלוח.
        {
            for (int i = 0; i < 81; i++)
            {
                if (board[i / 9, i % 9] == 0)
                    return false;
            }
            return true;
        }

        private void MistakesButton_Click(object sender, RoutedEventArgs e)
        // פעולה: MistakesButton_Click — כפתור "טעויות": מחליף בין הצגת טעויות באדום (עם הורדת לב) לבין "מצב אתגר" בלי סימון.
        {
            GameData.ShowMistakes = !GameData.ShowMistakes;
            StatusText.Text = GameData.ShowMistakes
                ? "טעויות יופיעו באדום ויורידו לב"
                : "מצב אתגר: בלי סימון טעויות ובלי לבבות שנשברים 🙈";
            Refresh();
        }

        private bool IsSolved()
        // פעולה: IsSolved — בודקת אם כל הלוח זהה לפתרון.
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
        // פעולה: RemoveNoteFromPeers — כשכותבים מספר נכון, מוחקת את אותו פתק מהשורה, מהעמודה ומהריבוע (כי הוא כבר לא אפשרי שם).
        {
            int bit = 1 << n;
            // אובייקט: bit — הביט של המספר בתוך הפתקים.
            for (int i = 0; i < 9; i++)
            {
                notes[row, i] &= ~bit;
                notes[i, col] &= ~bit;
            }
            int br = row / 3 * 3, bc = col / 3 * 3;
            // אובייקט: br, bc — הפינה של הריבוע 3×3.
            for (int r = br; r < br + 3; r++)
            {
                for (int c = bc; c < bc + 3; c++)
                    notes[r, c] &= ~bit;
            }
        }

        private void PlaceValue(int row, int col, int n)
        // פעולה: PlaceValue — כותבת מספר במשבצת, מוחקת את הפתקים שלה, ואם המספר נכון — מנקה אותו גם מהפתקים של השכנים.
        {
            board[row, col] = n;
            notes[row, col] = 0;
            if (n == solution[row, col])
                RemoveNoteFromPeers(row, col, n);
        }

        private void Win()
        // פעולה: Win — הלוח נפתר! עוצרת את השעון, מחשבת כוכבים, ומציגה חלון ניצחון: לזן, למסע ולאתגר היומי יש חלון משלהם,
        // ובשלב רגיל — הזמן, השיא, פירוט המטבעות, והכפתור לשלב הבא. בסוף מדווחת להישגים ולהתקדמות.
        {
            finished = true;
            timer.Stop();

            int earnedStars = StarsFor(mistakes + hintsUsed);
            // אובייקט: earnedStars — כמה כוכבים השחקן הרוויח.
            ClearSavedGame();
            Mascot("🥳", stay: true);

            if (isZen || adventure != null)
            {
                if (isZen)
                    WinZen();
                else
                    WinAdventure(earnedStars);
                WinOverlay.Visibility = Visibility.Visible;
                Sounds.Play("win");
                Celebrate();
                Refresh();
                return;
            }

            if (isDaily)
            {
                WinDaily(earnedStars);
                WinOverlay.Visibility = Visibility.Visible;
                Sounds.Play("win");
                Celebrate();
                Refresh();
                return;
            }

            int oldBest = GameData.GetBestTime(level);
            // אובייקט: oldBest — השיא הקודם בשלב.
            int earnedCoins = GameData.RecordWin(level, earnedStars, seconds);
            // אובייקט: earnedCoins — כמה מטבעות השחקן הרוויח.
            bool newBest = oldBest == 0 || seconds < oldBest;
            // אובייקט: newBest — אמת אם זה שיא חדש.
            int? next = Levels.Next(level);
            // אובייקט: next — השלב הבא (או null אם זה היה האחרון).
            var difficulty = Levels.DifficultyFor(level);
            // אובייקט: difficulty — רמת הקושי של השלב.
            bool finishedDifficulty = Levels.NumberOf(level) == Levels.PerDifficulty;
            // אובייקט: finishedDifficulty — אמת אם זה היה השלב האחרון ברמת הקושי.

            WinStarsText.Text = StarsText(earnedStars);
            if (next == null)
                WinText.Text = "סיימת את כל השלבים בכל הדרגות! איזו אלופה 💖";
            else if (finishedDifficulty)
                WinText.Text = $"סיימת את כל דרגת {difficulty.Name}! {difficulty.Emoji} 🎉";
            else
                WinText.Text = $"פתרת את שלב {Levels.NumberOf(level)} 🎉";
            WinDetailsText.Text =
                $"⏱ {FormatTime(seconds)}" +
                (newBest ? "   🏆 שיא חדש!" : $"   (שיא: {FormatTime(oldBest)})") +
                $"\n\nשלב: {GameData.LevelCoins(level)}+" +
                $"\nכוכבים: {GameData.StarCoins(earnedStars)}+" +
                $"\nמהירות ⚡: {GameData.SpeedCoins(level, seconds)}+" +
                $"\nבונוס כפול: ×{GameData.CoinMultiplier}" +
                $"\n🪙 סה\"כ: {earnedCoins}+ מטבעות";
            if (next == null)
                NextLevelButton.Content = "חזרה לשלבים";
            else if (finishedDifficulty)
                NextLevelButton.Content = $"לדרגה הבאה: {Levels.DifficultyFor(next.Value).Name}  ▶";
            else
                NextLevelButton.Content = "לשלב הבא  ▶";
            BackToLevelsButton.Visibility = next == null ? Visibility.Collapsed : Visibility.Visible;
            WinOverlay.Visibility = Visibility.Visible;

            Sounds.Play("win");
            Celebrate();
            Refresh();
            CheckLevelAchievements(earnedStars);
            if (Levels.DifficultyOf(level) == 3)
                Fireworks();
            Progression.LevelSolved(level, earnedStars, seconds, mistakes, hintsUsed, earnedCoins);
        }

        private void GameOver()
        // פעולה: GameOver — נגמרו הלבבות: עוצרת את המשחק ומציגה חלון שמציע לקנות לב נוסף.
        {
            finished = true;
            timer.Stop();
            ClearSavedGame();
            Mascot("😢", stay: true);
            BuyHeartButton.Content = $"❤ לב נוסף · {HeartPrice} 🪙";
            GameOverMessage.Text = "לא נורא! כל טעות היא חלק מהלמידה 💕";
            GameOverNewBoardButton.Visibility = NewBoardButton.Visibility;   // not in the daily challenge, adventure or duels
            GameOverOverlay.Visibility = Visibility.Visible;
        }

        // Buy one heart and keep playing the same board
        private void BuyHeartButton_Click(object sender, RoutedEventArgs e)
        // פעולה: BuyHeartButton_Click — קונה לב אחד וממשיכה את אותו לוח.
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
        // פעולה: FormatTime — הופכת שניות לשעון, למשל 125 → "02:05".

        // ---------- Drawing ----------

        private void Refresh()
        // פעולה: Refresh — מציירת מחדש את כל הלוח: מספרים (נתונים, של השחקן, או שגויים באדום), פתקים,
        // והדגשה של המשבצת שנבחרה, השורה, העמודה, הריבוע ומספרים זהים. אחר כך מעדכנת את הכפתורים והמידע.
        {
            int selectedValue = selRow >= 0 ? board[selRow, selCol] : 0;
            // אובייקט: selectedValue — המספר שבמשבצת שנבחרה, כדי להדגיש את כל המספרים הזהים.

            for (int r = 0; r < 9; r++)
            {
                for (int c = 0; c < 9; c++)
                {
                    int value = board[r, c];
                    // אובייקט: value — המספר שבמשבצת.
                    var text = cellTexts[r, c];
                    // אובייקט: text — הטקסט של המשבצת.
                    text.Text = value == 0 ? "" : Symbol(value);

                    if (puzzle[r, c] != 0)
                    {
                        text.Foreground = Res("BerryBrush");
                        text.FontWeight = FontWeights.Bold;
                    }
                    else
                    {
                        text.Foreground = value != 0 && value != solution[r, c] && MistakesShown ? WrongBrush : Res("RoseBrush");
                        text.FontWeight = hinted[r, c] ? FontWeights.Bold : FontWeights.SemiBold;
                    }

                    for (int k = 0; k < 9; k++)
                    {
                        bool show = value == 0 && (notes[r, c] & (1 << (k + 1))) != 0;
                        // אובייקט: show — אמת אם צריך להציג את הפתק הזה.
                        noteTexts[r, c, k].Text = show ? Symbol(k + 1) : "";
                    }

                    Brush bg = PlainBg;
                    // אובייקט: bg — צבע הרקע של המשבצת.
                    if (selRow >= 0)
                    {
                        bool related = r == selRow || c == selCol || (r / 3 == selRow / 3 && c / 3 == selCol / 3);
                        // אובייקט: related — אמת אם המשבצת באותה שורה, עמודה או ריבוע של המשבצת שנבחרה.
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
        // פעולה: UpdateNumberPad — מסתירה כפתור של מספר אחרי שכל 9 העותקים שלו כבר על הלוח (הכפתור שומר על המקום, כדי שהשאר לא יזוזו).
        {
            var placed = new int[10];
            // אובייקט: placed — כמה פעמים כל מספר כבר מופיע נכון על הלוח.
            for (int r = 0; r < 9; r++)
            {
                for (int c = 0; c < 9; c++)
                {
                    // With mistakes hidden, count every placed number, so the pad doesn't give away which are right
                    if (board[r, c] != 0 && (board[r, c] == solution[r, c] || !MistakesShown))
                        placed[board[r, c]]++;
                }
            }

            foreach (Button button in NumberPad.Children)
            {
                int n = (int)button.Tag;
                // אובייקט: n — המספר של הכפתור.
                bool done = placed[n] == 9;
                // אובייקט: done — אמת אם כל 9 העותקים של המספר כבר הונחו.
                button.Content = Symbol(n);
                button.Opacity = done ? 0 : 1;
                button.IsHitTestVisible = !done;
                button.IsTabStop = !done;
            }
        }

        private void UpdateStats()
        // פעולה: UpdateStats — מעדכנת את השורה העליונה: שעון, לבבות, מטבעות, בונוס המהירות שיורד עם הזמן, והשיא.
        {
            TimerText.Text = $"⏱ {FormatTime(seconds)}";
            LivesText.Text = new string('♥', lives) + new string('♡', Math.Max(0, MaxLives - lives))
                + (GameData.BonusLives > 0 ? $"  +{GameData.BonusLives}" : "");
            CoinsText.Text = $"🪙 {GameData.Coins}";

            // Speed bonus shrinks as time passes, so the player sees it counting down
            SpeedText.Text = $"⚡ +{GameData.SpeedCoins(level, seconds) * GameData.CoinMultiplier}";
            int best = GameData.GetBestTime(level);
            // אובייקט: best — השיא בשלב.
            BestText.Text = best > 0 ? $"🏆 {FormatTime(best)}" : "🏆 --:--";
        }

        private void UpdateTools()
        // פעולה: UpdateTools — מעדכנת את הכפתורים: כמה רמזים נשארו, מצב הטעויות, מצב הפתקים, והאייקון של הצליל.
        {
            HintText.Text = $"רמז ({HintsLeft})";
            MistakesText.Text = GameData.ShowMistakes ? "👁 טעויות: מוצגות" : "🙈 טעויות: מוסתרות";
            NotesText.Text = notesMode ? "פתקים: פועל" : "פתקים";
            NotesButton.Background = notesMode ? Res("SelectedCellBrush") : PlainBg;
            NotesButton.BorderBrush = notesMode ? Res("RoseBrush") : Res("SoftPinkBrush");
            SoundIcon.Glyph = GameData.SoundOn ? "" : "";
        }

        private void Celebrate()
        // פעולה: Celebrate — חגיגה: 45 לבבות ופרחים נופלים ומסתובבים על המסך.
        {
            CelebrationCanvas.Children.Clear();
            double width = ActualWidth, height = ActualHeight;
            // אובייקט: width, height — הגודל של המסך.
            string[] shapes = { "♥", "✿", "✦", "❀", "♥", "💖", "🌸" };
            // אובייקט: shapes — הצורות שנופלות בחגיגה.
            string[] brushes = { "RoseBrush", "DecoBrush", "SoftPinkBrush" };
            // אובייקט: brushes — הצבעים של הצורות.
            var storyboard = new Storyboard();
            // אובייקט: storyboard — ה"תסריט" שמריץ את כל האנימציות ביחד.

            for (int i = 0; i < 45; i++)
            {
                var transform = new CompositeTransform();
                // אובייקט: transform — הנפילה והסיבוב של הצורה.
                var piece = new TextBlock
                // אובייקט: piece — צורה אחת שנופלת.
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
                // אובייקט: duration — כמה זמן הנפילה לוקחת.
                var delay = TimeSpan.FromSeconds(Rnd.NextDouble() * 1.2);
                // אובייקט: delay — כמה לחכות לפני שהצורה מתחילה ליפול.

                var fall = new DoubleAnimation { To = height + 100, Duration = duration, BeginTime = delay };
                // אובייקט: fall — האנימציה של הנפילה.
                Storyboard.SetTarget(fall, transform);
                Storyboard.SetTargetProperty(fall, "TranslateY");
                storyboard.Children.Add(fall);

                var spin = new DoubleAnimation { To = Rnd.Next(-360, 360), Duration = duration, BeginTime = delay };
                // אובייקט: spin — האנימציה של הסיבוב.
                Storyboard.SetTarget(spin, transform);
                Storyboard.SetTargetProperty(spin, "Rotation");
                storyboard.Children.Add(spin);
            }

            storyboard.Completed += (s, e) => CelebrationCanvas.Children.Clear();
            storyboard.Begin();
        }

        // ---------- Input ----------

        private void Cell_Tapped(object sender, TappedRoutedEventArgs e)
        // פעולה: Cell_Tapped — לחיצה על משבצת: בוחרת אותה.
        {
            int index = (int)((Border)sender).Tag;
            // אובייקט: index — המספר של המשבצת שנלחצה.
            selRow = index / 9;
            selCol = index % 9;
            Refresh();
        }

        private void NumberButton_Click(object sender, RoutedEventArgs e) => PlaceNumber((int)((Button)sender).Tag);
        // פעולה: NumberButton_Click — לחיצה על כפתור מספר: כותבת אותו במשבצת שנבחרה.

        /// <summary>Mistakes are shown (red, and cost a heart) unless the player turned that off. Duels always show them.</summary>
        private bool MistakesShown => IsDuel || IsAiDuel || GameData.ShowMistakes;
        // אובייקט: MistakesShown — אמת אם מראים טעויות (באדום, ומורידים לב). בדו-קרב תמיד מראים.

        private void PlaceNumber(int n)
        // פעולה: PlaceNumber — הלב של המשחק: כותבת מספר במשבצת שנבחרה.
        // במצב פתקים — מוסיפה או מוחקת פתק. מספר שגוי — מוריד לב, מנער את המשבצת ומדווח לדו-קרב.
        // מספר נכון — חוגג, מנקה פתקים, ובודק אם הלוח נפתר.
        {
            if (finished || selRow < 0 || puzzle[selRow, selCol] != 0)
                return;

            if (notesMode)
            {
                if (board[selRow, selCol] != 0)
                    return;
                PushHistory();
                notes[selRow, selCol] ^= 1 << n;
                if ((notes[selRow, selCol] & (1 << n)) != 0)
                    Progression.NoteWritten();
                Refresh();
                PopCell(selRow, selCol, 1.2);
                SaveCurrentGame();
                return;
            }

            if (board[selRow, selCol] == n)
                return;

            PushHistory();
            hinted[selRow, selCol] = false;
            PlaceValue(selRow, selCol, n);

            // Challenge mode: no red, no hearts lost; a full board that isn't right just says so
            if (!MistakesShown)
            {
                if (n != solution[selRow, selCol])
                    mistakes++;
                Sounds.Play("correct");
                Refresh();
                PopCell(selRow, selCol);
                Mascot("😊");
                SaveCurrentGame();
                if (IsSolved())
                    Win();
                else if (BoardFull())
                    StatusText.Text = "הלוח מלא, אבל משהו לא נכון — חפשו את הטעות 🔍";
                return;
            }

            if (n != solution[selRow, selCol])
            {
                mistakes++;
                if (!isZen)
                    lives--;   // zen mode never takes hearts
                Sounds.Play("wrong");
                StatusText.Text = "אופס, המספר הזה לא מתאים כאן";
                ShakeCell(selRow, selCol);
                BreakHeart();
                Mascot("😮");
                if (IsDuel)
                {
                    DuelMove(false, selRow, selCol);
                }
                else if (IsAiDuel)
                {
                    AiPlayerMove(false, selRow, selCol);
                }
                else if (lives <= 0 && GameData.UseBonusLife())
                {
                    lives = 1;
                    StatusText.Text = "השתמשנו בלב שמור מהחנות 💖";
                }
                Refresh();
                SaveCurrentGame();
                if (lives <= 0)
                {
                    if (IsDuel)
                        DuelFinish(solved: false);
                    else if (IsAiDuel)
                        AiDuelOver(playerWon: false);
                    else
                        GameOver();
                }
                return;
            }

            Sounds.Play("correct");
            StatusText.Text = "יופי! 💕";
            if (IsDuel)
                DuelMove(true, selRow, selCol);
            else if (IsAiDuel)
                AiPlayerMove(true, selRow, selCol);
            Refresh();
            PopCell(selRow, selCol);
            FlashCell(selRow, selCol);
            Mascot("😄");
            CelebrateCompletedUnits(selRow, selCol);
            SaveCurrentGame();
            if (IsSolved())
            {
                if (IsDuel)
                    DuelFinish(solved: true);
                else if (IsAiDuel)
                    AiDuelOver(playerWon: true);
                else
                    Win();
            }
        }

        private void HintButton_Click(object sender, RoutedEventArgs e)
        // פעולה: HintButton_Click — כפתור "רמז": מגלה את המספר הנכון במשבצת שנבחרה (או במשבצת אקראית שעוד לא נפתרה).
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
            // אובייקט: row, col — המשבצת שבה ייכתב הרמז.
            if (selRow >= 0 && puzzle[selRow, selCol] == 0 && board[selRow, selCol] != solution[selRow, selCol])
            {
                row = selRow;
                col = selCol;
            }
            else
            {
                var open = new List<int>();
                // אובייקט: open — כל המשבצות שעוד לא נכונות.
                for (int i = 0; i < 81; i++)
                {
                    if (board[i / 9, i % 9] != solution[i / 9, i % 9])
                        open.Add(i);
                }
                if (open.Count == 0)
                    return;
                int pick = open[Rnd.Next(open.Count)];
                // אובייקט: pick — משבצת אקראית מתוכן.
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
            PopCell(row, col);
            Mascot("😉");
            SaveCurrentGame();
            if (IsSolved())
                Win();
        }

        private void UndoButton_Click(object sender, RoutedEventArgs e)
        // פעולה: UndoButton_Click — כפתור "בטל": מחזיר את הלוח לצילום האחרון שנשמר.
        {
            if (finished || history.Count == 0)
                return;

            BoardSnapshot last = history.Pop();
            // אובייקט: last — הצילום האחרון מהערימה.
            board = last.Board;
            notes = last.Notes;
            hinted = last.Hinted;
            StatusText.Text = "ביטלנו את הצעד האחרון ↩";
            Refresh();
            SaveCurrentGame();
        }

        private void EraseButton_Click(object sender, RoutedEventArgs e)
        // פעולה: EraseButton_Click — כפתור "מחק": מוחק את המספר והפתקים מהמשבצת שנבחרה.
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
            SaveCurrentGame();
        }

        private void NotesButton_Click(object sender, RoutedEventArgs e)
        // פעולה: NotesButton_Click — כפתור "פתקים": מפעיל או מכבה את מצב הפתקים.
        {
            notesMode = !notesMode;
            StatusText.Text = notesMode
                ? "מצב פתקים: המספרים ייכתבו קטן, בתור אפשרויות ✏️"
                : "בחרו משבצת ריקה ואז מספר";
            UpdateTools();
        }

        // Same board, wipes everything the player did
        private void RestartButton_Click(object sender, RoutedEventArgs e)
        // פעולה: RestartButton_Click — כפתור "מההתחלה": אותו לוח, ומוחק את כל מה שהשחקן עשה.
        {
            StartBoard();
        }

        // A different board at the same level
        private void NewBoardButton_Click(object sender, RoutedEventArgs e)
        // פעולה: NewBoardButton_Click — כפתור "לוח חדש": לוח אחר באותו שלב.
        {
            NewGame();
        }

        private void NextLevelButton_Click(object sender, RoutedEventArgs e)
        // פעולה: NextLevelButton_Click — הכפתור בחלון הניצחון: ממשיך לשלב הבא (או חוזר למסע, לתפריט או לרשימת השלבים).
        {
            if (isZen)
            {
                StartZen();
                return;
            }
            if (adventure != null)
            {
                if (Frame.CanGoBack)
                    Frame.GoBack();
                return;
            }
            if (isDaily)
            {
                Frame.Navigate(typeof(MenuPage));
                Frame.BackStack.Clear();
                return;
            }

            int? next = Levels.Next(level);
            // אובייקט: next — השלב הבא.
            if (next == null)
            {
                // That was the very last level
                if (Frame.CanGoBack)
                    Frame.GoBack();
                return;
            }

            level = next.Value;
            NewGame();
        }

        // Stop: freezes the timer and hides the board until the player continues
        private void PauseButton_Click(object sender, RoutedEventArgs e)
        // פעולה: PauseButton_Click — כפתור "עצירה": עוצרת את השעון ומסתירה את הלוח עד שממשיכים.
        {
            if (finished)
                return;

            paused = true;
            timer.Stop();
            PauseTimeText.Text = $"הזמן נעצר על ⏱ {FormatTime(seconds)}";
            PauseNewBoardButton.Visibility = NewBoardButton.Visibility;   // not in the daily challenge, adventure or duels
            PauseOverlay.Visibility = Visibility.Visible;
        }

        private void ResumeButton_Click(object sender, RoutedEventArgs e)
        // פעולה: ResumeButton_Click — כפתור "להמשיך": מחזירה את הלוח ומפעילה שוב את השעון.
        {
            paused = false;
            PauseOverlay.Visibility = Visibility.Collapsed;
            if (!finished)
                timer.Start();
        }

        private void SoundButton_Click(object sender, RoutedEventArgs e)
        // פעולה: SoundButton_Click — כפתור הצליל: מפעיל או מכבה צלילים.
        {
            GameData.SetSound(!GameData.SoundOn);
            UpdateTools();
        }

        private void HelpButton_Click(object sender, RoutedEventArgs e)
        // פעולה: HelpButton_Click — כפתור "עזרה": עובר למסך ההסברים.
        {
            Frame.Navigate(typeof(HelpPage));
        }

        // Overrides the back button of BasePage: leaving in the middle of a duel tells the server first
        protected override void BackButton_Click(object sender, RoutedEventArgs e)
        // פעולה: BackButton_Click — משנה את כפתור "חזרה" של BasePage: יציאה באמצע דו-קרב מודיעה קודם לשרת.
        {
            if (IsDuel)
                LeaveDuel();
            EndAiDuel();
            base.BackButton_Click(sender, e);
        }
    }
}
