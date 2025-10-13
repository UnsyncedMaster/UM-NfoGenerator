using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UM_NfoGenerator
{
    internal class Duration
    {
        public static double GetDuration(string path)
        {
            string ext = Path.GetExtension(path).ToLowerInvariant();
            try
            {
                using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read))
                using (var br = new BinaryReader(fs))
                {
                    if (ext == ".wav")
                    {
                        fs.Position = 0;
                        if (Encoding.ASCII.GetString(br.ReadBytes(4)) != "RIFF")
                            return 0;

                        fs.Position += 4;
                        if (Encoding.ASCII.GetString(br.ReadBytes(4)) != "WAVE")
                            return 0;

                        int channels = 0;
                        int sampleRate = 0;
                        int bitsPerSample = 0;
                        int dataSize = 0;

                        while (fs.Position < fs.Length - 8)
                        {
                            string chunkId = Encoding.ASCII.GetString(br.ReadBytes(4));
                            int chunkSize = br.ReadInt32();

                            if (chunkId == "fmt ")
                            {
                                short audioFormat = br.ReadInt16();
                                channels = br.ReadInt16();
                                sampleRate = br.ReadInt32();
                                fs.Position += 6;
                                bitsPerSample = br.ReadInt16();
                                fs.Position += chunkSize - 16;
                            }
                            else if (chunkId == "data")
                            {
                                dataSize = chunkSize;
                                break;
                            }
                            else
                            {
                                fs.Position += chunkSize;
                            }
                        }

                        if (sampleRate > 0 && channels > 0 && bitsPerSample > 0 && dataSize > 0)
                        {
                            return dataSize / (double)(sampleRate * channels * (bitsPerSample / 8.0));
                        }

                        return 0;
                    }

                    else if (ext == ".flac")
                    {
                        fs.Position = 4;
                        bool lastBlock = false;
                        while (!lastBlock && fs.Position < fs.Length)
                        {
                            byte header = br.ReadByte();
                            lastBlock = (header & 0x80) != 0;
                            int blockType = header & 0x7F;
                            int length = (br.ReadByte() << 16) | (br.ReadByte() << 8) | br.ReadByte();

                            if (blockType == 0)
                            {
                                byte[] data = br.ReadBytes(length);
                                if (data.Length >= 18)
                                {
                                    int sampleRate = (data[10] << 12) | (data[11] << 4) | ((data[12] & 0xF0) >> 4);
                                    long totalSamples =
                                        ((long)(data[13] & 0x0F) << 32) |
                                        ((long)data[14] << 24) |
                                        ((long)data[15] << 16) |
                                        ((long)data[16] << 8) |
                                        (long)data[17];
                                    if (sampleRate > 0)
                                        return totalSamples / (double)sampleRate;
                                }
                                break;
                            }
                            else fs.Position += length;
                        }
                        return 0;
                    }

                    if (ext == ".m4a" || ext == ".mp4")
                    {
                        using var fsM4a = File.OpenRead(path);
                        using var brM4a = new BinaryReader(fsM4a);
                        return Mp4Duration(fsM4a, brM4a, fsM4a.Length);
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
            catch
            {
                return 0;
            }
            return 0;
        }

        static uint ReadUInt32BigEndian(BinaryReader br)
        {
            byte[] bytes = br.ReadBytes(4);
            if (bytes.Length < 4) return 0;
            Array.Reverse(bytes);
            return BitConverter.ToUInt32(bytes, 0);
        }
        static ulong ReadUInt64BigEndian(BinaryReader br)
        {
            byte[] bytes = br.ReadBytes(8);
            if (bytes.Length < 8) return 0;
            Array.Reverse(bytes);
            return BitConverter.ToUInt64(bytes, 0);
        }

        static double Mp4Duration(FileStream fs2, BinaryReader br2, long end)
        {
            double bestDuration = 0;

            while (fs2.Position < end - 8)
            {
                long atomStart = fs2.Position;
                uint atomSize = ReadUInt32BigEndian(br2);
                string atomType = new string(br2.ReadChars(4));

                if (atomSize < 8 || atomSize > fs2.Length - atomStart)
                    break;

                long atomEnd = atomStart + atomSize;

                if (atomType == "mdhd")
                {
                    byte version = br2.ReadByte();
                    br2.ReadBytes(3);

                    if (version == 1)
                    {
                        br2.ReadBytes(16);
                        uint timescale = ReadUInt32BigEndian(br2);
                        ulong duration = ReadUInt64BigEndian(br2);
                        if (timescale > 0)
                            bestDuration = Math.Max(bestDuration, duration / (double)timescale);
                    }
                    else
                    {
                        br2.ReadBytes(8);
                        uint timescale = ReadUInt32BigEndian(br2);
                        uint duration = ReadUInt32BigEndian(br2);
                        if (timescale > 0)
                            bestDuration = Math.Max(bestDuration, duration / (double)timescale);
                    }
                }
                else if (atomType == "trak" || atomType == "moov" || atomType == "mdia")
                {
                    double nested = Mp4Duration(fs2, br2, atomEnd);
                    bestDuration = Math.Max(bestDuration, nested);
                }
                else
                {
                    fs2.Position = atomEnd;
                }
            }

            return bestDuration;
        }
        public static string FormatDuration(double totalSeconds)
        {
            int minutes = (int)(totalSeconds / 60); int seconds = (int)(totalSeconds % 60); return $"{minutes:D2}:{seconds:D2}";
        }

    }
    }
