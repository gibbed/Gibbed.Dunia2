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
using System.Linq;
using Gibbed.Dunia2.FileFormats;
using NDesk.Options;

namespace RebuildFileLists
{
    internal class Program
    {
        private static string GetExecutableName()
        {
            return Path.GetFileName(System.Reflection.Assembly.GetExecutingAssembly().CodeBase);
        }

        private static string GetListPath(string installPath, string inputPath)
        {
            installPath = installPath.ToLowerInvariant();
            inputPath = inputPath.ToLowerInvariant();

            if (inputPath.StartsWith(installPath) == false)
            {
                return null;
            }

            var baseName = inputPath.Substring(installPath.Length + 1);

            string outputPath;
            outputPath = Path.Combine("files", baseName);
            outputPath = Path.ChangeExtension(outputPath, ".filelist");
            return outputPath;
        }

        public static void Main(string[] args)
        {
            bool showHelp = false;
            string currentProject = null;

            var options = new OptionSet()
            {
                {
                    "h|help",
                    "show this message and exit",
                    v => showHelp = v != null
                    },
                {
                    "p|project=",
                    "override current project",
                    v => currentProject = v
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

            if (extras.Count != 0 || showHelp == true)
            {
                Console.WriteLine("Usage: {0} [OPTIONS]+", GetExecutableName());
                Console.WriteLine();
                Console.WriteLine("Options:");
                options.WriteOptionDescriptions(Console.Out);
                return;
            }

            Console.WriteLine("Loading project...");

            var manager = Gibbed.ProjectData.Manager.Load(currentProject);
            if (manager.ActiveProject == null)
            {
                Console.WriteLine("Nothing to do: no active project loaded.");
                return;
            }

            var project = manager.ActiveProject;
            var version = -1;
            Gibbed.ProjectData.HashList<ulong> hashes = null;

            var installPath = project.InstallPath;
            var listsPath = project.ListsPath;

            if (installPath == null)
            {
                Console.WriteLine("Could not detect install path.");
                return;
            }

            if (listsPath == null)
            {
                Console.WriteLine("Could not detect lists path.");
                return;
            }

            Console.WriteLine("Searching for archives...");
            var inputPaths = new List<string>();
            inputPaths.AddRange(Directory.GetFiles(installPath, "*.fat", SearchOption.AllDirectories));

            var outputPaths = new List<string>();

            var breakdown = new Breakdown();

            Console.WriteLine("Processing...");
            foreach (var inputPath in inputPaths)
            {
                var outputPath = GetListPath(installPath, inputPath);
                if (outputPath == null)
                {
                    throw new InvalidOperationException();
                }

                Console.WriteLine(outputPath);
                outputPath = Path.Combine(listsPath, outputPath);

                if (outputPaths.Contains(outputPath) == true)
                {
                    throw new InvalidOperationException();
                }

                outputPaths.Add(outputPath);

                var fat = new BigFile();

                if (File.Exists(inputPath + ".bak") == true)
                {
                    using (var input = File.OpenRead(inputPath + ".bak"))
                    {
                        fat.Deserialize(input);
                    }
                }
                else
                {
                    using (var input = File.OpenRead(inputPath))
                    {
                        fat.Deserialize(input);
                    }
                }

                if (version == -1)
                {
                    version = fat.Version;

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
                }
                else if (version != fat.Version)
                {
                    throw new InvalidOperationException();
                }

                var localBreakdown = new Breakdown();

                var names = new List<string>();
                foreach (var nameHash in fat.Entries.Select(e => e.NameHash).Distinct())
                {
                    var name = hashes[nameHash];
                    if (name != null)
                    {
                        names.Add(name);
                    }

                    localBreakdown.Total++;
                }

                var distinctNames = names.Distinct().ToArray();
                localBreakdown.Known += distinctNames.Length;

                breakdown.Known += localBreakdown.Known;
                breakdown.Total += localBreakdown.Total;

                var outputParent = Path.GetDirectoryName(outputPath);
                if (string.IsNullOrEmpty(outputParent) == false)
                {
                    Directory.CreateDirectory(outputParent);
                }
                
                using (var writer = new StringWriter())
                {
                    writer.WriteLine("; {0}", localBreakdown);

                    foreach (string name in distinctNames.OrderBy(dn => dn))
                    {
                        writer.WriteLine(name);
                    }

                    writer.Flush();

                    using (var output = new StreamWriter(outputPath))
                    {
                        output.Write(writer.GetStringBuilder());
                    }
                }
            }

            using (var output = new StreamWriter(Path.Combine(Path.Combine(listsPath, "files"), "status.txt")))
            {
                output.WriteLine("{0}", breakdown);
            }
        }
    }
}
