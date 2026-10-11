using System;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;

namespace Smart_Sudoku.Pages
{
    /// <summary>
    /// Creates a new user in the database and signs them in.
    /// </summary>
    public sealed partial class RegisterPage : Page
    {
        public RegisterPage()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            // Offer to carry over the guest's coins, stars and purchases, if there are any
            TransferGuestBox.Visibility = GuestStore.Load().HasAnyProgress ? Visibility.Visible : Visibility.Collapsed;
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            if (Frame.CanGoBack)
                Frame.GoBack();
        }

        private void Field_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Enter)
                Register_Click(sender, e);
        }

        private void Register_Click(object sender, RoutedEventArgs e)
        {
            string name = UserNameBox.Text.Trim();
            string password = PasswordBox.Password;

            if (name.Length < 2 || name.Length > 20)
            {
                ShowError("שם המשתמש צריך להיות בין 2 ל-20 תווים");
                return;
            }
            if (password.Length < 4)
            {
                ShowError("הסיסמה צריכה להיות לפחות 4 תווים");
                return;
            }
            if (password != ConfirmBox.Password)
            {
                ShowError("הסיסמאות לא זהות");
                return;
            }

            var startingProgress = TransferGuestBox.Visibility == Visibility.Visible && TransferGuestBox.IsChecked == true
                ? GuestStore.Load()
                : new PlayerProgress();

            int? userId;
            string error;
            try
            {
                userId = Database.Register(name, password, startingProgress, out error);
            }
            catch (Exception)
            {
                ShowError("הייתה בעיה בשמירה במאגר המשתמשים. נסו שוב.");
                return;
            }

            if (userId is not int id)
            {
                ShowError(error);
                return;
            }

            Session.SignIn(id, name);

            // Back to a fresh menu, which now shows the "Let's play" button
            Frame.Navigate(typeof(MenuPage));
            Frame.BackStack.Clear();
        }

        private void SignInLink_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(SingInPage));
        }

        private void ShowError(string message)
        {
            ErrorText.Text = message;
            ErrorText.Visibility = Visibility.Visible;
        }
    }
}
