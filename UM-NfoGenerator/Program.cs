using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("UM-NfoGenerator - Generate and Edit NFO Files for Your Music!\n");

        if (args.Length == 0) {
            ShowMenu();
            return;
        }

        RunGenerator(args);
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
        Console.Write("Enter The Path To The .NFO File To Edit: ");
        string path = Console.ReadLine()?.Trim() ?? "";

        if (!File.Exists(path)) {
            Console.WriteLine("File Not Found.\n");
            return;
        }

        string content = File.ReadAllText(path);
        Console.WriteLine("\nCurrent Content:\n");
        Console.WriteLine(content);
        Console.WriteLine("\nEnter New Content (Leave Empty To Keep The Existing!):");
        string? newContent = Console.ReadLine();

        if (!string.IsNullOrWhiteSpace(newContent)) {
            File.WriteAllText(path, newContent, new UTF8Encoding(false));
            Console.WriteLine("NFO Updated Succesfully.\n");
        }
        else {
            Console.WriteLine("No Changes Made.\n");
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

        string template = File.Exists(opts.TemplatePath) ? File.ReadAllText(opts.TemplatePath) : DefaultTemplate(); string folderName = Path.GetFileName(opts.InputPath.TrimEnd(Path.DirectorySeparatorChar));

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
        var exts = new[] { ".mp3", ".flac", ".wav", ".aac", ".m4a", ".ogg", ".wma" };
        return Directory.EnumerateFiles(directory, "*.*", recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly)
                        .Where(f => exts.Contains(Path.GetExtension(f).ToLowerInvariant()));
    }

    static string BuildTracklist(List<string> files)
    {
        var sb = new StringBuilder(); int trackNum = 1; double totalSeconds = 0;

        foreach (var f in files.OrderBy(x => x)) {
            string fileName = Path.GetFileNameWithoutExtension(f); double durationSec = GetDurationSeconds(f); totalSeconds += durationSec; string duration = durationSec > 0 ? FormatDuration(durationSec) : "Unknown";

            sb.AppendLine($"{trackNum:00} - {fileName} ({duration})");
            trackNum++;
        }

        string totalLength = FormatDuration(totalSeconds);
        sb.AppendLine($"\nTotal Album Length: {totalLength}");
        return sb.ToString().TrimEnd();
    }

    static string FormatDuration(double totalSeconds)
    {
        int minutes = (int)(totalSeconds / 60); int seconds = (int)(totalSeconds % 60);  return $"{minutes:D2}:{seconds:D2}";
    }

    static double GetDurationSeconds(string path)
    {
        string ext = Path.GetExtension(path).ToLowerInvariant();
        try
        {
            using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read))
            using (var br = new BinaryReader(fs))
            {
                if (ext == ".wav")
                {
                    fs.Position = 0x28; int dataSize = br.ReadInt32();
                    fs.Position = 0x18; int sampleRate = br.ReadInt32();
                    fs.Position = 0x22; short bitsPerSample = br.ReadInt16();
                    fs.Position = 0x20; short channels = br.ReadInt16();
                    return dataSize / (sampleRate * channels * bitsPerSample / 8.0);
                }

                else if (ext == ".flac")
                {
                    fs.Position = 4;
                    bool lastBlock = false;
                    while (!lastBlock)
                    {
                        byte header = br.ReadByte();
                        lastBlock = (header & 0x80) != 0;
                        int blockType = header & 0x7F;
                        int length = (br.ReadByte() << 16) | (br.ReadByte() << 8) | br.ReadByte();
                        if (blockType == 0)
                        {
                            br.ReadBytes(10);
                            byte[] buf = br.ReadBytes(8);
                            long totalSamples = ((buf[4] & 0x0F) << 32) |
                                                (buf[5] << 24) |
                                                (buf[6] << 16) |
                                                (buf[7] << 8) |
                                                buf[8];
                            int sampleRate = ((buf[0] << 12) | (buf[1] << 4) | (buf[2] >> 4));
                            if (sampleRate > 0)
                                return totalSamples / (double)sampleRate;
                        }
                        fs.Position += length;
                    }
                    return 0;
                }

                else if (ext == ".aac")
                {
                    fs.Position = 0;
                    int frameCount = 0;
                    int sampleRate = 0;
                    while (fs.Position < fs.Length - 7)
                    {
                        byte b1 = br.ReadByte();
                        byte b2 = br.ReadByte();
                        if (b1 == 0xFF && (b2 & 0xF6) == 0xF0)
                        {
                            int srIndex = (b2 & 0x3C) >> 2;
                            int[] sampleRates = { 96000, 88200, 64000, 48000, 44100,
                            32000, 24000, 22050, 16000, 12000, 11025, 8000, 7350, 0, 0 };
                            sampleRate = sampleRates[Math.Min(srIndex, sampleRates.Length - 1)];
                            frameCount++;
                            fs.Position += 5;
                        }
                        else fs.Position -= 1;
                    }
                    if (sampleRate > 0) return frameCount * (1024.0 / sampleRate);
                    return 0;
                }

                else if (ext == ".mp3")
                {
                    if (fs.Length > 10)
                    {
                        byte[] header = br.ReadBytes(10);
                        if (header[0] == 'I' && header[1] == 'D' && header[2] == '3')
                        {
                            int size = (header[6] & 0x7F) << 21 |
                                       (header[7] & 0x7F) << 14 |
                                       (header[8] & 0x7F) << 7 |
                                       (header[9] & 0x7F);
                            fs.Position = 10 + size;
                        }
                        else fs.Position = 0;
                    }

                    double totalSamples = 0;
                    int sampleRate = 0;

                    while (fs.Position < fs.Length - 4)
                    {
                        byte b1 = br.ReadByte();
                        if (b1 != 0xFF) continue;

                        byte b2 = br.ReadByte(); if ((b2 & 0xE0) != 0xE0) { fs.Position -= 1; continue; }

                        byte b3 = br.ReadByte(); byte b4 = br.ReadByte();

                        int versionBits = (b2 >> 3) & 0x03;
                        int layerBits = (b2 >> 1) & 0x03;
                        int sampleRateIndex = (b3 >> 2) & 0x03;

                        int version = versionBits switch { 0b11 => 1, 0b10 => 2, 0b00 => 25, _ => -1 };
                        if (version == -1) continue;

                        int layer = layerBits switch { 0b01 => 3, 0b10 => 2, 0b11 => 1, _ => -1 };
                        if (layer == -1) continue;

                        int[] sr1 = { 44100, 48000, 32000, 0 }; int[] sr2 = { 22050, 24000, 16000, 0 }; int[] sr25 = { 11025, 12000, 8000, 0 };
                        sampleRate = version switch
                        {
                            1 => sr1[sampleRateIndex],
                            2 => sr2[sampleRateIndex],
                            25 => sr25[sampleRateIndex],
                            _ => 0
                        };
                        if (sampleRate == 0) continue;

                        int samplesPerFrame = (layer == 1) ? 384 : (version == 1 ? 1152 : 576);
                        totalSamples += samplesPerFrame;

                        int bitrateIndex = (b3 >> 4) & 0x0F;
                        int padding = (b3 >> 1) & 0x01;

                        int[,] bitratesMpeg1 = {
                        {0,32,64,96,128,160,192,224,256,288,320,352,384,416,448},
                        {0,32,48,56,64,80,96,112,128,160,192,224,256,320,384},
                        {0,32,40,48,56,64,80,96,112,128,160,192,224,256,320}
                    };
                        int[,] bitratesMpeg2 = {
                        {0,32,48,56,64,80,96,112,128,144,160,176,192,224,256},
                        {0,8,16,24,32,40,48,56,64,80,96,112,128,144,160},
                        {0,8,16,24,32,40,48,56,64,80,96,112,128,144,160}
                    };

                        int bitrate = version == 1
                            ? bitratesMpeg1[layer - 1, bitrateIndex]
                            : bitratesMpeg2[layer - 1, bitrateIndex];
                        if (bitrate == 0) continue;

                        int frameSize = (layer == 1)
                            ? (12 * bitrate * 1000 / sampleRate + padding) * 4
                            : (144 * bitrate * 1000 / sampleRate + padding);

                        if (frameSize <= 4) continue;
                        fs.Position += frameSize - 4;
                    }

                    return sampleRate > 0 ? totalSamples / sampleRate : 0;
                }
            }
        }
        catch { }
        return 0;
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

    static string DefaultTemplate()
    {
        return @"───────────────────────────────
Album:   {ALBUM}
Artist:  {ARTIST}
Year:    {YEAR}
Genre:   {GENRE}
───────────────────────────────

Tracklist (Duration):
{TRACKLIST}

Files:
{FILELIST}

-------MD5 Checksums-------
// Not Implemented Yet!!!!!!
───────────────────────────────
Ripped by: {RIPPER}
Encoded by: {ENCODER}
Date: {DATE}
───────────────────────────────
Comment:
{COMMENT}
";
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

class Options
{
    public string? InputPath { get; set; } public string? TemplatePath { get; set; } public string? OutputPath { get; set; } public bool Recursive { get; set; } = false; public bool ShowHelp { get; set; } = false; public string? Ripper { get; set; } public string? Encoder { get; set; } public string? Comment { get; set; } public bool IncludeMD5 { get; set; } = false; public string? Album { get; set; } public string? Artist { get; set; } public string? Year { get; set; } public string? Genre { get; set; }

    public static Options Parse(string[] args)
    {
        var o = new Options();
        if (args == null || args.Length == 0) return o;
        for (int i = 0; i < args.Length; i++) {
            var a = args[i];
            switch (a) {
                case "--help": case "-h": o.ShowHelp = true; break; case "--template": o.TemplatePath = NextArg(args, ref i); break; case "--out": case "--output": o.OutputPath = NextArg(args, ref i); break; case "--recursive": o.Recursive = true; break; case "--ripper": o.Ripper = NextArg(args, ref i); break; case "--encoder": o.Encoder = NextArg(args, ref i); break; case "--comment": o.Comment = NextArg(args, ref i); break; case "--md5": case "-m": o.IncludeMD5 = true; break; case "--album": case "-a": o.Album = NextArg(args, ref i); break; case "--artist": case "-A": o.Artist = NextArg(args, ref i); break; case "--year": case "-y": o.Year = NextArg(args, ref i); break; case "--genre": case "-g": o.Genre = NextArg(args, ref i); break;
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