using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace UM_NfoGenerator
{
    internal static class UpdateChecker
    {
        private const string CurrentVersion = "0.0.3-Alpha";
        private const string VersionUrl = "https://raw.githubusercontent.com/UnsyncedMaster/UM-NfoGenerator/refs/heads/main/version.txt";

        public static async Task CheckForUpdatesAsync()
        {
            try
            {
                using var http = new HttpClient();
                http.Timeout = TimeSpan.FromSeconds(5);

                string remoteVersion = await http.GetStringAsync(VersionUrl);
                remoteVersion = remoteVersion.Trim();

                if (remoteVersion != CurrentVersion)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"\n[!] A New Ver Is Available: {remoteVersion}");
                    Console.WriteLine($"[i] You Are Currently Using: {CurrentVersion}");
                    Console.WriteLine("[>] Visit the GitHub Repo To Update!");
                    Console.WriteLine("[>] https://github.com/UnsyncedMaster/UM-NfoGenerator/releases\n");
                    Console.ResetColor();
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("\n[✓] You Are Running The Latest Ver!\n");
                    Console.ResetColor();
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine($"\n[!] Update Check Skipped: {ex.Message}\n");
                Console.ResetColor();
            }
        }
    }
}
