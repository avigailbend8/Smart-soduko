using System;
using System.Linq;

namespace Smart_Sudoku
{
    /// <summary>
    /// Small Sudoku boards: 4×4 (boxes of 2×2) and 6×6 (boxes of 2 rows × 3 columns),
    /// with exactly one solution, like the big ones.
    /// </summary>
    public static class MiniSudoku
    // אובייקט: MiniSudoku — בונה לוחות סודוקו קטנים: 4×4 (ריבועים של 2×2) ו-6×6 (ריבועים של 2×3), עם פתרון אחד בלבד.
    {
        private static readonly Random Rnd = new Random();
        // אובייקט: Rnd — מחולל מספרים אקראיים.

        public static int BoxRows(int size) => 2;
        // פעולה: BoxRows — כמה שורות יש בכל ריבוע קטן (תמיד 2).

        public static int BoxCols(int size) => size == 4 ? 2 : 3;
        // פעולה: BoxCols — כמה עמודות יש בכל ריבוע קטן: 2 בלוח 4×4, ו-3 בלוח 6×6.

        public static (int[,] Puzzle, int[,] Solution) Create(int size)
        // פעולה: Create — יוצרת לוח קטן: ממלאת לוח שלם, ומוחקת משבצות כל עוד נשאר פתרון אחד בלבד.
        {
            var solution = new int[size, size];
            // אובייקט: solution — הלוח המלא (הפתרון).
            Fill(solution, size, 0);

            var puzzle = (int[,])solution.Clone();
            // אובייקט: puzzle — החידה: עותק של הפתרון שממנו מוחקים מספרים.
            int target = size == 4 ? 9 : 20;
            // אובייקט: target — כמה משבצות לנסות למחוק (9 בלוח קטן, 20 בבינוני).
            int removed = 0;
            // אובייקט: removed — כמה משבצות כבר נמחקו.
            foreach (int i in Enumerable.Range(0, size * size).OrderBy(_ => Rnd.Next()))
            {
                if (removed >= target)
                    break;
                int r = i / size, c = i % size, keep = puzzle[r, c];
                // אובייקט: r, c — השורה והעמודה של המשבצת. keep — המספר שהיה בה, כדי להחזיר אותו אם צריך.
                puzzle[r, c] = 0;
                if (CountSolutions((int[,])puzzle.Clone(), size, 2) == 1)
                    removed++;
                else
                    puzzle[r, c] = keep;
            }
            return (puzzle, solution);
        }

        public static bool CanPlace(int[,] g, int size, int r, int c, int n)
        // פעולה: CanPlace — בודקת אם מותר לשים מספר במשבצת: לא באותה שורה, לא באותה עמודה ולא באותו ריבוע.
        {
            for (int i = 0; i < size; i++)
            {
                if (g[r, i] == n || g[i, c] == n)
                    return false;
            }
            int br = r / BoxRows(size) * BoxRows(size), bc = c / BoxCols(size) * BoxCols(size);
            // אובייקט: br, bc — הפינה של הריבוע הקטן שבו המשבצת נמצאת.
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
        // פעולה: Fill — ממלאת את הלוח במספרים אקראיים, וחוזרת אחורה כשנתקעים.
        {
            if (pos == size * size)
                return true;
            int r = pos / size, c = pos % size;
            // אובייקט: r, c — השורה והעמודה של המשבצת שממלאים עכשיו.
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
        // פעולה: CountSolutions — סופרת כמה פתרונות יש ללוח (עד limit), כדי לוודא שיש רק אחד.
        {
            for (int pos = 0; pos < size * size; pos++)
            {
                int r = pos / size, c = pos % size;
                // אובייקט: r, c — השורה והעמודה של המשבצת שבודקים.
                if (g[r, c] != 0)
                    continue;

                int found = 0;
                // אובייקט: found — כמה פתרונות נמצאו עד עכשיו.
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
