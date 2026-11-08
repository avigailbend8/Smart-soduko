using System;
using Smart_Sudoku.Duel;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Smart_Sudoku.Pages
{
    /// <summary>
    /// Duel against the computer: the player and the computer race on the same board.
    /// The computer solves its own copy (see AiOpponent). Whoever fills the whole board first wins.
    /// Points are counted like in the network duel: +10 for a right cell, -5 for a mistake, +100 for finishing.
    /// </summary>
    public sealed partial class GamePage
    {
        private const int AiLoserCoins = 10;   // a few coins just for playing

        private AiOpponent? ai;
        private DispatcherTimer? aiTimer;
        private int[,] aiBoard = new int[9, 9];
        private int aiScore;        // the computer's points
        private int myAiScore;      // the player's points
        private int aiCellsLeft;    // empty cells left on the computer's board
        private bool IsAiDuel => ai != null;

        private void StartAiDuel(AiDuelStart start)
        {
            EndAiDuel();
            ai = AiOpponent.Get(start.OpponentId);

            // Both get the same board: the middle level of the opponent's difficulty
            level = Levels.Id(ai.Difficulty, Levels.PerDifficulty / 2);
            (puzzle, solution) = SudokuGenerator.Create(Levels.EmptyCells(level));
            StartBoard();

            aiBoard = (int[,])puzzle.Clone();
            aiCellsLeft = CountEmpty(aiBoard);
            aiScore = 0;
            myAiScore = 0;
            duelScored = new bool[9, 9];

            LevelText.Text = $"🤖 דו-קרב נגד {ai.Emoji} {ai.Name}";
            SetDuelUi(true);
            EmojiBar.Visibility = Visibility.Collapsed;   // no one to send reactions to
            CountdownHint.Text = $"{ai.Emoji} {ai.Name} מקבל/ת את אותו לוח. מי שמסיים ראשון מנצח!";
            RefreshAiScoreboard();
            RunCountdown();   // the clocks (the player's and the computer's) start after 5-4-3-2-1
        }

        private void EndAiDuel()
        {
            aiTimer?.Stop();
            aiTimer = null;
            if (ai == null)
                return;
            ai = null;
            SetDuelUi(false);
            DuelOverlay.Visibility = Visibility.Collapsed;
            CountdownOverlay.Visibility = Visibility.Collapsed;
            CountdownHint.Text = "כולם מקבלים את אותו לוח — בהצלחה!";
        }

        /// <summary>Called when the countdown ends: the computer starts thinking.</summary>
        private void StartAiTimer()
        {
            if (ai == null)
                return;
            aiTimer?.Stop();
            aiTimer = new DispatcherTimer { Interval = ai.NextThinkTime() };
            aiTimer.Tick += AiTimer_Tick;
            aiTimer.Start();
        }

        // The computer makes one move, then waits a new random time for the next one
        private void AiTimer_Tick(object? sender, object e)
        {
            if (ai == null || aiTimer == null || finished)
                return;

            AiMove? move = ai.ChooseMove(aiBoard, solution);
            if (move == null)
                return;

            if (move.Correct)
            {
                aiBoard[move.Row, move.Col] = move.Number;
                aiCellsLeft--;
                aiScore += DuelScoring.CorrectCell;
            }
            else
            {
                // A mistake costs points; the computer erases it and tries again later
                aiScore = Math.Max(0, aiScore - DuelScoring.Mistake);
            }
            RefreshAiScoreboard();

            if (aiCellsLeft == 0)
            {
                aiScore += DuelScoring.FinishBonus(1);
                AiDuelOver(playerWon: false);
                return;
            }

            aiTimer.Interval = ai.NextThinkTime();
        }

        /// <summary>Called after each of the player's moves. A cell scores only the first time it's right.</summary>
        private void AiPlayerMove(bool correct, int row, int col)
        {
            if (correct)
            {
                if (duelScored[row, col])
                    return;
                duelScored[row, col] = true;
                myAiScore += DuelScoring.CorrectCell;
            }
            else
            {
                myAiScore = Math.Max(0, myAiScore - DuelScoring.Mistake);
            }
            RefreshAiScoreboard();
        }

        /// <summary>The duel is over: the player solved first (won), or the computer did / the hearts ran out (lost).</summary>
        private void AiDuelOver(bool playerWon)
        {
            if (ai == null)
                return;

            finished = true;
            timer.Stop();
            aiTimer?.Stop();

            int coins;
            if (playerWon)
            {
                myAiScore += DuelScoring.FinishBonus(1);
                coins = GameData.RewardCoins(level, StarsFor(mistakes), seconds) + ai.WinBonus;
                Sounds.Play("win");
                Celebrate();
                Mascot("🥳", stay: true);
                Achievements.Unlock("ai-win");
                if (ai.Id == "hard")
                    Achievements.Unlock("ai-hard");
            }
            else
            {
                coins = AiLoserCoins;
                Mascot("😢", stay: true);
            }
            GameData.AddCoins(coins);
            Progression.AiDuelPlayed(playerWon);
            RefreshAiScoreboard();
            UpdateStats();

            DuelTitle.Text = playerWon ? "ניצחת את המחשב! 🏆" : $"{ai.Emoji} {ai.Name} ניצח/ה הפעם";
            string text;
            if (playerWon)
                text = $"סיימת את הלוח לפני {ai.Name}! ⏱ {FormatTime(seconds)}\n🪙 +{coins} מטבעות (כולל בונוס ניצחון {ai.WinBonus})";
            else if (lives <= 0)
                text = $"נגמרו הלבבות 💔\n🪙 +{coins} מטבעות על ההשתתפות";
            else
                text = $"{ai.Name} סיים/ה את הלוח ראשון. נסו שוב!\n🪙 +{coins} מטבעות על ההשתתפות";
            DuelText.Text = text;

            // The winner is the one who finished first, so they're listed first (even with fewer points)
            string me = $"{Session.PlayerName ?? "אני"} — {myAiScore} נקודות";
            string computer = $"{ai.Emoji} {ai.Name} — {aiScore} נקודות";
            DuelRanking.Children.Clear();
            DuelRanking.Children.Add(RankingLine("🥇  " + (playerWon ? me : computer), bold: true));
            DuelRanking.Children.Add(RankingLine("🥈  " + (playerWon ? computer : me), bold: false));
            DuelButtons.Visibility = Visibility.Visible;
            DuelOverlay.Visibility = Visibility.Visible;
        }

        // The score bar above the board: me, and the computer with how many cells it has left
        private void RefreshAiScoreboard()
        {
            if (ai == null)
                return;
            DuelScoreboard.Children.Clear();
            DuelScoreboard.Children.Add(ScoreChip($"{Session.PlayerName ?? "אני"}  {myAiScore}", me: true));
            DuelScoreboard.Children.Add(ScoreChip($"{ai.Emoji} {ai.Name}  {aiScore}  ·  נשארו {aiCellsLeft} משבצות", me: false));
        }

        private static Border ScoreChip(string text, bool me)
        {
            return new Border
            {
                Background = Res(me ? "SelectedCellBrush" : "RelatedCellBrush"),
                BorderBrush = Res(me ? "RoseBrush" : "SoftPinkBrush"),
                BorderThickness = new Thickness(1.5),
                CornerRadius = new CornerRadius(14),
                Padding = new Thickness(14, 6, 14, 6),
                Child = new TextBlock
                {
                    Text = text,
                    FontSize = 18,
                    FontWeight = me ? FontWeights.Bold : FontWeights.SemiBold,
                    Foreground = Res("BerryBrush"),
                },
            };
        }

        private static TextBlock RankingLine(string text, bool bold)
        {
            return new TextBlock
            {
                Text = text,
                FontSize = 19,
                FontWeight = bold ? FontWeights.Bold : FontWeights.Normal,
                Foreground = Res("BerryBrush"),
                HorizontalAlignment = HorizontalAlignment.Center,
            };
        }

        private static int CountEmpty(int[,] grid)
        {
            int count = 0;
            for (int r = 0; r < 9; r++)
            {
                for (int c = 0; c < 9; c++)
                {
                    if (grid[r, c] == 0)
                        count++;
                }
            }
            return count;
        }
    }
}
