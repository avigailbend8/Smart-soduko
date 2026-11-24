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
    // אובייקט: MenuPage — מסך הפתיחה: כניסה, הרשמה או משחק כאורח, ואחר כך כל הכפתורים של המשחק (שלבים, אתגר יומי, דו-קרב, חנות...).
    {
        public MenuPage()
        // פעולה: MenuPage (בנאי) — טוענת את העיצוב של העמוד מקובץ ה-XAML.
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        // פעולה: OnNavigatedTo — רצה כשנכנסים לעמוד, ובודקת מה להציג.
        {
            base.OnNavigatedTo(e);
            UpdateChoice();
        }

        private void UpdateChoice()
        // פעולה: UpdateChoice — אם השחקן עוד לא בחר (כניסה או אורח) — מציגה את כפתורי הבחירה.
        // אחרת היא טוענת את ההתקדמות והעיצוב שלו, כותבת ברכה, ומציגה את כפתור "בואו נשחק" עם הקישוטים.
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

            SignOutButton.Content = Session.UserId != null ? "🚪 התנתקות" : "🚪 יציאה ממצב אורח";
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
        // פעולה: SignInButton_Click — כפתור "כניסה": עובר לעמוד ההתחברות.
        {
            Frame.Navigate(typeof(SingInPage));
        }

        private void RegisterButton_Click(object sender, RoutedEventArgs e)
        // פעולה: RegisterButton_Click — כפתור "הרשמה": עובר לעמוד ההרשמה.
        {
            Frame.Navigate(typeof(RegisterPage));
        }

        private async void GuestButton_Click(object sender, RoutedEventArgs e)
        // פעולה: GuestButton_Click — כפתור "המשך כאורח": קודם מקפיצה חלון שמסביר מה מפסידים כאורח.
        // אפשר לבחור להירשם, להתחבר, או בכל זאת להמשיך כאורח.
        {
            var dialog = new ContentDialog
            // אובייקט: dialog — החלון שמסביר מה מפסידים כשמשחקים כאורח.
            {
                Title = "רגע לפני שממשיכים כאורח 🙈",
                Content = new TextBlock
                {
                    Text = "כאורח מפסידים:\n"
                         + "• שם משלך ותמונת פרופיל\n"
                         + "• מקום בטבלת השיאים\n"
                         + "• רשימת חברים ומתנות לחברים\n"
                         + "• סיסמה ושחזור סיסמה\n"
                         + "• כל מי שמשחק במחשב כאורח משתמש באותה התקדמות, ואפשר לאבד אותה\n\n"
                         + "רוצה ליצור משתמש או להתחבר?",
                    TextWrapping = TextWrapping.Wrap,
                    FontSize = 16,
                },
                PrimaryButtonText = "להירשם",
                SecondaryButtonText = "להתחבר",
                CloseButtonText = "להמשיך כאורח",
                DefaultButton = ContentDialogButton.Primary,
                FlowDirection = FlowDirection.RightToLeft,
            };

            var answer = await dialog.ShowAsync();
            // אובייקט: answer — הכפתור שנלחץ בחלון.
            if (answer == ContentDialogResult.Primary)
            {
                Frame.Navigate(typeof(RegisterPage));
                return;
            }
            if (answer == ContentDialogResult.Secondary)
            {
                Frame.Navigate(typeof(SingInPage));
                return;
            }

            Session.PlayAsGuest();
            UpdateChoice();
        }

        private void SwitchUserButton_Click(object sender, RoutedEventArgs e)
        // פעולה: SwitchUserButton_Click — כפתור "התנתקות": מנתקת את השחקן, מחזירה את העיצוב הוורוד וחוזרת לכפתורי הבחירה.
        {
            Session.SignOut();
            Themes.Apply("pink");
            DecorStoryboard.Stop();
            UpdateChoice();
        }

        /// <summary>Shows the player's profile picture above the greeting (registered users only).</summary>
        private async void ShowAvatar()
        // פעולה: ShowAvatar — מציגה את תמונת הפרופיל מעל הברכה (רק למשתמש רשום).
        {
            if (Session.UserId is not int userId)
            {
                AvatarHolder.Content = null;
                return;
            }
            var picture = await Avatars.LoadAsync(userId);
            // אובייקט: picture — תמונת הפרופיל של השחקן.
            AvatarHolder.Content = Avatars.Circle(picture, Session.PlayerName ?? "", 96, Cosmetics.CurrentFrameId);
        }

        private void LeaderboardButton_Click(object sender, RoutedEventArgs e)
        // פעולה: LeaderboardButton_Click — כפתור טבלת השיאים: עובר לעמוד השיאים.
        {
            Frame.Navigate(typeof(LeaderboardPage));
        }

        private void UpdateFeatureButtons()
        // פעולה: UpdateFeatureButtons — מעדכנת את הכיתוב של הכפתורים לפי המצב: "להמשיך" אם יש משחק שמור, הרצף באתגר היומי,
        // האם גלגל המזל כבר סובב היום, רמה וליגה, ומספר הפרסים שמחכים במשימות.
        {
            ShowAvatar();
            var saved = GameData.SavedGame;
            // אובייקט: saved — המשחק השמור (שלב שעזבו באמצע), או null.
            ContinueButton.Visibility = saved != null ? Visibility.Visible : Visibility.Collapsed;
            if (saved != null)
            {
                ContinueButton.Content = saved.Daily
                    ? "▶ להמשיך את האתגר היומי"
                    : $"▶ להמשיך Level {Levels.NumberOf(saved.LevelId)} · {Levels.DifficultyFor(saved.LevelId).Name}";
            }

            int streak = DailyChallenge.Streak;
            // אובייקט: streak — כמה ימים ברצף השחקן פתר את האתגר היומי.
            DailyButton.Content = DailyChallenge.DoneToday
                ? $"📅 אתגר יומי ✓ 🔥{streak}"
                : streak > 0 ? $"📅 אתגר יומי 🔥{streak}" : "📅 אתגר יומי";

            WheelButton.Content = GameData.GetExtra("wheel.last") == DateTime.Now.ToString("yyyy-MM-dd")
                ? "🎡 גלגל מזל ✓"
                : "🎡 גלגל מזל ✨";

            var league = Progression.CurrentLeague;
            // אובייקט: league — הליגה של השחקן השבוע.
            LevelChip.Text = $"⭐ רמה {Progression.PlayerLevel}   ·   {league.Emoji} ליגת {league.Name}";

            int missionsReady = Progression.TodaysMissions.Count(m => Progression.MissionDone(m) && !Progression.MissionClaimed(m.Id))
                + (Progression.AllMissionsDone && !Progression.ChestClaimed ? 1 : 0);
                // אובייקט: missionsReady — כמה פרסים מחכים לאיסוף בעמוד המשימות.
            MissionsButton.Content = missionsReady > 0 ? $"🎯 משימות 🎁{missionsReady}" : "🎯 משימות";

            FriendsButton.IsEnabled = Session.UserId != null;
            Sounds.ApplyMusicSetting();
        }

        private void ContinueButton_Click(object sender, RoutedEventArgs e)
        // פעולה: ContinueButton_Click — כפתור "להמשיך": פותחת את המשחק השמור מאיפה שעזבו.
        {
            if (GameData.SavedGame is SavedGame saved)
                Frame.Navigate(typeof(GamePage), saved);
        }

        private void DailyButton_Click(object sender, RoutedEventArgs e)
        // פעולה: DailyButton_Click — כפתור "אתגר יומי": פותחת את הלוח של היום.
        {
            Frame.Navigate(typeof(GamePage), new DailyChallengeStart());
        }

        // One of the three computer opponents was chosen (its id is in the item's Tag)
        private void AiOpponent_Click(object sender, RoutedEventArgs e)
        // פעולה: AiOpponent_Click — בחירת יריב מחשב מהתפריט הקטן: פותחת דו-קרב נגדו.
        {
            string opponentId = (string)((MenuFlyoutItem)sender).Tag;
            // אובייקט: opponentId — המזהה של היריב שנבחר (שמור ב-Tag של הפריט בתפריט).
            Frame.Navigate(typeof(GamePage), new AiDuelStart(opponentId));
        }

        private void DuelButton_Click(object sender, RoutedEventArgs e)
        // פעולה: DuelButton_Click — כפתור "דו-קרב ברשת": עובר לחדר ההמתנה של הדו-קרב.
        {
            Frame.Navigate(typeof(DuelLobbyPage));
        }

        private void MiniButton_Click(object sender, RoutedEventArgs e)
        // פעולה: MiniButton_Click — כפתור "סודוקו קטן": עובר לעמוד הלוחות הקטנים.
        {
            Frame.Navigate(typeof(MiniSudokuPage));
        }

        private void WheelButton_Click(object sender, RoutedEventArgs e)
        // פעולה: WheelButton_Click — כפתור "גלגל מזל": עובר לעמוד הגלגל.
        {
            Frame.Navigate(typeof(LuckyWheelPage));
        }

        private void AchievementsButton_Click(object sender, RoutedEventArgs e)
        // פעולה: AchievementsButton_Click — כפתור "הישגים": עובר לעמוד ההישגים.
        {
            Frame.Navigate(typeof(AchievementsPage));
        }

        private void AdventureButton_Click(object sender, RoutedEventArgs e) => Frame.Navigate(typeof(AdventureMapPage));
        // פעולה: AdventureButton_Click — כפתור "מסע": עובר למפת המסע.

        private void ZenButton_Click(object sender, RoutedEventArgs e) => Frame.Navigate(typeof(GamePage), new ZenStart());
        // פעולה: ZenButton_Click — כפתור "זן": פותחת לוח רגוע, בלי שעון ובלי לבבות.

        private void MissionsButton_Click(object sender, RoutedEventArgs e) => Frame.Navigate(typeof(MissionsPage));
        // פעולה: MissionsButton_Click — כפתור "משימות": עובר לעמוד המשימות.

        private void StatsButton_Click(object sender, RoutedEventArgs e) => Frame.Navigate(typeof(StatsPage));
        // פעולה: StatsButton_Click — כפתור "סטטיסטיקות": עובר לעמוד הסטטיסטיקות.

        private void FriendsButton_Click(object sender, RoutedEventArgs e) => Frame.Navigate(typeof(FriendsPage));
        // פעולה: FriendsButton_Click — כפתור "חברים": עובר לעמוד החברים.

        private void SettingsButton_Click(object sender, RoutedEventArgs e) => Frame.Navigate(typeof(SettingsPage));
        // פעולה: SettingsButton_Click — כפתור "הגדרות": עובר לעמוד ההגדרות.

        private void LetsPlayButton_Click(object sender, RoutedEventArgs e)
        // פעולה: LetsPlayButton_Click — כפתור "בואו נשחק": עובר לעמוד בחירת השלבים.
        {
            Frame.Navigate(typeof(LevelsPage));
        }
    }
}
