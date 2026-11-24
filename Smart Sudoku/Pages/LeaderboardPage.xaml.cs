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
    // אובייקט: LeaderboardPage — טבלת השיאים של כל המשתמשים במחשב הזה (ממסד הנתונים), עם תמונות הפרופיל. גם האורח מופיע אם יש לו התקדמות.
    {
        private enum SortBy { Stars, Levels, Coins }
        // אובייקט: SortBy — שלוש דרכים למיין את הטבלה: לפי כוכבים, לפי שלבים או לפי מטבעות.

        private static SortBy sortBy = SortBy.Stars;
        // אובייקט: sortBy — לפי מה הטבלה ממוינת עכשיו (מתחיל בכוכבים).
        private List<LeaderboardRow> rows = new List<LeaderboardRow>();
        // אובייקט: rows — כל השורות של הטבלה.

        public LeaderboardPage()
        // פעולה: LeaderboardPage (בנאי) — טוענת את העיצוב של העמוד מקובץ ה-XAML.
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        // פעולה: OnNavigatedTo — רצה כשנכנסים לעמוד: טוענת את כל המשתמשים, מוסיפה שורה לאורח, ובונה את הכפתורים ואת הטבלה.
        {
            base.OnNavigatedTo(e);

            rows = Database.GetLeaderboard();

            // The guest is left out of the database query (it has no password), and gets its own line only if it played
            var guest = Database.LoadProgress(Database.GuestUserId());
            // אובייקט: guest — ההתקדמות של האורח מתוך מסד הנתונים. מקבל שורה רק אם כבר שיחק.
            if (guest.HasAnyProgress)
            {
                int fastest = 0;
                // אובייקט: fastest — הזמן הכי מהיר של האורח בשלב כלשהו.
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
        // פעולה: BuildSortButtons — בונה את שלושת כפתורי המיון. הכפתור שנבחר מודגש, ולחיצה ממיינת מחדש.
        {
            SortPanel.Children.Clear();
            foreach (var (mode, label) in new[] { (SortBy.Stars, "⭐ כוכבים"), (SortBy.Levels, "✅ שלבים"), (SortBy.Coins, "🪙 מטבעות") })
            {
                bool selected = mode == sortBy;
                // אובייקט: selected — אמת אם זה כפתור המיון שנבחר.
                var button = new Button
                // אובייקט: button — כפתור מיון אחד.
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
        // פעולה: ShowRowsAsync — ממיינת את השורות ובונה שורה לכל שחקן: מקום (עם מדליה), תמונה, שם, כוכבים, שלבים, מטבעות וזמן הכי מהיר.
        // השורה של השחקן הנוכחי מודגשת.
        {
            List<LeaderboardRow> sorted;
            // אובייקט: sorted — השורות אחרי המיון.
            if (sortBy == SortBy.Levels)
                sorted = rows.OrderByDescending(r => r.Solved).ThenByDescending(r => r.Stars).ToList();
            else if (sortBy == SortBy.Coins)
                sorted = rows.OrderByDescending(r => r.Coins).ToList();
            else
                sorted = rows.OrderByDescending(r => r.Stars).ThenByDescending(r => r.Solved).ToList();

            RowsPanel.Children.Clear();
            EmptyText.Visibility = rows.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

            int place = 1;
            // אובייקט: place — המקום בטבלה (1, 2, 3...).
            foreach (var row in sorted)
            {
                // The guest's line has UserId 0
                bool me;
                // אובייקט: me — אמת אם השורה הזאת היא של השחקן שמשחק עכשיו.
                if (Session.IsGuest)
                    me = row.UserId == 0;
                else
                    me = Session.UserId != null && row.UserId == Session.UserId;
                var picture = row.HasAvatar ? await Avatars.LoadAsync(row.UserId) : null;
                // אובייקט: picture — תמונת הפרופיל של השחקן (או null).

                var line = new Grid
                // אובייקט: line — השורה בטבלה: טבלה קטנה עם 7 עמודות.
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
                // אובייקט: medal — המדליה או מספר המקום.
                Add(line, 0, Text(medal, 24, FontWeights.Bold));
                var circle = Avatars.Circle(picture, row.Name, 48, me ? Cosmetics.CurrentFrameId : Database.GetSetting(row.UserId, "avatarFrame"));
                // אובייקט: circle — העיגול של תמונת הפרופיל, עם המסגרת של השחקן.
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
        // פעולה: Text — יוצרת תא טקסט בטבלה בגודל ובעובי שנבחרו. טקסט ארוך מדי נחתך עם "...".
        {
            Text = text,
            FontSize = size,
            FontWeight = weight,
            Foreground = Res("BerryBrush"),
            VerticalAlignment = VerticalAlignment.Center,
            TextTrimming = TextTrimming.CharacterEllipsis,
        };

        private static void Add(Grid grid, int column, FrameworkElement element)
        // פעולה: Add — מכניסה רכיב לעמודה מסוימת בשורה.
        {
            Grid.SetColumn(element, column);
            grid.Children.Add(element);
        }
    }
}
