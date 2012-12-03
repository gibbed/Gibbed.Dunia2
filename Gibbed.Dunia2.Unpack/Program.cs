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
using System.Text.RegularExpressions;
using Gibbed.Dunia2.FileFormats;
using NDesk.Options;
using EntryDecompression = Gibbed.Dunia2.FileFormats.Big.EntryDecompression;

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
            string filterPattern = null;
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
                    "f|filter=",
                    "only extract files using pattern",
                    v => filterPattern = v
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
                Console.WriteLine("Usage: {0} [OPTIONS]+ input_fat [output_dir]", GetExecutableName());
                Console.WriteLine();
                Console.WriteLine("Unpack files from a Big File (FAT/DAT pair).");
                Console.WriteLine();
                Console.WriteLine("Options:");
                options.WriteOptionDescriptions(Console.Out);
                return;
            }

            string fatPath = extras[0];
            string outputPath = extras.Count > 1 ? extras[1] : Path.ChangeExtension(fatPath, null) + "_unpack";
            string datPath;

            Regex filter = null;
            if (string.IsNullOrEmpty(filterPattern) == false)
            {
                filter = new Regex(filterPattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);
            }

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

                var duplicates = new Dictionary<ulong, int>();

                foreach (var entry in fat.Entries.OrderBy(e => e.Offset))
                {
                    current++;

                    string entryName;
                    if (GetEntryName(data, fat, entry, hashes, extractUnknowns, out entryName) == false)
                    {
                        continue;
                    }

                    if (entry.SubFatIndex >= 0)
                    {
                        entryName = Path.Combine("__SUBFAT", entry.SubFatIndex.ToString(), entryName);
                    }

                    if (duplicates.ContainsKey(entry.NameHash) == true)
                    {
                        var number = duplicates[entry.NameHash]++;
                        var e = Path.GetExtension(entryName);
                        var nn =
                            Path.ChangeExtension(
                                Path.ChangeExtension(entryName, null) + "__DUPLICATE_" + number.ToString(), e);
                        entryName = Path.Combine("__DUPLICATE", nn);
                    }
                    else
                    {
                        duplicates[entry.NameHash] = 0;
                    }

                    if (filter != null &&
                        filter.IsMatch(entryName) == false)
                    {
                        continue;
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
                        EntryDecompression.Decompress(entry, data, output);
                    }
                }
            }
        }

        private static bool GetEntryName(Stream input,
                                         BigFile fat,
                                         FileFormats.Big.Entry entry,
                                         ProjectData.HashList<ulong> hashes,
                                         bool extractUnknowns,
                                         out string entryName)
        {
            entryName = hashes[entry.NameHash];

            if (entryName == null)
            {
                if (extractUnknowns == false)
                {
                    return false;
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

            return true;
        }
    }
}
