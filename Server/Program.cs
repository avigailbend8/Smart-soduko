using System.Net;
using System.Net.Sockets;

namespace Server
{
    internal static class Program
    {
        public const int Port = 9000;

        private static void Main()
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.Title = "Smart Sudoku Server";

            var database = new Database(Path.Combine(AppContext.BaseDirectory, "database.db"));
            var server = new Server(Port, database);
            server.Start();

            Console.WriteLine($"Smart Sudoku duel server is running on port {Port}.");
            var top = database.TopPlayers(5);
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
    }
}
