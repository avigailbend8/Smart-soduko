using System;

namespace Smart_Sudoku
{
    /// <summary>Passed to GamePage to open today's daily challenge.</summary>
    public sealed class DailyChallengeStart
    // אובייקט: DailyChallengeStart — "פתק" שנשלח למסך המשחק כדי לפתוח את האתגר היומי של היום.
    {
    }

    /// <summary>
    /// One board a day, the same for everyone (made from the date). Solving it on consecutive days builds a streak
    /// that adds bonus coins.
    /// </summary>
    public static class DailyChallenge
    // אובייקט: DailyChallenge — האתגר היומי: לוח אחד ביום, זהה לכולם (נוצר מהתאריך). פתרון כמה ימים ברצף נותן בונוס.
    {
        /// <summary>Daily boards are medium; coins are counted like level 10 of medium.</summary>
        public static int LevelId => Levels.Id(1, 10);
        // אובייקט: LevelId — לאיזה שלב רגיל האתגר היומי דומה (בינוני, שלב 10), כדי לדעת כמה משבצות ריקות וכמה מטבעות.

        private static string DateKey(DateTime day) => day.ToString("yyyy-MM-dd");
        // פעולה: DateKey — הופכת תאריך לטקסט כמו "2026-11-10", כדי לשמור ולהשוות תאריכים.

        public static string Today => DateKey(DateTime.Now.Date);
        // אובייקט: Today — התאריך של היום, כטקסט.

        public static (int[,] Puzzle, int[,] Solution) TodaysBoard()
        // פעולה: TodaysBoard — יוצרת את הלוח של היום. ה"זרע" נבנה מהתאריך, ולכן כל השחקנים מקבלים בדיוק אותו לוח.
        {
            var today = DateTime.Now.Date;
            // אובייקט: today — התאריך של היום.
            int seed = today.Year * 10000 + today.Month * 100 + today.Day;
            // אובייקט: seed — מספר שנבנה מהתאריך (למשל 20261110), ומשמש כ"זרע" ליצירת הלוח.
            return SudokuGenerator.Create(Levels.EmptyCells(LevelId), seed);
        }

        public static bool DoneToday => GameData.GetExtra("daily.last") == Today;
        // אובייקט: DoneToday — אמת אם השחקן כבר פתר את האתגר של היום.

        /// <summary>The current streak (days in a row), or 0 if it was broken.</summary>
        public static int Streak
        // אובייקט: Streak — כמה ימים ברצף השחקן פתר את האתגר. אם פספס יום — הרצף מתאפס.
        {
            get
            {
                string last = GameData.GetExtra("daily.last");
                // אובייקט: last — היום האחרון שבו השחקן פתר את האתגר.
                bool alive = last == Today || last == DateKey(DateTime.Now.Date.AddDays(-1));
                // אובייקט: alive — אמת אם הרצף עדיין חי (פתר היום או אתמול).
                return alive && int.TryParse(GameData.GetExtra("daily.streak", "0"), out int s) ? s : 0;
            }
        }

        public static int StreakBonus(int streak) => Math.Min(streak, 10) * 20;
        // פעולה: StreakBonus — מחשבת את בונוס הרצף: 20 מטבעות לכל יום ברצף, עד 10 ימים.

        /// <summary>Records today's win. Returns the coins earned and the new streak (no coins if already done today).</summary>
        public static (int Coins, int Streak) Complete(int stars, int seconds)
        // פעולה: Complete — רושמת שהשחקן פתר את האתגר היום: מגדילה את הרצף, נותנת מטבעות ופותחת הישגים.
        // אם כבר פתר היום — לא נותנת שוב מטבעות.
        {
            if (DoneToday)
                return (0, Streak);

            int streak = Streak + 1;
            // אובייקט: streak — הרצף החדש, כולל היום.
            GameData.SetExtra("daily.last", Today);
            GameData.SetExtra("daily.streak", streak.ToString());

            int coins = GameData.RewardCoins(LevelId, stars, seconds) + StreakBonus(streak);
            // אובייקט: coins — כמה מטבעות השחקן מקבל: לפי הכוכבים והזמן, ועוד בונוס הרצף.
            GameData.AddCoins(coins);

            Achievements.Unlock("daily");
            if (streak >= 7)
                Achievements.Unlock("streak7");
            return (coins, streak);
        }
    }
}
