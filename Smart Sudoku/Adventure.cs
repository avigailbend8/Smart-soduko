using System.Collections.Generic;

namespace Smart_Sudoku
{
    /// <summary>Passed to GamePage to open zen mode: no clock, no hearts, calm music.</summary>
    public sealed class ZenStart
    // אובייקט: ZenStart — "פתק" שנשלח למסך המשחק כדי לפתוח מצב זן: בלי שעון, בלי לבבות ועם מוזיקה רגועה.
    {
    }

    /// <summary>Passed to GamePage to open one stage of the adventure.</summary>
    public sealed record AdventureStart(int World, int Stage);
    // אובייקט: AdventureStart — "פתק" שנשלח למסך המשחק כדי לפתוח שלב במסע: באיזה עולם ובאיזה שלב.

    public sealed record AdventureWorld(string Name, string Emoji, string Boss, int Difficulty, string ThemeId);
    // אובייקט: AdventureWorld — עולם במסע: שם, אימוג'י, הבוס של העולם, רמת קושי ועיצוב.

    /// <summary>
    /// The adventure map: 4 worlds with 6 stages each. Stage 6 of every world is a boss.
    /// Finishing a world's boss opens the next world.
    /// </summary>
    public static class Adventure
    // אובייקט: Adventure — מפת המסע: 4 עולמות, ובכל אחד 6 שלבים. השלב השישי הוא תמיד בוס, וניצחון עליו פותח את העולם הבא.
    {
        public const int StagesPerWorld = 6;
        // אובייקט: StagesPerWorld — כמה שלבים יש בכל עולם (6).
        public const int BossBonus = 300;
        // אובייקט: BossBonus — כמה מטבעות בונוס מקבלים על ניצחון בבוס.

        public static readonly IReadOnlyList<AdventureWorld> Worlds = new[]
        // אובייקט: Worlds — ארבעת העולמות: יער הקסמים, מעמקי הים, הטירה והחלל.
        {
            new AdventureWorld("יער הקסמים", "🌲", "🦉", 0, "garden"),
            new AdventureWorld("מעמקי הים", "🌊", "🐙", 1, "ocean"),
            new AdventureWorld("הטירה", "🏰", "🐉", 2, "princess"),
            new AdventureWorld("החלל", "🚀", "👾", 3, "space"),
        };

        public static bool IsBoss(int stage) => stage == StagesPerWorld;
        // פעולה: IsBoss — בודקת אם השלב הוא שלב הבוס (השלב האחרון בעולם).

        /// <summary>The matching normal level: used for how many cells are empty and for the coins.</summary>
        public static int LevelIdFor(int world, int stage) =>
        // פעולה: LevelIdFor — מחזירה את השלב הרגיל שמתאים לשלב במסע, כדי לדעת כמה משבצות להשאיר ריקות וכמה מטבעות לתת.
            Levels.Id(Worlds[world].Difficulty, IsBoss(stage) ? Levels.PerDifficulty : stage * 3);

        /// <summary>Bosses have a couple more empty cells than the hardest level of their world.</summary>
        public static int EmptyCells(int world, int stage) =>
        // פעולה: EmptyCells — מחשבת כמה משבצות ריקות יהיו בשלב במסע. לבוס יש עוד 2 משבצות ריקות.
            Levels.EmptyCells(LevelIdFor(world, stage)) + (IsBoss(stage) ? 2 : 0);

        public static int Stars(int world, int stage) =>
        // פעולה: Stars — מחזירה כמה כוכבים השחקן השיג בשלב מסוים במסע.
            int.TryParse(GameData.GetExtra($"adv.{world}.{stage}", "0"), out int s) ? s : 0;

        public static bool IsOpen(int world, int stage)
        // פעולה: IsOpen — בודקת אם השלב פתוח: השלב הראשון נפתח אחרי הבוס של העולם הקודם, וכל שלב אחר — אחרי השלב שלפניו.
        {
            if (stage == 1)
                return world == 0 || Stars(world - 1, StagesPerWorld) > 0;
            return Stars(world, stage - 1) > 0;
        }

        public static void Record(int world, int stage, int stars)
        // פעולה: Record — שומרת את הכוכבים שהשחקן השיג בשלב, אבל רק אם זו תוצאה טובה יותר מקודם.
        {
            if (stars > Stars(world, stage))
                GameData.SetExtra($"adv.{world}.{stage}", stars.ToString());
        }
    }
}
