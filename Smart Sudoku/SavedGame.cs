using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Smart_Sudoku
{
    /// <summary>A level left in the middle, so it can be continued later. Cells are row by row (81 each).</summary>
    public sealed class SavedGame
    // אובייקט: SavedGame — שלב שעזבו באמצע, שמור כדי שאפשר יהיה להמשיך אותו אחר כך.
    {
        public int LevelId { get; set; }
        // אובייקט: LevelId — מספר השלב.
        public bool Daily { get; set; }
        // אובייקט: Daily — אמת אם זה האתגר היומי.
        public int[] Puzzle { get; set; } = new int[81];
        // אובייקט: Puzzle — החידה המקורית (81 משבצות, 0 = ריקה).
        public int[] Solution { get; set; } = new int[81];
        // אובייקט: Solution — הפתרון המלא.
        public int[] Board { get; set; } = new int[81];
        // אובייקט: Board — מה שהשחקן כבר מילא עד עכשיו.
        public int[] Notes { get; set; } = new int[81];
        // אובייקט: Notes — הפתקים הקטנים (מספרים אפשריים) שהשחקן כתב בכל משבצת.
        public bool[] Hinted { get; set; } = new bool[81];
        // אובייקט: Hinted — אילו משבצות התגלו בעזרת רמז.
        public int Lives { get; set; }
        // אובייקט: Lives — כמה לבבות נשארו.
        public int Mistakes { get; set; }
        // אובייקט: Mistakes — כמה טעויות היו.
        public int HintsUsed { get; set; }
        // אובייקט: HintsUsed — בכמה רמזים השתמשו.
        public int Seconds { get; set; }
        // אובייקט: Seconds — כמה שניות עברו מתחילת השלב.
    }

    /// <summary>JSON for the app's own saved data. Source-generated, so it works in trimmed builds.</summary>
    [JsonSerializable(typeof(Dictionary<string, string>))]
    [JsonSerializable(typeof(SavedGame))]
    [JsonSerializable(typeof(PlayerProgress))]
    public partial class AppJson : JsonSerializerContext
    // אובייקט: AppJson — עוזר שהופך את המידע השמור של המשחק לטקסט JSON ובחזרה, כדי לשמור אותו בקובץ.
    {
    }
}
