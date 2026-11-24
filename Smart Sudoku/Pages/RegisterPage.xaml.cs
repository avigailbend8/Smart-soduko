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
    public sealed partial class RegisterPage : BasePage
    // אובייקט: RegisterPage — עמוד ההרשמה: יוצר משתמש חדש במסד הנתונים ומחבר אותו.
    {
        public RegisterPage()
        // פעולה: RegisterPage (בנאי) — טוענת את העיצוב של העמוד מקובץ ה-XAML.
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        // פעולה: OnNavigatedTo — רצה כשנכנסים לעמוד: אם לאורח כבר יש התקדמות, מציגה תיבה שמציעה להעביר אותה לחשבון החדש.
        {
            base.OnNavigatedTo(e);

            // Offer to carry over the guest's coins, stars and purchases, if there are any
            TransferGuestBox.Visibility = Database.LoadProgress(Database.GuestUserId()).HasAnyProgress ? Visibility.Visible : Visibility.Collapsed;
        }

        private void Field_KeyDown(object sender, KeyRoutedEventArgs e)
        // פעולה: Field_KeyDown — כשלוחצים Enter באחד השדות — זה כמו ללחוץ על "הרשמה".
        {
            if (e.Key == VirtualKey.Enter)
                Register_Click(sender, e);
        }

        private void Register_Click(object sender, RoutedEventArgs e)
        // פעולה: Register_Click — כפתור "הרשמה": בודקת שהשם והסיסמה תקינים ושהסיסמאות זהות,
        // יוצרת משתמש חדש (עם ההתקדמות של האורח אם ביקשו), מחברת אותו וחוזרת לתפריט.
        {
            string name = UserNameBox.Text.Trim();
            // אובייקט: name — שם המשתמש שהוקלד, בלי רווחים מיותרים.
            string password = PasswordBox.Password;
            // אובייקט: password — הסיסמה שהוקלדה.

            if (name.Length < 2 || name.Length > 20)
            {
                ShowError("שם המשתמש צריך להיות בין 2 ל-20 תווים");
                return;
            }
            if (PasswordRules.Check(password) is string passwordProblem)
            {
                ShowError(passwordProblem);
                _ = ShowPopupAsync("🔒 הסיסמה לא מספיק חזקה", passwordProblem + "\n\n" + PasswordRules.Hint);
                return;
            }
            if (password != ConfirmBox.Password)
            {
                ShowError("הסיסמאות לא זהות");
                return;
            }

            var startingProgress = TransferGuestBox.Visibility == Visibility.Visible && TransferGuestBox.IsChecked == true
                ? Database.LoadProgress(Database.GuestUserId())
                : new PlayerProgress();
                // אובייקט: startingProgress — ההתקדמות שאיתה החשבון החדש מתחיל: של האורח (אם סימנו את התיבה), או התחלה חדשה.

            int? userId;
            // אובייקט: userId — המספר של המשתמש החדש (או null אם ההרשמה נכשלה).
            string error;
            // אובייקט: error — הודעת השגיאה, אם ההרשמה נכשלה.
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
        // פעולה: SignInLink_Click — הקישור "יש לי כבר חשבון": עובר לעמוד ההתחברות.
        {
            Frame.Navigate(typeof(SingInPage));
        }

        private void ShowError(string message)
        // פעולה: ShowError — מציגה הודעת שגיאה באדום על המסך.
        {
            ErrorText.Text = message;
            ErrorText.Visibility = Visibility.Visible;
        }
    }
}
