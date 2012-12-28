/* Copyright (c) 2012 Rick (rick 'at' gibbed 'dot' us)
 * 
 * This software is provided 'as-is', without any express or implied
 * warranty. In no event will the authors be held liable for any damages
 * arising from the use of this software.
 * 
 * Permission is granted to anyone to use this software for any purpose,
 * including commercial applications, and to alter it and redistribute it
 * freely, subject to the following restrictions:
 * 
 * 1. The origin of this software must not be misrepresented; you must not
 *    claim that you wrote the original software. If you use this software
 *    in a product, an acknowledgment in the product documentation would
 *    be appreciated but is not required.
 * 
 * 2. Altered source versions must be plainly marked as such, and must not
 *    be misrepresented as being the original software.
 * 
 * 3. This notice may not be removed or altered from any source
 *    distribution.
 */

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Gibbed.FarCry3.FileFormats;
using Gibbed.IO;
using NDesk.Options;
using Assembly = System.Reflection.Assembly;

namespace Gibbed.FarCry3.CustomMapUnpack
{
    internal class Program
    {
        private static string GetExecutableName()
        {
            return Path.GetFileName(Assembly.GetExecutingAssembly().Location);
        }

        private static void Main(string[] args)
        {
            bool showHelp = false;
            bool verbose = false;

            var options = new OptionSet()
            {
                {
                    "v|verbose",
                    "be verbose",
                    v => verbose = v != null
                },
                {
                    "h|help",
                    "show this message and exit",
                    v => showHelp = v != null
                },
            };

            List<string> extras;

            try
            {
                extras = options.Parse(args);
            }
            catch (OptionException e)
            {
                Console.Write("{0}: ", GetExecutableName());
                Console.WriteLine(e.Message);
                Console.WriteLine("Try `{0} --help' for more information.", GetExecutableName());
                return;
            }

            if (extras.Count < 1 || extras.Count > 2 || showHelp == true)
            {
                Console.WriteLine("Usage: {0} [OPTIONS]+ input_fc3map [output_dir]", GetExecutableName());
                Console.WriteLine();
                Console.WriteLine("Options:");
                options.WriteOptionDescriptions(Console.Out);
                return;
            }

            string inputPath = extras[0];
            string outputPath = extras.Count > 1 ? extras[1] : Path.ChangeExtension(inputPath, null) + "_unpack";

            var manager = ProjectData.Manager.Load();
            if (manager.ActiveProject == null)
            {
                Console.WriteLine("Warning: no active project loaded.");
            }

            var map = new CustomMapGameFile();
            using (var input = File.OpenRead(inputPath))
            {
                map.Deserialize(input);
            }

            Directory.CreateDirectory(outputPath);

            var config = new MapConfiguration();

            if (map.Header != null)
            {
                config.Header = new MapHeader()
                {
                    Unknown2 = map.Header.Unknown2,
                    Unknown3 = map.Header.Unknown3,
                    Unknown4 = map.Header.Unknown4,
                    Unknown5 = map.Header.Unknown5,
                    Creator = map.Header.Creator,
                    Unknown7 = map.Header.Unknown7,
                    Author = map.Header.Author,
                    Name = map.Header.Name,
                    MapId = new MapId()
                    {
                        Guid = map.Header.MapId.Guid,
                        Unknown2 = map.Header.MapId.Unknown2,
                        Unknown3 = map.Header.MapId.Unknown3,
                    },
                    VersionId = map.Header.VersionId,
                    TimeModified = map.Header.TimeModified,
                    TimeCreated = map.Header.TimeCreated,
                    MapSize = map.Header.MapSize,
                    PlayerRange = map.Header.PlayerRange,
                    Unknown16 = map.Header.Unknown16,
                    Unknown17 = map.Header.Unknown17,
                };
            }

            if (map.Snapshot != null)
            {
                using (var temp = new MemoryStream())
                {
                    var extension = ExportSnapshot(map.Snapshot, temp);
                    config.SnapshotPath = Path.ChangeExtension("snapshot", extension);

                    using (var output = File.Create(Path.Combine(outputPath, config.SnapshotPath)))
                    {
                        temp.Position = 0;
                        output.WriteFromStream(temp, temp.Length);
                    }
                }
            }

            if (map.ExtraSnapshot != null)
            {
                using (var temp = new MemoryStream())
                {
                    var extension = ExportSnapshot(map.ExtraSnapshot, temp);
                    config.SnapshotPath = Path.ChangeExtension("extra_snapshot", extension);

                    using (var output = File.Create(Path.Combine(outputPath, config.SnapshotPath)))
                    {
                        temp.Position = 0;
                        output.WriteFromStream(temp, temp.Length);
                    }
                }
            }

            if (map.Data != null)
            {
                config.Data = new MapData()
                {
                    Unknown1 = map.Data.Unknown1,
                    Unknown2Path = "data_unknown2.bin",
                };

                using (var output = File.Create(Path.Combine(outputPath, "data_unknown2.bin")))
                {
                    output.WriteBytes(map.Data.Unknown2);
                }
            }

            using (var input = map.Archive.Header.Unpack())
            {
                config.FilesystemHeaderPath = "filesystem.fat";
                using (var output = File.Create(Path.Combine(outputPath, config.FilesystemHeaderPath)))
                {
                    output.WriteFromStream(input, input.Length);
                }
            }

            using (var input = map.Archive.Data.Unpack())
            {
                config.FilesystemDataPath = "filesystem.dat";
                using (var output = File.Create(Path.Combine(outputPath, config.FilesystemDataPath)))
                {
                    output.WriteFromStream(input, input.Length);
                }
            }

            using (var input = map.Archive.Descriptor.Unpack())
            {
                config.FilesystemDescriptorPath = "filesystem.xml";
                using (var output = File.Create(Path.Combine(outputPath, config.FilesystemDescriptorPath)))
                {
                    output.WriteFromStream(input, input.Length);
                }
            }

            using (var output = new StreamWriter(Path.Combine(outputPath, "config.json"), false, Encoding.Unicode))
            using (var writer = new Newtonsoft.Json.JsonTextWriter(output))
            {
                writer.Indentation = 2;
                writer.IndentChar = ' ';
                writer.Formatting = Newtonsoft.Json.Formatting.Indented;

                var serializer = new Newtonsoft.Json.JsonSerializer();
                serializer.Serialize(writer, config);
            }

            using (var output = new StreamWriter(Path.Combine(outputPath, "filesystem_unpack.bat")))
            {
                output.WriteLine("@echo off");
                output.WriteLine("\"{0}\" -v -o \"filesystem.fat\" \"filesystem\"",
                                 Assembly.GetAssembly(typeof(Dunia2.Unpack.Dummy)).Location);
                output.WriteLine("pause");
            }

            using (var output = new StreamWriter(Path.Combine(outputPath, "filesystem_pack.bat")))
            {
                output.WriteLine("@echo off");
                output.WriteLine("\"{0}\" -v \"filesystem.fat\" \"filesystem\"",
                                 Assembly.GetAssembly(typeof(Dunia2.Pack.Dummy)).Location);
                output.WriteLine("pause");
            }
        }

