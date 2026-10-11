using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace Smart_Sudoku.Pages
{
    /// <summary>
    /// Shows the levels of the chosen difficulty with their stars;
    /// picking an unlocked one opens the game.
    /// </summary>
    public sealed partial class LevelsPage : Page
    {
        private const int Columns = 5;

        // Remembered while the app is open, so coming back shows the same difficulty
        private static int selectedDifficulty;

        public LevelsPage()
        {
            this.InitializeComponent();

            for (int i = 0; i < Columns; i++)
                LevelsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            for (int i = 0; i < Levels.PerDifficulty / Columns; i++)
                LevelsGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            // Rebuild every time so new stars and unlocked levels show up
            CoinsText.Text = $"🪙 {GameData.Coins}";
            BuildDifficultyTabs();
            BuildLevelButtons();
        }

        private static Brush Res(string key) => (Brush)Application.Current.Resources[key];

        private void BuildDifficultyTabs()
        {
            DifficultyPanel.Children.Clear();
            foreach (var difficulty in Levels.Difficulties)
            {
                bool selected = difficulty.Index == selectedDifficulty;
                var tab = new Button
                {
                    Content = $"{difficulty.Emoji} {difficulty.Name}",
                    FontSize = 20,
                    Padding = new Thickness(26, 10, 26, 10),
                    Tag = difficulty.Index,
                    Background = Res(selected ? "SelectedCellBrush" : "ButtonHoverBrush"),
                    BorderBrush = Res(selected ? "RoseBrush" : "SoftPinkBrush"),
                    BorderThickness = new Thickness(selected ? 2.5 : 1.5),
                };
                tab.Click += (s, e) =>
                {
                    selectedDifficulty = (int)((Button)s).Tag;
                    BuildDifficultyTabs();
                    BuildLevelButtons();
                };
                DifficultyPanel.Children.Add(tab);
            }
        }

        private void BuildLevelButtons()
        {
            LevelsGrid.Children.Clear();
            int unlockedUpTo = GameData.UnlockedLevel(selectedDifficulty);

            for (int number = 1; number <= Levels.PerDifficulty; number++)
            {
                int levelId = Levels.Id(selectedDifficulty, number);
                bool unlocked = number <= unlockedUpTo;
                int stars = GameData.GetStars(levelId);

                var content = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center, Spacing = 2 };
                content.Children.Add(new TextBlock
                {
                    Text = "Level",
                    FontSize = 14,
                    FontWeight = FontWeights.Normal,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Foreground = Res("MutedTextBrush"),
                });
                content.Children.Add(new TextBlock
                {
                    Text = number.ToString(),
                    FontSize = 32,
                    FontWeight = FontWeights.Bold,
                    HorizontalAlignment = HorizontalAlignment.Center,
                });
                content.Children.Add(new TextBlock
                {
                    Text = unlocked ? new string('★', stars) + new string('☆', 3 - stars) : "🔒",
                    FontFamily = new FontFamily("Segoe UI Symbol"),
                    FontSize = 16,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Foreground = Res(stars > 0 ? "RoseBrush" : "SoftPinkBrush"),
                });

                var button = new Button
                {
                    Content = content,
                    Width = 104,
                    Height = 116,
                    Padding = new Thickness(0),
                    CornerRadius = new CornerRadius(26),
                    Tag = levelId,
                    IsEnabled = unlocked,
                };
                Windows.UI.Xaml.Automation.AutomationProperties.SetName(button, $"Level {number}");
                button.Click += LevelButton_Click;

                Grid.SetRow(button, (number - 1) / Columns);
                Grid.SetColumn(button, (number - 1) % Columns);
                LevelsGrid.Children.Add(button);
            }
        }

        private void LevelButton_Click(object sender, RoutedEventArgs e)
        {
            int levelId = (int)((Button)sender).Tag;
            Frame.Navigate(typeof(GamePage), levelId);
        }

        private void StoreButton_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(StorePage));
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            if (Frame.CanGoBack)
                Frame.GoBack();
        }
    }
}
