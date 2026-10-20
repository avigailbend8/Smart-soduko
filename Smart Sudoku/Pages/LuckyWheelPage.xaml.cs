using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Shapes;

namespace Smart_Sudoku.Pages
{
    /// <summary>
    /// Two lucky wheels. The regular one has a free spin every day; the golden one has richer prizes.
    /// The prizes change after every spin, and can include store items the player doesn't own yet.
    /// Extra spins can be bought with coins.
    /// </summary>
    public sealed partial class LuckyWheelPage : Page
    {
        /// <param name="Label">Short text on the wheel slice.</param>
        /// <param name="Message">What the player is told when they win it.</param>
        /// <param name="Weight">How often it comes up (bigger = more often).</param>
        private sealed record Prize(string Label, string Message, int Weight, Action Give);

        private const double Radius = 200;
        private const int SliceCount = 8;
        private const int RegularSpinPrice = 50;
        private const int GoldenSpinPrice = 200;

        private static bool golden;
        private readonly Random random = new Random();
        private List<Prize> prizes = new List<Prize>();
        private bool spinning;

        public LuckyWheelPage()
        {
            this.InitializeComponent();
        }

        private static Brush Res(string key) => (Brush)Application.Current.Resources[key];

        private static SolidColorBrush Solid(uint argb) => new SolidColorBrush(Themes.ToColor(argb));

        private static string Today => DateTime.Now.ToString("yyyy-MM-dd");

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            NewPrizes();
        }

        // ---------- Spins ----------

        private bool FreeSpinToday => !golden && GameData.GetExtra("wheel.last") != Today;

        private string SpinsKey => golden ? "wheel.golden" : "wheel.spins";

        private int BoughtSpins => int.TryParse(GameData.GetExtra(SpinsKey, "0"), out int n) ? n : 0;

        private int SpinPrice => golden ? GoldenSpinPrice : RegularSpinPrice;

        private void UpdateState()
        {
            CoinsText.Text = $"🪙 {GameData.Coins:N0}";
            RegularTab.Background = Res(golden ? "CardBrush" : "SelectedCellBrush");
            GoldenTab.Background = golden ? Solid(0xFFFFE7A8) : Res("CardBrush");
            TitleText.Text = golden ? "👑 גלגל הזהב" : "🎡 גלגל המזל";

            int spins = BoughtSpins;
            bool canSpin = !spinning && (FreeSpinToday || spins > 0);
            SpinButton.IsEnabled = canSpin;
            SpinButton.Content = spinning ? "מסתובב... 🌀"
                : FreeSpinToday ? "סובבו! (חינם היום) 🎡"
                : spins > 0 ? $"סובבו! (נשארו {spins} סיבובים)"
                : "אין סיבובים — אפשר לקנות 👇";
            BuySpinButton.Content = $"🎟️ קנו סיבוב · {SpinPrice} 🪙";
            BuySpinButton.IsEnabled = !spinning;

            InfoText.Text = golden
                ? "פרסים שווים במיוחד: הרבה מטבעות, חבילות רמזים ולבבות, וערכות מהחנות! הפרסים מתחלפים אחרי כל סיבוב."
                : "סיבוב אחד חינם בכל יום, ואפשר לקנות עוד. הפרסים מתחלפים אחרי כל סיבוב.";
        }

        private void BuySpin_Click(object sender, RoutedEventArgs e)
        {
            if (!GameData.Spend(SpinPrice))
            {
                ResultText.Text = "אין מספיק מטבעות 😅";
                return;
            }
            GameData.SetExtra(SpinsKey, (BoughtSpins + 1).ToString());
            Sounds.Play("coin");
            ResultText.Text = "קנית סיבוב! 🎟️";
            UpdateState();
        }

        // ---------- Prizes ----------

        private Prize Coins(int amount, int weight) =>
            new Prize($"{amount} 🪙", $"זכית ב-{amount:N0} מטבעות!", weight, () => GameData.AddCoins(amount));

        private Prize Hints(int count, int weight) =>
            new Prize($"💡 ×{count}", $"זכית ב-{count} רמזים!", weight, () => GameData.BuyHints(0, count));

        private Prize Hearts(int count, int weight) =>
            new Prize($"💖 ×{count}", count == 1 ? "זכית בלב נוסף!" : $"זכית ב-{count} לבבות!", weight, () => GameData.BuyLives(0, count));

        private Prize ExtraSpin(int weight) =>
            new Prize("🎟️ +1", "זכית בסיבוב נוסף!", weight, () => GameData.SetExtra(SpinsKey, (BoughtSpins + 1).ToString()));

