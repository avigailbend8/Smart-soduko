using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace Smart_Sudoku.Pages
{
    /// <summary>Small building blocks for pages that build their content in code (missions, stats, friends...).</summary>
    internal static class Ui
    // אובייקט: Ui — "קופסת כלים" עם אבני בניין קטנות לעמודים שבונים את התוכן שלהם בקוד (משימות, סטטיסטיקות, חברים...).
    {
        public static Brush Res(string key) => (Brush)Application.Current.Resources[key];
        // פעולה: Res — מחזירה צבע ("מברשת") מתוך App.xaml לפי השם שלו.

        public static TextBlock Text(string text, double size = 16, string brush = "BerryBrush", bool bold = false) => new TextBlock
        // פעולה: Text — יוצרת שורת טקסט מוכנה: גודל, צבע, ומודגש או לא.
        {
            Text = text,
            FontSize = size,
            FontWeight = bold ? FontWeights.Bold : FontWeights.Normal,
            Foreground = Res(brush),
            TextWrapping = TextWrapping.Wrap,
            VerticalAlignment = VerticalAlignment.Center,
        };

        /// <summary>A soft rounded card, like the other pages' cards.</summary>
        public static Border Card(UIElement content, bool highlight = false) => new Border
        // פעולה: Card — יוצרת "כרטיס" רך עם פינות מעוגלות, כמו הכרטיסים בשאר העמודים. highlight = כרטיס מודגש.
        {
            Background = Res(highlight ? "SelectedCellBrush" : "CardBrush"),
            BorderBrush = Res(highlight ? "RoseBrush" : "SoftPinkBrush"),
            BorderThickness = new Thickness(highlight ? 2 : 1),
            CornerRadius = new CornerRadius(18),
            Padding = new Thickness(18, 14, 18, 14),
            Child = content,
        };

        /// <summary>A rounded progress bar from 0 to 1.</summary>
        public static Grid Bar(double fraction, double height = 14)
        // פעולה: Bar — יוצרת פס התקדמות מעוגל. fraction = כמה מהפס מלא (מ-0 עד 1).
        {
            fraction = System.Math.Max(0, System.Math.Min(1, fraction));
            var bar = new Grid { Height = height };
            // אובייקט: bar — הפס עצמו, שמחולק לחלק המלא ולחלק הריק.
            bar.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(fraction, GridUnitType.Star) });
            bar.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1 - fraction, GridUnitType.Star) });
            var back = new Border { Background = Res("RelatedCellBrush"), CornerRadius = new CornerRadius(height / 2) };
            // אובייקט: back — הרקע של הפס (החלק הריק).
            Grid.SetColumnSpan(back, 2);
            bar.Children.Add(back);
            if (fraction > 0)
                bar.Children.Add(new Border { Background = Res("RoseBrush"), CornerRadius = new CornerRadius(height / 2) });
            return bar;
        }

        /// <summary>Two items side by side: content that stretches, and something (a button) at the end.</summary>
        public static Grid Row(UIElement main, UIElement? end, UIElement? start = null)
        // פעולה: Row — יוצרת שורה עם שני חלקים זה לצד זה: תוכן שמתמתח, ומשהו בסוף (למשל כפתור).
        {
            var row = new Grid { ColumnSpacing = 16 };
            // אובייקט: row — השורה, עם שלוש עמודות: התחלה, אמצע וסוף.
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            if (start != null)
                row.Children.Add(start);
            Grid.SetColumn((FrameworkElement)main, 1);
            row.Children.Add(main);
            if (end != null)
            {
                Grid.SetColumn((FrameworkElement)end, 2);
                row.Children.Add(end);
            }
            return row;
        }

        public static string Time(int seconds) =>
        // פעולה: Time — הופכת מספר שניות לשעון, למשל 125 → "02:05".
            seconds >= 3600 ? $"{seconds / 3600}:{seconds / 60 % 60:00}:{seconds % 60:00}" : $"{seconds / 60:00}:{seconds % 60:00}";
    }
}
