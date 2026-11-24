using System.Collections.Generic;
using System.Linq;

namespace Smart_Sudoku
{
    /// <summary>
    /// The rules for a new password: at least 6 characters, with an uppercase English letter,
    /// a lowercase English letter, a digit and a special character.
    /// </summary>
    public static class PasswordRules
    // אובייקט: PasswordRules — הכללים לסיסמה חדשה: לפחות 6 תווים, ובהם אות גדולה באנגלית, אות קטנה באנגלית, ספרה ותו מיוחד.
    {
        public const int MinLength = 6;
        // אובייקט: MinLength — האורך המינימלי של סיסמה (6 תווים).

        /// <summary>Short text that explains the rules (for hints under the password box).</summary>
        public const string Hint = "לפחות 6 תווים: אות גדולה באנגלית, אות קטנה באנגלית, ספרה ותו מיוחד (כמו ! @ #)";
        // אובייקט: Hint — הסבר קצר על הכללים, שמופיע מתחת לתיבת הסיסמה.

        /// <summary>Checks a new password. Returns null if it is good, or a message that says what is missing.</summary>
        public static string? Check(string password)
        // פעולה: Check — בודקת סיסמה חדשה. מחזירה null אם היא תקינה, ואחרת הודעה שאומרת בדיוק מה חסר.
        {
            var missing = new List<string>();
            // אובייקט: missing — רשימת הדברים שחסרים בסיסמה.
            if (password.Length < MinLength)
                missing.Add($"לפחות {MinLength} תווים");
            if (!password.Any(c => c >= 'A' && c <= 'Z'))
                missing.Add("אות גדולה באנגלית");
            if (!password.Any(c => c >= 'a' && c <= 'z'))
                missing.Add("אות קטנה באנגלית");
            if (!password.Any(char.IsDigit))
                missing.Add("ספרה");
            if (!password.Any(IsSpecial))
                missing.Add("תו מיוחד (כמו ! @ # $)");

            return missing.Count == 0 ? null : "הסיסמה חייבת לכלול: " + string.Join(", ", missing);
        }

        /// <summary>A special character: anything that isn't a letter, a digit or a space.</summary>
        private static bool IsSpecial(char c) => !char.IsLetterOrDigit(c) && !char.IsWhiteSpace(c);
        // פעולה: IsSpecial — בודקת אם התו הוא תו מיוחד: לא אות, לא ספרה ולא רווח.
    }
}
