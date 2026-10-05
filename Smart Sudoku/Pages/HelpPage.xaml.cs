using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Smart_Sudoku.Pages
{
    /// <summary>
    /// Short explanation of Sudoku and how to play.
    /// </summary>
    public sealed partial class HelpPage : Page
    {
        public HelpPage()
        {
            this.InitializeComponent();
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            if (Frame.CanGoBack)
                Frame.GoBack();
        }
    }
}
