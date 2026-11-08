using System;
using System.Collections.Generic;
using System.Linq;
using Windows.System;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;

namespace Smart_Sudoku.Pages
{
    /// <summary>
    /// Extras for the game screen: animations, the mascot, keyboard play, board frames,
    /// saving a game to continue later, and the daily challenge.
    /// </summary>
    public sealed partial class GamePage
    {
        private bool isDaily;
        private bool isZen;
        private AdventureStart? adventure;

        // ---------- Keyboard ----------

        private void HookKeyboard() => Window.Current.CoreWindow.KeyDown += OnKeyDown;

        private void UnhookKeyboard() => Window.Current.CoreWindow.KeyDown -= OnKeyDown;

        // 1-9 place a number, arrows move, Backspace/Delete erase, N toggles notes, Ctrl+Z undoes
        private void OnKeyDown(CoreWindow sender, KeyEventArgs e)
        {
            if (finished || paused || CountdownOverlay.Visibility == Visibility.Visible)
                return;

            var key = e.VirtualKey;
            int digit = key >= VirtualKey.Number1 && key <= VirtualKey.Number9 ? key - VirtualKey.Number0
                : key >= VirtualKey.NumberPad1 && key <= VirtualKey.NumberPad9 ? key - VirtualKey.NumberPad0
                : 0;
            bool ctrl = sender.GetKeyState(VirtualKey.Control).HasFlag(CoreVirtualKeyStates.Down);

            if (digit > 0)
            {
                if (selRow < 0)
                    selRow = selCol = 0;
                PlaceNumber(digit);
            }
            else if (key == VirtualKey.Back || key == VirtualKey.Delete)
                EraseButton_Click(this, new RoutedEventArgs());
            else if (key == VirtualKey.N)
                NotesButton_Click(this, new RoutedEventArgs());
            else if (key == VirtualKey.Z && ctrl)
                UndoButton_Click(this, new RoutedEventArgs());
            else if (key == VirtualKey.Up || key == VirtualKey.Down || key == VirtualKey.Left || key == VirtualKey.Right)
            {
                if (selRow < 0)
                    selRow = selCol = 0;
                else if (key == VirtualKey.Up)
                    selRow = (selRow + 8) % 9;
                else if (key == VirtualKey.Down)
                    selRow = (selRow + 1) % 9;
                else if (key == VirtualKey.Left)
                    selCol = (selCol + 8) % 9;
                else
                    selCol = (selCol + 1) % 9;
                Refresh();
            }
            else
                return;

            e.Handled = true;
        }

        // ---------- Animations ----------

        private static readonly SolidColorBrush FlashBrush = new SolidColorBrush(Color.FromArgb(255, 200, 240, 210));

        private static void Animate(DependencyObject target, string property, double from, double to, double seconds,
            bool autoReverse = false, EasingFunctionBase? easing = null)
        {
            var animation = new DoubleAnimation
            {
                From = from,
                To = to,
                Duration = TimeSpan.FromSeconds(seconds),
                AutoReverse = autoReverse,
                EasingFunction = easing,
            };
            Storyboard.SetTarget(animation, target);
            Storyboard.SetTargetProperty(animation, property);
            var storyboard = new Storyboard();
            storyboard.Children.Add(animation);
            storyboard.Begin();
        }

        // The element's grow/shrink transform (made the first time it is needed)
        private static ScaleTransform ScaleOf(UIElement element)
        {
            var scale = element.RenderTransform as ScaleTransform;
            if (scale == null)
            {
                scale = new ScaleTransform();
                element.RenderTransform = scale;
                element.RenderTransformOrigin = new Windows.Foundation.Point(0.5, 0.5);
            }
            return scale;
        }

        /// <summary>The number grows for a moment and settles.</summary>
        private void PopCell(int row, int col, double size = 1.45)
        {
            var scale = ScaleOf(cellTexts[row, col]);
            var ease = new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.6 };
            Animate(scale, "ScaleX", size, 1, 0.35, easing: ease);
            Animate(scale, "ScaleY", size, 1, 0.35, easing: ease);
        }

