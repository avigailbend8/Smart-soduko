using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace Smart_Sudoku.Pages
{
    /// <summary>The player level (XP), this week's league, and today's 3 missions with their prize chest.</summary>
    public sealed partial class MissionsPage : BasePage
    {
        public MissionsPage()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            Refresh();
        }

        private void Refresh()
        {
            SubtitleText.Text = "כל משחק נותן נקודות ניסיון (XP). משימות חדשות מחכות כל יום!";
            ContentPanel.Children.Clear();

            // Player level
            int level = Progression.PlayerLevel, xp = Progression.Xp;
            int from = Progression.XpAtLevel(level), to = Progression.XpAtLevel(level + 1);
            ContentPanel.Children.Add(Ui.Card(new StackPanel
            {
                Spacing = 8,
                Children =
                {
                    Ui.Row(Ui.Text($"רמה {level}", 26, "RoseBrush", bold: true), Ui.Text($"{xp - from} / {to - from} XP", 16, "MutedTextBrush"), Ui.Text("⭐", 34)),
                    Ui.Bar((double)(xp - from) / (to - from)),
                    Ui.Text($"בעלייה לרמה {level + 1} מקבלים {(level + 1) * 50} 🪙", 14, "MutedTextBrush"),
                },
            }, highlight: true));

            // Weekly league
            var league = Progression.CurrentLeague;
            var next = Progression.NextLeague;
            int points = Progression.WeekPoints;
            var leagueInfo = new StackPanel
            {
                Spacing = 8,
                Children =
                {
                    Ui.Row(Ui.Text($"ליגת {league.Name}", 22, "RoseBrush", bold: true), Ui.Text($"{points} נקודות השבוע", 16, "MutedTextBrush"), Ui.Text(league.Emoji, 34)),
                },
            };
            if (next != null)
            {
                leagueInfo.Children.Add(Ui.Bar((double)(points - league.MinPoints) / (next.MinPoints - league.MinPoints)));
                leagueInfo.Children.Add(Ui.Text($"עוד {next.MinPoints - points} נקודות לליגת {next.Name} {next.Emoji}. הליגה מתחילה מחדש כל שבוע.", 14, "MutedTextBrush"));
            }
            else
            {
                leagueInfo.Children.Add(Ui.Text("הגעת לליגה הכי גבוהה! 💎", 15, "BerryBrush", bold: true));
            }
            if (Progression.LastWeekLeague is League last)
                leagueInfo.Children.Add(Ui.Text($"בשבוע שעבר: {last.Emoji} {last.Name}", 14, "MutedTextBrush"));
            ContentPanel.Children.Add(Ui.Card(leagueInfo));

            // Today's missions
            ContentPanel.Children.Add(Ui.Text("📋 המשימות של היום", 22, "RoseBrush", bold: true));
            foreach (var mission in Progression.TodaysMissions)
            {
                int progress = System.Math.Min(mission.Target, Progression.MissionProgress(mission.Id));
                bool done = Progression.MissionDone(mission);
                bool claimed = Progression.MissionClaimed(mission.Id);

                var claim = new Button
                {
                    Content = claimed ? "✓ נאסף" : $"+{Progression.MissionReward} 🪙",
                    IsEnabled = done && !claimed,
                    MinWidth = 110,
                    FontSize = 16,
                };
                claim.Click += (s, e) =>
                {
                    if (Progression.ClaimMission(mission))
                    {
                        Sounds.Play("coin");
                        App.ShowToast($"+{Progression.MissionReward} 🪙");
                    }
                    Refresh();
                };

                ContentPanel.Children.Add(Ui.Card(Ui.Row(new StackPanel
                {
                    Spacing = 6,
                    Children =
                    {
                        Ui.Text(mission.Text, 18, bold: true),
                        Ui.Bar((double)progress / mission.Target, 10),
                        Ui.Text($"{progress} / {mission.Target}", 13, "MutedTextBrush"),
                    },
                }, claim, Ui.Text(done ? "✅" : "🎯", 30)), highlight: done && !claimed));
            }

            // The chest for finishing all three
            bool allDone = Progression.AllMissionsDone;
            var open = new Button
            {
                Content = Progression.ChestClaimed ? "✓ נפתח" : "לפתוח!",
                IsEnabled = allDone && !Progression.ChestClaimed,
                MinWidth = 110,
                FontSize = 16,
            };
            open.Click += (s, e) =>
            {
                if (Progression.ClaimChest())
                {
                    Sounds.Play("win");
                    App.ShowToast($"🎁 קיבלת {Progression.ChestCoins} 🪙 + {Progression.ChestHints} רמזים + 50 XP!");
                }
                Refresh();
            };
            ContentPanel.Children.Add(Ui.Card(Ui.Row(new StackPanel
            {
                Spacing = 4,
                Children =
                {
                    Ui.Text("תיבת הפתעה", 20, "RoseBrush", bold: true),
                    Ui.Text($"מסיימים את 3 המשימות ומקבלים {Progression.ChestCoins} 🪙, {Progression.ChestHints} רמזים ו-50 XP", 14, "MutedTextBrush"),
                },
            }, open, Ui.Text(Progression.ChestClaimed ? "📭" : "🎁", 40)), highlight: allDone && !Progression.ChestClaimed));
        }
    }
}
