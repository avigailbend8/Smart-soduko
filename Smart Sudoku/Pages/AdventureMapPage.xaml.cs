using System.Linq;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace Smart_Sudoku.Pages
{
    /// <summary>
    /// The adventure map: 4 worlds, each a winding path of 6 stages ending with a boss.
    /// Beating a boss opens the next world.
    /// </summary>
    public sealed partial class AdventureMapPage : BasePage
    {
        // How far each stage sits from the top of its path, so the path winds up and down
        private static readonly double[] PathOffsets = { 40, 0, 50, 10, 55, 20 };

        public AdventureMapPage()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            int stars = 0, total = Adventure.Worlds.Count * Adventure.StagesPerWorld * 3;
            for (int w = 0; w < Adventure.Worlds.Count; w++)
                for (int s = 1; s <= Adventure.StagesPerWorld; s++)
                    stars += Adventure.Stars(w, s);
            SubtitleText.Text = $"עוברים שלב אחרי שלב ומנצחים את הבוס של כל עולם! ⭐ {stars}/{total}";

            ContentPanel.Children.Clear();
            for (int w = 0; w < Adventure.Worlds.Count; w++)
                ContentPanel.Children.Add(WorldCard(w));
        }

        private UIElement WorldCard(int world)
        {
            var info = Adventure.Worlds[world];
            bool open = Adventure.IsOpen(world, 1);
            int worldStars = Enumerable.Range(1, Adventure.StagesPerWorld).Sum(s => Adventure.Stars(world, s));

            var path = new Grid { Height = 140, ColumnSpacing = 6 };
            for (int stage = 1; stage <= Adventure.StagesPerWorld; stage++)
            {
                path.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                var node = Node(world, stage);
                node.Margin = new Thickness(0, PathOffsets[stage - 1], 0, 0);
                Grid.SetColumn(node, stage - 1);
                path.Children.Add(node);
            }

            var header = Ui.Row(
                Ui.Text($"עולם {world + 1}: {info.Name}", 22, "RoseBrush", bold: true),
                Ui.Text(open ? $"⭐ {worldStars}/{Adventure.StagesPerWorld * 3}" : "🔒 מנצחים את הבוס הקודם", 15, "MutedTextBrush"),
                Ui.Text(info.Emoji, 34));

            var card = Ui.Card(new StackPanel { Spacing = 8, Children = { header, path } }, highlight: open);
            card.Opacity = open ? 1 : 0.55;
            return card;
        }

        // A lock if closed, the boss's emoji for a boss, otherwise the stage number
        private static string NodeText(int world, int stage, bool open, bool boss)
        {
            if (!open)
                return "🔒";
            if (boss)
                return Adventure.Worlds[world].Boss;
            return stage.ToString();
        }

        private FrameworkElement Node(int world, int stage)
        {
            bool open = Adventure.IsOpen(world, stage);
            bool boss = Adventure.IsBoss(stage);
            int stars = Adventure.Stars(world, stage);
            double size = boss ? 74 : 60;

            var button = new Button
            {
                Width = size,
                Height = size,
                Padding = new Thickness(0),
                CornerRadius = new CornerRadius(size / 2),
                IsEnabled = open,
                Background = Ui.Res(stars > 0 ? "SelectedCellBrush" : "CardBrush"),
                BorderBrush = Ui.Res(boss ? "BerryBrush" : "RoseBrush"),
                BorderThickness = new Thickness(boss ? 3 : 2),
                Content = new TextBlock
                {
                    Text = NodeText(world, stage, open, boss),
                    FontSize = boss || !open ? size * 0.45 : 24,
                    FontWeight = FontWeights.Bold,
                    Foreground = Ui.Res("BerryBrush"),
                },
            };
            // The name a screen reader says for this button
            Windows.UI.Xaml.Automation.AutomationProperties.SetName(button, boss ? $"בוס עולם {world + 1}" : $"שלב {stage} עולם {world + 1}");
            ToolTipService.SetToolTip(button, boss ? $"הבוס! +{Adventure.BossBonus} 🪙" : $"שלב {stage}");
            button.Click += (s, e) => Frame.Navigate(typeof(GamePage), new AdventureStart(world, stage));

            return new StackPanel
            {
                Spacing = 2,
                HorizontalAlignment = HorizontalAlignment.Center,
                Children =
                {
                    button,
                    new TextBlock
                    {
                        Text = open ? StarsText(stars) : "",
                        FontSize = 14,
                        Foreground = Ui.Res("RoseBrush"),
                        HorizontalAlignment = HorizontalAlignment.Center,
                        FontFamily = new FontFamily("Segoe UI Symbol"),
                    },
                },
            };
        }
    }
}
