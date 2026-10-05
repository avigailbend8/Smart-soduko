using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace Smart_Sudoku.Pages
{
    /// <summary>
    /// Start screen: sign in or continue as a guest, then go to the levels.
    /// </summary>
    public sealed partial class MenuPage : Page
    {
        public MenuPage()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            UpdateChoice();
        }

        private void UpdateChoice()
        {
            if (!Session.HasChosen)
                return;

            GreetingText.Text = Session.PlayerName != null
                ? $"שלום, {Session.PlayerName}! 💖"
                : "משחקים כאורח 💖";

            // Swap the two choice buttons for "Let's play" and its decorations
            ChoicePanel.Visibility = Visibility.Collapsed;
            PlayPanel.Visibility = Visibility.Visible;
            ShowPlayStoryboard.Begin();
            DecorStoryboard.Begin();
        }

        private void SignInButton_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(SingInPage));
        }

        private void GuestButton_Click(object sender, RoutedEventArgs e)
        {
            Session.PlayerName = null;
            Session.IsGuest = true;
            UpdateChoice();
        }

        private void LetsPlayButton_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(LevelsPage));
        }
    }
}
