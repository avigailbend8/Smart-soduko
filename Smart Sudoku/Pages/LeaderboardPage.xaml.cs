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
    public sealed partial class LeaderboardPage : BasePage
    {
        private enum SortBy { Stars, Levels, Coins }

        private static SortBy sortBy = SortBy.Stars;
        private List<LeaderboardRow> rows = new List<LeaderboardRow>();

        public LeaderboardPage()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            rows = Database.GetLeaderboard();

            // The guest isn't in the database, so its line is made from the guest's saved progress
            var guest = GuestStore.Load();
            if (guest.HasAnyProgress)
            {
                int fastest = 0;
                foreach (int time in guest.BestTimes.Values)
                {
                    if (time > 0 && (fastest == 0 || time < fastest))
                        fastest = time;
                }
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
            List<LeaderboardRow> sorted;
            if (sortBy == SortBy.Levels)
                sorted = rows.OrderByDescending(r => r.Solved).ThenByDescending(r => r.Stars).ToList();
            else if (sortBy == SortBy.Coins)
                sorted = rows.OrderByDescending(r => r.Coins).ToList();
            else
                sorted = rows.OrderByDescending(r => r.Stars).ThenByDescending(r => r.Solved).ToList();

            RowsPanel.Children.Clear();
            EmptyText.Visibility = rows.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

            int place = 1;
            foreach (var row in sorted)
            {
                // The guest's line has UserId 0
                bool me;
                if (Session.IsGuest)
                    me = row.UserId == 0;
                else
                    me = Session.UserId != null && row.UserId == Session.UserId;
                var picture = row.HasAvatar ? await Avatars.LoadAsync(row.UserId) : null;

                var line = new Grid
                {
                    Background = Res(me ? "SelectedCellBrush" : "RelatedCellBrush"),
                    BorderBrush = Res(me ? "RoseBrush" : "SoftPinkBrush"),
                    BorderThickness = new Thickness(me ? 2 : 0),
                    CornerRadius = new CornerRadius(16),
                    Padding = new Thickness(14, 8, 14, 8),
                };
                // Columns: place, picture, name (takes the free space), stars, levels, coins, fastest time
                line.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(56) });
                line.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(64) });
                line.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                line.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(90) });
                line.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(90) });
                line.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });
                line.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(90) });

                string medal = place <= 3 ? Medal(place) : place.ToString();
                Add(line, 0, Text(medal, 24, FontWeights.Bold));
                var circle = Avatars.Circle(picture, row.Name, 48, me ? Cosmetics.CurrentFrameId : Database.GetSetting(row.UserId, "avatarFrame"));
                Grid.SetColumn(circle, 1);
                line.Children.Add(circle);
                Add(line, 2, Text(row.Name + (me ? "  (אני)" : ""), 19, FontWeights.SemiBold));
                Add(line, 3, Text(row.Stars.ToString(), 18, FontWeights.Bold));
                Add(line, 4, Text(row.Solved.ToString(), 18, FontWeights.Normal));
                Add(line, 5, Text(row.Coins.ToString("N0"), 18, FontWeights.Normal));
                Add(line, 6, Text(row.FastestSeconds > 0 ? Ui.Time(row.FastestSeconds) : "—", 18, FontWeights.Normal));

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
    }
}
