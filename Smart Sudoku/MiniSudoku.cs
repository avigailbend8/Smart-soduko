using System;
using System.Linq;

namespace Smart_Sudoku
{
    /// <summary>
    /// Small Sudoku boards: 4×4 (boxes of 2×2) and 6×6 (boxes of 2 rows × 3 columns),
    /// with exactly one solution, like the big ones.
    /// </summary>
    public static class MiniSudoku
    {
        private static readonly Random Rnd = new Random();

        public static int BoxRows(int size) => 2;

        public static int BoxCols(int size) => size == 4 ? 2 : 3;

        public static (int[,] Puzzle, int[,] Solution) Create(int size)
        {
            var solution = new int[size, size];
            Fill(solution, size, 0);

            var puzzle = (int[,])solution.Clone();
            int target = size == 4 ? 9 : 20;
            int removed = 0;
            foreach (int i in Enumerable.Range(0, size * size).OrderBy(_ => Rnd.Next()))
            {
                if (removed >= target)
                    break;
                int r = i / size, c = i % size, keep = puzzle[r, c];
                puzzle[r, c] = 0;
                if (CountSolutions((int[,])puzzle.Clone(), size, 2) == 1)
                    removed++;
                else
                    puzzle[r, c] = keep;
            }
            return (puzzle, solution);
        }

        public static bool CanPlace(int[,] g, int size, int r, int c, int n)
        {
            for (int i = 0; i < size; i++)
            {
                if (g[r, i] == n || g[i, c] == n)
                    return false;
            }
            int br = r / BoxRows(size) * BoxRows(size), bc = c / BoxCols(size) * BoxCols(size);
            for (int i = br; i < br + BoxRows(size); i++)
            {
                for (int j = bc; j < bc + BoxCols(size); j++)
                {
                    if (g[i, j] == n)
                        return false;
                }
            }
            return true;
        }

        private static bool Fill(int[,] g, int size, int pos)
        {
            if (pos == size * size)
                return true;
            int r = pos / size, c = pos % size;
            foreach (int n in Enumerable.Range(1, size).OrderBy(_ => Rnd.Next()))
            {
                if (CanPlace(g, size, r, c, n))
                {
                    g[r, c] = n;
                    if (Fill(g, size, pos + 1))
                        return true;
                    g[r, c] = 0;
                }
            }
            return false;
        }

        private static int CountSolutions(int[,] g, int size, int limit)
        {
            for (int pos = 0; pos < size * size; pos++)
            {
                int r = pos / size, c = pos % size;
                if (g[r, c] != 0)
                    continue;

                int found = 0;
                for (int n = 1; n <= size && found < limit; n++)
                {
                    if (CanPlace(g, size, r, c, n))
                    {
                        g[r, c] = n;
                        found += CountSolutions(g, size, limit - found);
                        g[r, c] = 0;
                    }
                }
                return found;
            }
            return 1;
        }
    }
}
