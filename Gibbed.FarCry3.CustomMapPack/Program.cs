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
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Xml;
using Gibbed.FarCry3.FileFormats;
using Gibbed.IO;
using NDesk.Options;
using Newtonsoft.Json;
using CustomMap = Gibbed.FarCry3.FileFormats.CustomMap;
using MapConfiguration = Gibbed.FarCry3.CustomMapUnpack.MapConfiguration;

namespace Gibbed.FarCry3.CustomMapPack
{
    public class Program
    {
        private static string GetExecutableName()
        {
            return Path.GetFileName(System.Reflection.Assembly.GetExecutingAssembly().CodeBase);
        }

        public static void Main(string[] args)
        {
            bool showHelp = false;
            bool useDescriptorFromFile = false;
            bool verbose = false;

            var options = new OptionSet()
            {
                {"v|verbose", "be verbose", v => verbose = v != null},
                {"fd|use-descriptor-from-file", v => useDescriptorFromFile = v != null},
                {"h|help", "show this message and exit", v => showHelp = v != null},
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
                Console.WriteLine("Usage: {0} [OPTIONS]+ input_dir [output_fc3map]", GetExecutableName());
                Console.WriteLine();
                Console.WriteLine("Options:");
                options.WriteOptionDescriptions(Console.Out);
                return;
            }

            string inputPath = extras[0];
            string outputPath = extras.Count > 1 ? extras[1] : Path.ChangeExtension(inputPath, null) + ".fc3map";

            MapConfiguration config;
            using (var input = File.OpenRead(Path.Combine(inputPath, "config.json")))
            using (var streamReader = new StreamReader(input))
            using (var jsonReader = new JsonTextReader(streamReader))
            {
                var serializer = new JsonSerializer();
                config = serializer.Deserialize<MapConfiguration>(jsonReader);
            }

            var map = new CustomMapGameFile()
            {
                Endian = config.Endian,
            };

            map.Header = new CustomMapGameFileHeader()
            {
                Unknown2 = config.Header.Unknown2,
                Unknown3 = config.Header.Unknown3,
                Unknown4 = config.Header.Unknown4,
                Unknown5 = config.Header.Unknown5,
                Creator = config.Header.Creator,
                Unknown7 = config.Header.Unknown7,
                Author = config.Header.Author,
                Name = config.Header.Name,
                MapId = new CustomMap.MapId()
                {
                    Guid = config.Header.MapId.Guid,
                    Unknown2 = config.Header.MapId.Unknown2,
                    Unknown3 = config.Header.MapId.Unknown3,
                },
                VersionId = config.Header.VersionId,
                TimeModified = config.Header.TimeModified,
                TimeCreated = config.Header.TimeCreated,
                MapSize = config.Header.MapSize,
                PlayerRange = config.Header.PlayerRange,
                Unknown16 = config.Header.Unknown16,
                Unknown17 = config.Header.Unknown17,
            };

            if (string.IsNullOrEmpty(config.SnapshotPath) == true)
            {
                map.Snapshot = new Snapshot();
            }
            else
            {
                using (var input = File.OpenRead(Path.Combine(inputPath, config.SnapshotPath)))
                {
                    map.Snapshot = ImportSnapshot(input, map.Endian);
                }
            }

            if (string.IsNullOrEmpty(config.ExtraSnapshotPath) == true)
            {
                map.ExtraSnapshot = new Snapshot();
            }
            else
            {
                using (var input = File.OpenRead(Path.Combine(inputPath, config.ExtraSnapshotPath)))
                {
                    map.ExtraSnapshot = ImportSnapshot(input, map.Endian);
                }
            }

            if (config.Data != null)
            {
                map.Data.Unknown1 = config.Data.Unknown1;

                if (string.IsNullOrEmpty(config.Data.Unknown2Path) == true)
                {
                    map.Data.Unknown2 = new byte[0];
                }
                else
                {
                    map.Data.Unknown2 = File.ReadAllBytes(Path.Combine(inputPath, config.Data.Unknown2Path));
                }
            }

            if (string.IsNullOrEmpty(config.FilesystemHeaderPath) == false)
            {
                using (var input = File.OpenRead(Path.Combine(inputPath, config.FilesystemHeaderPath)))
                {
                    map.Archive.Header = CustomMap.CompressedData.Pack(input);
                }
            }

            if (string.IsNullOrEmpty(config.FilesystemDataPath) == false)
            {
                using (var input = File.OpenRead(Path.Combine(inputPath, config.FilesystemDataPath)))
                {
                    map.Archive.Data = CustomMap.CompressedData.Pack(input);
                }
            }

            if (useDescriptorFromFile == true)
            {
                if (string.IsNullOrEmpty(config.FilesystemDescriptorPath) == false)
                {
                    using (var input = File.OpenRead(Path.Combine(inputPath, config.FilesystemDescriptorPath)))
                    {
                        map.Archive.Descriptor = CustomMap.CompressedData.Pack(input);
                    }
                }
            }
            else
            {
                using (var input = File.OpenRead(Path.Combine(inputPath, config.FilesystemHeaderPath)))
                {
                    var fat = new Dunia2.FileFormats.BigFile();
                    fat.Deserialize(input);

                    using (var output = new MemoryStream())
                    {
                        var settings = new XmlWriterSettings();
                        settings.Indent = true;
                        settings.OmitXmlDeclaration = true;
                        settings.IndentChars = "\t";
                        settings.Encoding = Encoding.ASCII;

                        using (var writer = XmlWriter.Create(output, settings))
                        {
                            writer.WriteStartDocument();
                            writer.WriteStartElement("FatInfo");

                            foreach (var entry in fat.Entries.OrderBy(e => e.NameHash))
                            {
                                writer.WriteStartElement("File");
                                writer.WriteAttributeString("Path",
                                                            entry.NameHash.ToString(CultureInfo.InvariantCulture));
                                writer.WriteAttributeString("Crc", entry.NameHash.ToString(CultureInfo.InvariantCulture));
                                writer.WriteAttributeString("CrcHex",
                                                            entry.NameHash.ToString("x16", CultureInfo.InvariantCulture));
                                writer.WriteAttributeString("FileTime", "0");
                                writer.WriteAttributeString("SeekPos",
                                                            entry.Offset.ToString(CultureInfo.InvariantCulture));
                                writer.WriteEndElement();
                            }

                            writer.WriteEndElement();
                            writer.WriteEndDocument();
                        }

                        output.Position = 0;
                        map.Archive.Descriptor = CustomMap.CompressedData.Pack(output);
                    }
                }
            }

            using (var output = File.Create(outputPath))
            {
                map.Serialize(output);
            }
        }

