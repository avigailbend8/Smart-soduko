using System;
using System.Collections.Generic;
using System.Linq;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace Smart_Sudoku.Pages
{
    /// <summary>
    /// The leaderboard of every user on this computer (from the database), with their profile pictures.
    /// The guest is included too, when it has progress.
    /// </summary>
    public sealed partial class LeaderboardPage : Page
    {
        private enum SortBy { Stars, Levels, Coins }

        private static SortBy sortBy = SortBy.Stars;
        private List<LeaderboardRow> rows = new List<LeaderboardRow>();

        public LeaderboardPage()
        {
            this.InitializeComponent();
        }

        private static Brush Res(string key) => (Brush)Application.Current.Resources[key];

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            rows = Database.GetLeaderboard();

            // The guest isn't in the database, so its line is made from the guest's saved progress
            var guest = GuestStore.Load();
            if (guest.HasAnyProgress)
            {
                int fastest = guest.BestTimes.Values.Where(t => t > 0).DefaultIfEmpty(0).Min();
                rows.Add(new LeaderboardRow(0, "אורח", guest.Coins, guest.Stars.Count, guest.Stars.Values.Sum(), fastest, false));
            }

            BuildSortButtons();
            _ = ShowRowsAsync();
        }

        private void BuildSortButtons()
        {
            SortPanel.Children.Clear();
            foreach (var (mode, label) in new[] { (SortBy.Stars, "⭐ כוכבים"), (SortBy.Levels, "✅ שלבים"), (SortBy.Coins, "🪙 מטבעות") })
            {
                bool selected = mode == sortBy;
                var button = new Button
                {
                    Content = label,
                    FontSize = 16,
                    Padding = new Thickness(18, 6, 18, 6),
                    Background = Res(selected ? "SelectedCellBrush" : "ButtonHoverBrush"),
                    BorderBrush = Res(selected ? "RoseBrush" : "SoftPinkBrush"),
                    BorderThickness = new Thickness(selected ? 2.5 : 1.5),
                };
                button.Click += (s, e) =>
                {
                    sortBy = mode;
                    BuildSortButtons();
                    _ = ShowRowsAsync();
                };
                SortPanel.Children.Add(button);
            }
        }

        private async System.Threading.Tasks.Task ShowRowsAsync()
        {
            var sorted = sortBy switch
            {
                SortBy.Levels => rows.OrderByDescending(r => r.Solved).ThenByDescending(r => r.Stars),
                SortBy.Coins => rows.OrderByDescending(r => r.Coins),
                _ => rows.OrderByDescending(r => r.Stars).ThenByDescending(r => r.Solved),
            };

            RowsPanel.Children.Clear();
            EmptyText.Visibility = rows.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

            int place = 1;
            foreach (var row in sorted.ToList())
            {
                bool me = row.UserId == (Session.UserId ?? 0) && (Session.UserId != null || Session.IsGuest);
                var picture = row.HasAvatar ? await Avatars.LoadAsync(row.UserId) : null;

                var line = new Grid
                {
                    Background = Res(me ? "SelectedCellBrush" : "RelatedCellBrush"),
                    BorderBrush = Res(me ? "RoseBrush" : "SoftPinkBrush"),
                    BorderThickness = new Thickness(me ? 2 : 0),
                    CornerRadius = new CornerRadius(16),
                    Padding = new Thickness(14, 8, 14, 8),
                };
                foreach (double width in new[] { 56.0, 64, -1, 90, 90, 100, 90 })
                    line.ColumnDefinitions.Add(new ColumnDefinition { Width = width < 0 ? new GridLength(1, GridUnitType.Star) : new GridLength(width) });

                string medal = place switch { 1 => "🥇", 2 => "🥈", 3 => "🥉", _ => $"{place}" };
                Add(line, 0, Text(medal, 24, FontWeights.Bold));
                var circle = Avatars.Circle(picture, row.Name, 48);
                Grid.SetColumn(circle, 1);
                line.Children.Add(circle);
                Add(line, 2, Text(row.Name + (me ? "  (אני)" : ""), 19, FontWeights.SemiBold));
                Add(line, 3, Text(row.Stars.ToString(), 18, FontWeights.Bold));
                Add(line, 4, Text(row.Solved.ToString(), 18, FontWeights.Normal));
                Add(line, 5, Text(row.Coins.ToString("N0"), 18, FontWeights.Normal));
                Add(line, 6, Text(row.FastestSeconds > 0 ? $"{row.FastestSeconds / 60:00}:{row.FastestSeconds % 60:00}" : "—", 18, FontWeights.Normal));

                RowsPanel.Children.Add(line);
                place++;
            }
        }

        private static TextBlock Text(string text, double size, FontWeight weight) => new TextBlock
        {
            Text = text,
            FontSize = size,
            FontWeight = weight,
            Foreground = Res("BerryBrush"),
            VerticalAlignment = VerticalAlignment.Center,
            TextTrimming = TextTrimming.CharacterEllipsis,
        };

        private static void Add(Grid grid, int column, FrameworkElement element)
        {
            Grid.SetColumn(element, column);
            grid.Children.Add(element);
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            if (Frame.CanGoBack)
                Frame.GoBack();
        }
    }
}
