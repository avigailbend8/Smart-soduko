using System.Collections.Generic;

namespace Smart_Sudoku
{
    /// <param name="FirstEmpty">Empty cells on the first level of this difficulty.</param>
    /// <param name="LastEmpty">Empty cells on the last level of this difficulty.</param>
    public sealed record Difficulty(int Index, string Name, string Emoji, int FirstEmpty, int LastEmpty);

    /// <summary>
    /// Levels are grouped into difficulties. A level id is difficulty * 100 + level number,
    /// so the easy levels keep the ids 1-20 they always had.
    /// </summary>
    public static class Levels
    {
        public const int PerDifficulty = 20;

        public static readonly IReadOnlyList<Difficulty> Difficulties = new[]
        {
            new Difficulty(0, "קל", "🌱", 28, 40),
            new Difficulty(1, "בינוני", "🌼", 40, 48),
            new Difficulty(2, "קשה", "🔥", 48, 54),
            new Difficulty(3, "מומחה", "👑", 54, 60),
        };

        public static int Id(int difficulty, int number) => difficulty * 100 + number;

        public static int DifficultyOf(int id) => id / 100;

        public static int NumberOf(int id) => id % 100;

        public static Difficulty DifficultyFor(int id) => Difficulties[DifficultyOf(id)];

        public static int EmptyCells(int id)
        {
            var d = DifficultyFor(id);
            return d.FirstEmpty + (d.LastEmpty - d.FirstEmpty) * (NumberOf(id) - 1) / (PerDifficulty - 1);
        }

        /// <summary>The level after this one, or null after the very last level.</summary>
        public static int? Next(int id)
        {
            if (NumberOf(id) < PerDifficulty)
                return id + 1;
            if (DifficultyOf(id) < Difficulties.Count - 1)
                return Id(DifficultyOf(id) + 1, 1);
            return null;
        }
    }
}
