using System.Collections.Generic;

namespace Smart_Sudoku
{
    /// <summary>
    /// Everything saved about one player. Stored in the database for registered users,
    /// and in the app's local settings for the guest.
    /// </summary>
    public sealed class PlayerProgress
    {
        public const int StartingCoins = 100;

        public int Coins { get; set; } = StartingCoins;
        public int BonusHints { get; set; }
        public int BonusLives { get; set; }
        public string Theme { get; set; } = "pink";
        public string Symbols { get; set; } = "numbers";
        public bool SoundOn { get; set; } = true;

        /// <summary>Store items the player owns (themes and symbol sets).</summary>
        public HashSet<string> Owned { get; set; } = new HashSet<string> { "pink", "numbers" };

        /// <summary>Level id -> best stars.</summary>
        public Dictionary<int, int> Stars { get; set; } = new Dictionary<int, int>();

        /// <summary>Level id -> best time in seconds.</summary>
        public Dictionary<int, int> BestTimes { get; set; } = new Dictionary<int, int>();

        /// <summary>Difficulty -> highest unlocked level number.</summary>
        public Dictionary<int, int> Unlocked { get; set; } = new Dictionary<int, int>();

        public bool HasAnyProgress => Stars.Count > 0 || Coins != StartingCoins || Owned.Count > 2;
    }
}
