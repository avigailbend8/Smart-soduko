using System;
using System.Collections.Generic;

namespace Smart_Sudoku
{
    /// <summary>Passed to GamePage to start a duel against a computer opponent.</summary>
    public sealed record AiDuelStart(string OpponentId);

    /// <summary>One move the computer makes: where it writes, what it writes, and whether that is right.</summary>
    public class AiMove
    {
        public int Row { get; }
        public int Col { get; }
        public int Number { get; }
        public bool Correct { get; }

        public AiMove(int row, int col, int number, bool correct)
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
    {
        public string Id { get; }
        public string Name { get; }
        public string Emoji { get; }

        /// <summary>The board's difficulty (index into Levels.Difficulties).</summary>
        public int Difficulty { get; }

        /// <summary>Extra coins for beating this opponent.</summary>
        public int WinBonus { get; }

        private readonly double minSeconds;     // shortest wait between two moves
        private readonly double maxSeconds;     // longest wait between two moves
        private readonly double mistakeChance;  // 0.1 = a mistake in 1 of every 10 moves
        private readonly Random random = new Random();

        public AiOpponent(string id, string name, string emoji, int difficulty, double minSeconds, double maxSeconds, double mistakeChance, int winBonus)
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
        {
            new AiOpponent("easy", "רובי המתחיל", "🐣", 0, 9, 13, 0.15, 50),
            new AiOpponent("medium", "רובוטית", "🤖", 1, 6, 9, 0.08, 100),
            new AiOpponent("hard", "המוח", "🧠", 2, 3.5, 6, 0.03, 200),
        };

        public static AiOpponent Get(string id)
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
        {
            return TimeSpan.FromSeconds(minSeconds + random.NextDouble() * (maxSeconds - minSeconds));
        }

        /// <summary>
        /// Chooses the next move on the computer's board, or returns null when the board is full.
        /// The computer's board only ever holds right numbers (a wrong one is erased right away),
        /// so the numbers that "fit" a cell always include the right one.
        /// </summary>
        public AiMove? ChooseMove(int[,] board, int[,] solution)
        {
            // Find the empty cell with the fewest numbers that fit
            int bestRow = -1, bestCol = -1;
            List<int> bestOptions = new List<int>();
            for (int r = 0; r < 9; r++)
            {
                for (int c = 0; c < 9; c++)
                {
                    if (board[r, c] != 0)
                        continue;

                    List<int> options = Options(board, r, c);
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

            // Sometimes the computer picks a wrong number, like a person who wasn't careful
            if (random.NextDouble() < mistakeChance)
            {
                int wrong = WrongNumber(bestOptions, right);
                return new AiMove(bestRow, bestCol, wrong, false);
            }

            return new AiMove(bestRow, bestCol, right, true);
        }

        // The numbers 1-9 that are not yet in this cell's row, column or box
        private static List<int> Options(int[,] board, int row, int col)
        {
            var options = new List<int>();
            for (int n = 1; n <= 9; n++)
            {
                if (SudokuGenerator.CanPlace(board, row, col, n))
                    options.Add(n);
            }
            return options;
        }

        // A believable wrong number: one that fits the cell if there is one, otherwise any other number
        private int WrongNumber(List<int> options, int right)
        {
            var wrongOptions = new List<int>();
            foreach (int n in options)
            {
                if (n != right)
                    wrongOptions.Add(n);
            }
            if (wrongOptions.Count > 0)
                return wrongOptions[random.Next(wrongOptions.Count)];

            int other = random.Next(1, 9);   // 1..8
            if (other >= right)
                other++;                      // skip the right number
            return other;
        }
    }
}
