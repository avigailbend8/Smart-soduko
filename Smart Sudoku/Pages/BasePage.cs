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
    {
        /// <summary>A color from App.xaml (it changes with the chosen theme).</summary>
        protected static Brush Res(string key)
        {
            return (Brush)Application.Current.Resources[key];
        }

        /// <summary>Stars for a result, for example 2 → "★★☆".</summary>
        protected static string StarsText(int stars)
        {
            return new string('★', stars) + new string('☆', 3 - stars);
        }

        /// <summary>🥇 🥈 🥉 for the first three places, then "4.", "5."... (duel results and tables).</summary>
        protected static string Medal(int place)
        {
            if (place == 1)
                return "🥇";
            if (place == 2)
                return "🥈";
            if (place == 3)
                return "🥉";
            return place + ".";
        }

        /// <summary>
        /// The "back" button of every page goes to the previous page.
        /// It is virtual, so a page that must do something first (like leaving a duel) can override it.
        /// </summary>
        protected virtual void BackButton_Click(object sender, RoutedEventArgs e)
        {
            if (Frame.CanGoBack)
                Frame.GoBack();
        }
    }
}
