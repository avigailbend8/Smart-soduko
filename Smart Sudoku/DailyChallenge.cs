using System;

namespace Smart_Sudoku
{
    /// <summary>Passed to GamePage to open today's daily challenge.</summary>
    public sealed class DailyChallengeStart
    {
    }

    /// <summary>
    /// One board a day, the same for everyone (made from the date). Solving it on consecutive days builds a streak
    /// that adds bonus coins.
    /// </summary>
    public static class DailyChallenge
    {
        /// <summary>Daily boards are medium; coins are counted like level 10 of medium.</summary>
        public static int LevelId => Levels.Id(1, 10);

        private static string DateKey(DateTime day) => day.ToString("yyyy-MM-dd");

        public static string Today => DateKey(DateTime.Now.Date);

        public static (int[,] Puzzle, int[,] Solution) TodaysBoard()
        {
            var today = DateTime.Now.Date;
            int seed = today.Year * 10000 + today.Month * 100 + today.Day;
            return SudokuGenerator.Create(Levels.EmptyCells(LevelId), seed);
        }

        public static bool DoneToday => GameData.GetExtra("daily.last") == Today;

        /// <summary>The current streak (days in a row), or 0 if it was broken.</summary>
        public static int Streak
        {
            get
            {
                string last = GameData.GetExtra("daily.last");
                bool alive = last == Today || last == DateKey(DateTime.Now.Date.AddDays(-1));
                return alive && int.TryParse(GameData.GetExtra("daily.streak", "0"), out int s) ? s : 0;
            }
        }

        public static int StreakBonus(int streak) => Math.Min(streak, 10) * 20;

        /// <summary>Records today's win. Returns the coins earned and the new streak (no coins if already done today).</summary>
        public static (int Coins, int Streak) Complete(int stars, int seconds)
        {
            if (DoneToday)
                return (0, Streak);

            int streak = Streak + 1;
            GameData.SetExtra("daily.last", Today);
            GameData.SetExtra("daily.streak", streak.ToString());

            int coins = GameData.RewardCoins(LevelId, stars, seconds) + StreakBonus(streak);
            GameData.AddCoins(coins);

            Achievements.Unlock("daily");
            if (streak >= 7)
                Achievements.Unlock("streak7");
            return (coins, streak);
        }
    }
}
