namespace Smart_Sudoku.Pages
{
    /// <summary>
    /// A copy of the board at one moment of the game. GamePage keeps a stack of these,
    /// so the Undo button can go back one move at a time.
    /// </summary>
    public class BoardSnapshot
    // אובייקט: BoardSnapshot — "צילום" של הלוח ברגע מסוים במשחק. מסך המשחק שומר ערימה של צילומים כאלה, וכך כפתור "בטל" חוזר מהלך אחד אחורה בכל פעם.
    {
        public int[,] Board { get; }
        // אובייקט: Board — המספרים שהיו בלוח באותו רגע.
        public int[,] Notes { get; }
        // אובייקט: Notes — הפתקים שהיו בכל משבצת באותו רגע.
        public bool[,] Hinted { get; }
        // אובייקט: Hinted — אילו משבצות התגלו ברמז עד אותו רגע.

        public BoardSnapshot(int[,] board, int[,] notes, bool[,] hinted)
        // פעולה: BoardSnapshot (בנאי) — יוצרת צילום חדש מהלוח, הפתקים והרמזים.
        {
            Board = board;
            Notes = notes;
            Hinted = hinted;
        }
    }
}
