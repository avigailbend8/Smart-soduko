using System;
using System.Linq;

namespace Smart_Sudoku
{
    /// <summary>
    /// Builds random Sudoku boards that have exactly one solution.
    /// Higher levels leave more empty cells.
    /// </summary>
    public static class SudokuGenerator
    // אובייקט: SudokuGenerator — "מכונה" שבונה לוחות סודוקו אקראיים שיש להם פתרון אחד בלבד.
    {
        private static readonly Random Rnd = new Random();
        // אובייקט: Rnd — מחולל מספרים אקראיים, כדי שכל לוח יהיה שונה.

        /// <param name="target">How many cells to leave empty (fewer if the puzzle would stop being unique).</param>
        public static (int[,] Puzzle, int[,] Solution) Create(int target) => Create(target, Rnd);
        // פעולה: Create — יוצרת לוח חדש. target = כמה משבצות להשאיר ריקות. מחזירה את החידה ואת הפתרון.

        /// <summary>The same seed always gives the same board (used for the daily challenge).</summary>
        public static (int[,] Puzzle, int[,] Solution) Create(int target, int seed) => Create(target, new Random(seed));
        // פעולה: Create (עם seed) — יוצרת לוח לפי "מספר זרע". אותו מספר תמיד נותן אותו לוח, וכך כולם מקבלים את אותו אתגר יומי.

        private static (int[,] Puzzle, int[,] Solution) Create(int target, Random random)
        // פעולה: Create (הפנימית) — ממלאת לוח שלם ונכון, ואז מוחקת משבצות אחת-אחת באקראי.
        // אחרי כל מחיקה היא בודקת שעדיין יש פתרון אחד בלבד. אם לא — היא מחזירה את המספר.
        {
            var solution = new int[9, 9];
            // אובייקט: solution — הלוח המלא והנכון (הפתרון).
            Fill(solution, 0, random);

            var puzzle = (int[,])solution.Clone();
            // אובייקט: puzzle — עותק של הפתרון שממנו מוחקים מספרים, וכך נוצרת החידה.
            int removed = 0;
            // אובייקט: removed — כמה משבצות כבר נמחקו.

            foreach (int i in Enumerable.Range(0, 81).OrderBy(_ => random.Next()))
            {
                if (removed >= target)
                    break;

                int r = i / 9, c = i % 9;
                int keep = puzzle[r, c];
                // אובייקט: keep — המספר שהיה במשבצת, כדי שאפשר יהיה להחזיר אותו אם המחיקה תקלקל את הלוח.
                puzzle[r, c] = 0;

                if (CountSolutions((int[,])puzzle.Clone(), 2) == 1)
                    removed++;
                else
                    puzzle[r, c] = keep;
            }

            return (puzzle, solution);
        }

        public static bool CanPlace(int[,] g, int r, int c, int n)
        // פעולה: CanPlace — בודקת אם מותר לשים את המספר n במשבצת (שורה r, עמודה c):
        // שהוא לא מופיע כבר באותה שורה, באותה עמודה ובאותו ריבוע 3×3.
        {
            for (int i = 0; i < 9; i++)
            {
                if (g[r, i] == n || g[i, c] == n)
                    return false;
            }

            int br = r / 3 * 3, bc = c / 3 * 3;
            // אובייקט: br, bc — השורה והעמודה של הפינה השמאלית העליונה של הריבוע 3×3 שבו נמצאת המשבצת.
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

        private static bool Fill(int[,] g, int pos, Random random)
        // פעולה: Fill — ממלאת לוח ריק במספרים, משבצת אחרי משבצת, בסדר אקראי.
        // אם נתקעים — היא חוזרת אחורה ומנסה מספר אחר (זה נקרא "רקורסיה עם חזרה לאחור").
        {
            if (pos == 81)
                return true;

            int r = pos / 9, c = pos % 9;
            // אובייקט: r, c — השורה והעמודה של המשבצת שממלאים עכשיו (pos הוא המיקום מ-0 עד 80).
            foreach (int n in Enumerable.Range(1, 9).OrderBy(_ => random.Next()))
            {
                if (CanPlace(g, r, c, n))
                {
                    g[r, c] = n;
                    if (Fill(g, pos + 1, random))
                        return true;
                    g[r, c] = 0;
                }
            }

            return false;
        }

        // Counts solutions up to `limit`, always branching on the empty cell with the fewest options.
        private static int CountSolutions(int[,] g, int limit)
        // פעולה: CountSolutions — סופרת כמה פתרונות יש ללוח (אבל עוצרת ב-limit, כי מספיק לדעת אם יש יותר מאחד).
        // כדי שיהיה מהיר, היא תמיד מתחילה מהמשבצת הריקה שיש לה הכי מעט אפשרויות.
        {
            int bestR = -1, bestC = -1, bestCount = 10;
            // אובייקט: bestR, bestC, bestCount — המשבצת הריקה עם הכי מעט מספרים אפשריים, וכמה אפשרויות יש לה.
            for (int r = 0; r < 9; r++)
            {
                for (int c = 0; c < 9; c++)
                {
                    if (g[r, c] != 0)
                        continue;

                    int count = 0;
                    // אובייקט: count — כמה מספרים מותר לשים במשבצת הזאת.
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
            // אובייקט: found — כמה פתרונות נמצאו עד עכשיו.
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
