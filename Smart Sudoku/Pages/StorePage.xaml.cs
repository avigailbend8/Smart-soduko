using System;
using System.Collections.Generic;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Shapes;

namespace Smart_Sudoku.Pages
{
    /// <summary>
    /// Spend coins on extra hints, color themes and symbol sets.
    /// </summary>
    public sealed partial class StorePage : BasePage
    // אובייקט: StorePage — החנות: קונים במטבעות רמזים, לבבות, ערכות עיצוב, מסגרות ללוח, סמלים, קמעות ומסגרות לתמונה.
    {
        public const int HintPackPrice = 30;
        // אובייקט: HintPackPrice — המחיר של חבילת רמזים (30 מטבעות).
        private const int HintPackSize = 3;
        // אובייקט: HintPackSize — כמה רמזים יש בחבילה (3).

        public StorePage()
        // פעולה: StorePage (בנאי) — טוענת את העיצוב של העמוד מקובץ ה-XAML.
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        // פעולה: OnNavigatedTo — רצה כשנכנסים לעמוד, ובונה את החנות.
        {
            base.OnNavigatedTo(e);
            RefreshStore();
        }

        private void RefreshStore()
        // פעולה: RefreshStore — בונה מחדש את כל החנות: המטבעות, הרמזים והלבבות, ומשבצת לכל פריט בכל קטגוריה.
        // היא רצה שוב אחרי כל קנייה, כדי שהמסך יתעדכן.
        {
            CoinsText.Text = $"🪙 {GameData.Coins}";
            BonusHintsText.Text = $"רמזים שאפשר להשתמש בהם אחרי 3 הרמזים החינמיים בכל שלב. יש לך עכשיו: {GameData.BonusHints}";
            BuyHintsButton.Content = $"{HintPackPrice} 🪙";
            BonusLivesText.Text = $"לב שמור שנכנס לפעולה לבד כשנגמרים הלבבות במשחק. יש לך עכשיו: {GameData.BonusLives}";
            BuyLifeButton.Content = $"{GamePage.HeartPrice} 🪙";

            ThemesGrid.Children.Clear();
            int index = 0;
            // אובייקט: index — מספר הפריט בקטגוריה, כדי לסדר את הפריטים בשורות של 4.
            foreach (var theme in Themes.All)
            {
                // Preview: the theme's background colors, with its accent and text colors on top
                var gradient = new LinearGradientBrush { StartPoint = new Windows.Foundation.Point(0, 0), EndPoint = new Windows.Foundation.Point(1, 1) };
                // אובייקט: gradient — מברשת עם צבעי הרקע של ערכת העיצוב, בשביל התצוגה המקדימה.
                foreach (var stop in Themes.MakeStops(theme.Background))
                    gradient.GradientStops.Add(stop);

                var dots = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
                // אובייקט: dots — שתי נקודות צבע שמראות את הצבעים העיקריים של הערכה.
                foreach (uint color in new[] { theme.Accent, theme.Dark })
                {
                    dots.Children.Add(new Ellipse
                    {
                        Width = 20,
                        Height = 20,
                        Fill = new SolidColorBrush(Themes.ToColor(color)),
                        Stroke = new SolidColorBrush(Windows.UI.Colors.White),
                        StrokeThickness = 2,
                    });
                }

                // Picture themes show their first illustration; decorated themes show a few decorations
                UIElement previewContent = dots;
                // אובייקט: previewContent — מה שמוצג בתצוגה המקדימה: איור, אימוג'ים או נקודות צבע.
                if (theme.Pictures != null)
                {
                    previewContent = new Image
                    {
                        Source = new Windows.UI.Xaml.Media.Imaging.BitmapImage(new Uri($"ms-appx:///Assets/Characters/{theme.Pictures[0]}")),
                        Stretch = Stretch.UniformToFill,
                    };
                }
                else if (theme.Decorations != null)
                {
                    previewContent = new TextBlock
                    {
                        Text = string.Join(" ", theme.Decorations[0], theme.Decorations[1], theme.Decorations[2]),
                        FontSize = 22,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                    };
                }

                var preview = new Border
                // אובייקט: preview — התצוגה המקדימה של ערכת העיצוב.
                {
                    Height = theme.Pictures != null ? 110 : 54,
                    CornerRadius = new CornerRadius(12),
                    Background = gradient,
                    BorderBrush = new SolidColorBrush(Themes.ToColor(theme.Soft)),
                    BorderThickness = new Thickness(1),
                    Child = previewContent,
                };

                AddItem(ThemesGrid, index++, preview, theme.Name, theme.Id, theme.Price,
                    selected: GameData.ThemeId == theme.Id,
                    select: () =>
                    {
                        GameData.SelectTheme(theme.Id);
                        Themes.Apply(theme.Id);

                        // Switch the board to the theme's own symbols too
                        SymbolSet? matching = SymbolSets.ForTheme(theme.Id);
                        // אובייקט: matching — ערכת הסמלים שמתאימה לעיצוב (אם יש כזאת).
                        if (matching != null && GameData.Owns(matching.Id))
                            GameData.SelectSymbols(matching.Id);
                    },
                    credit: ThemeCredit(theme));
            }

            FramesGrid.Children.Clear();
            index = 0;
            foreach (var frame in BoardFrames.All)
            {
                // Preview: a small board corner with the frame's border and decoration
                var sample = new Grid { Height = 70 };
                // אובייקט: sample — תצוגה מקדימה של מסגרת: פינה קטנה של לוח עם המסגרת והקישוט.
                sample.Children.Add(new Border
                {
                    Margin = new Thickness(10),
                    CornerRadius = new CornerRadius(10),
                    Background = Res("CardBrush"),
                    BorderThickness = new Thickness(frame.Thickness),
                    BorderBrush = frame.Colors == null ? Res("RoseBrush") : BoardFrames.Gradient(frame.Colors),
                    Child = new TextBlock
                    {
                        Text = "1 2 3",
                        FontWeight = FontWeights.Bold,
                        Foreground = Res("BerryBrush"),
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                        FlowDirection = FlowDirection.LeftToRight,
                    },
                });
                if (frame.Corner != null)
                {
                    sample.Children.Add(new TextBlock { Text = frame.Corner, FontSize = 18, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top });
                    sample.Children.Add(new TextBlock { Text = frame.Corner, FontSize = 18, HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Bottom });
                }

                AddItem(FramesGrid, index++, sample, frame.Name, "frame-" + frame.Id, frame.Price,
                    selected: GameData.FrameId == frame.Id,
                    select: () => GameData.FrameId = frame.Id);
            }

            SymbolsGrid.Children.Clear();
            index = 0;
            foreach (var set in SymbolSets.All)
            {
                var preview = new TextBlock
                // אובייקט: preview — שלושת הסמלים הראשונים של הערכה.
                {
                    Text = string.Join(" ", set.Symbols[0], set.Symbols[1], set.Symbols[2]),
                    FontSize = 24,
                    FontWeight = FontWeights.Bold,
                    Foreground = Res("BerryBrush"),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    FlowDirection = FlowDirection.LeftToRight,
                };

                // Theme-only sets can't be bought on their own; they come with their theme
                string? themeName = set.ThemeId != null ? Themes.Get(set.ThemeId).Name : null;
                // אובייקט: themeName — שם ערכת העיצוב שהסמלים שייכים לה (אם יש).
                AddItem(SymbolsGrid, index++, preview, set.Name, set.Id, set.Price,
                    selected: GameData.SymbolsId == set.Id,
                    select: () => GameData.SelectSymbols(set.Id),
                    credit: themeName != null ? $"מתאים לערכה {themeName}" : null,
                    lockedText: set.Price == 0 && themeName != null ? $"מגיע עם {themeName}" : null);
            }

            MascotsGrid.Children.Clear();
            index = 0;
            foreach (var mascot in Cosmetics.Mascots)
            {
                var preview = new TextBlock
                // אובייקט: preview — שלושה פרצופים של הקמע.
                {
                    Text = string.Join(" ", mascot.Faces["😊"], mascot.Faces["🥳"], mascot.Faces["😢"]),
                    FontSize = 26,
                    HorizontalAlignment = HorizontalAlignment.Center,
                };
                AddItem(MascotsGrid, index++, preview, mascot.Name, "mascot-" + mascot.Id, mascot.Price,
                    selected: Cosmetics.CurrentMascot.Id == mascot.Id,
                    select: () => GameData.SetExtra("mascot", mascot.Id));
            }

            AvatarFramesGrid.Children.Clear();
            index = 0;
            foreach (var frame in Cosmetics.AvatarFrames)
            {
                var preview = new Grid { Height = 70 };
                // אובייקט: preview — תצוגה מקדימה של מסגרת התמונה.
                var circle = Avatars.Circle(null, Session.PlayerName ?? "🙂", 58, frame.Id);
                // אובייקט: circle — עיגול לדוגמה עם המסגרת.
                circle.HorizontalAlignment = HorizontalAlignment.Center;
                preview.Children.Add(circle);
                AddItem(AvatarFramesGrid, index++, preview, frame.Name, "aframe-" + frame.Id, frame.Price,
                    selected: Cosmetics.CurrentFrameId == frame.Id,
                    select: () => GameData.SetExtra("avatarFrame", frame.Id));
            }
        }

        // The small lines under a theme: which board symbols it includes, and who drew its pictures
        private static string? ThemeCredit(ThemePalette theme)
        // פעולה: ThemeCredit — בונה את השורות הקטנות מתחת לערכת עיצוב: אילו סמלים היא כוללת, ומי צייר את האיורים.
        {
            var lines = new List<string>();
            // אובייקט: lines — השורות של הטקסט.
            SymbolSet? included = SymbolSets.ForTheme(theme.Id);
            // אובייקט: included — ערכת הסמלים שמגיעה עם העיצוב.
            if (included != null)
                lines.Add("כולל סמלים בלוח: " + included.Symbols[0] + " " + included.Symbols[1] + " " + included.Symbols[2]);
            if (theme.Credit != null)
                lines.Add(theme.Credit);

            if (lines.Count == 0)
                return null;
            return string.Join("\n", lines);
        }

        // One store tile: preview, name, and a buy / choose button. Tiles wrap into rows of 4.
        private void AddItem(Grid grid, int index, UIElement preview, string name, string id, int price, bool selected, Action select,
            string? credit = null, string? lockedText = null)
            // פעולה: AddItem — בונה משבצת אחת בחנות: תצוגה מקדימה, שם, וכפתור. הכפתור קונה (אם אין מספיק מטבעות — מודיע),
            // או בוחר פריט שכבר קנו. המשבצות מסתדרות בשורות של 4.
        {
            const int perRow = 4;
            // אובייקט: perRow — כמה משבצות בכל שורה.
            while (grid.ColumnDefinitions.Count < perRow)
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            while (grid.RowDefinitions.Count <= index / perRow)
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // Free items (like the classic frame) are owned from the start
            bool owned = GameData.Owns(id) || (price == 0 && lockedText == null);
            // אובייקט: owned — אמת אם הפריט כבר שייך לשחקן (פריטים חינמיים שייכים מההתחלה).
            string buttonText;
            // אובייקט: buttonText — מה כתוב על הכפתור: "בשימוש", "לבחירה", או המחיר.
            if (selected)
                buttonText = "✓ בשימוש";
            else if (owned)
                buttonText = "לבחירה";
            else if (lockedText != null)
                buttonText = lockedText;
            else
                buttonText = $"{price} 🪙";

            var button = new Button
            // אובייקט: button — כפתור הקנייה או הבחירה.
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                FontSize = 16,
                Padding = new Thickness(8, 8, 8, 8),
                Content = buttonText,
                IsEnabled = !selected && (owned || lockedText == null),
            };
            button.Click += (s, e) =>
            {
                if (!GameData.Owns(id))
                {
                    if (!GameData.Buy(id, price))
                    {
                        ShowMessage("אין מספיק מטבעות 😅 — אפשר להרוויח עוד בשלבים");
                        return;
                    }
                    Sounds.Play("coin");
                    ShowMessage($"קנית את \"{name}\"! 🎉");
                    Achievements.Unlock("shopper");
                }
                select();
                RefreshStore();
            };

            var tile = new Border
            // אובייקט: tile — המשבצת עצמה בחנות, עם המסגרת והפינות המעוגלות.
            {
                Background = Res(selected ? "RelatedCellBrush" : "ButtonHoverBrush"),
                BorderBrush = Res(selected ? "RoseBrush" : "SoftPinkBrush"),
                BorderThickness = new Thickness(selected ? 2 : 1),
                CornerRadius = new CornerRadius(18),
                Padding = new Thickness(12, 16, 12, 12),
                Child = new StackPanel
                {
                    Spacing = 10,
                    Children =
                    {
                        preview,
                        new TextBlock
                        {
                            Text = name,
                            FontSize = 18,
                            FontWeight = FontWeights.SemiBold,
                            Foreground = Res("BerryBrush"),
                            HorizontalAlignment = HorizontalAlignment.Center,
                        },
                        button,
                    },
                },
            };

            // Small credit line for the illustrators
            if (credit != null)
            {
                ((StackPanel)tile.Child).Children.Insert(2, new TextBlock
                {
                    Text = credit,
                    FontSize = 11,
                    Foreground = Res("MutedTextBrush"),
                    TextWrapping = TextWrapping.Wrap,
                    TextAlignment = TextAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center,
                });
            }

            Grid.SetRow(tile, index / 4);
            Grid.SetColumn(tile, index % 4);
            grid.Children.Add(tile);
        }

