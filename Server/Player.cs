// Also used by the Smart Sudoku app (linked into it), so the server and the game share one Player class.
namespace Smart_Sudoku.Duel
{
    public static class PlayerStatus
    {
        public const string Waiting = "waiting";
        public const string Playing = "playing";
        public const string Finish = "finish";
    }

    public sealed class Player
    {
        public string Name { get; set; } = "";
        public int Score { get; set; }
        public string Status { get; set; } = PlayerStatus.Waiting;

        /// <summary>True when the player filled the whole board (not just ran out of hearts).</summary>
        public bool Solved { get; set; }
    }
}
