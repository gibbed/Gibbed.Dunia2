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
using System.IO;
using System.Xml;
using Gibbed.Dunia2.FileFormats;
using Gibbed.FarCry3.FileFormats;
using Gibbed.IO;
using NDesk.Options;
using Big = Gibbed.Dunia2.FileFormats.Big;
using CompressionScheme = Gibbed.Dunia2.FileFormats.Big.CompressionScheme;
using EntryDecompression = Gibbed.Dunia2.FileFormats.Big.EntryDecompression;

namespace Gibbed.FarCry3.MapUnpack
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

            var map = new MapFile();
            using (var input = File.OpenRead(inputPath))
            {
                map.Deserialize(input);
            }

            Directory.CreateDirectory(outputPath);
            using (var output = File.Create(Path.Combine(outputPath, "map.xml")))
            {
                var settings = new XmlWriterSettings();
                settings.Indent = true;

                using (var writer = XmlWriter.Create(output, settings))
                {
                    writer.WriteStartDocument();
                    writer.WriteStartElement("map");

                    writer.WriteStartElement("info");
                    writer.WriteElementString("name", map.Info.Name);
                    writer.WriteElementString("creator", map.Info.Creator);
                    writer.WriteElementString("author", map.Info.Author);
                    writer.WriteStartElement("map_id");
                    writer.WriteElementString("guid", map.Info.Id.Guid.ToString());
                    writer.WriteElementString("unknown2", map.Info.Id.Unknown2.ToString());
                    writer.WriteElementString("unknown3", map.Info.Id.Unknown3.ToString());
                    writer.WriteEndElement();
                    writer.WriteElementString("version_id", map.Info.VersionId.ToString());
                    writer.WriteElementString("time_modified", map.Info.TimeModified.ToString());
                    writer.WriteElementString("time_created", map.Info.TimeCreated.ToString());
                    writer.WriteElementString("unknown2", map.Info.Unknown2.ToString());
                    writer.WriteElementString("unknown3", map.Info.Unknown3.ToString());
                    writer.WriteElementString("unknown4", map.Info.Unknown4.ToString());
                    writer.WriteElementString("unknown5", map.Info.Unknown5.ToString());
                    writer.WriteElementString("unknown7", map.Info.Unknown7.ToString());
                    writer.WriteElementString("map_size", map.Info.Size.ToString());
                    writer.WriteElementString("player_range", map.Info.PlayerRange.ToString());
                    writer.WriteElementString("unknown16", map.Info.Unknown16.ToString());
                    writer.WriteElementString("unknown17", map.Info.Unknown17.ToString());
                    writer.WriteEndElement();

                    writer.WriteStartElement("snapshot");
                    writer.WriteElementString("width", map.Snapshot.Width.ToString());
                    writer.WriteElementString("height", map.Snapshot.Height.ToString());
                    writer.WriteElementString("bpp", map.Snapshot.BytesPerPixel.ToString());
                    writer.WriteElementString("unknown4", map.Snapshot.Unknown4.ToString());
                    writer.WriteEndElement();

                    if (map.ExtraSnapshot != null)
                    {
                        writer.WriteStartElement("extra_snapshot");
                        writer.WriteElementString("width", map.ExtraSnapshot.Width.ToString());
                        writer.WriteElementString("height", map.ExtraSnapshot.Height.ToString());
                        writer.WriteElementString("bpp", map.ExtraSnapshot.BytesPerPixel.ToString());
                        writer.WriteElementString("unknown4", map.ExtraSnapshot.Unknown4.ToString());
                        writer.WriteEndElement();
                    }

                    writer.WriteStartElement("data");
                    writer.WriteElementString("unknown1", map.Data.Unknown1);
                    writer.WriteEndElement();

                    writer.WriteEndElement();
                    writer.WriteEndDocument();
                }
            }

            using (var input = map.Archive.Descriptor.Unpack())
            {
                using (var output = File.Create(Path.Combine(outputPath, "archive.xml")))
                {
                    output.WriteFromStream(input, input.Length);
                }
            }

            using (var output = File.Create(Path.Combine(outputPath, "snapshot.bin")))
            {
                output.Write(map.Snapshot.Data, 0, map.Snapshot.Data.Length);
            }

            if (map.ExtraSnapshot != null)
            {
                using (var output = File.Create(Path.Combine(outputPath, "extra_snapshot.bin")))
                {
                    output.Write(map.ExtraSnapshot.Data, 0, map.ExtraSnapshot.Data.Length);
                }
            }

            using (var output = File.Create(Path.Combine(outputPath, "snapshot.png")))
            {
                output.Write(map.Data.Unknown2, 0, map.Data.Unknown2.Length);
            }

            var fat = new BigFile();
            using (var input = map.Archive.Header.Unpack())
            {
                input.Position = 0;
                fat.Deserialize(input);
            }

            var hashes = manager.LoadListsFileNames(fat.Version);

            var dataPath = Path.Combine(outputPath, "archive");
            Directory.CreateDirectory(dataPath);

            using (var data = map.Archive.Data.Unpack())
            {
                long current = 0;
                long total = fat.Entries.Count;

                foreach (var entry in fat.Entries)
                {
                    current++;

                    var entryName = GetEntryName(data, fat, entry, hashes);

                    var entryPath = Path.Combine(dataPath, entryName);

                    var entryParent = Path.GetDirectoryName(entryPath);
                    if (string.IsNullOrEmpty(entryParent) == false)
                    {
                        Directory.CreateDirectory(entryParent);
                    }

                    if (verbose == true)
                    {
                        Console.WriteLine("[{0}/{1}] {2}",
                                          current,
                                          total,
                                          entryName);
                    }

                    using (var output = File.Create(entryPath))
                    {
                        EntryDecompression.Decompress(entry, data, output);
                    }
                }
            }
        }

        private static string GetEntryName(Stream input,
                                           BigFile fat,
                                           Big.Entry entry,
                                           ProjectData.HashList<ulong> hashes)
        {
            var entryName = hashes[entry.NameHash];

            if (entryName == null)
            {
                string type;
                string extension;
                {
                    var guess = new byte[64];
                    int read = 0;

                    if (entry.CompressionScheme == CompressionScheme.None)
                    {
                        if (entry.CompressedSize > 0)
                        {
                            input.Seek(entry.Offset, SeekOrigin.Begin);
                            read = input.Read(guess, 0, (int)Math.Min(entry.CompressedSize, guess.Length));
                        }
                    }
                    else
                    {
                        using (var temp = new MemoryStream())
                        {
                            EntryDecompression.Decompress(entry, input, temp);
                            temp.Position = 0;
                            read = temp.Read(guess, 0, (int)Math.Min(temp.Length, guess.Length));
                        }
                    }

                    var tuple = FileExtensions.Detect(guess, Math.Min(guess.Length, read));
                    type = tuple != null ? tuple.Item1 : "unknown";
                    extension = tuple != null ? tuple.Item2 : null;
                }

                if (fat.Version >= 9)
                {
                    entryName = entry.NameHash.ToString("X16");
                }
                else
                {
                    entryName = entry.NameHash.ToString("X8");
                }

                if (string.IsNullOrEmpty(extension) == false)
                {
                    entryName = Path.ChangeExtension(entryName, "." + extension);
                }

                if (string.IsNullOrEmpty(type) == false)
                {
                    entryName = Path.Combine(type, entryName);
                }

                entryName = Path.Combine("__UNKNOWN", entryName);
            }
            else
            {
                entryName = entryName.Replace("/", "\\");
                if (entryName.StartsWith("\\") == true)
                {
                    entryName = entryName.Substring(1);
                }
            }

            return entryName;
        }
    }
}
