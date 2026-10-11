namespace Smart_Sudoku
{
    /// <summary>
    /// Remembers who is playing for as long as the app is open.
    /// </summary>
    public static class Session
    {
        public static string? PlayerName { get; private set; }

        /// <summary>Database id of the signed-in user, or null for the guest.</summary>
        public static int? UserId { get; private set; }

        public static bool IsGuest { get; private set; }

        public static bool HasChosen => IsGuest || UserId != null;

        public static void SignIn(int userId, string userName)
        {
            UserId = userId;
            PlayerName = userName;
            IsGuest = false;
        }

        public static void PlayAsGuest()
        {
            UserId = null;
            PlayerName = null;
            IsGuest = true;
        }

        public static void SignOut()
        {
            UserId = null;
            PlayerName = null;
            IsGuest = false;
        }
    }
}
