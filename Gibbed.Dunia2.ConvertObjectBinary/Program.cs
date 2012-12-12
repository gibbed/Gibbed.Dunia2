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
using System.Xml.XPath;
using Gibbed.Dunia2.FileFormats;
using NDesk.Options;

namespace Gibbed.Dunia2.ConvertObjectBinary
{
    internal class Program
    {
        private static string GetExecutableName()
        {
            return Path.GetFileName(System.Reflection.Assembly.GetExecutingAssembly().CodeBase);
        }

        private static void Main(string[] args)
        {
            var mode = Mode.Unknown;
            string baseName = "";
            bool showHelp = false;
            bool verbose = false;

            var options = new OptionSet()
            {
                {
                    "i|import|fcb",
                    "convert XML to FCB",
                    v => mode = v != null ? Mode.Import : mode
                    },
                {
                    "e|export|xml",
                    "convert FCB to XML",
                    v => mode = v != null ? Mode.Export : mode
                    },
                {
                    "b|base-name=",
                    "when converting, use specified base name instead of file name",
                    v => baseName = v
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

            if (mode == Mode.Unknown &&
                extras.Count >= 1)
            {
                var extension = Path.GetExtension(extras[0]);

                if (string.IsNullOrEmpty(extension) == false)
                {
                    extension = extension.ToLowerInvariant();
                }

                if (extension == ".fcb" ||
                    extension == ".obj" ||
                    extension == ".lib")
                {
                    mode = Mode.Export;
                }
                else if (extension == ".xml")
                {
                    mode = Mode.Import;
                }
            }

            if (showHelp == true ||
                mode == Mode.Unknown ||
                extras.Count < 1 ||
                extras.Count > 2)
            {
                Console.WriteLine("Usage: {0} [OPTIONS]+ input [output]", GetExecutableName());
                Console.WriteLine();
                Console.WriteLine("Options:");
                options.WriteOptionDescriptions(Console.Out);
                return;
            }

            if (verbose == true)
            {
                Console.WriteLine("Loading project...");
            }

            var manager = ProjectData.Manager.Load();
            if (manager.ActiveProject == null)
            {
                Console.WriteLine("Warning: no active project loaded.");
                return;
            }

            var project = manager.ActiveProject;

            if (verbose == true)
            {
                Console.WriteLine("Loading binary class and object definitions...");
            }

            DefinitionManager definitionManager;

            if (System.Diagnostics.Debugger.IsAttached == false)
            {
                try
                {
                    definitionManager = DefinitionManager.Load(project);
                }
                catch (Definitions.LoadException e)
                {
                    Console.WriteLine("Failed to load binary definitions!");
                    Console.WriteLine("  {0}", e.Message);
                    return;
                }
                catch (Definitions.XmlLoadException e)
                {
                    Console.WriteLine("Failed to load binary definitions!");
                    Console.WriteLine("  in \"{0}\"", e.FilePath);
                    Console.WriteLine("  {0}", e.Message);
                    return;
                }
                catch (Exception e)
                {
                    Console.WriteLine("Exception while loading binary definitions!");
                    Console.WriteLine();
                    Console.WriteLine("{0}", e);
                    return;
                }
            }
            else
            {
                definitionManager = DefinitionManager.Load(project);
            }

            if (mode == Mode.Import)
            {
                string inputPath = extras[0];
                string outputPath;
                string basePath;

                if (extras.Count > 1)
                {
                    outputPath = extras[1];
                }
                else
                {
                    outputPath = Path.ChangeExtension(inputPath, null);
                    outputPath += "_converted.fcb";
                }

                basePath = Path.ChangeExtension(inputPath, null);

                if (string.IsNullOrEmpty(baseName) == true)
                {
                    baseName = Path.GetFileNameWithoutExtension(inputPath);

                    if (string.IsNullOrEmpty(baseName) == false &&
                        baseName.EndsWith("_converted") == true)
                    {
                        baseName = baseName.Substring(0, baseName.Length - 10);
                    }
                }

                inputPath = Path.GetFullPath(inputPath);
                outputPath = Path.GetFullPath(outputPath);
                basePath = Path.GetFullPath(basePath);

                var bof = new BinaryObjectFile();

                using (var input = File.OpenRead(inputPath))
                {
                    var doc = new XPathDocument(input);
                    var nav = doc.CreateNavigator();

                    var root = nav.SelectSingleNode("/object");
                    if (root == null)
                    {
                        throw new FormatException();
                    }

                    if (verbose == true)
                    {
                        Console.WriteLine("Reading XML...");
                    }

                    var objectFileDef = definitionManager.GetObjectFileDefinition(baseName);
                    if (objectFileDef == null)
                    {
                        Console.WriteLine("Warning: could not find binary object file definition '{0}'", baseName);
                    }

                    var objectDef = objectFileDef != null ? objectFileDef.ObjectDefinition : null;
                    bof.Root = Importing.Import(definitionManager, objectDef, basePath, root);
                }

                if (verbose == true)
                {
                    Console.WriteLine("Writing FCB...");
                }

                using (var output = File.Create(outputPath))
                {
                    bof.Serialize(output);
                }
            }
            else if (mode == Mode.Export)
            {
                string inputPath = extras[0];
                string outputPath;
                string basePath;

                if (extras.Count > 1)
                {
                    outputPath = extras[1];
                    basePath = Path.ChangeExtension(outputPath, null);
                }
                else
                {
                    outputPath = Path.ChangeExtension(inputPath, null);
                    outputPath += "_converted";
                    basePath = outputPath;
                    outputPath += ".xml";
                }

                if (string.IsNullOrEmpty(baseName) == true)
                {
                    baseName = Path.GetFileNameWithoutExtension(inputPath);
                }

                if (string.IsNullOrEmpty(baseName) == true)
                {
                    throw new InvalidOperationException();
                }

                inputPath = Path.GetFullPath(inputPath);
                outputPath = Path.GetFullPath(outputPath);
                basePath = Path.GetFullPath(basePath);

                var objectFileDef = definitionManager.GetObjectFileDefinition(baseName);

                if (verbose == true)
                {
                    Console.WriteLine("Reading FCB...");
                }

                var bof = new BinaryObjectFile();
                using (var input = File.OpenRead(inputPath))
                {
                    bof.Deserialize(input);
                }

                if (verbose == true)
                {
                    Console.WriteLine("Writing XML...");
                }

                if (Exporting.IsSuitableForMultiExport(bof) == true)
                {
                    Exporting.MultiExport(objectFileDef, basePath, outputPath, definitionManager, bof);
                }
                else
                {
                    Exporting.Export(objectFileDef, outputPath, definitionManager, bof);
                }
            }
            else
            {
                throw new InvalidOperationException();
            }
        }
    }
}
