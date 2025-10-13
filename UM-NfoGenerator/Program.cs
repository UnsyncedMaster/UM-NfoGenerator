using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UM_NfoGenerator;
class Program
{
    static async Task Main(string[] args)
    {
        UM_NfoGenerator.Splash.ShowSplash();

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("UM-NfoGenerator — Generate and Edit NFO Files for Your Music!\n");
        Console.ResetColor();

        await UpdateChecker.CheckForUpdatesAsync();

        if (args.Length > 0)
        {
            RunGenerator(args);
            return;
        }

        ShowMenu();
    }

    static void ShowMenu()
    {
        while (true) {
            Console.WriteLine("Menu:");
            Console.WriteLine("1) Create a new .NFO");
            Console.WriteLine("2) Edit an existing .NFO");
            Console.WriteLine("3) Exit");
            Console.Write("Select An Option (1-3): ");
            string? choice = Console.ReadLine();

            switch (choice) {
                case "1":
                    RunGenerator(new string[0]); 
                    break;
                case "2":
                    EditNfo();
                    break;
                case "3":
                    Console.WriteLine("Exiting...");
                    return;
                default:
                    Console.WriteLine("Invalid Choice. Please Enter 1, 2, Or 3!\n");
                    break;
            }
        }
    }

    static void EditNfo()
    {
        Console.Write("Enter The Path To The Folder Containing The .nfo File: ");
        string folder = Console.ReadLine()?.Trim() ?? "";

        if (!Directory.Exists(folder))
        {
            Console.WriteLine("Folder Not Found!\n");
            return;
        }

        var nfoFiles = Directory.GetFiles(folder, "*.nfo");
        if (nfoFiles.Length == 0)
        {
            Console.WriteLine("No .NFO Files Found In Specified Folder!\n");
            return;
        }

        Console.WriteLine("\nFound The .NFO File:");
        for (int i = 0; i < nfoFiles.Length; i++)
            Console.WriteLine($"{i + 1}) {Path.GetFileName(nfoFiles[i])}");

        Console.Write("\nSelect A File To Edit (Number): ");
        if (!int.TryParse(Console.ReadLine(), out int choice) || choice < 1 || choice > nfoFiles.Length)
        {
            Console.WriteLine("Invalid Selection!\n");
            return;
        }

        string path = nfoFiles[choice - 1];
        string content = File.ReadAllText(path);
        Console.WriteLine("\nCurrent Content:\n");
        Console.WriteLine(content);
        Console.WriteLine("\nEnter New (Leave Blank To Keep Current Content):");

        string? newContent = Console.ReadLine();
        if (!string.IsNullOrWhiteSpace(newContent))
        {
            File.WriteAllText(path, newContent, new UTF8Encoding(false));
            Console.WriteLine("NFO Updated Succesfully!\n");
        }
        else
        {
            Console.WriteLine("No Changes Made!\n");
        }
    }


