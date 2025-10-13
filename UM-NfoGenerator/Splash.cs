using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;
using System.Threading;

namespace UM_NfoGenerator
{
    internal class Splash
    {

        public static void ShowSplash()
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Cyan;

            string[] logo =
            {
        "╔══════════════════════════════════════════════════════════════════════════════╗",
        "║                                                                              ║",
        "║   ██╗   ██╗███╗   ███╗ ███╗   ██╗ ██████╗                                    ║",
        "║   ██║   ██║████╗ ████║ ████╗  ██║██╔════╝                                    ║",
        "║   ██║   ██║██╔████╔██║ ██╔██╗ ██║██║  ███╗                                   ║",
        "║   ██║   ██║██║╚██╔╝██║ ██║╚██╗██║██║   ██║                                   ║",
        "║   ╚██████╔╝██║ ╚═╝ ██║ ██║ ╚████║╚██████╔╝                                   ║",
        "║    ╚═════╝ ╚═╝     ╚═╝ ╚═╝  ╚═══╝ ╚═════╝                                    ║",
        "║                                                                              ║",
        "║                       UMNG - UnsyncedMaster's .NFO Generator                 ║",
        "║                         v0.0.3-Alpha  |  Why Did I Make This..               ║",
        "║                           © 2025  UnsyncedMaster                             ║",
        "║                                                                              ║",
        "╚══════════════════════════════════════════════════════════════════════════════╝"
    };

            foreach (string line in logo)
            {
                Console.WriteLine(line);
                Thread.Sleep(30); 
            }

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine();

            string[] loading =
            {
        "[*] Initializing UM-NfoGenerator",
        "[✓] UM-NfoGenerator Ready!",
        "[*] Checking For Updates.."
    };

            foreach (var step in loading)
            {
                Console.WriteLine(step);
                Thread.Sleep(400);
            }

            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine();
            string[] taglines =
            {
        "I Dont Have Any Taglines.",
        "Gamer",
        "UMNG: UrMomNotGooning",
        "What"
    };
            Console.WriteLine(taglines[new Random().Next(taglines.Length)]);
            Thread.Sleep(1200);

            Console.ResetColor();
            Console.Clear();
        }


    }
}
