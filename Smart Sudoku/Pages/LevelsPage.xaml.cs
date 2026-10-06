using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Smart_Sudoku.Pages
{
    /// <summary>
    /// Shows the 10 levels; picking one opens the game at that level.
    /// </summary>
    public sealed partial class LevelsPage : Page
    {
        public const int LevelCount = 20;
        private const int Columns = 5;

        public LevelsPage()
        {
            this.InitializeComponent();
            BuildLevelButtons();
        }

        private void BuildLevelButtons()
        {
            for (int i = 0; i < Columns; i++)
                LevelsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            for (int i = 0; i < LevelCount / Columns; i++)
                LevelsGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            for (int level = 1; level <= LevelCount; level++)
            {
                var content = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center };
                content.Children.Add(new TextBlock
                {
                    Text = "Level",
                    FontSize = 15,
                    FontWeight = Windows.UI.Text.FontWeights.Normal,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Foreground = (Windows.UI.Xaml.Media.Brush)Application.Current.Resources["MutedTextBrush"],
                });
                content.Children.Add(new TextBlock
                {
                    Text = level.ToString(),
                    FontSize = 36,
                    FontWeight = Windows.UI.Text.FontWeights.Bold,
                    HorizontalAlignment = HorizontalAlignment.Center,
                });

                var button = new Button
                {
                    Content = content,
                    Width = 110,
                    Height = 110,
                    Padding = new Thickness(0),
                    CornerRadius = new CornerRadius(26),
                    Tag = level,
                };
                Windows.UI.Xaml.Automation.AutomationProperties.SetName(button, $"Level {level}");
                button.Click += LevelButton_Click;

                Grid.SetRow(button, (level - 1) / Columns);
                Grid.SetColumn(button, (level - 1) % Columns);
                LevelsGrid.Children.Add(button);
            }
        }

        private void LevelButton_Click(object sender, RoutedEventArgs e)
        {
            int level = (int)((Button)sender).Tag;
            Frame.Navigate(typeof(GamePage), level);
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            if (Frame.CanGoBack)
                Frame.GoBack();
        }
    }
}
