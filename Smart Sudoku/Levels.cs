using System.Collections.Generic;

namespace Smart_Sudoku
{
    /// <param name="FirstEmpty">Empty cells on the first level of this difficulty.</param>
    /// <param name="LastEmpty">Empty cells on the last level of this difficulty.</param>
    public sealed record Difficulty(int Index, string Name, string Emoji, int FirstEmpty, int LastEmpty);
    // אובייקט: Difficulty — רמת קושי אחת: מספר, שם, אימוג'י, וכמה משבצות ריקות יש בשלב הראשון ובשלב האחרון שלה.

    /// <summary>
    /// Levels are grouped into difficulties. A level id is difficulty * 100 + level number,
    /// so the easy levels keep the ids 1-20 they always had.
    /// </summary>
    public static class Levels
    // אובייקט: Levels — כל השלבים של המשחק, מחולקים לרמות קושי. מספר שלב = רמה × 100 + מספר השלב (למשל 203 = קשה, שלב 3).
    {
        public const int PerDifficulty = 20;
        // אובייקט: PerDifficulty — כמה שלבים יש בכל רמת קושי (20).

        public static readonly IReadOnlyList<Difficulty> Difficulties = new[]
        // אובייקט: Difficulties — ארבע רמות הקושי: קל, בינוני, קשה ומומחה, וכמה משבצות ריקות יש בכל אחת.
        {
            new Difficulty(0, "קל", "🌱", 28, 40),
            new Difficulty(1, "בינוני", "🌼", 40, 48),
            new Difficulty(2, "קשה", "🔥", 48, 54),
            new Difficulty(3, "מומחה", "👑", 54, 60),
        };

        public static int Id(int difficulty, int number) => difficulty * 100 + number;
        // פעולה: Id — מחשבת את מספר השלב מתוך רמת הקושי ומספר השלב ברמה.

        public static int DifficultyOf(int id) => id / 100;
        // פעולה: DifficultyOf — מחזירה מאיזו רמת קושי השלב (המאות במספר).

        public static int NumberOf(int id) => id % 100;
        // פעולה: NumberOf — מחזירה את מספר השלב בתוך הרמה (שתי הספרות האחרונות).

        public static Difficulty DifficultyFor(int id) => Difficulties[DifficultyOf(id)];
        // פעולה: DifficultyFor — מחזירה את כל המידע על רמת הקושי של השלב.

        public static int EmptyCells(int id)
        // פעולה: EmptyCells — מחשבת כמה משבצות ריקות יהיו בשלב. ככל שהשלב מתקדם — יותר משבצות ריקות.
        {
            var d = DifficultyFor(id);
            // אובייקט: d — רמת הקושי של השלב.
            return d.FirstEmpty + (d.LastEmpty - d.FirstEmpty) * (NumberOf(id) - 1) / (PerDifficulty - 1);
        }

        /// <summary>The level after this one, or null after the very last level.</summary>
        public static int? Next(int id)
        // פעולה: Next — מחזירה את השלב הבא. אחרי השלב ה-20 עוברים לרמה הבאה, ואחרי השלב האחרון במשחק מחזירה null.
        {
            if (NumberOf(id) < PerDifficulty)
                return id + 1;
            if (DifficultyOf(id) < Difficulties.Count - 1)
                return Id(DifficultyOf(id) + 1, 1);
            return null;
        }
    }
}