        /// <summary>Store items the player doesn't own yet, as prizes.</summary>
        private IEnumerable<Prize> StorePrizes(int weight)
        {
            foreach (var theme in Themes.All.Where(t => t.Price > 0 && !GameData.Owns(t.Id) && (golden || t.Price <= 120)))
            {
                string emoji = theme.Name.Split(' ').Last();
                string mark = emoji.Length <= 3 ? emoji : "🎨";
                yield return new Prize($"🎨 {mark}", $"זכית בערכת הנושא \"{theme.Name}\"! 🎉", weight, () => GameData.GiveItem(theme.Id));
            }
            foreach (var frame in BoardFrames.All.Where(f => f.Price > 0 && !GameData.Owns("frame-" + f.Id) && (golden || f.Price <= 100)))
                yield return new Prize($"🖼️ {frame.Corner}", $"זכית במסגרת \"{frame.Name}\"! 🎉", weight, () => GameData.GiveItem("frame-" + frame.Id));
            foreach (var set in SymbolSets.All.Where(s => s.Price > 0 && !GameData.Owns(s.Id)))
                yield return new Prize($"🔣 {set.Symbols[0]}", $"זכית בסמלים \"{set.Name}\"! 🎉", weight, () => GameData.GiveItem(set.Id));
        }

        /// <summary>Picks a fresh set of 8 prizes for the current wheel.</summary>
        private void NewPrizes()
        {
            var basics = golden
                ? new List<Prize> { Coins(150, 18), Coins(250, 15), Coins(400, 10), Coins(600, 6), Coins(1000, 3), Coins(2000, 1), Hints(5, 10), Hearts(3, 9), ExtraSpin(5) }
                : new List<Prize> { Coins(20, 18), Coins(30, 16), Coins(50, 14), Coins(80, 9), Coins(100, 6), Coins(200, 2), Hints(3, 8), Hearts(1, 8), ExtraSpin(5) };

            // Up to 2 (regular) or 3 (golden) store items, then fill the rest with basics
            var items = StorePrizes(golden ? 5 : 3).OrderBy(_ => random.Next()).Take(golden ? 3 : 2).ToList();
            var chosen = new List<Prize>(items);
            foreach (var prize in basics.OrderBy(_ => random.Next()))
            {
                if (chosen.Count >= SliceCount)
                    break;
                chosen.Add(prize);
            }
            prizes = chosen.OrderBy(_ => random.Next()).ToList();

            WheelRotation.Angle = 0;
            DrawWheel();
            UpdateState();
        }

        // ---------- Drawing ----------

        private void DrawWheel()
        {
            Wheel.Children.Clear();

            // Golden wheel: gold and deep purple, a glowing halo and little lights around the rim
            Brush fillA = golden ? (Brush)GoldGradient() : Res("SelectedCellBrush");
            Brush fillB = golden ? Solid(0xFF3B1F5E) : Res("CardBrush");
            Brush stroke = golden ? Solid(0xFFC98B16) : Res("RoseBrush");
            Brush textA = golden ? Solid(0xFF4A2A00) : Res("BerryBrush");
            Brush textB = golden ? Solid(0xFFFFF4D6) : Res("BerryBrush");

            WheelHalo.Opacity = golden ? 1 : 0;
            WheelHalo.Fill = Solid(0x55FFD166);
            Hub.Fill = golden ? Solid(0xFF3B1F5E) : Res("CardBrush");
            Hub.Stroke = stroke;
            HubEmoji.Text = golden ? "👑" : "💖";
            Pointer.Fill = golden ? (Brush)GoldGradient() : Res("BerryBrush");
            Pointer.Stroke = golden ? Solid(0xFF8A5A00) : null;
            Pointer.StrokeThickness = 2;

            double slice = 360.0 / prizes.Count;
            for (int i = 0; i < prizes.Count; i++)
            {
                // Slices start at the top and go clockwise
                double start = -90 + i * slice, end = start + slice;
                var figure = new PathFigure { StartPoint = new Point(Radius, Radius), IsClosed = true };
                figure.Segments.Add(new LineSegment { Point = PointAt(start, Radius) });
                figure.Segments.Add(new ArcSegment { Point = PointAt(end, Radius), Size = new Size(Radius, Radius), SweepDirection = SweepDirection.Clockwise });
                var geometry = new PathGeometry();
                geometry.Figures.Add(figure);
                Wheel.Children.Add(new Path { Data = geometry, Fill = i % 2 == 0 ? fillA : fillB, Stroke = stroke, StrokeThickness = 2 });

                // The label sits in the middle of the slice, turned to face outward
                double middle = start + slice / 2;
                var label = new TextBlock
                {
                    Text = prizes[i].Label,
                    FontSize = 19,
                    FontWeight = FontWeights.Bold,
                    Foreground = i % 2 == 0 ? textA : textB,
                    Width = 120,
                    TextAlignment = TextAlignment.Center,
                    RenderTransformOrigin = new Point(0.5, 0.5),
                    RenderTransform = new RotateTransform { Angle = middle + 90 },
                };
                var spot = PointAt(middle, Radius * 0.64);
                Canvas.SetLeft(label, spot.X - 60);
                Canvas.SetTop(label, spot.Y - 14);
                Wheel.Children.Add(label);
            }

            Wheel.Children.Add(new Ellipse { Width = Radius * 2, Height = Radius * 2, Stroke = stroke, StrokeThickness = golden ? 10 : 6 });

            if (golden)
            {
                for (int i = 0; i < 16; i++)
                {
                    var bulb = PointAt(i * 22.5, Radius - 5);
                    var light = new Ellipse { Width = 10, Height = 10, Fill = Solid(i % 2 == 0 ? 0xFFFFFDF0u : 0xFFFFD166u) };
                    Canvas.SetLeft(light, bulb.X - 5);
                    Canvas.SetTop(light, bulb.Y - 5);
                    Wheel.Children.Add(light);
                }
            }
        }

