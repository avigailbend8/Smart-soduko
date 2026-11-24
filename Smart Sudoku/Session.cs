namespace Smart_Sudoku
{
    /// <summary>
    /// Remembers who is playing for as long as the app is open.
    /// </summary>
    public static class Session
    // אובייקט: Session — זוכר מי משחק עכשיו, כל עוד האפליקציה פתוחה.
    {
        public static string? PlayerName { get; private set; }
        // אובייקט: PlayerName — שם השחקן המחובר (או null לאורח).

        /// <summary>Database id of the signed-in user, or null for the guest.</summary>
        public static int? UserId { get; private set; }
        // אובייקט: UserId — המספר של המשתמש במסד הנתונים (או null לאורח).

        public static bool IsGuest { get; private set; }
        // אובייקט: IsGuest — אמת אם משחקים כאורח, בלי חשבון.

        public static bool HasChosen => IsGuest || UserId != null;
        // אובייקט: HasChosen — אמת אם השחקן כבר בחר: נכנס לחשבון או בחר לשחק כאורח.

        public static void SignIn(int userId, string userName)
        // פעולה: SignIn — מחברת משתמש: שומרת את המספר והשם שלו.
        {
            UserId = userId;
            PlayerName = userName;
            IsGuest = false;
        }

        public static void PlayAsGuest()
        // פעולה: PlayAsGuest — מתחילה משחק כאורח, בלי חשבון.
        {
            UserId = null;
            PlayerName = null;
            IsGuest = true;
        }

        public static void SignOut()
        // פעולה: SignOut — מנתקת את המשתמש וחוזרת למצב שבו אף אחד לא מחובר.
        {
            UserId = null;
            PlayerName = null;
            IsGuest = false;
        }
    }
}
