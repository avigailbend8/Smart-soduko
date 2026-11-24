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
    // אובייקט: GamePage (חלק הדו-קרב נגד המחשב) — השחקן והמחשב מתחרים על אותו לוח. המחשב פותר עותק משלו, ומי שממלא ראשון את כל הלוח מנצח.
    // הנקודות נספרות כמו בדו-קרב ברשת: +10 על משבצת נכונה, −5 על טעות, +100 על סיום.
    {
        private const int AiLoserCoins = 10;   // a few coins just for playing
        // אובייקט: AiLoserCoins — כמה מטבעות מקבלים גם בהפסד, רק על ההשתתפות (10).

        private AiOpponent? ai;
        // אובייקט: ai — יריב המחשב (או null כשלא משחקים נגד המחשב).
        private DispatcherTimer? aiTimer;
        // אובייקט: aiTimer — השעון שקובע מתי המחשב עושה את המהלך הבא.
        private int[,] aiBoard = new int[9, 9];
        // אובייקט: aiBoard — הלוח של המחשב (העותק שהוא פותר).
        private int aiScore;        // the computer's points
        // אובייקט: aiScore — הנקודות של המחשב.
        private int myAiScore;      // the player's points
        // אובייקט: myAiScore — הנקודות של השחקן.
        private int aiCellsLeft;    // empty cells left on the computer's board
        // אובייקט: aiCellsLeft — כמה משבצות ריקות נשארו ללוח של המחשב.
        private bool IsAiDuel => ai != null;
        // אובייקט: IsAiDuel — אמת כשמשחקים עכשיו נגד המחשב.

        private void StartAiDuel(AiDuelStart start)
        // פעולה: StartAiDuel — מתחילה דו-קרב נגד המחשב: בוחרת יריב, יוצרת לוח אחד לשניהם (שלב באמצע הרמה של היריב),
        // מאפסת נקודות, מציגה את לוח התוצאות, ומפעילה ספירה לאחור 5-4-3-2-1.
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
        // פעולה: EndAiDuel — מסיימת את מצב הדו-קרב נגד המחשב: עוצרת את השעון שלו ומסתירה את החלונות של הדו-קרב.
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
        // פעולה: StartAiTimer — רצה בסוף הספירה לאחור: המחשב מתחיל "לחשוב".
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
        // פעולה: AiTimer_Tick — המחשב עושה מהלך אחד: נכון — מוסיף לו נקודות, שגוי — מוריד. אם הוא סיים את הלוח — הוא ניצח.
        // אחרי כל מהלך הוא מגריל זמן חדש עד המהלך הבא.
        {
            if (ai == null || aiTimer == null || finished)
                return;

            AiMove? move = ai.ChooseMove(aiBoard, solution);
            // אובייקט: move — המהלך שהמחשב בחר.
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
        // פעולה: AiPlayerMove — רצה אחרי כל מהלך של השחקן ומעדכנת את הנקודות שלו. משבצת נותנת נקודות רק בפעם הראשונה שהיא נכונה.
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
        // פעולה: AiDuelOver — הדו-קרב נגמר: השחקן ניצח (פתר ראשון) או הפסיד (המחשב פתר ראשון, או שנגמרו הלבבות).
        // נותנת מטבעות, הישגים, ומציגה חלון תוצאות עם המקום של כל אחד.
        {
            if (ai == null)
                return;

            finished = true;
            timer.Stop();
            aiTimer?.Stop();

            int coins;
            // אובייקט: coins — כמה מטבעות השחקן מקבל.
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
            // אובייקט: text — ההסבר שמופיע בחלון התוצאות.
            if (playerWon)
                text = $"סיימת את הלוח לפני {ai.Name}! ⏱ {FormatTime(seconds)}\n🪙 +{coins} מטבעות (כולל בונוס ניצחון {ai.WinBonus})";
            else if (lives <= 0)
                text = $"נגמרו הלבבות 💔\n🪙 +{coins} מטבעות על ההשתתפות";
            else
                text = $"{ai.Name} סיים/ה את הלוח ראשון. נסו שוב!\n🪙 +{coins} מטבעות על ההשתתפות";
            DuelText.Text = text;

            // The winner is the one who finished first, so they're listed first (even with fewer points)
            string me = $"{Session.PlayerName ?? "אני"} — {myAiScore} נקודות";
            // אובייקט: me — השורה של השחקן בטבלת התוצאות.
            string computer = $"{ai.Emoji} {ai.Name} — {aiScore} נקודות";
            // אובייקט: computer — השורה של המחשב בטבלת התוצאות.
            DuelRanking.Children.Clear();
            DuelRanking.Children.Add(RankingLine("🥇  " + (playerWon ? me : computer), bold: true));
            DuelRanking.Children.Add(RankingLine("🥈  " + (playerWon ? computer : me), bold: false));
            DuelButtons.Visibility = Visibility.Visible;
            DuelOverlay.Visibility = Visibility.Visible;
        }

        // The score bar above the board: me, and the computer with how many cells it has left
        private void RefreshAiScoreboard()
        // פעולה: RefreshAiScoreboard — מעדכנת את פס התוצאות שמעל הלוח: הנקודות שלי, והנקודות של המחשב וכמה משבצות נשארו לו.
        {
            if (ai == null)
                return;
            DuelScoreboard.Children.Clear();
            DuelScoreboard.Children.Add(ScoreChip($"{Session.PlayerName ?? "אני"}  {myAiScore}", me: true));
            DuelScoreboard.Children.Add(ScoreChip($"{ai.Emoji} {ai.Name}  {aiScore}  ·  נשארו {aiCellsLeft} משבצות", me: false));
        }

        private static Border ScoreChip(string text, bool me)
        // פעולה: ScoreChip — יוצרת "תווית" מעוגלת עם תוצאה (התווית שלי מודגשת).
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
        // פעולה: RankingLine — יוצרת שורה בטבלת התוצאות של סוף הדו-קרב.
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
        // פעולה: CountEmpty — סופרת כמה משבצות ריקות יש בלוח.
        {
            int count = 0;
            // אובייקט: count — המונה של המשבצות הריקות.
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
