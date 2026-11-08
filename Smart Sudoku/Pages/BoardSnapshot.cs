namespace Smart_Sudoku.Pages
{
    /// <summary>
    /// A copy of the board at one moment of the game. GamePage keeps a stack of these,
    /// so the Undo button can go back one move at a time.
    /// </summary>
    public class BoardSnapshot
    {
        public int[,] Board { get; }
        public int[,] Notes { get; }
        public bool[,] Hinted { get; }

        public BoardSnapshot(int[,] board, int[,] notes, bool[,] hinted)
        {
            Board = board;
            Notes = notes;
            Hinted = hinted;
        }
    }
}
