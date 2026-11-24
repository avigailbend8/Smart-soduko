using System.Net;
using System.Net.Sockets;

namespace Server
{
    internal static class Program
    // אובייקט: Program — נקודת ההתחלה של תוכנת השרת. מכאן הכול מתחיל לרוץ.
    {
        public const int Port = 9000;
        // אובייקט: Port — מספר ה"דלת" (פורט) שבה השרת מחכה לשחקנים. המשחק צריך להתחבר לאותו מספר.

        private static void Main()
        // פעולה: Main — הפעולה הראשונה שרצה כשמפעילים את השרת. היא פותחת את מסד הנתונים, מפעילה את השרת,
        // מדפיסה למסך את טבלת האלופים ואת הכתובות שאליהן אפשר להתחבר, ואז מחכה עד שלוחצים Ctrl+C כדי לכבות.
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.Title = "Smart Sudoku Server";

            var database = new Database(Path.Combine(AppContext.BaseDirectory, "database.db"));
            // אובייקט: database — מסד הנתונים של השרת (הקובץ database.db) ששומר תוצאות של דו-קרבות ושיאים.
            var server = new Server(Port, database);
            // אובייקט: server — השרת עצמו: מקבל חיבורים מהשחקנים ומנהל את חדרי הדו-קרב.
            server.Start();

            Console.WriteLine($"Smart Sudoku duel server is running on port {Port}.");
            var top = database.TopPlayers(5);
            // אובייקט: top — רשימה של 5 השחקנים עם הכי הרבה ניצחונות, כדי להציג אותם כשהשרת עולה.
            if (top.Count > 0)
            {
                Console.WriteLine("Top players:");
                foreach (var entry in top)
                    Console.WriteLine($"  {entry.Name}: {entry.Wins} wins in {entry.GamesPlayed} games, best score {entry.BestScore}");
            }
            Console.WriteLine("Players on this computer connect to: 127.0.0.1");
            foreach (string ip in LocalAddresses())
                Console.WriteLine($"Players on the same network connect to: {ip}");
            Console.WriteLine("Press Ctrl+C to stop the server.");

            // Keep running until Ctrl+C
            using var stop = new ManualResetEventSlim();
            // אובייקט: stop — "מתג" שמחכה. כשלוחצים Ctrl+C המתג נדלק, והשרת יודע שהגיע הזמן להיסגר.
            Console.CancelKeyPress += (s, e) =>
            {
                e.Cancel = true;
                stop.Set();
            };
            stop.Wait();

            server.Stop();
        }

        private static IEnumerable<string> LocalAddresses() =>
            Dns.GetHostEntry(Dns.GetHostName()).AddressList
                .Where(a => a.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(a))
                .Select(a => a.ToString());
                // פעולה: LocalAddresses — מחזירה את כתובות ה-IP של המחשב ברשת הביתית,
                // כדי שחברים באותה רשת יידעו לאיזו כתובת להתחבר.
    }
}
