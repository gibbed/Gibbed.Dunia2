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
using Gibbed.Dunia2.FileFormats;
using Gibbed.IO;
using ICSharpCode.SharpZipLib.Zip.Compression;
using ICSharpCode.SharpZipLib.Zip.Compression.Streams;
using NDesk.Options;

namespace Gibbed.Dunia2.Unpack
{
    internal class Program
    {
        private static string GetExecutableName()
        {
            return Path.GetFileName(System.Reflection.Assembly.GetExecutingAssembly().CodeBase);
        }

        public static void Main(string[] args)
        {
            bool showHelp = false;
            bool extractUnknowns = true;
            bool overwriteFiles = false;
            bool verbose = false;

            var options = new OptionSet()
            {
                {
                    "o|overwrite",
                    "overwrite existing files",
                    v => overwriteFiles = v != null
                    },
                {
                    "nu|no-unknowns",
                    "don't extract unknown files",
                    v => extractUnknowns = v == null
                    },
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
                Console.WriteLine("Usage: {0} [OPTIONS]+ input_sfar [output_dir]", GetExecutableName());
                Console.WriteLine();
                Console.WriteLine("Options:");
                options.WriteOptionDescriptions(Console.Out);
                return;
            }

            string fatPath = extras[0];
            string outputPath = extras.Count > 1 ? extras[1] : Path.ChangeExtension(fatPath, null) + "_unpack";
            string datPath;

            if (Path.GetExtension(fatPath) == ".dat")
            {
                datPath = fatPath;
                fatPath = Path.ChangeExtension(fatPath, ".fat");
            }
            else
            {
                datPath = Path.ChangeExtension(fatPath, ".dat");
            }

            var manager = ProjectData.Manager.Load();
            if (manager.ActiveProject == null)
            {
                Console.WriteLine("Warning: no active project loaded.");
            }

            ProjectData.HashList<ulong> hashes;

            BigFile fat;
            using (var header = File.OpenRead(fatPath))
            {
                fat = new BigFile();
                fat.Deserialize(header);
            }

            if (fat.Version >= 9) // TODO: check if this is right...
            {
                hashes = manager.LoadLists("*.filelist",
                                           a => CRC64.Hash(a.ToLowerInvariant()),
                                           s => s.Replace("/", "\\"));
            }
            else
            {
                hashes = manager.LoadLists("*.filelist",
                                           a => (ulong)CRC32.Hash(a.ToLowerInvariant()),
                                           s => s.Replace("\\", "/"));
            }

            using (var data = File.OpenRead(datPath))
            {
                long current = 0;
                long total = fat.Entries.Count;
                var padding = total.ToString(CultureInfo.InvariantCulture).Length;

                foreach (var entry in fat.Entries.OrderBy(e => e.Offset))
                {
                    current++;

                    var entryName = hashes[entry.NameHash];
                    if (entryName == null)
                    {
                        if (extractUnknowns == false)
                        {
                            continue;
                        }

                        string type;
                        string extension;
                        {
                            var guess = new byte[64];
                            int read = 0;

                            if (entry.CompressionScheme == FileFormats.Big.CompressionScheme.None)
                            {
                                if (entry.CompressedSize > 0)
                                {
                                    data.Seek(entry.Offset, SeekOrigin.Begin);
                                    read = data.Read(guess, 0, (int)Math.Min(entry.CompressedSize, guess.Length));
                                }
                            }
                            else
                            {
                                using (var temp = new MemoryStream())
                                {
                                    DecompressEntry(entry, data, temp);
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

                    var entryPath = Path.Combine(outputPath, entryName);
                    if (overwriteFiles == false &&
                        File.Exists(entryPath) == true)
                    {
                        continue;
                    }

                    if (verbose == true)
                    {
                        Console.WriteLine("[{0}/{1}] {2}",
                                          current.ToString(CultureInfo.InvariantCulture).PadLeft(padding),
                                          total,
                                          entryName);
                    }

                    data.Seek(entry.Offset, SeekOrigin.Begin);

                    var entryParent = Path.GetDirectoryName(entryPath);
                    if (string.IsNullOrEmpty(entryParent) == false)
                    {
                        Directory.CreateDirectory(entryParent);
                    }

                    using (var output = File.Create(entryPath))
                    {
                        DecompressEntry(entry, data, output);
                    }
                }
            }
        }

        private static void DecompressEntry(FileFormats.Big.Entry entry,
                                            Stream input,
                                            Stream output)
        {
            input.Seek(entry.Offset, SeekOrigin.Begin);

            if (entry.CompressionScheme == FileFormats.Big.CompressionScheme.None)
            {
                output.WriteFromStream(input, entry.CompressedSize);
            }
            else if (entry.CompressionScheme == FileFormats.Big.CompressionScheme.LZO1x)
            {
                DecompressLzoEntry(entry, input, output);
            }
            else if (entry.CompressionScheme == FileFormats.Big.CompressionScheme.Zlib)
            {
                DecompressZlibEntry(entry, input, output);
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        private static void DecompressLzoEntry(FileFormats.Big.Entry entry, Stream input, Stream output)
        {
            input.Seek(entry.Offset, SeekOrigin.Begin);

            var compressedData = new byte[entry.CompressedSize];
            if (input.Read(compressedData, 0, compressedData.Length) != compressedData.Length)
            {
                throw new EndOfStreamException();
            }

            var uncompressedData = new byte[entry.UncompressedSize];
            int actualUncompressedLength = uncompressedData.Length;

            var result = LZO.Decompress(compressedData,
                                        0,
                                        compressedData.Length,
                                        uncompressedData,
                                        0,
                                        ref actualUncompressedLength);
            if (result != LZO.ErrorCode.Success)
            {
                throw new FormatException(string.Format("LZO decompression failure ({0})", result));
            }

            if (actualUncompressedLength != uncompressedData.Length)
            {
                throw new FormatException("LZO decompression failure (uncompressed size mismatch)");
            }

            output.Write(uncompressedData, 0, uncompressedData.Length);
        }

        private static void DecompressZlibEntry(FileFormats.Big.Entry entry, Stream input, Stream output)
        {
            if (entry.CompressedSize < 16)
            {
                throw new FormatException();
            }

            var sizes = new ushort[8];
            for (int i = 0; i < 8; i++)
            {
                sizes[i] = input.ReadValueU16(Endian.Little);
            }

            var blockCount = sizes[0];
            var maximumUncompressedBlockSize = 16 * (sizes[1] + 1);

            long left = entry.UncompressedSize;
            for (int i = 0, c = 2; i < blockCount; i++, c++)
            {
                if (c == 8)
                {
                    for (int j = 0; j < 8; j++)
                    {
                        sizes[j] = input.ReadValueU16(Endian.Little);
                    }

                    c = 0;
                }

                uint compressedBlockSize = sizes[c];
                if (compressedBlockSize != 0)
                {
                    var uncompressedBlockSize = i + 1 < blockCount
                                                    ? Math.Min(maximumUncompressedBlockSize, left)
                                                    : left;
                    //var uncompressedBlockSize = Math.Min(maximumUncompressedBlockSize, left);

                    using (var temp = input.ReadToMemoryStream(compressedBlockSize))
                    {
                        var zlib = new InflaterInputStream(temp, new Inflater(true));
                        output.WriteFromStream(zlib, uncompressedBlockSize);
                        left -= uncompressedBlockSize;
                    }

                    var padding = (16 - (compressedBlockSize % 16)) % 16;
                    if (padding > 0)
                    {
                        input.Seek(padding, SeekOrigin.Current);
                    }
                }
                else
                {
                    throw new NotImplementedException();
                }
            }

            if (left > 0)
            {
                Console.WriteLine("WAT");
            }
        }
    }
}
