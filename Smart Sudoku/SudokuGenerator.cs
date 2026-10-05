using System;
using System.Linq;

namespace Smart_Sudoku
{
    /// <summary>
    /// Builds random Sudoku boards that have exactly one solution.
    /// Higher levels leave more empty cells.
    /// </summary>
    public static class SudokuGenerator
    {
        private static readonly Random Rnd = new Random();

        public static (int[,] Puzzle, int[,] Solution) Create(int level)
        {
            var solution = new int[9, 9];
            Fill(solution, 0);

            var puzzle = (int[,])solution.Clone();
            int target = Math.Min(30 + (level - 1) * 3, 58);
            int removed = 0;

            foreach (int i in Enumerable.Range(0, 81).OrderBy(_ => Rnd.Next()))
            {
                if (removed >= target)
                    break;

                int r = i / 9, c = i % 9;
                int keep = puzzle[r, c];
                puzzle[r, c] = 0;

                if (CountSolutions((int[,])puzzle.Clone(), 2) == 1)
                    removed++;
                else
                    puzzle[r, c] = keep;
            }

            return (puzzle, solution);
        }

        public static bool CanPlace(int[,] g, int r, int c, int n)
        {
            for (int i = 0; i < 9; i++)
            {
                if (g[r, i] == n || g[i, c] == n)
                    return false;
            }

            int br = r / 3 * 3, bc = c / 3 * 3;
            for (int i = br; i < br + 3; i++)
            {
                for (int j = bc; j < bc + 3; j++)
                {
                    if (g[i, j] == n)
                        return false;
                }
            }

            return true;
        }

        private static bool Fill(int[,] g, int pos)
        {
            if (pos == 81)
                return true;

            int r = pos / 9, c = pos % 9;
            foreach (int n in Enumerable.Range(1, 9).OrderBy(_ => Rnd.Next()))
            {
                if (CanPlace(g, r, c, n))
                {
                    g[r, c] = n;
                    if (Fill(g, pos + 1))
                        return true;
                    g[r, c] = 0;
                }
            }

            return false;
        }

        // Counts solutions up to `limit`, always branching on the empty cell with the fewest options.
        private static int CountSolutions(int[,] g, int limit)
        {
            int bestR = -1, bestC = -1, bestCount = 10;
            for (int r = 0; r < 9; r++)
            {
                for (int c = 0; c < 9; c++)
                {
                    if (g[r, c] != 0)
                        continue;

                    int count = 0;
                    for (int n = 1; n <= 9; n++)
                    {
                        if (CanPlace(g, r, c, n))
                            count++;
                    }

                    if (count < bestCount)
                    {
                        bestR = r;
                        bestC = c;
                        bestCount = count;
                    }
                }
            }

            if (bestR == -1)
                return 1;
            if (bestCount == 0)
                return 0;

            int found = 0;
            for (int n = 1; n <= 9 && found < limit; n++)
            {
                if (CanPlace(g, bestR, bestC, n))
                {
                    g[bestR, bestC] = n;
                    found += CountSolutions(g, limit - found);
                    g[bestR, bestC] = 0;
                }
            }

            return found;
        }
    }
}