        private void ShowMessage(string text)
        // פעולה: ShowMessage — מציגה הודעה בראש החנות (למשל "קנית!" או "אין מספיק מטבעות").
        {
            MessageText.Text = text;
            MessageText.Visibility = Visibility.Visible;
        }

        private void BuyHintsButton_Click(object sender, RoutedEventArgs e)
        // פעולה: BuyHintsButton_Click — קונה חבילה של 3 רמזים.
        {
            if (!GameData.BuyHints(HintPackPrice, HintPackSize))
            {
                ShowMessage("אין מספיק מטבעות 😅 — אפשר להרוויח עוד בשלבים");
                return;
            }
            Sounds.Play("coin");
            ShowMessage("קנית 3 רמזים! 💡");
            Achievements.Unlock("shopper");
            RefreshStore();
        }

        private void BuyLifeButton_Click(object sender, RoutedEventArgs e)
        // פעולה: BuyLifeButton_Click — קונה לב נוסף.
        {
            if (!GameData.BuyLives(GamePage.HeartPrice, 1))
            {
                ShowMessage("אין מספיק מטבעות 😅 — אפשר להרוויח עוד בשלבים");
                return;
            }
            Sounds.Play("coin");
            ShowMessage("קנית לב נוסף! 💖");
            Achievements.Unlock("shopper");
            RefreshStore();
        }

