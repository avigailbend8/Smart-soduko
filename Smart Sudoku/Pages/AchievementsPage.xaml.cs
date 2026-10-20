using System.Linq;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace Smart_Sudoku.Pages
{
    /// <summary>All the badges: unlocked ones in color, locked ones faded with how to get them.</summary>
    public sealed partial class AchievementsPage : Page
    {
        private const int PerRow = 3;

        public AchievementsPage()
        {
            this.InitializeComponent();
        }

        private static Brush Res(string key) => (Brush)Application.Current.Resources[key];

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            int unlocked = Achievements.All.Count(a => GameData.HasAchievement(a.Id));
            ProgressText.Text = $"פתחת {unlocked} מתוך {Achievements.All.Count} הישגים. כל הישג נותן מטבעות!";

            BadgesGrid.Children.Clear();
            for (int i = 0; i < PerRow; i++)
                BadgesGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            int index = 0;
            foreach (var achievement in Achievements.All)
            {
                if (BadgesGrid.RowDefinitions.Count <= index / PerRow)
                    BadgesGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                bool has = GameData.HasAchievement(achievement.Id);
                var badge = new Border
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

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            if (Frame.CanGoBack)
                Frame.GoBack();
        }
    }
}
