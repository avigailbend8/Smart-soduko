using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace Smart_Sudoku.Pages
{
    /// <summary>
    /// Start screen: sign in, register or continue as a guest, then go to the levels.
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
            {
                ChoicePanel.Visibility = Visibility.Visible;
                PlayPanel.Visibility = Visibility.Collapsed;
                return;
            }

            // Each player has their own saved progress and theme
            if (Session.UserId is int userId)
                GameData.LoadUser(userId);
            else
                GameData.LoadGuest();
            Themes.Apply(GameData.ThemeId);

            GreetingText.Text = Session.PlayerName != null
                ? $"שלום, {Session.PlayerName}! 💖"
                : "משחקים כאורח 💖";

            // Swap the choice buttons for "Let's play" and its decorations
            ChoicePanel.Visibility = Visibility.Collapsed;
            PlayPanel.Visibility = Visibility.Visible;
            ShowPlayStoryboard.Begin();
            DecorStoryboard.Begin();
        }

        private void SignInButton_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(SingInPage));
        }

        private void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(RegisterPage));
        }

        private void GuestButton_Click(object sender, RoutedEventArgs e)
        {
            Session.PlayAsGuest();
            UpdateChoice();
        }

        private void SwitchUserButton_Click(object sender, RoutedEventArgs e)
        {
            Session.SignOut();
            Themes.Apply("pink");
            DecorStoryboard.Stop();
            UpdateChoice();
        }

        private void LetsPlayButton_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(LevelsPage));
        }
    }
}
