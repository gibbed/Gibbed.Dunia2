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
using NDesk.Options;
using Big = Gibbed.Dunia2.FileFormats.Big;
using EntryCompression = Gibbed.Dunia2.FileFormats.Big.EntryCompression;

namespace Gibbed.Dunia2.Pack
{
    internal class Program
    {
        private static string GetExecutableName()
        {
            return Path.GetFileName(System.Reflection.Assembly.GetExecutingAssembly().CodeBase);
        }

        private static int ParsePackageVersion(string text)
        {
            int value;
            if (int.TryParse(text, out value) == false)
            {
                throw new FormatException("invalid package version");
            }
            return value;
        }

        private static Big.Platform ParsePackagePlatform(string text)
        {
            Big.Platform value;

            if (Enum.TryParse(text, true, out value) == false)
            {
                throw new FormatException("invalid package platform");
            }
            return value;
        }

        public static void Main(string[] args)
        {
            bool showHelp = false;
            bool verbose = false;
            bool compress = false;

            int packageVersion = 9;
            Big.Platform packagePlatform = Big.Platform.PC;

            var options = new OptionSet()
            {
                {"v|verbose", "be verbose", v => verbose = v != null},
                {"c|compress", "compress data with LZO1x", v => compress = v != null},
                {"pv|package-version", "package version (default 9)", v => packageVersion = ParsePackageVersion(v)},
                {"pp|package-platform", "package platform (default PC)", v => packagePlatform = ParsePackagePlatform(v)},
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

            if (extras.Count < 1 || showHelp == true)
            {
                Console.WriteLine("Usage: {0} [OPTIONS]+ output_fat input_directory+", GetExecutableName());
                Console.WriteLine();
                Console.WriteLine("Pack files from input directories into a Big File (FAT/DAT pair).");
                Console.WriteLine();
                Console.WriteLine("Options:");
                options.WriteOptionDescriptions(Console.Out);
                return;
            }

            var inputPaths = new List<string>();
            string fatPath, datPath;

            if (extras.Count == 1)
            {
                inputPaths.Add(extras[0]);
                fatPath = Path.ChangeExtension(extras[0], ".fat");
                datPath = Path.ChangeExtension(extras[0], ".dat");
            }
            else
            {
                fatPath = extras[0];

                if (Path.GetExtension(fatPath) != ".fat")
                {
                    datPath = fatPath;
                    fatPath = Path.ChangeExtension(datPath, ".fat");
                }
                else
                {
                    datPath = Path.ChangeExtension(fatPath, ".dat");
                }

                inputPaths.AddRange(extras.Skip(1));
            }

            var pendingEntries = new SortedDictionary<ulong, PendingEntry>();

            if (verbose == true)
            {
                Console.WriteLine("Finding files...");
            }

            foreach (var relativePath in inputPaths)
            {
                string inputPath = Path.GetFullPath(relativePath);

                if (inputPath.EndsWith(Path.DirectorySeparatorChar.ToString(CultureInfo.InvariantCulture)) == true)
                {
                    inputPath = inputPath.Substring(0, inputPath.Length - 1);
                }

                foreach (string path in Directory.GetFiles(inputPath, "*", SearchOption.AllDirectories))
                {
                    PendingEntry pendingEntry;

                    string fullPath = Path.GetFullPath(path);
                    string partPath = fullPath.Substring(inputPath.Length + 1).ToLowerInvariant();

                    pendingEntry.FullPath = fullPath;
                    pendingEntry.PartPath = partPath;
                    pendingEntry.SubfatIndex = -1;

                    var pieces = partPath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                    int index = 0;

                    if (index >= pieces.Length)
                    {
                        continue;
                    }

                    if (pieces[index].ToUpperInvariant() == "__SUBFAT")
                    {
                        index++;

                        if (index >= pieces.Length)
                        {
                            continue;
                        }

                        if (int.TryParse(pieces[index], out pendingEntry.SubfatIndex) == false)
                        {
                            continue;
                        }

                        index++;
                    }

                    if (index >= pieces.Length)
                    {
                        continue;
                    }

                    if (pieces[index].ToUpperInvariant() == "__UNKNOWN")
                    {
                        var partName = Path.GetFileNameWithoutExtension(partPath);

                        if (string.IsNullOrEmpty(partName) == true)
                        {
                            continue;
                        }

                        if (partName.Length > 16)
                        {
                            partName = partName.Substring(0, 16);
                        }

                        pendingEntry.Name = null;
                        pendingEntry.NameHash = ulong.Parse(partName, NumberStyles.AllowHexSpecifier);
                    }
                    else
                    {
                        pendingEntry.Name = string.Join("\\", pieces.Skip(index).ToArray()).ToLowerInvariant();
                        pendingEntry.NameHash = CRC64.Hash(pendingEntry.Name);
                    }

                    if (pendingEntries.ContainsKey(pendingEntry.NameHash) == true)
                    {
                        Console.WriteLine("Ignoring duplicate of {0:X}: {1}", pendingEntry.NameHash, partPath);

                        if (verbose == true)
                        {
                            Console.WriteLine("  Previously added from: {0}",
                                              pendingEntries[pendingEntry.NameHash].PartPath);
                        }

                        continue;
                    }

                    pendingEntries[pendingEntry.NameHash] = pendingEntry;
                }
            }

            var fat = new BigFile
            {
                Version = packageVersion,
                Platform = packagePlatform
            };

            // reasonable default?
            // need to figure out what this value actually does
            if (packagePlatform == Big.Platform.PC)
            {
                fat.Unknown74 = 3;
            }
            else if (packagePlatform == Big.Platform.PS3 ||
                     packagePlatform == Big.Platform.X360)
            {
                fat.Unknown74 = 4;
            }
            else
            {
                throw new InvalidOperationException();
            }

            using (var output = File.Create(datPath))
            {
                long current = 0;
                long total = pendingEntries.Count;
                var padding = total.ToString(CultureInfo.InvariantCulture).Length;

                foreach (var pendingEntry in pendingEntries.Select(kv => kv.Value))
                {
                    current++;

                    if (verbose == true)
                    {
                        Console.WriteLine("[{0}/{1}] {2}",
                                          current.ToString(CultureInfo.InvariantCulture).PadLeft(padding),
                                          total,
                                          pendingEntry.PartPath);
                    }

                    var entry = new Big.Entry();
                    entry.NameHash = pendingEntry.NameHash;
                    entry.SubFatIndex = pendingEntry.SubfatIndex;
                    entry.Offset = output.Position;

                    using (var input = File.OpenRead(pendingEntry.FullPath))
                    {
                        EntryCompression.Compress(fat.Platform, ref entry, input, compress, output);
                        output.Seek(output.Position.Align(16), SeekOrigin.Begin);
                    }

                    fat.Entries.Add(entry);
                }
            }

            using (var output = File.Create(fatPath))
            {
                fat.Serialize(output);
            }
        }
    }
}