        private static Snapshot ImportSnapshot(Stream input, Endian endian)
        {
            using (var bitmap = (Bitmap)Image.FromStream(input, false, true))
            {
                var snapshot = new Snapshot()
                {
                    Width = (uint)bitmap.Width,
                    Height = (uint)bitmap.Height,
                    BytesPerPixel = 4,
                    BitsPerComponent = 8,
                    Data = GetArgbBytesFromBitmap(bitmap),
                };

                if (endian == Endian.Big)
                {
                    snapshot.Data = SwapArgb(snapshot.Data);
                }

                return snapshot;
            }
        }

        private static byte[] SwapArgb(byte[] bytes)
        {
            var temp = new byte[bytes.Length];
            for (uint i = 0; i < bytes.Length; i += 4)
            {
                temp[i + 0] = bytes[i + 3];
                temp[i + 1] = bytes[i + 2];
                temp[i + 2] = bytes[i + 1];
                temp[i + 3] = bytes[i + 0];
            }
            return temp;
        }

        private static byte[] GetArgbBytesFromBitmap(Bitmap bitmap)
        {
            var bytes = new byte[bitmap.Width * bitmap.Height * 4];

            var area = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
            var data = bitmap.LockBits(area, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            var scan = data.Scan0;
            var run = data.Width * 4;
            for (int x = 0, y = 0; y < data.Height; x += run, y++)
            {
                Marshal.Copy(scan, bytes, x, run);
                scan += data.Stride;
            }
            bitmap.UnlockBits(data);
            return bytes;
        }
    }
}
