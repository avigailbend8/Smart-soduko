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
    public sealed partial class LevelsPage : BasePage
    // אובייקט: LevelsPage — עמוד בחירת השלבים: מציג את השלבים של רמת הקושי שנבחרה עם הכוכבים. לחיצה על שלב פתוח מתחילה אותו.
    {
        private const int Columns = 5;
        // אובייקט: Columns — כמה שלבים מוצגים בכל שורה (5).

        // Remembered while the app is open, so coming back shows the same difficulty
        private static int selectedDifficulty;
        // אובייקט: selectedDifficulty — רמת הקושי שנבחרה. נשמרת כל עוד האפליקציה פתוחה, כדי שבחזרה לעמוד תופיע אותה רמה.

        public LevelsPage()
        // פעולה: LevelsPage (בנאי) — טוענת את העיצוב, ומכינה טבלה של 5 עמודות ו-4 שורות לכפתורי השלבים.
        {
            this.InitializeComponent();

            for (int i = 0; i < Columns; i++)
                LevelsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            for (int i = 0; i < Levels.PerDifficulty / Columns; i++)
                LevelsGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        // פעולה: OnNavigatedTo — רצה כשנכנסים לעמוד: מציגה את המטבעות, ובונה מחדש את לשוניות הרמות ואת כפתורי השלבים.
        {
            base.OnNavigatedTo(e);

            // Rebuild every time so new stars and unlocked levels show up
            CoinsText.Text = $"🪙 {GameData.Coins}";
            BuildDifficultyTabs();
            BuildLevelButtons();
        }

        private void BuildDifficultyTabs()
        // פעולה: BuildDifficultyTabs — בונה לשונית לכל רמת קושי (קל, בינוני, קשה, מומחה). הלשונית שנבחרה מודגשת.
        {
            DifficultyPanel.Children.Clear();
            foreach (var difficulty in Levels.Difficulties)
            {
                bool selected = difficulty.Index == selectedDifficulty;
                // אובייקט: selected — אמת אם זו רמת הקושי שנבחרה.
                var tab = new Button
                // אובייקט: tab — הכפתור של הלשונית.
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
        // פעולה: BuildLevelButtons — בונה כפתור לכל אחד מ-20 השלבים ברמה: מספר השלב, והכוכבים (או מנעול אם השלב עוד נעול).
        {
            LevelsGrid.Children.Clear();
            int unlockedUpTo = GameData.UnlockedLevel(selectedDifficulty);
            // אובייקט: unlockedUpTo — עד איזה שלב הכול פתוח ברמה הזאת.

            for (int number = 1; number <= Levels.PerDifficulty; number++)
            {
                int levelId = Levels.Id(selectedDifficulty, number);
                // אובייקט: levelId — המספר המלא של השלב.
                bool unlocked = number <= unlockedUpTo;
                // אובייקט: unlocked — אמת אם השלב פתוח.
                int stars = GameData.GetStars(levelId);
                // אובייקט: stars — כמה כוכבים השחקן השיג בשלב.

                var content = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center, Spacing = 2 };
                // אובייקט: content — מה שכתוב בתוך הכפתור: "Level", המספר והכוכבים.
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
                    Text = unlocked ? StarsText(stars) : "🔒",
                    FontFamily = new FontFamily("Segoe UI Symbol"),
                    FontSize = 16,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Foreground = Res(stars > 0 ? "RoseBrush" : "SoftPinkBrush"),
                });

                var button = new Button
                // אובייקט: button — הכפתור של השלב.
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
        // פעולה: LevelButton_Click — לחיצה על שלב: פותחת את מסך המשחק עם השלב הזה.
        {
            int levelId = (int)((Button)sender).Tag;
            // אובייקט: levelId — מספר השלב ששמור בכפתור שנלחץ.
            Frame.Navigate(typeof(GamePage), levelId);
        }

        private void StoreButton_Click(object sender, RoutedEventArgs e)
        // פעולה: StoreButton_Click — כפתור החנות: עובר לעמוד החנות.
        {
            Frame.Navigate(typeof(StorePage));
        }
    }
}
