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
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using Gibbed.FarCry3.FileFormats;
using Gibbed.IO;
using ICSharpCode.SharpZipLib.Zip.Compression.Streams;
using NDesk.Options;
using Assembly = System.Reflection.Assembly;
using CustomMap = Gibbed.FarCry3.FileFormats.CustomMap;

namespace Gibbed.FarCry3.CustomMapStrip
{
    internal class Program
    {
        private static string GetExecutableName()
        {
            return Path.GetFileName(Assembly.GetExecutingAssembly().Location);
        }

        private static CustomMap.CompressedData MakeCompressedData(Stream input)
        {
            var cd = new CustomMap.CompressedData();

            using (var data = new MemoryStream())
            {
                uint virtualOffset = 0;
                uint realOffset = 4;
                while (input.Position < input.Length)
                {
                    var length = (uint)Math.Min(0x40000, input.Length - input.Position);

                    using (var block = new MemoryStream())
                    {
                        var zlib = new DeflaterOutputStream(block); //, new Deflater(9, false));
                        zlib.WriteFromStream(input, length);
                        zlib.Finish();

                        cd.Blocks.Add(new CustomMap.CompressedData.Block()
                        {
                            VirtualOffset = virtualOffset,
                            FileOffset = realOffset,
                            IsCompressed = true,
                        });

                        block.Position = 0;
                        data.WriteFromStream(block, block.Length);

                        realOffset += (uint)block.Length;
                    }

                    virtualOffset += length;
                }

                data.Position = 0;
                cd.Data = new byte[data.Length];
                data.Read(cd.Data, 0, cd.Data.Length);

                cd.Blocks.Add(new CustomMap.CompressedData.Block()
                {
                    VirtualOffset = virtualOffset,
                    FileOffset = realOffset,
                    IsCompressed = true,
                });
            }

            return cd;
        }

        private static void Main(string[] args)
        {
            bool showHelp = false;
            bool verbose = false;

            var options = new OptionSet()
            {
                {"v|verbose", "be verbose", v => verbose = v != null},
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
                Console.WriteLine("Usage: {0} [OPTIONS]+ input_fc3map [output_fc3map]", GetExecutableName());
                Console.WriteLine();
                Console.WriteLine("Options:");
                options.WriteOptionDescriptions(Console.Out);
                return;
            }

            string inputPath = extras[0];
            string outputPath = extras.Count > 1
                                    ? extras[1]
                                    : Path.ChangeExtension(Path.ChangeExtension(inputPath, null) + "_stripped",
                                                           Path.GetExtension(inputPath));

            var map = new CustomMapGameFile();
            using (var input = File.OpenRead(inputPath))
            {
                map.Deserialize(input);
            }

            var wantedFileNamesToStrip = new[]
            {
                @"ige\collection.mask",
                @"ige\heightmap.raw",
                @"ige\holemap.raw",
                @"ige\map.xml",
                @"ige\texture.mask",
            };
            var wantedFileHashesToStrip = wantedFileNamesToStrip
                .Select(fn => Dunia2.FileFormats.CRC64.Hash(fn))
                .ToArray();

            var strippedMap = (CustomMapGameFile)map.Clone();

            var fat = new Dunia2.FileFormats.BigFile();
            using (var input = map.Archive.Header.Unpack())
            {
                fat.Deserialize(input);
            }

            var fileHashesToStrip = fat.Entries
                                       .Select(e => e.NameHash)
                                       .Intersect(wantedFileHashesToStrip)
                                       .ToArray();

            if (verbose == true)
            {
                Console.WriteLine("Stripping {0} files...", fileHashesToStrip.Length);
            }

            if (fileHashesToStrip.Length > 0)
            {
                var strippedFat = new Dunia2.FileFormats.BigFile
                {
                    Version = fat.Version,
                    Platform = fat.Platform,
                    Unknown74 = fat.Unknown74
                };

                using (var input = map.Archive.Data.Unpack())
                using (var output = new MemoryStream())
                {
                    var strippedEntries = new List<Dunia2.FileFormats.Big.Entry>();

                    foreach (var entry in fat.Entries.Where(e => fileHashesToStrip.Contains(e.NameHash) == false))
                    {
                        var rebuiltEntry = new Dunia2.FileFormats.Big.Entry
                        {
                            NameHash = entry.NameHash,
                            UncompressedSize = entry.UncompressedSize,
                            CompressedSize = entry.CompressedSize,
                            Offset = output.Position,
                            CompressionScheme = entry.CompressionScheme
                        };

                        input.Seek(entry.Offset, SeekOrigin.Begin);
                        output.WriteFromStream(input, entry.CompressedSize);
                        output.Seek(output.Position.Align(16), SeekOrigin.Begin);

                        strippedEntries.Add(rebuiltEntry);
                    }

                    strippedFat.Entries.AddRange(strippedEntries.OrderBy(e => e.NameHash));

                    output.Position = 0;
                    strippedMap.Archive.Data = MakeCompressedData(output);
                }

                using (var output = new MemoryStream())
                {
                    strippedFat.Serialize(output);
                    output.Position = 0;
                    strippedMap.Archive.Header = MakeCompressedData(output);
                }

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
                    strippedMap.Archive.Descriptor = MakeCompressedData(output);
                }
            }

            using (var output = File.Create(outputPath))
            {
                strippedMap.Serialize(output);
            }
        }
    }
}
