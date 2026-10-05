namespace Smart_Sudoku
{
    /// <summary>
    /// Remembers who is playing for as long as the app is open.
    /// </summary>
    public static class Session
    {
        public static string? PlayerName { get; set; }

        public static bool IsGuest { get; set; }

        public static bool HasChosen => IsGuest || PlayerName != null;
    }
}
