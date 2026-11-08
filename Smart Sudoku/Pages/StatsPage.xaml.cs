using System;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace Smart_Sudoku.Pages
{
    /// <summary>The player's numbers: games, time, mistakes, records, a chart of the last 7 days and progress per difficulty.</summary>
    public sealed partial class StatsPage : BasePage
    {
        private static readonly string[] DayNames = { "א׳", "ב׳", "ג׳", "ד׳", "ה׳", "ו׳", "ש׳" };

        public StatsPage()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            int games = Progression.Stat("games");
            SubtitleText.Text = games == 0
                ? "עוד אין נתונים — פותרים לוח ראשון והמספרים יופיעו כאן!"
                : $"רמה {Progression.PlayerLevel} · ליגת {Progression.CurrentLeague.Name} {Progression.CurrentLeague.Emoji}";
            ContentPanel.Children.Clear();

            // Number cards, 3 in a row
            int duels = Progression.Stat("duels"), wins = Progression.Stat("duelWins");
            var cards = new (string Emoji, string Value, string Label)[]
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
            for (int c = 0; c < 3; c++)
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            for (int i = 0; i < cards.Length; i++)
            {
                if (i % 3 == 0)
                    grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                var stack = new StackPanel { Spacing = 2, HorizontalAlignment = HorizontalAlignment.Center };
                stack.Children.Add(Center(Ui.Text(cards[i].Emoji, 28)));
                stack.Children.Add(Center(Ui.Text(cards[i].Value, 24, "RoseBrush", bold: true)));
                stack.Children.Add(Center(Ui.Text(cards[i].Label, 14, "MutedTextBrush")));
                var card = Ui.Card(stack);
                Grid.SetRow(card, i / 3);
                Grid.SetColumn(card, i % 3);
                grid.Children.Add(card);
            }
            ContentPanel.Children.Add(grid);

            ContentPanel.Children.Add(Ui.Card(WeekChart()));
            ContentPanel.Children.Add(Ui.Card(DifficultyProgress()));
        }

        private static TextBlock Center(TextBlock text)
        {
            text.HorizontalAlignment = HorizontalAlignment.Center;
            text.TextAlignment = TextAlignment.Center;
            return text;
        }

        /// <summary>A bar for each of the last 7 days: how many boards were solved.</summary>
        private static UIElement WeekChart()
        {
            const double ChartHeight = 140;
            var days = Enumerable.Range(0, 7).Select(i => DateTime.Now.Date.AddDays(i - 6)).ToList();
            var counts = days.Select(Progression.SolvesOn).ToList();
            int max = Math.Max(1, counts.Max());

            var bars = new Grid { ColumnSpacing = 12, Height = ChartHeight + 50 };
            for (int i = 0; i < days.Count; i++)
            {
                bars.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                bool today = i == days.Count - 1;
                var column = new StackPanel { VerticalAlignment = VerticalAlignment.Bottom, Spacing = 4 };
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
        {
            var panel = new StackPanel { Spacing = 10 };
            panel.Children.Add(Ui.Text("🎚️ התקדמות בכל רמת קושי", 20, "RoseBrush", bold: true));
            foreach (var difficulty in Levels.Difficulties)
            {
                int solved = Enumerable.Range(1, Levels.PerDifficulty).Count(n => GameData.GetStars(Levels.Id(difficulty.Index, n)) > 0);
                panel.Children.Add(Ui.Row(
                    Ui.Bar((double)solved / Levels.PerDifficulty),
                    Ui.Text($"{solved}/{Levels.PerDifficulty}", 15, "MutedTextBrush"),
                    new TextBlock { Text = $"{difficulty.Emoji} {difficulty.Name}", FontSize = 16, Foreground = Ui.Res("BerryBrush"), Width = 110, VerticalAlignment = VerticalAlignment.Center }));
            }
            return panel;
        }
    }
}
