using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

namespace Smart_Sudoku.Pages
{
    /// <summary>
    /// Lets an existing player enter their user name.
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

        private void UserNameBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Enter)
                SignIn_Click(sender, e);
        }

        private void SignIn_Click(object sender, RoutedEventArgs e)
        {
            string name = UserNameBox.Text.Trim();
            if (name.Length == 0)
            {
                ErrorText.Text = "צריך להקליד שם משתמש";
                ErrorText.Visibility = Visibility.Visible;
                return;
            }

            Session.PlayerName = name;
            Session.IsGuest = false;

            // Back to the menu, which now shows the "Let's play" button
            if (Frame.CanGoBack)
                Frame.GoBack();
        }
    }
}
