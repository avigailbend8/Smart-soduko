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

        private static ScaleTransform ScaleOf(UIElement element)
        {
            if (element.RenderTransform is not ScaleTransform scale)
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
            if (cell.RenderTransform is not TranslateTransform move)
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
            var units = new List<List<(int R, int C)>>();
            var rowCells = Enumerable.Range(0, 9).Select(c => (row, c)).ToList();
            var colCells = Enumerable.Range(0, 9).Select(r => (r, col)).ToList();
            int br = row / 3 * 3, bc = col / 3 * 3;
            var boxCells = Enumerable.Range(0, 9).Select(i => (br + i / 3, bc + i % 3)).ToList();
            foreach (var unit in new[] { rowCells, colCells, boxCells })
            {
                if (unit.All(p => board[p.Item1, p.Item2] == solution[p.Item1, p.Item2]))
                    units.Add(unit);
            }
            if (units.Count == 0)
                return;

            var wave = units.SelectMany(u => u).Distinct().ToList();
            int step = 0;
            var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(45) };
            timer.Tick += (s, e) =>
            {
                if (step >= wave.Count)
                {
                    timer.Stop();
                    return;
                }
                var (r, c) = wave[step++];
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

        // ---------- Mascot ----------

        private DispatcherTimer? mascotTimer;

        /// <summary>The little face next to the timer reacts to the game, then goes back to smiling.</summary>
        private void Mascot(string face, bool stay = false)
        {
            MascotFace.Text = face;
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
                MascotFace.Text = notesMode ? "🤔" : "😊";
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

        private static int[] Flat(int[,] grid) => Enumerable.Range(0, 81).Select(i => grid[i / 9, i % 9]).ToArray();

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
            if (IsDuel || !hasGame || finished)
                return;

            GameData.SavedGame = new SavedGame
            {
                LevelId = level,
                Daily = isDaily,
                Puzzle = Flat(puzzle),
                Solution = Flat(solution),
                Board = Flat(board),
                Notes = Flat(notes),
                Hinted = Enumerable.Range(0, 81).Select(i => hinted[i / 9, i % 9]).ToArray(),
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
            level = DailyChallenge.LevelId;
            (puzzle, solution) = DailyChallenge.TodaysBoard();
            StartBoard();
            SetTitle();
        }

        private void SetTitle()
        {
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

            WinStarsText.Text = new string('★', stars) + new string('☆', 3 - stars);
            WinText.Text = already ? "פתרת שוב את האתגר של היום 🎉" : $"פתרת את האתגר היומי! 🔥 רצף: {streak} ימים";
            WinDetailsText.Text = already
                ? "המטבעות על האתגר כבר התקבלו היום. נתראה מחר! 💖"
                : $"⏱ {FormatTime(seconds)}\n\nפתרון: +{coins - DailyChallenge.StreakBonus(streak)}\nבונוס רצף: +{DailyChallenge.StreakBonus(streak)}\n🪙 סה\"כ: {coins}+ מטבעות";
            NextLevelButton.Content = "לתפריט";
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
            if (Enumerable.Range(1, Levels.PerDifficulty).All(n => GameData.GetStars(Levels.Id(0, n)) > 0))
                Achievements.Unlock("easy-done");
        }
    }
}
