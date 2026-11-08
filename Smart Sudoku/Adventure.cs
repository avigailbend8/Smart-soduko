using System.Collections.Generic;

namespace Smart_Sudoku
{
    /// <summary>Passed to GamePage to open zen mode: no clock, no hearts, calm music.</summary>
    public sealed class ZenStart
    {
    }

    /// <summary>Passed to GamePage to open one stage of the adventure.</summary>
    public sealed record AdventureStart(int World, int Stage);

    public sealed record AdventureWorld(string Name, string Emoji, string Boss, int Difficulty, string ThemeId);

    /// <summary>
    /// The adventure map: 4 worlds with 6 stages each. Stage 6 of every world is a boss.
    /// Finishing a world's boss opens the next world.
    /// </summary>
    public static class Adventure
    {
        public const int StagesPerWorld = 6;
        public const int BossBonus = 300;

        public static readonly IReadOnlyList<AdventureWorld> Worlds = new[]
        {
            new AdventureWorld("יער הקסמים", "🌲", "🦉", 0, "garden"),
            new AdventureWorld("מעמקי הים", "🌊", "🐙", 1, "ocean"),
            new AdventureWorld("הטירה", "🏰", "🐉", 2, "princess"),
            new AdventureWorld("החלל", "🚀", "👾", 3, "space"),
        };

        public static bool IsBoss(int stage) => stage == StagesPerWorld;

        /// <summary>The matching normal level: used for how many cells are empty and for the coins.</summary>
        public static int LevelIdFor(int world, int stage) =>
            Levels.Id(Worlds[world].Difficulty, IsBoss(stage) ? Levels.PerDifficulty : stage * 3);

        /// <summary>Bosses have a couple more empty cells than the hardest level of their world.</summary>
        public static int EmptyCells(int world, int stage) =>
            Levels.EmptyCells(LevelIdFor(world, stage)) + (IsBoss(stage) ? 2 : 0);

        public static int Stars(int world, int stage) =>
            int.TryParse(GameData.GetExtra($"adv.{world}.{stage}", "0"), out int s) ? s : 0;

        public static bool IsOpen(int world, int stage)
        {
            if (stage == 1)
                return world == 0 || Stars(world - 1, StagesPerWorld) > 0;
            return Stars(world, stage - 1) > 0;
        }

        public static void Record(int world, int stage, int stars)
        {
            if (stars > Stars(world, stage))
                GameData.SetExtra($"adv.{world}.{stage}", stars.ToString());
        }
    }
}
