using System;
using System.Linq;
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
    public sealed partial class StorePage : Page
    {
        private const int HintPackPrice = 30;
        private const int HintPackSize = 3;

        public StorePage()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            RefreshStore();
        }

        private static Brush Res(string key) => (Brush)Application.Current.Resources[key];

        private void RefreshStore()
        {
            CoinsText.Text = $"🪙 {GameData.Coins}";
            BonusHintsText.Text = $"רמזים שאפשר להשתמש בהם אחרי 3 הרמזים החינמיים בכל שלב. יש לך עכשיו: {GameData.BonusHints}";
            BuyHintsButton.Content = $"{HintPackPrice} 🪙";
            BonusLivesText.Text = $"לב שמור שנכנס לפעולה לבד כשנגמרים הלבבות במשחק. יש לך עכשיו: {GameData.BonusLives}";
            BuyLifeButton.Content = $"{GamePage.HeartPrice} 🪙";

            ThemesGrid.Children.Clear();
            int index = 0;
            foreach (var theme in Themes.All)
            {
                // Preview: the theme's background colors, with its accent and text colors on top
                var gradient = new LinearGradientBrush { StartPoint = new Windows.Foundation.Point(0, 0), EndPoint = new Windows.Foundation.Point(1, 1) };
                foreach (var stop in Themes.MakeStops(theme.Background))
                    gradient.GradientStops.Add(stop);

                var dots = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
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
                        if (SymbolSets.ForTheme(theme.Id) is SymbolSet matching && GameData.Owns(matching.Id))
                            GameData.SelectSymbols(matching.Id);
                    },
                    credit: string.Join("\n", new[]
                    {
                        SymbolSets.ForTheme(theme.Id) is SymbolSet included
                            ? "כולל סמלים בלוח: " + string.Join(" ", included.Symbols[0], included.Symbols[1], included.Symbols[2])
                            : null,
                        theme.Credit,
                    }.Where(line => line != null)) is string text && text.Length > 0 ? text : null);
            }

            SymbolsGrid.Children.Clear();
            index = 0;
            foreach (var set in SymbolSets.All)
            {
                var preview = new TextBlock
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
                AddItem(SymbolsGrid, index++, preview, set.Name, set.Id, set.Price,
                    selected: GameData.SymbolsId == set.Id,
                    select: () => GameData.SelectSymbols(set.Id),
                    credit: themeName != null ? $"מתאים לערכה {themeName}" : null,
                    lockedText: set.Price == 0 && themeName != null ? $"מגיע עם {themeName}" : null);
            }
        }

        // One store tile: preview, name, and a buy / choose button. Tiles wrap into rows of 4.
        private void AddItem(Grid grid, int index, UIElement preview, string name, string id, int price, bool selected, Action select,
            string? credit = null, string? lockedText = null)
        {
            const int perRow = 4;
            while (grid.ColumnDefinitions.Count < perRow)
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            while (grid.RowDefinitions.Count <= index / perRow)
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            bool owned = GameData.Owns(id);
            var button = new Button
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                FontSize = 16,
                Padding = new Thickness(8, 8, 8, 8),
                Content = selected ? "✓ בשימוש" : owned ? "לבחירה" : lockedText ?? $"{price} 🪙",
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
                }
                select();
                RefreshStore();
            };

            var tile = new Border
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
        {
            MessageText.Text = text;
            MessageText.Visibility = Visibility.Visible;
        }

        private void BuyHintsButton_Click(object sender, RoutedEventArgs e)
        {
            if (!GameData.BuyHints(HintPackPrice, HintPackSize))
            {
                ShowMessage("אין מספיק מטבעות 😅 — אפשר להרוויח עוד בשלבים");
                return;
            }
            Sounds.Play("coin");
            ShowMessage("קנית 3 רמזים! 💡");
            RefreshStore();
        }

        private void BuyLifeButton_Click(object sender, RoutedEventArgs e)
        {
            if (!GameData.BuyLives(GamePage.HeartPrice, 1))
            {
                ShowMessage("אין מספיק מטבעות 😅 — אפשר להרוויח עוד בשלבים");
                return;
            }
            Sounds.Play("coin");
            ShowMessage("קנית לב נוסף! 💖");
            RefreshStore();
        }

        private void GiftButton_Click(object sender, RoutedEventArgs e)
        {
            GiftPanel.Visibility = GiftPanel.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
            if (GiftPanel.Visibility == Visibility.Visible)
                GiftCodeBox.Focus(FocusState.Programmatic);
        }

        private void GiftCodeBox_KeyDown(object sender, Windows.UI.Xaml.Input.KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
                RedeemGift_Click(sender, e);
        }

        private void RedeemGift_Click(object sender, RoutedEventArgs e)
        {
            switch (GameData.RedeemGiftCode(GiftCodeBox.Password))
            {
                case GameData.GiftResult.Granted:
                    Sounds.Play("coin");
                    GiftResultText.Text = $"🍫 קיבלת {GameData.GiftCoins} מטבעות!";
                    GiftPanel.Visibility = Visibility.Collapsed;
                    break;
                case GameData.GiftResult.AlreadyUsed:
                    GiftResultText.Text = "כבר השתמשת בסיסמה הזאת 🙂";
                    break;
                default:
                    GiftResultText.Text = "סיסמה לא נכונה";
                    break;
            }
            GiftCodeBox.Password = "";
            RefreshStore();
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            if (Frame.CanGoBack)
                Frame.GoBack();
        }
    }
}
