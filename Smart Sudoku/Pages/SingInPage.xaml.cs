using System;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

namespace Smart_Sudoku.Pages
{
    /// <summary>
    /// Signs in a registered user with their user name and password.
    /// </summary>
    public sealed partial class SingInPage : Page
    {
        public SingInPage()
        {
            this.InitializeComponent();
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            if (Frame.CanGoBack)
                Frame.GoBack();
        }

        private void Field_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Enter)
                SignIn_Click(sender, e);
        }

        private void SignIn_Click(object sender, RoutedEventArgs e)
        {
            string name = UserNameBox.Text.Trim();
            if (name.Length == 0 || PasswordBox.Password.Length == 0)
            {
                ShowError("צריך להקליד שם משתמש וסיסמה");
                return;
            }

            int? userId;
            try
            {
                userId = Database.Login(name, PasswordBox.Password);
            }
            catch (Exception)
            {
                ShowError("הייתה בעיה בגישה למאגר המשתמשים. נסו שוב.");
                return;
            }

            if (userId is not int id)
            {
                ShowError("שם המשתמש או הסיסמה לא נכונים");
                PasswordBox.Password = "";
                return;
            }

            Session.SignIn(id, Database.GetUserName(id));

            // Back to a fresh menu, which now shows the "Let's play" button
            Frame.Navigate(typeof(MenuPage));
            Frame.BackStack.Clear();
        }

        private void RegisterLink_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(RegisterPage));
        }

        private void ShowError(string message)
        {
            ErrorText.Text = message;
            ErrorText.Visibility = Visibility.Visible;
        }
    }
}
