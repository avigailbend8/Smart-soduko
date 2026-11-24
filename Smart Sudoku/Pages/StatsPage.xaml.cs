using System;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace Smart_Sudoku.Pages
{
    /// <summary>The player's numbers: games, time, mistakes, records, a chart of the last 7 days and progress per difficulty.</summary>
    public sealed partial class StatsPage : BasePage
    // אובייקט: StatsPage — עמוד הסטטיסטיקות: כמה לוחות נפתרו, זמן משחק, טעויות, שיאים, גרף של 7 הימים האחרונים והתקדמות בכל רמת קושי.
    {
        private static readonly string[] DayNames = { "א׳", "ב׳", "ג׳", "ד׳", "ה׳", "ו׳", "ש׳" };
        // אובייקט: DayNames — שמות ימי השבוע בקיצור (א׳, ב׳...), בשביל הגרף.

        public StatsPage()
        // פעולה: StatsPage (בנאי) — טוענת את העיצוב של העמוד מקובץ ה-XAML.
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        // פעולה: OnNavigatedTo — רצה כשנכנסים לעמוד: כותבת כותרת משנה, ובונה את כרטיסי המספרים, הגרף השבועי וההתקדמות ברמות.
        {
            base.OnNavigatedTo(e);

            int games = Progression.Stat("games");
            // אובייקט: games — כמה לוחות השחקן פתר בסך הכול.
            SubtitleText.Text = games == 0
                ? "עוד אין נתונים — פותרים לוח ראשון והמספרים יופיעו כאן!"
                : $"רמה {Progression.PlayerLevel} · ליגת {Progression.CurrentLeague.Name} {Progression.CurrentLeague.Emoji}";
            ContentPanel.Children.Clear();

            // Number cards, 3 in a row
            int duels = Progression.Stat("duels"), wins = Progression.Stat("duelWins");
            // אובייקט: duels, wins — בכמה דו-קרבות השחקן שיחק, ובכמה ניצח.
            var cards = new (string Emoji, string Value, string Label)[]
            // אובייקט: cards — רשימת כרטיסי המספרים: לכל כרטיס אימוג'י, מספר ותיאור.
            {
                ("🧩", games.ToString(), "לוחות שנפתרו"),
                ("⏱️", Ui.Time(Progression.Stat("seconds")), "זמן משחק"),
                ("⚡", Progression.Stat("fastest") > 0 ? Ui.Time(Progression.Stat("fastest")) : "—", "הפתרון הכי מהיר"),
                ("❌", games > 0 ? $"{(double)Progression.Stat("mistakes") / games:0.0}" : "—", "טעויות בממוצע ללוח"),
                ("⭐", GameData.SolvedLevelsCount.ToString(), "שלבים שנפתרו"),
                ("🌟", GameData.ThreeStarLevelsCount.ToString(), "שלבים עם 3 כוכבים"),
                ("⚔️", duels > 0 ? $"{wins}/{duels}" : "—", "ניצחונות בדו-קרב"),
                ("🔢", Progression.Stat("minis").ToString(), "סודוקו קטן"),
                ("🧘", Progression.Stat("zen").ToString(), "לוחות זן"),
            };
            var grid = new Grid { ColumnSpacing = 14, RowSpacing = 14 };
            // אובייקט: grid — טבלה של 3 עמודות שבה מסודרים הכרטיסים.
            for (int c = 0; c < 3; c++)
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            for (int i = 0; i < cards.Length; i++)
            {
                if (i % 3 == 0)
                    grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                var stack = new StackPanel { Spacing = 2, HorizontalAlignment = HorizontalAlignment.Center };
                // אובייקט: stack — העמודה שבתוך כרטיס: אימוג'י, מספר ותיאור, זה מתחת לזה.
                stack.Children.Add(Center(Ui.Text(cards[i].Emoji, 28)));
                stack.Children.Add(Center(Ui.Text(cards[i].Value, 24, "RoseBrush", bold: true)));
                stack.Children.Add(Center(Ui.Text(cards[i].Label, 14, "MutedTextBrush")));
                var card = Ui.Card(stack);
                // אובייקט: card — הכרטיס המעוגל עם המספר.
                Grid.SetRow(card, i / 3);
                Grid.SetColumn(card, i % 3);
                grid.Children.Add(card);
            }
            ContentPanel.Children.Add(grid);

            ContentPanel.Children.Add(Ui.Card(WeekChart()));
            ContentPanel.Children.Add(Ui.Card(DifficultyProgress()));
        }

        private static TextBlock Center(TextBlock text)
        // פעולה: Center — ממרכזת שורת טקסט.
        {
            text.HorizontalAlignment = HorizontalAlignment.Center;
            text.TextAlignment = TextAlignment.Center;
            return text;
        }

        /// <summary>A bar for each of the last 7 days: how many boards were solved.</summary>
        private static UIElement WeekChart()
        // פעולה: WeekChart — בונה גרף עמודות: לכל אחד מ-7 הימים האחרונים — כמה לוחות נפתרו בו. היום צבוע בצבע בולט.
        {
            const double ChartHeight = 140;
            // אובייקט: ChartHeight — הגובה של העמודה הכי גבוהה בגרף.
            var days = Enumerable.Range(0, 7).Select(i => DateTime.Now.Date.AddDays(i - 6)).ToList();
            // אובייקט: days — שבעת הימים האחרונים, מלפני שישה ימים ועד היום.
            var counts = days.Select(Progression.SolvesOn).ToList();
            // אובייקט: counts — כמה לוחות נפתרו בכל אחד מהימים.
            int max = Math.Max(1, counts.Max());
            // אובייקט: max — המספר הכי גדול, כדי לקבוע את הגובה של כל עמודה ביחס אליו.

            var bars = new Grid { ColumnSpacing = 12, Height = ChartHeight + 50 };
            // אובייקט: bars — הטבלה שבה עומדות העמודות.
            for (int i = 0; i < days.Count; i++)
            {
                bars.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                bool today = i == days.Count - 1;
                // אובייקט: today — אמת אם העמודה הזאת היא של היום.
                var column = new StackPanel { VerticalAlignment = VerticalAlignment.Bottom, Spacing = 4 };
                // אובייקט: column — עמודה אחת: המספר, העמודה הצבעונית ושם היום.
                column.Children.Add(Center(Ui.Text(counts[i].ToString(), 14, "BerryBrush", bold: true)));
                column.Children.Add(new Border
                {
                    Height = Math.Max(4, ChartHeight * counts[i] / max),
                    CornerRadius = new CornerRadius(8, 8, 3, 3),
                    Background = Ui.Res(today ? "RoseBrush" : "SoftPinkBrush"),
                });
                column.Children.Add(Center(Ui.Text(today ? "היום" : DayNames[(int)days[i].DayOfWeek], 13, "MutedTextBrush", bold: today)));
                Grid.SetColumn(column, i);
                bars.Children.Add(column);
            }

            return new StackPanel
            {
                Spacing = 12,
                Children = { Ui.Text("📈 לוחות שנפתרו בשבוע האחרון", 20, "RoseBrush", bold: true), bars },
            };
        }

        /// <summary>How many levels are solved in each difficulty.</summary>
        private static UIElement DifficultyProgress()
        // פעולה: DifficultyProgress — בונה פס התקדמות לכל רמת קושי: כמה שלבים מתוך 20 כבר נפתרו.
        {
            var panel = new StackPanel { Spacing = 10 };
            // אובייקט: panel — הרשימה שבה מסודרים הפסים.
            panel.Children.Add(Ui.Text("🎚️ התקדמות בכל רמת קושי", 20, "RoseBrush", bold: true));
            foreach (var difficulty in Levels.Difficulties)
            {
                int solved = Enumerable.Range(1, Levels.PerDifficulty).Count(n => GameData.GetStars(Levels.Id(difficulty.Index, n)) > 0);
                // אובייקט: solved — כמה שלבים נפתרו ברמת הקושי הזאת.
                panel.Children.Add(Ui.Row(
                    Ui.Bar((double)solved / Levels.PerDifficulty),
                    Ui.Text($"{solved}/{Levels.PerDifficulty}", 15, "MutedTextBrush"),
                    new TextBlock { Text = $"{difficulty.Emoji} {difficulty.Name}", FontSize = 16, Foreground = Ui.Res("BerryBrush"), Width = 110, VerticalAlignment = VerticalAlignment.Center }));
            }
            return panel;
        }
    }
}
