using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace Smart_Sudoku.Pages
{
    /// <summary>
    /// The base class of every page in the game (inheritance: every page "is a" BasePage, and a BasePage "is a" Page).
    /// Things that all the pages need are written here once, instead of being copied into each page.
    /// </summary>
    public partial class BasePage : Page
    // אובייקט: BasePage — מחלקת הבסיס של כל העמודים במשחק (ירושה: כל עמוד "הוא" BasePage, ו-BasePage "הוא" Page).
    // דברים שכל העמודים צריכים כתובים כאן פעם אחת, במקום להעתיק אותם לכל עמוד.
    {
        /// <summary>A color from App.xaml (it changes with the chosen theme).</summary>
        protected static Brush Res(string key)
        // פעולה: Res — מחזירה צבע ("מברשת") מתוך App.xaml. הצבע משתנה לפי ערכת העיצוב שנבחרה.
        {
            return (Brush)Application.Current.Resources[key];
        }

        /// <summary>Stars for a result, for example 2 → "★★☆".</summary>
        protected static string StarsText(int stars)
        // פעולה: StarsText — הופכת מספר כוכבים לטקסט, למשל 2 → "★★☆".
        {
            return new string('★', stars) + new string('☆', 3 - stars);
        }

        /// <summary>🥇 🥈 🥉 for the first three places, then "4.", "5."... (duel results and tables).</summary>
        protected static string Medal(int place)
        // פעולה: Medal — מחזירה מדליה לשלושת המקומות הראשונים (🥇 🥈 🥉), ואחר כך "4.", "5."...
        {
            if (place == 1)
                return "🥇";
            if (place == 2)
                return "🥈";
            if (place == 3)
                return "🥉";
            return place + ".";
        }

        /// <summary>A small pop-up window with a title, a message and an "OK" button.</summary>
        protected static async System.Threading.Tasks.Task ShowPopupAsync(string title, string message)
        // פעולה: ShowPopupAsync — מקפיצה חלון קטן עם כותרת, הודעה וכפתור "הבנתי".
        {
            var dialog = new ContentDialog
            // אובייקט: dialog — החלון הקופץ.
            {
                Title = title,
                Content = new TextBlock { Text = message, TextWrapping = TextWrapping.Wrap, FontSize = 16 },
                CloseButtonText = "הבנתי",
                FlowDirection = FlowDirection.RightToLeft,
            };
            await dialog.ShowAsync();
        }

        /// <summary>
        /// The "back" button of every page goes to the previous page.
        /// It is virtual, so a page that must do something first (like leaving a duel) can override it.
        /// </summary>
        protected virtual void BackButton_Click(object sender, RoutedEventArgs e)
        // פעולה: BackButton_Click — כפתור "חזרה" בכל עמוד: חוזר לעמוד הקודם. היא virtual, כך שעמוד שצריך לעשות משהו קודם (כמו לעזוב דו-קרב) יכול לשנות אותה.
        {
            if (Frame.CanGoBack)
                Frame.GoBack();
        }
    }
}