        private void GiftButton_Click(object sender, RoutedEventArgs e)
        // פעולה: GiftButton_Click — כפתור "טוויקס לבן": פותח או סוגר את האזור שבו מקלידים קוד סודי.
        {
            GiftPanel.Visibility = GiftPanel.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
            if (GiftPanel.Visibility == Visibility.Visible)
                GiftCodeBox.Focus(FocusState.Programmatic);
        }

        private void GiftCodeBox_KeyDown(object sender, Windows.UI.Xaml.Input.KeyRoutedEventArgs e)
        // פעולה: GiftCodeBox_KeyDown — כשלוחצים Enter בתיבת הקוד — בודקים את הקוד.
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
                RedeemGift_Click(sender, e);
        }

        private void RedeemGift_Click(object sender, RoutedEventArgs e)
        // פעולה: RedeemGift_Click — בודקת את הקוד הסודי. אם הוא נכון — נותנת את המטבעות שלו.
        {
            int coins = GameData.RedeemGiftCode(GiftCodeBox.Password);
            // אובייקט: coins — כמה מטבעות הקוד נתן (0 = קוד שגוי).
            if (coins > 0)
            {
                Sounds.Play("coin");
                GiftResultText.Text = $"🍫 קיבלת {coins} מטבעות!";
            }
            else
            {
                GiftResultText.Text = "סיסמה לא נכונה";
            }
            GiftCodeBox.Password = "";
            RefreshStore();
        }
    }
}
