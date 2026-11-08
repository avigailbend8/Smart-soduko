using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Smart_Sudoku
{
    public sealed record League(string Name, string Emoji, int MinPoints);

    public sealed record Mission(string Id, string Text, int Target);

    /// <summary>
    /// The player's long-term progress: experience points (XP) and player level, weekly leagues,
    /// daily missions, and statistics. Games report what happened here ("a level was solved"),
    /// and everything else updates from that.
    /// </summary>
    public static class Progression
    {
        // ---------- Player level (XP) ----------

        public static int Xp => Read("xp");

        /// <summary>Level 1 at 0 XP; each level needs a bit more than the one before.</summary>
        public static int LevelFor(int xp) => 1 + (int)Math.Sqrt(xp / 40.0);

        public static int XpAtLevel(int level) => (level - 1) * (level - 1) * 40;

        public static int PlayerLevel => LevelFor(Xp);

        /// <summary>Adds XP (also to this week's league points). Reaching a new level gives coins.</summary>
        public static void AddXp(int amount)
        {
            int before = PlayerLevel;
            Write("xp", Xp + amount);
            AddLeaguePoints(amount);

            int after = PlayerLevel;
            if (after > before)
            {
                int reward = after * 50;
                GameData.AddCoins(reward);
                App.ShowToast($"⭐ עלית לרמה {after}!  +{reward} 🪙");
                if (after >= 10)
                    Achievements.Unlock("level10");
            }
        }

        // ---------- Weekly leagues ----------

        public static readonly IReadOnlyList<League> Leagues = new[]
        {
            new League("ארד", "🥉", 0),
            new League("כסף", "🥈", 150),
            new League("זהב", "🥇", 400),
            new League("יהלום", "💎", 800),
        };

        private static string ThisWeek
        {
            get
            {
                var today = DateTime.Now;
                return $"{ISOWeek.GetYear(today)}-W{ISOWeek.GetWeekOfYear(today):00}";
            }
        }

        /// <summary>Points earned this week (they start again from 0 every week).</summary>
        public static int WeekPoints => GameData.GetExtra("league.week") == ThisWeek ? Read("league.points") : 0;

        public static League LeagueFor(int points) => Leagues.Last(l => points >= l.MinPoints);

        public static League CurrentLeague => LeagueFor(WeekPoints);

        /// <summary>The next league up, or null at the top.</summary>
        public static League? NextLeague => Leagues.FirstOrDefault(l => l.MinPoints > WeekPoints);

        /// <summary>The league reached last week, or null if there was no play last week.</summary>
        public static League? LastWeekLeague
        {
            get
            {
                string last = GameData.GetExtra("league.lastWeek");
                return last.Length == 0 ? null : Leagues.FirstOrDefault(l => l.Name == last);
            }
        }

        private static void AddLeaguePoints(int amount)
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
            Write("league.points", Read("league.points") + amount);
            if (CurrentLeague != before)
                App.ShowToast($"{CurrentLeague.Emoji} עלית לליגת {CurrentLeague.Name}!");
        }

        // ---------- Daily missions ----------

        private static readonly Mission[] AllMissions =
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
        public const int ChestCoins = 150;
        public const int ChestHints = 3;

        private static string Today => DateTime.Now.ToString("yyyy-MM-dd");

        /// <summary>Today's 3 missions: the same all day, different every day.</summary>
        public static IReadOnlyList<Mission> TodaysMissions
        {
            get
            {
                var today = DateTime.Now.Date;
                var random = new Random(today.Year * 10000 + today.Month * 100 + today.Day);
                return AllMissions.OrderBy(_ => random.Next()).Take(3).ToList();
            }
        }

        public static int MissionProgress(string id) => Read($"m.{Today}.{id}");

        public static bool MissionDone(Mission mission) => MissionProgress(mission.Id) >= mission.Target;

        public static bool MissionClaimed(string id) => GameData.GetExtra($"m.{Today}.{id}.claimed") == "1";

        public static bool ChestClaimed => GameData.GetExtra($"m.{Today}.chest") == "1";

        public static bool AllMissionsDone => TodaysMissions.All(MissionDone);

        /// <summary>Counts toward a mission, if it's one of today's.</summary>
        private static void Advance(string id, int amount = 1)
        {
            var mission = TodaysMissions.FirstOrDefault(m => m.Id == id);
            if (mission == null || MissionDone(mission))
                return;

            Write($"m.{Today}.{id}", MissionProgress(id) + amount);
            if (MissionDone(mission))
                App.ShowToast($"✅ משימה הושלמה: {mission.Text}");
        }

        public static bool ClaimMission(Mission mission)
        {
            if (!MissionDone(mission) || MissionClaimed(mission.Id))
                return false;
            GameData.SetExtra($"m.{Today}.{mission.Id}.claimed", "1");
            GameData.AddCoins(MissionReward);
            return true;
        }

        /// <summary>The prize chest for finishing all 3 missions.</summary>
        public static bool ClaimChest()
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

        public static int SolvesOn(DateTime day) => Read("stats.day." + day.ToString("yyyy-MM-dd"));

        private static void CountSolve(int seconds, int mistakes)
        {
            Write("stats.games", Stat("games") + 1);
            Write("stats.seconds", Stat("seconds") + seconds);
            Write("stats.mistakes", Stat("mistakes") + mistakes);
            Write("stats.day." + Today, SolvesOn(DateTime.Now) + 1);
            int fastest = Stat("fastest");
            if (seconds > 0 && (fastest == 0 || seconds < fastest))
                Write("stats.fastest", seconds);
        }

        // ---------- What happened in a game ----------

        public static void LevelSolved(int levelId, int stars, int seconds, int mistakes, int hints, int coins)
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
        {
            CountSolve(seconds, mistakes);
            AddXp(50);
            Advance("daily");
            Advance("solve");
        }

        public static void AdventureSolved(int seconds, int mistakes, bool boss)
        {
            CountSolve(seconds, mistakes);
            AddXp(boss ? 120 : 40);
            Advance("adventure");
            Advance("solve");
        }

        public static void ZenSolved(int seconds)
        {
            CountSolve(seconds, 0);
            Write("stats.zen", Stat("zen") + 1);
            AddXp(15);
            Advance("zen");
        }

        public static void DuelPlayed(bool won)
        {
            Write("stats.duels", Stat("duels") + 1);
            if (won)
                Write("stats.duelWins", Stat("duelWins") + 1);
            AddXp(won ? 60 : 30);
            Advance("duel");
        }

        public static void AiDuelPlayed(bool won)
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
        {
            Write("stats.minis", Stat("minis") + 1);
            AddXp(10);
            Advance("mini");
        }

        public static void NoteWritten() => Advance("notes");

        // ---------- Saved numbers ----------

        private static int Read(string key) => int.TryParse(GameData.GetExtra(key, "0"), out int value) ? value : 0;

        private static void Write(string key, int value) => GameData.SetExtra(key, value.ToString(CultureInfo.InvariantCulture));
    }
}