    static void RunGenerator(string[] args)
    {
        var opts = Options.Parse(args);

        if (string.IsNullOrEmpty(opts.InputPath)) {
            Console.Write("Enter The Path To The Album Folder: ");
            opts.InputPath = Console.ReadLine()?.Trim() ?? "";
        }

        if (!Directory.Exists(opts.InputPath)) {
            Console.WriteLine($"Error: Input Directory '{opts.InputPath}' Doesn't Exist.");
            return;
        }

        var audioFiles = GetAudioFiles(opts.InputPath, opts.Recursive).ToList();
        if (!audioFiles.Any()) {
            Console.WriteLine("No Audio Files Found In The Folder! (If There Is, And The Program Doesnt See Them, Open A Issue On The Github Page).");
            return;
        }

        string template = File.Exists(opts.TemplatePath) ? File.ReadAllText(opts.TemplatePath) : DefaultTemp.DefaultTemplate(); string folderName = Path.GetFileName(opts.InputPath.TrimEnd(Path.DirectorySeparatorChar));

        opts.Album = opts.Album ?? Prompt($"Album Name [{folderName}]: ", folderName); opts.Artist = opts.Artist ?? Prompt("Artist Name [Unknown Artist]: ", "Unknown Artist"); opts.Genre = opts.Genre ?? Prompt("Genre [Unknown Genre]: ", "Unknown Genre"); opts.Ripper = opts.Ripper ?? Prompt("Who Ripped This Album? [Unknown Ripper]: ", "Unknown Ripper");

        bool addComment = false;
        string comment = "";
        string answer = Prompt("Add A Comment? (y/N): ", "N").ToLower();
        if (answer == "y" || answer == "yes") {
            addComment = true;
            comment = Prompt("Enter Comment: ", "");
        }
        opts.Comment = addComment ? comment : "";

        string tracklist = BuildTracklist(audioFiles);
        string filelist = string.Join("\n", audioFiles.Select(f => Path.GetFileName(f)));

        var replacements = new Dictionary<string, string> {
            { "ALBUM", opts.Album },
            { "ARTIST", opts.Artist },
            { "YEAR", opts.Year ?? ParseYear(folderName) },
            { "GENRE", opts.Genre },
            { "TRACKLIST", tracklist },
            { "RIPPER", opts.Ripper },
            { "ENCODER", opts.Encoder ?? "Unknown Encoder" },
            { "DATE", DateTime.UtcNow.ToString("yyyy-MM-dd") },
            { "COMMENT", opts.Comment },
            { "FILELIST", filelist },
            //{ "MD5SUMS", md5s } // soon maybe
        };

        string content = ApplyTemplate(template, replacements);

        string safeAlbumName = MakeSafeFilename(opts.Album);
        string outDir = opts.OutputPath ?? opts.InputPath;
        Directory.CreateDirectory(outDir);
        string outFile = Path.Combine(outDir, $"{safeAlbumName}.nfo");
        File.WriteAllText(outFile, content, new UTF8Encoding(false));
        Console.WriteLine($"\nNFO File Written To: {outFile}\n");
    }

    static string Prompt(string message, string defaultValue)
    {
        Console.Write(message);
        string? input = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(input))
            return defaultValue;
        return input.Trim();
    }


    static IEnumerable<string> GetAudioFiles(string directory, bool recursive)
    {
        var exts = new[] { ".mp3", ".flac", ".wav", ".m4a", ".ogg", ".wma" };
        return Directory.EnumerateFiles(directory, "*.*", recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly)
                        .Where(f => exts.Contains(Path.GetExtension(f).ToLowerInvariant()));
    }

    static string BuildTracklist(List<string> files)
    {
        var sb = new StringBuilder(); int trackNum = 1; double totalSeconds = 0;

        foreach (var f in files.OrderBy(x => x)) {
            string fileName = Path.GetFileNameWithoutExtension(f); double durationSec = Duration.GetDuration(f); totalSeconds += durationSec; string duration = durationSec > 0 ? Duration.FormatDuration(durationSec) : "Unknown";

            sb.AppendLine($"{trackNum:00} - {fileName} ({duration})");
            trackNum++;
        }

        string totalLength = Duration.FormatDuration(totalSeconds);
        sb.AppendLine($"\nTotal Album Length: {totalLength}");
        return sb.ToString().TrimEnd();
    }

    static string ApplyTemplate(string template, Dictionary<string, string> repl)
    {
        string result = template;
        foreach (var kv in repl) {
            result = result.Replace("{" + kv.Key + "}", kv.Value);
        }
        return result;
    }


    static string MakeSafeFilename(string s)
    {
        foreach (var c in Path.GetInvalidFileNameChars()) s = s.Replace(c, '_');
        return s;
    }



    static string ParseYear(string folderName)
    {
        int start = folderName.IndexOf("("); int end = folderName.IndexOf(")");
        if (start >= 0 && end > start) {
            string inside = folderName.Substring(start + 1, end - start - 1); if (int.TryParse(inside, out _)) return inside;
        }
        return "Unknown Year";
    }
}