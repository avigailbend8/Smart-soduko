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
    public sealed partial class SingInPage : BasePage
    // אובייקט: SingInPage — עמוד ההתחברות: מחבר משתמש רשום לפי שם משתמש וסיסמה, ויש בו גם "שכחתי סיסמה".
    {
        public SingInPage()
        // פעולה: SingInPage (בנאי) — טוענת את העיצוב של העמוד מקובץ ה-XAML.
        {
            this.InitializeComponent();
        }

        private void Field_KeyDown(object sender, KeyRoutedEventArgs e)
        // פעולה: Field_KeyDown — כשלוחצים Enter באחד השדות — זה כמו ללחוץ על "כניסה".
        {
            if (e.Key == VirtualKey.Enter)
                SignIn_Click(sender, e);
        }

        private void SignIn_Click(object sender, RoutedEventArgs e)
        // פעולה: SignIn_Click — כפתור "כניסה": בודקת את השם והסיסמה במסד הנתונים. אם הם נכונים — מחברת וחוזרת לתפריט.
        {
            string name = UserNameBox.Text.Trim();
            // אובייקט: name — שם המשתמש שהוקלד.
            if (name.Length == 0 || PasswordBox.Password.Length == 0)
            {
                ShowError("צריך להקליד שם משתמש וסיסמה");
                return;
            }

            int? userId;
            // אובייקט: userId — המספר של המשתמש אם הכניסה הצליחה, או null.
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
        // פעולה: RegisterLink_Click — הקישור "אין לי חשבון": עובר לעמוד ההרשמה.
        {
            Frame.Navigate(typeof(RegisterPage));
        }

        private void ForgotLink_Click(object sender, RoutedEventArgs e)
        // פעולה: ForgotLink_Click — הקישור "שכחתי סיסמה": פותחת או סוגרת את האזור של איפוס הסיסמה.
        {
            ForgotPanel.Visibility = ForgotPanel.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
            ForgotNameBox.Text = UserNameBox.Text;
        }

        private void ResetPassword_Click(object sender, RoutedEventArgs e)
        // פעולה: ResetPassword_Click — כפתור "איפוס סיסמה": אם המילה הסודית נכונה — קובעת סיסמה חדשה.
        {
            if (ForgotNameBox.Text.Trim().Length == 0 || SecretWordBox.Password.Length == 0)
            {
                ForgotResultText.Text = "צריך שם משתמש, מילת סוד וסיסמה חדשה";
                return;
            }
            if (PasswordRules.Check(NewPasswordBox.Password) is string passwordProblem)
            {
                ForgotResultText.Text = passwordProblem;
                _ = ShowPopupAsync("🔒 הסיסמה החדשה לא מספיק חזקה", passwordProblem + "\n\n" + PasswordRules.Hint);
                return;
            }

            if (Database.ResetPassword(ForgotNameBox.Text, SecretWordBox.Password, NewPasswordBox.Password))
            {
                ForgotResultText.Text = "✅ הסיסמה שונתה! אפשר להתחבר עם הסיסמה החדשה.";
                UserNameBox.Text = ForgotNameBox.Text.Trim();
                PasswordBox.Password = "";
            }
            else
            {
                ForgotResultText.Text = "שם המשתמש או מילת הסוד לא נכונים (אולי עוד לא נבחרה מילת סוד בהגדרות)";
            }
            SecretWordBox.Password = "";
            NewPasswordBox.Password = "";
        }

        private void ShowError(string message)
        // פעולה: ShowError — מציגה הודעת שגיאה באדום על המסך.
        {
            ErrorText.Text = message;
            ErrorText.Visibility = Visibility.Visible;
        }
    }
}
