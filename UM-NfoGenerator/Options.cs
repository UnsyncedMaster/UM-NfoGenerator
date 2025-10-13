using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UM_NfoGenerator
{
    internal class Options
    {
        public string? InputPath { get; set; }
        public string? TemplatePath { get; set; }
        public string? OutputPath { get; set; }
        public bool Recursive { get; set; } = false; public bool ShowHelp { get; set; } = false; public string? Ripper { get; set; }
        public string? Encoder { get; set; }
        public string? Comment { get; set; }
        public bool IncludeMD5 { get; set; } = false; public string? Album { get; set; }
        public string? Artist { get; set; }
        public string? Year { get; set; }
        public string? Genre { get; set; }

        public static Options Parse(string[] args)
        {
            var o = new Options();
            if (args == null || args.Length == 0) return o;
            for (int i = 0; i < args.Length; i++)
            {
                var a = args[i];
                switch (a)
                {
                    case "--help": case "-h": o.ShowHelp = true; break;
                    case "--template": o.TemplatePath = NextArg(args, ref i); break;
                    case "--out": case "--output": o.OutputPath = NextArg(args, ref i); break;
                    case "--recursive": o.Recursive = true; break;
                    case "--ripper": o.Ripper = NextArg(args, ref i); break;
                    case "--encoder": o.Encoder = NextArg(args, ref i); break;
                    case "--comment": o.Comment = NextArg(args, ref i); break;
                    case "--md5": case "-m": o.IncludeMD5 = true; break;
                    case "--album": case "-a": o.Album = NextArg(args, ref i); break;
                    case "--artist": case "-A": o.Artist = NextArg(args, ref i); break;
                    case "--year": case "-y": o.Year = NextArg(args, ref i); break;
                    case "--genre": case "-g": o.Genre = NextArg(args, ref i); break;
                    default:
                        if (a.StartsWith("-")) Console.Error.WriteLine($"Unknown option: {a}");
                        else if (string.IsNullOrEmpty(o.InputPath)) o.InputPath = a;
                        break;
                }
            }
            return o;

            static string NextArg(string[] args, ref int i)
            {
                if (i + 1 >= args.Length) return ""; i++; return args[i];
            }
        }
    }
}
