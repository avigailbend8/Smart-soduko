using System.Linq;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace Smart_Sudoku.Pages
{
    /// <summary>All the badges: unlocked ones in color, locked ones faded with how to get them.</summary>
    public sealed partial class AchievementsPage : BasePage
    // אובייקט: AchievementsPage — עמוד ההישגים: הישגים שנפתחו מופיעים בצבע, ואלה שעוד נעולים מופיעים חיוורים עם הסבר איך משיגים אותם.
    {
        private const int PerRow = 3;
        // אובייקט: PerRow — כמה הישגים מוצגים בכל שורה (3).

        public AchievementsPage()
        // פעולה: AchievementsPage (בנאי) — טוענת את העיצוב של העמוד מקובץ ה-XAML.
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        // פעולה: OnNavigatedTo — רצה כשנכנסים לעמוד: כותבת כמה הישגים נפתחו, ובונה כרטיס לכל הישג בטבלה של 3 עמודות.
        {
            base.OnNavigatedTo(e);

            int unlocked = Achievements.All.Count(a => GameData.HasAchievement(a.Id));
            // אובייקט: unlocked — כמה הישגים השחקן כבר פתח.
            ProgressText.Text = $"פתחת {unlocked} מתוך {Achievements.All.Count} הישגים. כל הישג נותן מטבעות!";

            BadgesGrid.Children.Clear();
            for (int i = 0; i < PerRow; i++)
                BadgesGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            int index = 0;
            // אובייקט: index — מספר ההישג ברשימה, כדי לדעת באיזו שורה ועמודה לשים אותו.
            foreach (var achievement in Achievements.All)
            {
                if (BadgesGrid.RowDefinitions.Count <= index / PerRow)
                    BadgesGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                bool has = GameData.HasAchievement(achievement.Id);
                // אובייקט: has — אמת אם השחקן כבר השיג את ההישג הזה.
                var badge = new Border
                // אובייקט: badge — הכרטיס של ההישג: אימוג'י (או מנעול), שם, הסבר, וכמה מטבעות הוא נותן.
                {
                    Background = Res(has ? "SelectedCellBrush" : "CardBrush"),
                    BorderBrush = Res(has ? "RoseBrush" : "SoftPinkBrush"),
                    BorderThickness = new Thickness(has ? 2 : 1),
                    CornerRadius = new CornerRadius(18),
                    Padding = new Thickness(12, 16, 12, 14),
                    Opacity = has ? 1 : 0.6,
                    Child = new StackPanel
                    {
                        Spacing = 6,
                        Children =
                        {
                            new TextBlock { Text = has ? achievement.Emoji : "🔒", FontSize = 40, HorizontalAlignment = HorizontalAlignment.Center },
                            new TextBlock
                            {
                                Text = achievement.Name,
                                FontSize = 18,
                                FontWeight = FontWeights.Bold,
                                Foreground = Res("BerryBrush"),
                                HorizontalAlignment = HorizontalAlignment.Center,
                            },
                            new TextBlock
                            {
                                Text = achievement.Description,
                                FontSize = 14,
                                Foreground = Res("MutedTextBrush"),
                                TextWrapping = TextWrapping.Wrap,
                                TextAlignment = TextAlignment.Center,
                            },
                            new TextBlock
                            {
                                Text = has ? "✓ נפתח" : $"+{achievement.RewardCoins} 🪙",
                                FontSize = 14,
                                FontWeight = FontWeights.SemiBold,
                                Foreground = Res("RoseBrush"),
                                HorizontalAlignment = HorizontalAlignment.Center,
                            },
                        },
                    },
                };
                Grid.SetRow(badge, index / PerRow);
                Grid.SetColumn(badge, index % PerRow);
                BadgesGrid.Children.Add(badge);
                index++;
            }
        }
    }
}