        private static string ExportSnapshot(Snapshot snapshot, MemoryStream output)
        {
            if (snapshot == null)
            {
                return null;
            }

            if (snapshot.BytesPerPixel == 4 &&
                snapshot.BitsPerComponent == 8)
            {
                using (var bitmap = MakeBitmapFromArgb(snapshot.Width, snapshot.Height, snapshot.Data))
                {
                    bitmap.Save(output, ImageFormat.Png);
                    return ".png";
                }
            }

            throw new NotSupportedException();
        }

        private static Bitmap MakeBitmapFromArgb(
            uint width, uint height, byte[] input)
        {
            var output = new byte[width * height * 4];
            var bitmap = new Bitmap((int)width,
                                    (int)height,
                                    PixelFormat.Format32bppArgb);

            for (uint i = 0; i < width * height * 4; i += 4)
            {
                output[i + 0] = input[i + 3];
                output[i + 1] = input[i + 2];
                output[i + 2] = input[i + 1];
                output[i + 3] = input[i + 0];
            }

            var area = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
            var data = bitmap.LockBits(area, ImageLockMode.WriteOnly, bitmap.PixelFormat);
            Marshal.Copy(output, 0, data.Scan0, output.Length);
            bitmap.UnlockBits(data);
            return bitmap;
        }
    }
}
