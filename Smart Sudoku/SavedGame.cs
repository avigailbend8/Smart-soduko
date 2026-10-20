using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Smart_Sudoku
{
    /// <summary>A level left in the middle, so it can be continued later. Cells are row by row (81 each).</summary>
    public sealed class SavedGame
    {
        public int LevelId { get; set; }
        public bool Daily { get; set; }
        public int[] Puzzle { get; set; } = new int[81];
        public int[] Solution { get; set; } = new int[81];
        public int[] Board { get; set; } = new int[81];
        public int[] Notes { get; set; } = new int[81];
        public bool[] Hinted { get; set; } = new bool[81];
        public int Lives { get; set; }
        public int Mistakes { get; set; }
        public int HintsUsed { get; set; }
        public int Seconds { get; set; }
    }

    /// <summary>JSON for the app's own saved data. Source-generated, so it works in trimmed builds.</summary>
    [JsonSerializable(typeof(Dictionary<string, string>))]
    [JsonSerializable(typeof(SavedGame))]
    public partial class AppJson : JsonSerializerContext
    {
    }
}