        /// <summary>A correct cell glows green for a moment.</summary>
        private void FlashCell(int row, int col)
        {
            var cell = cells[row, col];
            cell.Background = FlashBrush;
            var back = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(380) };
            back.Tick += (s, e) =>
            {
                back.Stop();
                Refresh();
            };
            back.Start();
        }

        /// <summary>A wrong cell shakes.</summary>
        private void ShakeCell(int row, int col)
        {
            var cell = cells[row, col];
            var move = cell.RenderTransform as TranslateTransform;
            if (move == null)
            {
                move = new TranslateTransform();
                cell.RenderTransform = move;
            }
            var shake = new DoubleAnimationUsingKeyFrames { Duration = TimeSpan.FromSeconds(0.4) };
            double[] offsets = { 0, -7, 7, -5, 5, -2, 0 };
            for (int i = 0; i < offsets.Length; i++)
                shake.KeyFrames.Add(new LinearDoubleKeyFrame { KeyTime = TimeSpan.FromSeconds(0.4 * i / (offsets.Length - 1)), Value = offsets[i] });
            Storyboard.SetTarget(shake, move);
            Storyboard.SetTargetProperty(shake, "X");
            var storyboard = new Storyboard();
            storyboard.Children.Add(shake);
            storyboard.Begin();
        }

        /// <summary>The hearts pulse when one is lost.</summary>
        private void BreakHeart()
        {
            var scale = ScaleOf(LivesText);
            Animate(scale, "ScaleX", 1, 1.35, 0.15, autoReverse: true);
            Animate(scale, "ScaleY", 1, 1.35, 0.15, autoReverse: true);
        }

        /// <summary>When a row, column or box is completed correctly, its cells pop one after another.</summary>
        private void CelebrateCompletedUnits(int row, int col)
        {
            // Cells are kept as one number: index = row * 9 + column
            var rowCells = new List<int>();
            var colCells = new List<int>();
            var boxCells = new List<int>();
            int br = row / 3 * 3, bc = col / 3 * 3;
            for (int i = 0; i < 9; i++)
            {
                rowCells.Add(row * 9 + i);
                colCells.Add(i * 9 + col);
                boxCells.Add((br + i / 3) * 9 + (bc + i % 3));
            }

            // The cells of every completed row / column / box, each cell once
            var wave = new List<int>();
            foreach (var unit in new[] { rowCells, colCells, boxCells })
            {
                if (!IsUnitCorrect(unit))
                    continue;
                foreach (int index in unit)
                {
                    if (!wave.Contains(index))
                        wave.Add(index);
                }
            }
            if (wave.Count == 0)
                return;

            int step = 0;
            var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(45) };
            timer.Tick += (s, e) =>
            {
                if (step >= wave.Count)
                {
                    timer.Stop();
                    return;
                }
                int r = wave[step] / 9, c = wave[step] % 9;
                step++;
                PopCell(r, c, 1.3);
                cells[r, c].Background = Res("SelectedCellBrush");
            };
            timer.Start();
            var settle = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(45 * wave.Count + 350) };
            settle.Tick += (s, e) =>
            {
                settle.Stop();
                Refresh();
            };
            settle.Start();
            Mascot("🤩");
        }

        // True when every cell in the list holds its right number
        private bool IsUnitCorrect(List<int> unit)
        {
            foreach (int index in unit)
            {
                if (board[index / 9, index % 9] != solution[index / 9, index % 9])
                    return false;
            }
            return true;
        }

        // ---------- Fireworks (expert levels and bosses) ----------

        private void Fireworks()
        {
            var random = new Random();
            uint[] colors = { 0xFFFF6B9Du, 0xFFFFD166u, 0xFF6BD4FFu, 0xFFB38BFFu, 0xFF7CE38Bu, 0xFFFF9F5Au };
            var storyboard = new Storyboard();

            for (int burst = 0; burst < 6; burst++)
            {
                double x = ActualWidth * (0.15 + random.NextDouble() * 0.7);
                double y = ActualHeight * (0.15 + random.NextDouble() * 0.4);
                var color = new SolidColorBrush(Themes.ToColor(colors[random.Next(colors.Length)]));
                var begin = TimeSpan.FromSeconds(burst * 0.4);

                // Sparks fly out in a circle and fade
                for (int i = 0; i < 22; i++)
                {
                    double angle = i * Math.PI * 2 / 22;
                    double distance = 110 + random.NextDouble() * 60;
                    var move = new TranslateTransform();
                    var spark = new TextBlock { Text = "✦", FontSize = 18, Foreground = color, RenderTransform = move, Opacity = 0 };
                    Canvas.SetLeft(spark, x);
                    Canvas.SetTop(spark, y);
                    CelebrationCanvas.Children.Add(spark);

                    // Fly sideways (X) and down a little (Y), in the spark's direction
                    storyboard.Children.Add(SparkFlight(move, "X", Math.Cos(angle) * distance, begin));
                    storyboard.Children.Add(SparkFlight(move, "Y", Math.Sin(angle) * distance + 30, begin));
                    var fade = new DoubleAnimationUsingKeyFrames { BeginTime = begin };
                    fade.KeyFrames.Add(new LinearDoubleKeyFrame { KeyTime = TimeSpan.Zero, Value = 1 });
                    fade.KeyFrames.Add(new LinearDoubleKeyFrame { KeyTime = TimeSpan.FromSeconds(0.6), Value = 1 });
                    fade.KeyFrames.Add(new LinearDoubleKeyFrame { KeyTime = TimeSpan.FromSeconds(1.2), Value = 0 });
                    Storyboard.SetTarget(fade, spark);
                    Storyboard.SetTargetProperty(fade, "Opacity");
                    storyboard.Children.Add(fade);
                }
            }
            storyboard.Begin();
        }

        private static DoubleAnimation SparkFlight(TranslateTransform move, string property, double to, TimeSpan begin)
        {
            var fly = new DoubleAnimation
            {
                To = to,
                BeginTime = begin,
                Duration = TimeSpan.FromSeconds(1.1),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut },
            };
            Storyboard.SetTarget(fly, move);
            Storyboard.SetTargetProperty(fly, property);
            return fly;
        }

        // ---------- Mascot ----------

        private DispatcherTimer? mascotTimer;

        /// <summary>The little face next to the timer reacts to the game, then goes back to smiling.</summary>
        private void Mascot(string face, bool stay = false)
        {
            MascotFace.Text = Cosmetics.Face(face);
            var scale = ScaleOf(MascotFace);
            Animate(scale, "ScaleX", 1, 1.3, 0.14, autoReverse: true);
            Animate(scale, "ScaleY", 1, 1.3, 0.14, autoReverse: true);

            mascotTimer?.Stop();
            if (stay)
                return;
            mascotTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1.8) };
            mascotTimer.Tick += (s, e) =>
            {
                mascotTimer.Stop();
                MascotFace.Text = Cosmetics.Face(notesMode ? "🤔" : "😊");
            };
            mascotTimer.Start();
        }

        // ---------- Board frames (bought in the store) ----------

        private void ApplyFrame()
        {
            var frame = BoardFrames.Get(GameData.FrameId);
            BoardFrame.BorderThickness = new Thickness(frame.Thickness);
            BoardFrame.BorderBrush = frame.Colors == null
                ? Res("RoseBrush")
                : BoardFrames.Gradient(frame.Colors);
            foreach (var corner in new[] { FrameCornerTL, FrameCornerTR, FrameCornerBL, FrameCornerBR })
                corner.Text = frame.Corner ?? "";
        }

        // ---------- Saving a game to continue later ----------

        // A 9×9 grid as one list of 81 cells, row by row (that's how it is saved)
        private static int[] Flat(int[,] grid)
        {
            var cells = new int[81];
            for (int i = 0; i < 81; i++)
                cells[i] = grid[i / 9, i % 9];
            return cells;
        }

        private static bool[] Flat(bool[,] grid)
        {
            var cells = new bool[81];
            for (int i = 0; i < 81; i++)
                cells[i] = grid[i / 9, i % 9];
            return cells;
        }

        private static int[,] Grid9(int[] cells)
        {
            var grid = new int[9, 9];
            for (int i = 0; i < 81; i++)
                grid[i / 9, i % 9] = cells[i];
            return grid;
        }

        /// <summary>Saves the board after each move (levels and the daily challenge, not duels).</summary>
        private void SaveCurrentGame()
        {
            // Duels, zen and adventure stages are short and aren't saved to continue later
            if (IsDuel || isZen || adventure != null || !hasGame || finished)
                return;

            GameData.SavedGame = new SavedGame
            {
                LevelId = level,
                Daily = isDaily,
                Puzzle = Flat(puzzle),
                Solution = Flat(solution),
                Board = Flat(board),
                Notes = Flat(notes),
                Hinted = Flat(hinted),
                Lives = lives,
                Mistakes = mistakes,
                HintsUsed = hintsUsed,
                Seconds = seconds,
            };
        }

        private void ClearSavedGame()
        {
            if (GameData.SavedGame != null)
                GameData.SavedGame = null;
        }

        private void ResumeGame(SavedGame saved)
        {
            level = saved.LevelId;
            isDaily = saved.Daily;
            isZen = false;
            adventure = null;
            puzzle = Grid9(saved.Puzzle);
            solution = Grid9(saved.Solution);
            StartBoard();

            board = Grid9(saved.Board);
            notes = Grid9(saved.Notes);
            for (int i = 0; i < 81; i++)
                hinted[i / 9, i % 9] = saved.Hinted[i];
            lives = saved.Lives;
            mistakes = saved.Mistakes;
            hintsUsed = saved.HintsUsed;
            seconds = saved.Seconds;
            SetTitle();
            StatusText.Text = "ממשיכים מאיפה שעצרת 💕";
            Refresh();
        }

        // ---------- Daily challenge ----------

        private void StartDaily()
        {
            isDaily = true;
            isZen = false;
            adventure = null;
            level = DailyChallenge.LevelId;
            (puzzle, solution) = DailyChallenge.TodaysBoard();
            StartBoard();
            SetTitle();
        }

        private void SetTitle()
        {
            // Zen hides the clock, the hearts and the records: just the board
            var zenHidden = isZen ? Visibility.Collapsed : Visibility.Visible;
            TimerText.Visibility = zenHidden;
            LivesText.Visibility = zenHidden;
            if (!IsDuel)
            {
                SpeedText.Visibility = isZen || adventure != null || isDaily ? Visibility.Collapsed : Visibility.Visible;
                BestText.Visibility = SpeedText.Visibility;
            }

            if (isZen)
            {
                LevelText.Text = "🧘 מצב זן";
                return;
            }
            if (adventure != null)
            {
                var world = Adventure.Worlds[adventure.World];
                if (Adventure.IsBoss(adventure.Stage))
                    LevelText.Text = $"{world.Emoji} {world.Name} · בוס {world.Boss}";
                else
                    LevelText.Text = $"{world.Emoji} {world.Name} · שלב {adventure.Stage}";
                return;
            }
            if (isDaily)
            {
                LevelText.Text = $"📅 אתגר יומי · {DateTime.Now:dd/MM}";
                return;
            }
            var difficulty = Levels.DifficultyFor(level);
            LevelText.Text = $"Level {Levels.NumberOf(level)} · {difficulty.Name} {difficulty.Emoji}";
        }

        private void WinDaily(int stars)
        {
            bool already = DailyChallenge.DoneToday;
            var (coins, streak) = DailyChallenge.Complete(stars, seconds);
            if (!already)
                Progression.DailySolved(seconds, mistakes);

            WinStarsText.Text = StarsText(stars);
            if (already)
            {
                WinText.Text = "פתרת שוב את האתגר של היום 🎉";
                WinDetailsText.Text = "המטבעות על האתגר כבר התקבלו היום. נתראה מחר! 💖";
            }
            else
            {
                int bonus = DailyChallenge.StreakBonus(streak);
                WinText.Text = $"פתרת את האתגר היומי! 🔥 רצף: {streak} ימים";
                WinDetailsText.Text = $"⏱ {FormatTime(seconds)}\n\nפתרון: +{coins - bonus}\nבונוס רצף: +{bonus}\n🪙 סה\"כ: {coins}+ מטבעות";
            }
            NextLevelButton.Content = "לתפריט";
            BackToLevelsButton.Visibility = Visibility.Collapsed;
        }

        // ---------- Zen mode ----------

        private void StartZen()
        {
            isZen = true;
            isDaily = false;
            adventure = null;
            level = Levels.Id(1, 5);
            (puzzle, solution) = SudokuGenerator.Create(Levels.EmptyCells(level));
            StartBoard();
            StatusText.Text = "נושמים עמוק... אין שעון, אין לבבות, רק סודוקו 🌿";
            Sounds.Music(GameData.SoundOn ? "zen" : null);
        }

        private void WinZen()
        {
            Progression.ZenSolved(seconds);
            Achievements.Unlock("zen");
            GameData.AddCoins(10);
            WinStarsText.Text = "🌸 🌿 🌸";
            WinText.Text = "פתרת בשלווה 🧘";
            WinDetailsText.Text = "🪙 +10 מטבעות";
            NextLevelButton.Content = "לוח זן נוסף  🌿";
            BackToLevelsButton.Visibility = Visibility.Visible;
        }

        // ---------- Adventure ----------

        private void StartAdventure(AdventureStart stage)
        {
            adventure = stage;
            isZen = false;
            isDaily = false;
            level = Adventure.LevelIdFor(stage.World, stage.Stage);
            (puzzle, solution) = SudokuGenerator.Create(Adventure.EmptyCells(stage.World, stage.Stage));
            StartBoard();
            if (Adventure.IsBoss(stage.Stage))
                StatusText.Text = $"זה הבוס של העולם! {Adventure.Worlds[stage.World].Boss} בהצלחה!";
        }

        private void WinAdventure(int stars)
        {
            if (adventure == null)
                return;
            AdventureStart stage = adventure;
            bool boss = Adventure.IsBoss(stage.Stage);
            Adventure.Record(stage.World, stage.Stage, stars);
            int coins = GameData.RewardCoins(level, stars, seconds) + (boss ? Adventure.BossBonus : 0);
            GameData.AddCoins(coins);
            Progression.AdventureSolved(seconds, mistakes, boss);
            if (boss)
            {
                Achievements.Unlock("boss");
                Fireworks();
                if (stage.World == Adventure.Worlds.Count - 1)
                    Achievements.Unlock("adventure-all");
            }

            var world = Adventure.Worlds[stage.World];
            bool lastWorld = stage.World == Adventure.Worlds.Count - 1;
            WinStarsText.Text = StarsText(stars);
            if (!boss)
                WinText.Text = $"עברת את שלב {stage.Stage} ב{world.Name}!";
            else if (!lastWorld)
                WinText.Text = $"ניצחת את הבוס {world.Boss}! עולם חדש נפתח 🗺️";
            else
                WinText.Text = "סיימת את כל המסע! איזו הרפתקנית 👑";
            WinDetailsText.Text = $"⏱ {FormatTime(seconds)}\n🪙 +{coins} מטבעות" + (boss ? $" (כולל בונוס בוס {Adventure.BossBonus})" : "");
            NextLevelButton.Content = "חזרה למפה 🗺️";
            BackToLevelsButton.Visibility = Visibility.Collapsed;
        }

        /// <summary>Achievements that depend on how a level was solved.</summary>
        private void CheckLevelAchievements(int stars)
        {
            Achievements.Unlock("first-win");
            if (mistakes == 0 && hintsUsed == 0)
                Achievements.Unlock("perfect");
            if (seconds < 120)
                Achievements.Unlock("speedy");
            if (GameData.ThreeStarLevelsCount >= 10)
                Achievements.Unlock("perfect10");
            if (Levels.DifficultyOf(level) == 3)
                Achievements.Unlock("expert");
            // Every easy level (difficulty 0) has at least one star
            bool allEasySolved = true;
            for (int n = 1; n <= Levels.PerDifficulty; n++)
            {
                if (GameData.GetStars(Levels.Id(0, n)) == 0)
                    allEasySolved = false;
            }
            if (allEasySolved)
                Achievements.Unlock("easy-done");
        }
    }
}
