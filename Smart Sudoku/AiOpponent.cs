using System;
using System.Collections.Generic;

namespace Smart_Sudoku
{
    /// <summary>Passed to GamePage to start a duel against a computer opponent.</summary>
    public sealed record AiDuelStart(string OpponentId);
    // אובייקט: AiDuelStart — "פתק" שנשלח למסך המשחק כדי להתחיל דו-קרב נגד יריב מחשב מסוים.

    /// <summary>One move the computer makes: where it writes, what it writes, and whether that is right.</summary>
    public class AiMove
    // אובייקט: AiMove — מהלך אחד של המחשב: באיזו שורה ועמודה הוא כותב, איזה מספר, והאם המספר נכון.
    {
        public int Row { get; }
        // אובייקט: Row — השורה של המשבצת.
        public int Col { get; }
        // אובייקט: Col — העמודה של המשבצת.
        public int Number { get; }
        // אובייקט: Number — המספר שהמחשב כותב.
        public bool Correct { get; }
        // אובייקט: Correct — אמת אם המספר נכון.

        public AiMove(int row, int col, int number, bool correct)
        // פעולה: AiMove (בנאי) — יוצרת מהלך חדש עם המקום, המספר והאם הוא נכון.
        {
            Row = row;
            Col = col;
            Number = number;
            Correct = correct;
        }
    }

