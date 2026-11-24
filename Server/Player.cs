// Also used by the Smart Sudoku app (linked into it), so the server and the game share one Player class.
namespace Smart_Sudoku.Duel
{
    public static class PlayerStatus
    // אובייקט: PlayerStatus — רשימה קבועה של המצבים האפשריים של שחקן בדו-קרב (מחכה, משחק, סיים).
    {
        public const string Waiting = "waiting";
        // אובייקט: Waiting — השחקן נמצא בחדר ומחכה שהמשחק יתחיל.
        public const string Playing = "playing";
        // אובייקט: Playing — השחקן באמצע המשחק.
        public const string Finish = "finish";
        // אובייקט: Finish — השחקן סיים את הלוח (או שנגמרו לו הלבבות).
    }

    public sealed class Player
    // אובייקט: Player — מייצג שחקן אחד בדו-קרב. השרת והמשחק משתמשים באותה מחלקה כדי לדבר באותה "שפה".
    {
        public string Name { get; set; } = "";
        // אובייקט: Name — השם של השחקן, כפי שהוא מופיע אצל כולם.
        public int Score { get; set; }
        // אובייקט: Score — הניקוד שהשחקן צבר בדו-קרב הנוכחי.
        public string Status { get; set; } = PlayerStatus.Waiting;
        // אובייקט: Status — המצב של השחקן עכשיו (אחד מהמצבים שב-PlayerStatus). מתחיל כ"מחכה".

        /// <summary>True when the player filled the whole board (not just ran out of hearts).</summary>
        public bool Solved { get; set; }
        // אובייקט: Solved — אמת אם השחקן מילא את כל הלוח נכון, ולא רק נפסל כי נגמרו לו הלבבות.

        /// <summary>In a team duel: Teams.Red or Teams.Blue. Empty otherwise.</summary>
        public string Team { get; set; } = "";
        // אובייקט: Team — באיזו קבוצה השחקן (אדום או כחול) כשמשחקים בקבוצות. ריק כשמשחקים כל אחד לעצמו.
    }
}
