using System;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace Smart_Sudoku.Pages
{
    /// <summary>
    /// Start screen: sign in, register or continue as a guest, then go to the levels.
    /// </summary>
    public sealed partial class MenuPage : BasePage
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

            UpdateFeatureButtons();

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

        /// <summary>Shows the player's profile picture above the greeting (registered users only).</summary>
        private async void ShowAvatar()
        {
            if (Session.UserId is not int userId)
            {
                AvatarHolder.Content = null;
                return;
            }
            var picture = await Avatars.LoadAsync(userId);
            AvatarHolder.Content = Avatars.Circle(picture, Session.PlayerName ?? "", 96, Cosmetics.CurrentFrameId);
        }

        private void LeaderboardButton_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(LeaderboardPage));
        }

        private void UpdateFeatureButtons()
        {
            ShowAvatar();
            var saved = GameData.SavedGame;
            ContinueButton.Visibility = saved != null ? Visibility.Visible : Visibility.Collapsed;
            if (saved != null)
            {
                ContinueButton.Content = saved.Daily
                    ? "▶ להמשיך את האתגר היומי"
                    : $"▶ להמשיך Level {Levels.NumberOf(saved.LevelId)} · {Levels.DifficultyFor(saved.LevelId).Name}";
            }

            int streak = DailyChallenge.Streak;
            DailyButton.Content = DailyChallenge.DoneToday
                ? $"📅 אתגר יומי ✓ 🔥{streak}"
                : streak > 0 ? $"📅 אתגר יומי 🔥{streak}" : "📅 אתגר יומי";

            WheelButton.Content = GameData.GetExtra("wheel.last") == DateTime.Now.ToString("yyyy-MM-dd")
                ? "🎡 גלגל מזל ✓"
                : "🎡 גלגל מזל ✨";

            var league = Progression.CurrentLeague;
            LevelChip.Text = $"⭐ רמה {Progression.PlayerLevel}   ·   {league.Emoji} ליגת {league.Name}";

            int missionsReady = Progression.TodaysMissions.Count(m => Progression.MissionDone(m) && !Progression.MissionClaimed(m.Id))
                + (Progression.AllMissionsDone && !Progression.ChestClaimed ? 1 : 0);
            MissionsButton.Content = missionsReady > 0 ? $"🎯 משימות 🎁{missionsReady}" : "🎯 משימות";

            FriendsButton.IsEnabled = Session.UserId != null;
            Sounds.ApplyMusicSetting();
        }

        private void ContinueButton_Click(object sender, RoutedEventArgs e)
        {
            if (GameData.SavedGame is SavedGame saved)
                Frame.Navigate(typeof(GamePage), saved);
        }

        private void DailyButton_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(GamePage), new DailyChallengeStart());
        }

        private void DuelButton_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(DuelLobbyPage));
        }

        private void MiniButton_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(MiniSudokuPage));
        }

        private void WheelButton_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(LuckyWheelPage));
        }

        private void AchievementsButton_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(AchievementsPage));
        }

        private void AdventureButton_Click(object sender, RoutedEventArgs e) => Frame.Navigate(typeof(AdventureMapPage));

        private void ZenButton_Click(object sender, RoutedEventArgs e) => Frame.Navigate(typeof(GamePage), new ZenStart());

        private void MissionsButton_Click(object sender, RoutedEventArgs e) => Frame.Navigate(typeof(MissionsPage));

        private void StatsButton_Click(object sender, RoutedEventArgs e) => Frame.Navigate(typeof(StatsPage));

        private void FriendsButton_Click(object sender, RoutedEventArgs e) => Frame.Navigate(typeof(FriendsPage));

        private void SettingsButton_Click(object sender, RoutedEventArgs e) => Frame.Navigate(typeof(SettingsPage));

        private void LetsPlayButton_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(LevelsPage));
        }
    }
}
