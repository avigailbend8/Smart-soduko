using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Smart_Sudoku
{
    public sealed record League(string Name, string Emoji, int MinPoints);
    // אובייקט: League — ליגה שבועית: שם, אימוג'י, וכמה נקודות צריך כדי להגיע אליה.

    public sealed record Mission(string Id, string Text, int Target);
    // אובייקט: Mission — משימה יומית: מזהה, מה צריך לעשות, וכמה פעמים.

    /// <summary>
    /// The player's long-term progress: experience points (XP) and player level, weekly leagues,
    /// daily missions, and statistics. Games report what happened here ("a level was solved"),
    /// and everything else updates from that.
    /// </summary>
    public static class Progression
    // אובייקט: Progression — ההתקדמות הארוכה של השחקן: נקודות ניסיון (XP) ורמת שחקן, ליגות שבועיות, משימות יומיות וסטטיסטיקות.
    // המשחקים מדווחים לכאן מה קרה ("נפתר שלב"), ומזה הכול מתעדכן.
    {
        // ---------- Player level (XP) ----------

        public static int Xp => Read("xp");
        // אובייקט: Xp — כמה נקודות ניסיון יש לשחקן.

        /// <summary>Level 1 at 0 XP; each level needs a bit more than the one before.</summary>
        public static int LevelFor(int xp) => 1 + (int)Math.Sqrt(xp / 40.0);
        // פעולה: LevelFor — מחשבת את רמת השחקן לפי נקודות הניסיון. כל רמה צריכה קצת יותר נקודות מהקודמת.

        public static int XpAtLevel(int level) => (level - 1) * (level - 1) * 40;
        // פעולה: XpAtLevel — מחזירה כמה נקודות ניסיון צריך כדי להגיע לרמה מסוימת.

        public static int PlayerLevel => LevelFor(Xp);
        // אובייקט: PlayerLevel — רמת השחקן עכשיו.

        /// <summary>Adds XP (also to this week's league points). Reaching a new level gives coins.</summary>
        public static void AddXp(int amount)
        // פעולה: AddXp — מוסיפה נקודות ניסיון (וגם לנקודות של הליגה השבועית). עלייה ברמה נותנת מטבעות והודעה.
        {
            int before = PlayerLevel;
            // אובייקט: before — הרמה לפני שהוספנו נקודות.
            Write("xp", Xp + amount);
            AddLeaguePoints(amount);

            int after = PlayerLevel;
            // אובייקט: after — הרמה אחרי שהוספנו נקודות.
            if (after > before)
            {
                int reward = after * 50;
                // אובייקט: reward — כמה מטבעות מקבלים על עלייה ברמה (50 כפול הרמה החדשה).
                GameData.AddCoins(reward);
                App.ShowToast($"⭐ עלית לרמה {after}!  +{reward} 🪙");
                if (after >= 10)
                    Achievements.Unlock("level10");
            }
        }

        // ---------- Weekly leagues ----------

        public static readonly IReadOnlyList<League> Leagues = new[]
        // אובייקט: Leagues — ארבע הליגות: ארד, כסף, זהב ויהלום, וכמה נקודות צריך לכל אחת.
        {
            new League("ארד", "🥉", 0),
            new League("כסף", "🥈", 150),
            new League("זהב", "🥇", 400),
            new League("יהלום", "💎", 800),
        };

        private static string ThisWeek
        // אובייקט: ThisWeek — השבוע הנוכחי כטקסט (למשל "2026-W46"), כדי לדעת מתי מתחיל שבוע חדש.
        {
            get
            {
                var today = DateTime.Now;
                // אובייקט: today — התאריך של היום.
                return $"{ISOWeek.GetYear(today)}-W{ISOWeek.GetWeekOfYear(today):00}";
            }
        }

        /// <summary>Points earned this week (they start again from 0 every week).</summary>
        public static int WeekPoints => GameData.GetExtra("league.week") == ThisWeek ? Read("league.points") : 0;
        // אובייקט: WeekPoints — כמה נקודות השחקן צבר השבוע (מתאפס כל שבוע).

        public static League LeagueFor(int points) => Leagues.Last(l => points >= l.MinPoints);
        // פעולה: LeagueFor — מחזירה את הליגה שמתאימה למספר נקודות.

        public static League CurrentLeague => LeagueFor(WeekPoints);
        // אובייקט: CurrentLeague — הליגה של השחקן השבוע.

        /// <summary>The next league up, or null at the top.</summary>
        public static League? NextLeague => Leagues.FirstOrDefault(l => l.MinPoints > WeekPoints);
        // אובייקט: NextLeague — הליגה הבאה שאפשר להגיע אליה (או null אם כבר בליגה הכי גבוהה).

        /// <summary>The league reached last week, or null if there was no play last week.</summary>
        public static League? LastWeekLeague
        // אובייקט: LastWeekLeague — הליגה שהשחקן הגיע אליה בשבוע שעבר (או null אם לא שיחק).
        {
            get
            {
                string last = GameData.GetExtra("league.lastWeek");
                // אובייקט: last — שם הליגה של השבוע שעבר.
                return last.Length == 0 ? null : Leagues.FirstOrDefault(l => l.Name == last);
            }
        }

        private static void AddLeaguePoints(int amount)
        // פעולה: AddLeaguePoints — מוסיפה נקודות לליגה. בשבוע חדש היא זוכרת את הליגה של השבוע שעבר ומתחילה מאפס.
        {
            if (GameData.GetExtra("league.week") != ThisWeek)
            {
                // A new week: remember last week's league, then start from 0
                if (GameData.GetExtra("league.week").Length > 0)
                    GameData.SetExtra("league.lastWeek", LeagueFor(Read("league.points")).Name);
                GameData.SetExtra("league.week", ThisWeek);
                Write("league.points", 0);
            }

            var before = CurrentLeague;
            // אובייקט: before — הליגה לפני שהוספנו נקודות, כדי לדעת אם עלינו ליגה.
            Write("league.points", Read("league.points") + amount);
            if (CurrentLeague != before)
                App.ShowToast($"{CurrentLeague.Emoji} עלית לליגת {CurrentLeague.Name}!");
        }

        // ---------- Daily missions ----------

        private static readonly Mission[] AllMissions =
        // אובייקט: AllMissions — כל המשימות האפשריות. כל יום נבחרות מתוכן 3.
        {
            new Mission("solve", "לפתור 3 שלבים", 3),
            new Mission("perfect", "לפתור שלב בלי טעויות ובלי רמזים", 1),
            new Mission("notes", "לכתוב 5 פתקים", 5),
            new Mission("mini", "לפתור סודוקו קטן", 1),
            new Mission("duel", "לשחק דו-קרב ברשת", 1),
            new Mission("daily", "לפתור את האתגר היומי", 1),
            new Mission("coins", "להרוויח 200 מטבעות בשלבים", 200),
            new Mission("fast", "לפתור שלב בפחות מ-4 דקות", 1),
            new Mission("zen", "לפתור לוח במצב זן", 1),
            new Mission("adventure", "לעבור שלב במסע ההרפתקאות", 1),
            new Mission("ai", "לנצח את המחשב בדו-קרב", 1),
        };

        public const int MissionReward = 30;
        // אובייקט: MissionReward — כמה מטבעות מקבלים על כל משימה שהושלמה (30).
        public const int ChestCoins = 150;
        // אובייקט: ChestCoins — כמה מטבעות יש בתיבת הפרס על שלוש המשימות (150).
        public const int ChestHints = 3;
        // אובייקט: ChestHints — כמה רמזים יש בתיבת הפרס (3).

        private static string Today => DateTime.Now.ToString("yyyy-MM-dd");
        // אובייקט: Today — התאריך של היום, כטקסט.

        /// <summary>Today's 3 missions: the same all day, different every day.</summary>
        public static IReadOnlyList<Mission> TodaysMissions
        // אובייקט: TodaysMissions — שלוש המשימות של היום: אותן משימות כל היום, ואחרות בכל יום.
        {
            get
            {
                var today = DateTime.Now.Date;
                // אובייקט: today — התאריך של היום.
                var random = new Random(today.Year * 10000 + today.Month * 100 + today.Day);
                // אובייקט: random — מחולל אקראי שה"זרע" שלו הוא התאריך, ולכן כל היום יוצאות אותן משימות.
                return AllMissions.OrderBy(_ => random.Next()).Take(3).ToList();
            }
        }

        public static int MissionProgress(string id) => Read($"m.{Today}.{id}");
        // פעולה: MissionProgress — כמה השחקן כבר התקדם במשימה היום.

        public static bool MissionDone(Mission mission) => MissionProgress(mission.Id) >= mission.Target;
        // פעולה: MissionDone — בודקת אם המשימה הושלמה.

        public static bool MissionClaimed(string id) => GameData.GetExtra($"m.{Today}.{id}.claimed") == "1";
        // פעולה: MissionClaimed — בודקת אם השחקן כבר לקח את הפרס על המשימה.

        public static bool ChestClaimed => GameData.GetExtra($"m.{Today}.chest") == "1";
        // אובייקט: ChestClaimed — אמת אם השחקן כבר פתח היום את תיבת הפרס.

        public static bool AllMissionsDone => TodaysMissions.All(MissionDone);
        // אובייקט: AllMissionsDone — אמת אם כל שלוש המשימות של היום הושלמו.

        /// <summary>Counts toward a mission, if it's one of today's.</summary>
        private static void Advance(string id, int amount = 1)
        // פעולה: Advance — מקדמת משימה (אם היא אחת מהמשימות של היום). כשהמשימה מושלמת — מציגה הודעה.
        {
            var mission = TodaysMissions.FirstOrDefault(m => m.Id == id);
            // אובייקט: mission — המשימה של היום עם המזהה הזה (או null אם היא לא של היום).
            if (mission == null || MissionDone(mission))
                return;

            Write($"m.{Today}.{id}", MissionProgress(id) + amount);
            if (MissionDone(mission))
                App.ShowToast($"✅ משימה הושלמה: {mission.Text}");
        }

        public static bool ClaimMission(Mission mission)
        // פעולה: ClaimMission — נותנת את הפרס על משימה שהושלמה (רק פעם אחת).
        {
            if (!MissionDone(mission) || MissionClaimed(mission.Id))
                return false;
            GameData.SetExtra($"m.{Today}.{mission.Id}.claimed", "1");
            GameData.AddCoins(MissionReward);
            return true;
        }

        /// <summary>The prize chest for finishing all 3 missions.</summary>
        public static bool ClaimChest()
        // פעולה: ClaimChest — פותחת את תיבת הפרס על שלוש המשימות: מטבעות, רמזים ונקודות ניסיון.
        {
            if (!AllMissionsDone || ChestClaimed)
                return false;
            GameData.SetExtra($"m.{Today}.chest", "1");
            GameData.AddCoins(ChestCoins);
            GameData.BuyHints(0, ChestHints);
            AddXp(50);
            return true;
        }

        // ---------- Statistics ----------

        public static int Stat(string name) => Read("stats." + name);
        // פעולה: Stat — מחזירה סטטיסטיקה לפי שם (למשל כמה משחקים שוחקו).

        public static int SolvesOn(DateTime day) => Read("stats.day." + day.ToString("yyyy-MM-dd"));
        // פעולה: SolvesOn — כמה לוחות השחקן פתר ביום מסוים.

        private static void CountSolve(int seconds, int mistakes)
        // פעולה: CountSolve — סופרת לוח שנפתר: מוסיפה למספר המשחקים, לזמן הכולל, לטעויות, ובודקת אם זה שיא מהירות.
        {
            Write("stats.games", Stat("games") + 1);
            Write("stats.seconds", Stat("seconds") + seconds);
            Write("stats.mistakes", Stat("mistakes") + mistakes);
            Write("stats.day." + Today, SolvesOn(DateTime.Now) + 1);
            int fastest = Stat("fastest");
            // אובייקט: fastest — הזמן הכי מהיר עד עכשיו.
            if (seconds > 0 && (fastest == 0 || seconds < fastest))
                Write("stats.fastest", seconds);
        }

        // ---------- What happened in a game ----------

        public static void LevelSolved(int levelId, int stars, int seconds, int mistakes, int hints, int coins)
        // פעולה: LevelSolved — מדווחת שנפתר שלב רגיל: סטטיסטיקה, נקודות ניסיון, וקידום משימות.
        {
            CountSolve(seconds, mistakes);
            AddXp(20 + stars * 10 + Levels.DifficultyOf(levelId) * 15);
            Advance("solve");
            Advance("coins", coins);
            if (mistakes == 0 && hints == 0)
                Advance("perfect");
            if (seconds < 240)
                Advance("fast");
        }

        public static void DailySolved(int seconds, int mistakes)
        // פעולה: DailySolved — מדווחת שנפתר האתגר היומי.
        {
            CountSolve(seconds, mistakes);
            AddXp(50);
            Advance("daily");
            Advance("solve");
        }

        public static void AdventureSolved(int seconds, int mistakes, bool boss)
        // פעולה: AdventureSolved — מדווחת שנפתר שלב במסע (בוס נותן יותר נקודות).
        {
            CountSolve(seconds, mistakes);
            AddXp(boss ? 120 : 40);
            Advance("adventure");
            Advance("solve");
        }

        public static void ZenSolved(int seconds)
        // פעולה: ZenSolved — מדווחת שנפתר לוח במצב זן.
        {
            CountSolve(seconds, 0);
            Write("stats.zen", Stat("zen") + 1);
            AddXp(15);
            Advance("zen");
        }

        public static void DuelPlayed(bool won)
        // פעולה: DuelPlayed — מדווחת על דו-קרב ברשת, ואם השחקן ניצח.
        {
            Write("stats.duels", Stat("duels") + 1);
            if (won)
                Write("stats.duelWins", Stat("duelWins") + 1);
            AddXp(won ? 60 : 30);
            Advance("duel");
        }

        public static void AiDuelPlayed(bool won)
        // פעולה: AiDuelPlayed — מדווחת על דו-קרב נגד המחשב, ואם השחקן ניצח.
        {
            Write("stats.aiDuels", Stat("aiDuels") + 1);
            if (won)
            {
                Write("stats.aiWins", Stat("aiWins") + 1);
                Advance("ai");
            }
            AddXp(won ? 50 : 20);
        }

        public static void MiniSolved()
        // פעולה: MiniSolved — מדווחת שנפתר סודוקו קטן.
        {
            Write("stats.minis", Stat("minis") + 1);
            AddXp(10);
            Advance("mini");
        }

        public static void NoteWritten() => Advance("notes");
        // פעולה: NoteWritten — מדווחת שהשחקן כתב פתק (בשביל משימת הפתקים).

        // ---------- Saved numbers ----------

        private static int Read(string key) => int.TryParse(GameData.GetExtra(key, "0"), out int value) ? value : 0;
        // פעולה: Read — קוראת מספר שנשמר בהגדרות (0 אם אין).

        private static void Write(string key, int value) => GameData.SetExtra(key, value.ToString(CultureInfo.InvariantCulture));
        // פעולה: Write — שומרת מספר בהגדרות.
    }
}