        private static LinearGradientBrush GoldGradient()
        {
            var brush = new LinearGradientBrush { StartPoint = new Point(0, 0), EndPoint = new Point(1, 1) };
            brush.GradientStops.Add(new GradientStop { Color = Themes.ToColor(0xFFFFF1B8), Offset = 0 });
            brush.GradientStops.Add(new GradientStop { Color = Themes.ToColor(0xFFF2B93B), Offset = 1 });
            return brush;
        }

        private static Point PointAt(double degrees, double distance)
        {
            double radians = degrees * Math.PI / 180;
            return new Point(Radius + distance * Math.Cos(radians), Radius + distance * Math.Sin(radians));
        }

        // ---------- Spinning ----------

        private void Spin_Click(object sender, RoutedEventArgs e)
        {
            if (spinning || !(FreeSpinToday || BoughtSpins > 0))
                return;

            // Use today's free spin first, then a bought one
            if (FreeSpinToday)
                GameData.SetExtra("wheel.last", Today);
            else
                GameData.SetExtra(SpinsKey, (BoughtSpins - 1).ToString());

            // Choose the prize first (by weight), then turn the wheel so it stops there
            int roll = random.Next(prizes.Sum(p => p.Weight));
            int index = 0;
            while (roll >= prizes[index].Weight)
            {
                roll -= prizes[index].Weight;
                index++;
            }

            spinning = true;
            ResultText.Text = "";
            UpdateState();

            double slice = 360.0 / prizes.Count;
            double stopAt = -(index * slice + slice / 2) + (random.NextDouble() - 0.5) * slice * 0.6;
            double target = 360 * 6 + ((stopAt % 360) + 360) % 360;
            var spin = new DoubleAnimation
            {
                From = 0,
                To = target,
                Duration = TimeSpan.FromSeconds(golden ? 5.5 : 4.5),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut },
            };
            Storyboard.SetTarget(spin, WheelRotation);
            Storyboard.SetTargetProperty(spin, "Angle");
            var storyboard = new Storyboard();
            storyboard.Children.Add(spin);
            var prize = prizes[index];
            storyboard.Completed += (s, args) =>
            {
                prize.Give();
                ResultText.Text = prize.Message;
                Sounds.Play("win");
                Achievements.Unlock("wheel");
                spinning = false;
                UpdateState();

                // New prizes for the next spin, after a moment to enjoy this one
                var later = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2.2) };
                later.Tick += (t, a) =>
                {
                    later.Stop();
                    if (!spinning)
                        NewPrizes();
                };
                later.Start();
            };
            Sounds.Play("hint");
            storyboard.Begin();
        }

        private void RegularTab_Click(object sender, RoutedEventArgs e)
        {
            if (spinning || !golden)
                return;
            golden = false;
            ResultText.Text = "";
            NewPrizes();
        }

        private void GoldenTab_Click(object sender, RoutedEventArgs e)
        {
            if (spinning || golden)
                return;
            golden = true;
            ResultText.Text = "";
            NewPrizes();
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            if (Frame.CanGoBack)
                Frame.GoBack();
        }
    }
}