    /// <summary>
    /// A computer opponent for the "duel against the computer" mode.
    /// It solves its own copy of the board like a person would: it always fills the empty cell
    /// that has the fewest possible numbers. It waits a little between moves, and sometimes makes a mistake.
    /// Each opponent is faster and more accurate than the one before.
    /// </summary>
    public class AiOpponent
    // אובייקט: AiOpponent — יריב מחשב במצב "דו-קרב נגד המחשב". הוא פותר עותק משלו של הלוח כמו בן אדם:
    // תמיד ממלא את המשבצת שיש לה הכי מעט אפשרויות, מחכה קצת בין מהלך למהלך, ולפעמים טועה.
    {
        public string Id { get; }
        // אובייקט: Id — מזהה קצר של היריב (easy, medium, hard).
        public string Name { get; }
        // אובייקט: Name — השם של היריב, כפי שהוא מופיע על המסך.
        public string Emoji { get; }
        // אובייקט: Emoji — האימוג'י של היריב.

        /// <summary>The board's difficulty (index into Levels.Difficulties).</summary>
        public int Difficulty { get; }
        // אובייקט: Difficulty — רמת הקושי של הלוח שמשחקים נגד היריב הזה.

        /// <summary>Extra coins for beating this opponent.</summary>
        public int WinBonus { get; }
        // אובייקט: WinBonus — כמה מטבעות בונוס מקבלים על ניצחון נגד היריב הזה.

        private readonly double minSeconds;     // shortest wait between two moves
        // אובייקט: minSeconds — הזמן הכי קצר שהמחשב מחכה בין שני מהלכים.
        private readonly double maxSeconds;     // longest wait between two moves
        // אובייקט: maxSeconds — הזמן הכי ארוך שהמחשב מחכה בין שני מהלכים.
        private readonly double mistakeChance;  // 0.1 = a mistake in 1 of every 10 moves
        // אובייקט: mistakeChance — הסיכוי שהמחשב יטעה במהלך (0.1 = טעות אחת בכל 10 מהלכים).
        private readonly Random random = new Random();
        // אובייקט: random — מחולל אקראי, לזמני החשיבה ולטעויות.

        public AiOpponent(string id, string name, string emoji, int difficulty, double minSeconds, double maxSeconds, double mistakeChance, int winBonus)
        // פעולה: AiOpponent (בנאי) — יוצרת יריב מחשב עם כל התכונות שלו: שם, קושי, מהירות, סיכוי לטעות ובונוס.
        {
            Id = id;
            Name = name;
            Emoji = emoji;
            Difficulty = difficulty;
            this.minSeconds = minSeconds;
            this.maxSeconds = maxSeconds;
            this.mistakeChance = mistakeChance;
            WinBonus = winBonus;
        }

        public static readonly IReadOnlyList<AiOpponent> All = new[]
        // אובייקט: All — שלושת יריבי המחשב: "רובי המתחיל" (איטי וטועה), "רובוטית" (בינונית) ו"המוח" (מהיר ומדויק).
        {
            new AiOpponent("easy", "רובי המתחיל", "🐣", 0, 9, 13, 0.15, 50),
            new AiOpponent("medium", "רובוטית", "🤖", 1, 6, 9, 0.08, 100),
            new AiOpponent("hard", "המוח", "🧠", 2, 3.5, 6, 0.03, 200),
        };

        public static AiOpponent Get(string id)
        // פעולה: Get — מחזירה יריב לפי המזהה שלו. אם לא נמצא — מחזירה את הראשון.
        {
            foreach (var opponent in All)
            {
                if (opponent.Id == id)
                    return opponent;
            }
            return All[0];
        }

        /// <summary>How long the computer "thinks" before its next move.</summary>
        public TimeSpan NextThinkTime()
        // פעולה: NextThinkTime — מגרילה כמה זמן המחשב "חושב" לפני המהלך הבא (בין הזמן הקצר לארוך).
        {
            return TimeSpan.FromSeconds(minSeconds + random.NextDouble() * (maxSeconds - minSeconds));
        }

        /// <summary>
        /// Chooses the next move on the computer's board, or returns null when the board is full.
        /// The computer's board only ever holds right numbers (a wrong one is erased right away),
        /// so the numbers that "fit" a cell always include the right one.
        /// </summary>
        public AiMove? ChooseMove(int[,] board, int[,] solution)
        // פעולה: ChooseMove — בוחרת את המהלך הבא של המחשב: מוצאת את המשבצת הריקה עם הכי מעט אפשרויות,
        // ובדרך כלל כותבת בה את המספר הנכון. לפעמים היא בוחרת מספר שגוי. כשהלוח מלא — מחזירה null.
        {
            // Find the empty cell with the fewest numbers that fit
            int bestRow = -1, bestCol = -1;
            // אובייקט: bestRow, bestCol — המשבצת הריקה שיש לה הכי מעט אפשרויות.
            List<int> bestOptions = new List<int>();
            // אובייקט: bestOptions — המספרים שמתאימים למשבצת הזאת.
            for (int r = 0; r < 9; r++)
            {
                for (int c = 0; c < 9; c++)
                {
                    if (board[r, c] != 0)
                        continue;

                    List<int> options = Options(board, r, c);
                    // אובייקט: options — המספרים שמתאימים למשבצת שבודקים עכשיו.
                    if (bestRow == -1 || options.Count < bestOptions.Count)
                    {
                        bestRow = r;
                        bestCol = c;
                        bestOptions = options;
                    }
                }
            }

            if (bestRow == -1)
                return null;   // nothing left to fill

            int right = solution[bestRow, bestCol];
            // אובייקט: right — המספר הנכון למשבצת (מתוך הפתרון).

            // Sometimes the computer picks a wrong number, like a person who wasn't careful
            if (random.NextDouble() < mistakeChance)
            {
                int wrong = WrongNumber(bestOptions, right);
                // אובייקט: wrong — מספר שגוי שהמחשב בוחר כשהוא "מתבלבל".
                return new AiMove(bestRow, bestCol, wrong, false);
            }

            return new AiMove(bestRow, bestCol, right, true);
        }

        // The numbers 1-9 that are not yet in this cell's row, column or box
        private static List<int> Options(int[,] board, int row, int col)
        // פעולה: Options — מחזירה את המספרים 1–9 שעוד לא נמצאים בשורה, בעמודה ובריבוע של המשבצת.
        {
            var options = new List<int>();
            // אובייקט: options — רשימת המספרים המתאימים.
            for (int n = 1; n <= 9; n++)
            {
                if (SudokuGenerator.CanPlace(board, row, col, n))
                    options.Add(n);
            }
            return options;
        }

        // A believable wrong number: one that fits the cell if there is one, otherwise any other number
        private int WrongNumber(List<int> options, int right)
        // פעולה: WrongNumber — בוחרת מספר שגוי אמין: עדיף אחד שמתאים למשבצת, ואם אין — כל מספר אחר.
        {
            var wrongOptions = new List<int>();
            // אובייקט: wrongOptions — המספרים המתאימים שאינם התשובה הנכונה.
            foreach (int n in options)
            {
                if (n != right)
                    wrongOptions.Add(n);
            }
            if (wrongOptions.Count > 0)
                return wrongOptions[random.Next(wrongOptions.Count)];

            int other = random.Next(1, 9);   // 1..8
            // אובייקט: other — מספר אקראי אחר, שמדלג על המספר הנכון.
            if (other >= right)
                other++;                      // skip the right number
            return other;
        }
    }
}
