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
    public sealed partial class LuckyWheelPage : BasePage
    // אובייקט: LuckyWheelPage — עמוד גלגלי המזל. בגלגל הרגיל יש סיבוב חינם כל יום, ובגלגל הזהב יש פרסים שווים יותר.
    // הפרסים מתחלפים אחרי כל סיבוב, ויכולים לכלול פריטים מהחנות. אפשר לקנות סיבובים נוספים במטבעות.
    {
        /// <param name="Label">Short text on the wheel slice.</param>
        /// <param name="Message">What the player is told when they win it.</param>
        /// <param name="Weight">How often it comes up (bigger = more often).</param>
        private sealed record Prize(string Label, string Message, int Weight, Action Give);
        // אובייקט: Prize — פרס אחד בגלגל: מה כתוב על הפלח, מה אומרים לשחקן כשזכה, כמה הוא נפוץ (Weight), ומה הוא נותן (Give).

        private const double Radius = 200;
        // אובייקט: Radius — הרדיוס של הגלגל (חצי מהקוטר).
        private const int SliceCount = 8;
        // אובייקט: SliceCount — כמה פלחים יש בגלגל (8).
        private const int RegularSpinPrice = 50;
        // אובייקט: RegularSpinPrice — המחיר של סיבוב בגלגל הרגיל (50 מטבעות).
        private const int GoldenSpinPrice = 200;
        // אובייקט: GoldenSpinPrice — המחיר של סיבוב בגלגל הזהב (200 מטבעות).

        private static bool golden;
        // אובייקט: golden — אמת כשמוצג גלגל הזהב, ושקר כשמוצג הגלגל הרגיל.
        private readonly Random random = new Random();
        // אובייקט: random — מחולל אקראי לבחירת הפרסים והזכייה.
        private List<Prize> prizes = new List<Prize>();
        // אובייקט: prizes — שמונת הפרסים שעל הגלגל עכשיו.
        private bool spinning;
        // אובייקט: spinning — אמת כשהגלגל מסתובב (כדי שאי אפשר יהיה ללחוץ שוב באמצע).

        public LuckyWheelPage()
        // פעולה: LuckyWheelPage (בנאי) — טוענת את העיצוב של העמוד מקובץ ה-XAML.
        {
            this.InitializeComponent();
        }

        private static SolidColorBrush Solid(uint argb) => new SolidColorBrush(Themes.ToColor(argb));
        // פעולה: Solid — יוצרת מברשת בצבע אחיד ממספר צבע.

        private static string Today => DateTime.Now.ToString("yyyy-MM-dd");
        // אובייקט: Today — התאריך של היום, כטקסט.

        protected override void OnNavigatedTo(NavigationEventArgs e)
        // פעולה: OnNavigatedTo — רצה כשנכנסים לעמוד: בוחרת פרסים ומציירת את הגלגל.
        {
            base.OnNavigatedTo(e);
            NewPrizes();
        }

        // ---------- Spins ----------

        private bool FreeSpinToday => !golden && GameData.GetExtra("wheel.last") != Today;
        // אובייקט: FreeSpinToday — אמת אם עוד לא השתמשו היום בסיבוב החינמי (רק בגלגל הרגיל).

        private string SpinsKey => golden ? "wheel.golden" : "wheel.spins";
        // אובייקט: SpinsKey — השם שתחתיו נשמר מספר הסיבובים שנקנו (נפרד לכל גלגל).

        private int BoughtSpins => int.TryParse(GameData.GetExtra(SpinsKey, "0"), out int n) ? n : 0;
        // אובייקט: BoughtSpins — כמה סיבובים קנויים נשארו.

        private int SpinPrice => golden ? GoldenSpinPrice : RegularSpinPrice;
        // אובייקט: SpinPrice — המחיר של סיבוב בגלגל הנוכחי.

        private void UpdateState()
        // פעולה: UpdateState — מעדכנת את המסך: מטבעות, איזו לשונית מודגשת, הכותרת, ומה כתוב על כפתור הסיבוב והקנייה.
        {
            CoinsText.Text = $"🪙 {GameData.Coins:N0}";
            RegularTab.Background = Res(golden ? "CardBrush" : "SelectedCellBrush");
            GoldenTab.Background = golden ? Solid(0xFFFFE7A8) : Res("CardBrush");
            TitleText.Text = golden ? "👑 גלגל הזהב" : "🎡 גלגל המזל";

            int spins = BoughtSpins;
            // אובייקט: spins — כמה סיבובים קנויים יש.
            bool canSpin = !spinning && (FreeSpinToday || spins > 0);
            // אובייקט: canSpin — אמת אם אפשר לסובב עכשיו.
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
        // פעולה: BuySpin_Click — קונה סיבוב נוסף במטבעות.
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
        // פעולה: Coins — יוצרת פרס של מטבעות.
            new Prize($"{amount} 🪙", $"זכית ב-{amount:N0} מטבעות!", weight, () => GameData.AddCoins(amount));

        private Prize Hints(int count, int weight) =>
        // פעולה: Hints — יוצרת פרס של רמזים.
            new Prize($"💡 ×{count}", $"זכית ב-{count} רמזים!", weight, () => GameData.BuyHints(0, count));

        private Prize Hearts(int count, int weight) =>
        // פעולה: Hearts — יוצרת פרס של לבבות.
            new Prize($"💖 ×{count}", count == 1 ? "זכית בלב נוסף!" : $"זכית ב-{count} לבבות!", weight, () => GameData.BuyLives(0, count));

        private Prize ExtraSpin(int weight) =>
        // פעולה: ExtraSpin — יוצרת פרס של סיבוב נוסף.
            new Prize("🎟️ +1", "זכית בסיבוב נוסף!", weight, () => GameData.SetExtra(SpinsKey, (BoughtSpins + 1).ToString()));

        /// <summary>Store items the player doesn't own yet, as prizes.</summary>
        private IEnumerable<Prize> StorePrizes(int weight)
        // פעולה: StorePrizes — יוצרת פרסים מהחנות: ערכות, מסגרות וסמלים שהשחקן עוד לא קנה.
        {
            foreach (var theme in Themes.All.Where(t => t.Price > 0 && !GameData.Owns(t.Id) && (golden || t.Price <= 120)))
            {
                string emoji = theme.Name.Split(' ').Last();
                // אובייקט: emoji — האימוג'י שבסוף השם של ערכת העיצוב.
                string mark = emoji.Length <= 3 ? emoji : "🎨";
                // אובייקט: mark — מה שיוצג על הפלח (האימוג'י, או 🎨 אם הוא ארוך מדי).
                yield return new Prize($"🎨 {mark}", $"זכית בערכת הנושא \"{theme.Name}\"! 🎉", weight, () => GameData.GiveItem(theme.Id));
            }
            foreach (var frame in BoardFrames.All.Where(f => f.Price > 0 && !GameData.Owns("frame-" + f.Id) && (golden || f.Price <= 100)))
                yield return new Prize($"🖼️ {frame.Corner}", $"זכית במסגרת \"{frame.Name}\"! 🎉", weight, () => GameData.GiveItem("frame-" + frame.Id));
            foreach (var set in SymbolSets.All.Where(s => s.Price > 0 && !GameData.Owns(s.Id)))
                yield return new Prize($"🔣 {set.Symbols[0]}", $"זכית בסמלים \"{set.Name}\"! 🎉", weight, () => GameData.GiveItem(set.Id));
        }

        /// <summary>Picks a fresh set of 8 prizes for the current wheel.</summary>
        private void NewPrizes()
        // פעולה: NewPrizes — בוחרת 8 פרסים חדשים לגלגל: עד 2–3 פריטים מהחנות, והשאר מטבעות, רמזים, לבבות וסיבובים. ואז מציירת את הגלגל.
        {
            var basics = golden
                ? new List<Prize> { Coins(150, 18), Coins(250, 15), Coins(400, 10), Coins(600, 6), Coins(1000, 3), Coins(2000, 1), Hints(5, 10), Hearts(3, 9), ExtraSpin(5) }
                : new List<Prize> { Coins(20, 18), Coins(30, 16), Coins(50, 14), Coins(80, 9), Coins(100, 6), Coins(200, 2), Hints(3, 8), Hearts(1, 8), ExtraSpin(5) };
                // אובייקט: basics — הפרסים הרגילים של הגלגל (בגלגל הזהב הם גדולים יותר).

            // Up to 2 (regular) or 3 (golden) store items, then fill the rest with basics
            var items = StorePrizes(golden ? 5 : 3).OrderBy(_ => random.Next()).Take(golden ? 3 : 2).ToList();
            // אובייקט: items — הפרסים מהחנות שנבחרו לגלגל.
            var chosen = new List<Prize>(items);
            // אובייקט: chosen — כל הפרסים שנבחרו עד עכשיו.
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
        // פעולה: DrawWheel — מציירת את הגלגל: 8 פלחים בשני צבעים לסירוגין, הטקסט של כל פרס, והמסגרת.
        // בגלגל הזהב יש גם הילה זוהרת ונורות קטנות מסביב.
        {
            Wheel.Children.Clear();

            // Golden wheel: gold and deep purple, a glowing halo and little lights around the rim
            Brush fillA = golden ? (Brush)GoldGradient() : Res("SelectedCellBrush");
            // אובייקט: fillA, fillB — שני הצבעים של הפלחים, לסירוגין.
            Brush fillB = golden ? Solid(0xFF3B1F5E) : Res("CardBrush");
            Brush stroke = golden ? Solid(0xFFC98B16) : Res("RoseBrush");
            // אובייקט: stroke — צבע הקווים בין הפלחים.
            Brush textA = golden ? Solid(0xFF4A2A00) : Res("BerryBrush");
            // אובייקט: textA, textB — צבעי הטקסט על הפלחים.
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
            // אובייקט: slice — הזווית של כל פלח (360 חלקי מספר הפלחים).
            for (int i = 0; i < prizes.Count; i++)
            {
                // Slices start at the top and go clockwise
                double start = -90 + i * slice, end = start + slice;
                // אובייקט: start, end — הזווית שבה הפלח מתחיל ונגמר.
                var figure = new PathFigure { StartPoint = new Point(Radius, Radius), IsClosed = true };
                // אובייקט: figure — הצורה של פלח: מהמרכז, אל השפה, לאורך הקשת, ובחזרה למרכז.
                figure.Segments.Add(new LineSegment { Point = PointAt(start, Radius) });
                figure.Segments.Add(new ArcSegment { Point = PointAt(end, Radius), Size = new Size(Radius, Radius), SweepDirection = SweepDirection.Clockwise });
                var geometry = new PathGeometry();
                // אובייקט: geometry — הצורה הסופית של הפלח, מוכנה לציור.
                geometry.Figures.Add(figure);
                Wheel.Children.Add(new Path { Data = geometry, Fill = i % 2 == 0 ? fillA : fillB, Stroke = stroke, StrokeThickness = 2 });

                // The label sits in the middle of the slice, turned to face outward
                double middle = start + slice / 2;
                // אובייקט: middle — הזווית של אמצע הפלח, שבה שמים את הטקסט.
                var label = new TextBlock
                // אובייקט: label — הטקסט של הפרס, מסובב כך שיפנה החוצה.
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
                // אובייקט: spot — הנקודה שבה מניחים את הטקסט.
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
                    // אובייקט: bulb — המקום של נורה על שפת גלגל הזהב.
                    var light = new Ellipse { Width = 10, Height = 10, Fill = Solid(i % 2 == 0 ? 0xFFFFFDF0u : 0xFFFFD166u) };
                    // אובייקט: light — נורה קטנה על שפת גלגל הזהב.
                    Canvas.SetLeft(light, bulb.X - 5);
                    Canvas.SetTop(light, bulb.Y - 5);
                    Wheel.Children.Add(light);
                }
            }
        }

        private static LinearGradientBrush GoldGradient()
        // פעולה: GoldGradient — יוצרת מברשת זהב מדורגת.
        {
            var brush = new LinearGradientBrush { StartPoint = new Point(0, 0), EndPoint = new Point(1, 1) };
            // אובייקט: brush — המברשת המדורגת.
            brush.GradientStops.Add(new GradientStop { Color = Themes.ToColor(0xFFFFF1B8), Offset = 0 });
            brush.GradientStops.Add(new GradientStop { Color = Themes.ToColor(0xFFF2B93B), Offset = 1 });
            return brush;
        }

        private static Point PointAt(double degrees, double distance)
        // פעולה: PointAt — מחשבת נקודה במרחק ובזווית מסוימים ממרכז הגלגל (בעזרת סינוס וקוסינוס).
        {
            double radians = degrees * Math.PI / 180;
            // אובייקט: radians — הזווית ברדיאנים (היחידה שהחישוב צריך).
            return new Point(Radius + distance * Math.Cos(radians), Radius + distance * Math.Sin(radians));
        }

        // ---------- Spinning ----------

        private void Spin_Click(object sender, RoutedEventArgs e)
        // פעולה: Spin_Click — כפתור "סובבו": משתמשת בסיבוב, בוחרת מראש את הפרס לפי הסיכויים,
        // ומסובבת את הגלגל כך שייעצר בדיוק עליו. בסוף נותנת את הפרס, ואחרי רגע מחליפה פרסים.
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
            // אובייקט: roll — מספר אקראי שקובע איזה פרס יצא (פרס נפוץ תופס יותר מספרים).
            int index = 0;
            // אובייקט: index — מספר הפרס שנבחר.
            while (roll >= prizes[index].Weight)
            {
                roll -= prizes[index].Weight;
                index++;
            }

            spinning = true;
            ResultText.Text = "";
            UpdateState();

            double slice = 360.0 / prizes.Count;
            // אובייקט: slice — הזווית של כל פלח.
            double stopAt = -(index * slice + slice / 2) + (random.NextDouble() - 0.5) * slice * 0.6;
            // אובייקט: stopAt — הזווית שבה הגלגל צריך לעצור (קצת אקראית בתוך הפלח).
            double target = 360 * 6 + ((stopAt % 360) + 360) % 360;
            // אובייקט: target — כמה מעלות הגלגל מסתובב בסך הכול: 6 סיבובים מלאים, ועוד עד העצירה.
            var spin = new DoubleAnimation
            // אובייקט: spin — האנימציה של הסיבוב, שמאטה לאט בסוף.
            {
                From = 0,
                To = target,
                Duration = TimeSpan.FromSeconds(golden ? 5.5 : 4.5),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut },
            };
            Storyboard.SetTarget(spin, WheelRotation);
            Storyboard.SetTargetProperty(spin, "Angle");
            var storyboard = new Storyboard();
            // אובייקט: storyboard — ה"תסריט" שמריץ את האנימציה.
            storyboard.Children.Add(spin);
            var prize = prizes[index];
            // אובייקט: prize — הפרס שבו הגלגל ייעצר.
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
                // אובייקט: later — שעון שמחכה 2.2 שניות לפני שמחליפים פרסים.
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
        // פעולה: RegularTab_Click — לשונית "גלגל רגיל": עוברת לגלגל הרגיל.
        {
            if (spinning || !golden)
                return;
            golden = false;
            ResultText.Text = "";
            NewPrizes();
        }

        private void GoldenTab_Click(object sender, RoutedEventArgs e)
        // פעולה: GoldenTab_Click — לשונית "גלגל זהב": עוברת לגלגל הזהב.
        {
            if (spinning || golden)
                return;
            golden = true;
            ResultText.Text = "";
            NewPrizes();
        }
    }
}
