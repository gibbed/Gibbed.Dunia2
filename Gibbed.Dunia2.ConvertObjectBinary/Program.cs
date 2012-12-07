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
                    "fcb",
                    "convert XML to FCB",
                    v => mode = v != null ? Mode.ToFcb : mode
                    },
                {
                    "xml",
                    "convert FCB to XML",
                    v => mode = v != null ? Mode.ToXml : mode
                    },
                {
                    "b|base-name",
                    "when converting FCB to XML, use specified base name instead of file name",
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
                    mode = Mode.ToXml;
                }
                else if (extension == ".xml")
                {
                    mode = Mode.ToFcb;
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

            if (mode == Mode.ToFcb)
            {
                throw new NotImplementedException();
            }
            else if (mode == Mode.ToXml)
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

                var config = Configuration.Load(project);

                if (verbose == true)
                {
                    Console.WriteLine("Reading binary...");
                }

                var bof = new BinaryObjectFile();
                using (var input = File.OpenRead(inputPath))
                {
                    bof.Deserialize(input);
                }

                var settings = new XmlWriterSettings();
                settings.Encoding = Encoding.UTF8;
                settings.Indent = true;
                settings.CheckCharacters = false;
                settings.OmitXmlDeclaration = false;

                var objectFileDef = config.GetObjectFileDefinition(baseName);
                var objectDef = objectFileDef != null ? objectFileDef.ObjectDefinition : null;

                using (var writer = XmlWriter.Create(outputPath, settings))
                {
                    writer.WriteStartDocument();
                    WriteNode(writer, bof.Root, objectDef);
                    writer.WriteEndDocument();
                }
            }
            else
            {
                throw new InvalidOperationException();
            }
        }

        private static void WriteNode(XmlWriter writer,
                                      BinaryObject node,
                                      Configuration.ObjectDefinition objectDef)
        {
            var classDef = objectDef != null ? objectDef.ClassDefinition : null;

            writer.WriteStartElement("object");

            if (classDef != null && classDef.Name != null && classDef.Hash == node.TypeHash)
            {
                writer.WriteAttributeString("type", classDef.Name);
            }
            else if (objectDef != null && objectDef.Name != null && objectDef.Hash == node.TypeHash)
            {
                writer.WriteAttributeString("type", objectDef.Name);
            }
            else
            {
                writer.WriteAttributeString("hash", node.TypeHash.ToString("X8"));
            }

            if (node.Values != null &&
                node.Values.Count > 0)
            {
                foreach (var kv in node.Values)
                {
                    writer.WriteStartElement("field");

                    var fieldDef = classDef != null ? classDef.GetFieldDefinition(kv.Key) : null;

                    if (fieldDef != null && fieldDef.Name != null)
                    {
                        writer.WriteAttributeString("name", fieldDef.Name);
                    }
                    else
                    {
                        writer.WriteAttributeString("hash", kv.Key.ToString("X8"));
                    }

                    if (fieldDef == null)
                    {
                        writer.WriteAttributeString("type", FieldType.BinHex.ToString());
                        writer.WriteBinHex(kv.Value, 0, kv.Value.Length);
                    }
                    else
                    {
                        writer.WriteAttributeString("type", fieldDef.Type.ToString());

                        switch (fieldDef.Type)
                        {
                            case FieldType.BinHex:
                            {
                                writer.WriteBinHex(kv.Value, 0, kv.Value.Length);
                                break;
                            }

                            case FieldType.Boolean:
                            {
                                if (kv.Value.Length != 1)
                                {
                                    throw new FormatException();
                                }

                                if (kv.Value[0] != 0 &&
                                    kv.Value[0] != 1)
                                {
                                    throw new FormatException();
                                }

                                writer.WriteString((kv.Value[0] != 0).ToString(CultureInfo.InvariantCulture));
                                break;
                            }

                            case FieldType.UInt8:
                            {
                                if (kv.Value.Length != 1)
                                {
                                    throw new FormatException();
                                }

                                writer.WriteString(kv.Value[0].ToString(CultureInfo.InvariantCulture));
                                break;
                            }

                            case FieldType.Int8:
                            {
                                if (kv.Value.Length != 1)
                                {
                                    throw new FormatException();
                                }

                                writer.WriteString(((sbyte)kv.Value[0]).ToString(CultureInfo.InvariantCulture));
                                break;
                            }

                            case FieldType.UInt16:
                            {
                                if (kv.Value.Length != 2)
                                {
                                    throw new FormatException();
                                }

                                writer.WriteString(
                                    BitConverter.ToUInt16(kv.Value, 0).ToString(CultureInfo.InvariantCulture));
                                break;
                            }

                            case FieldType.Int16:
                            {
                                if (kv.Value.Length != 2)
                                {
                                    throw new FormatException();
                                }

                                writer.WriteValue(
                                    BitConverter.ToInt16(kv.Value, 0).ToString(CultureInfo.InvariantCulture));
                                break;
                            }

                            case FieldType.UInt32:
                            {
                                if (kv.Value.Length != 4)
                                {
                                    throw new FormatException();
                                }

                                writer.WriteString(
                                    BitConverter.ToUInt32(kv.Value, 0).ToString(CultureInfo.InvariantCulture));
                                break;
                            }

                            case FieldType.Int32:
                            {
                                if (kv.Value.Length != 4)
                                {
                                    throw new FormatException();
                                }

                                writer.WriteValue(
                                    BitConverter.ToInt32(kv.Value, 0).ToString(CultureInfo.InvariantCulture));
                                break;
                            }

                            case FieldType.UInt64:
                            {
                                if (kv.Value.Length != 8)
                                {
                                    throw new FormatException();
                                }

                                writer.WriteString(
                                    BitConverter.ToUInt64(kv.Value, 0).ToString(CultureInfo.InvariantCulture));
                                break;
                            }

                            case FieldType.Int64:
                            {
                                if (kv.Value.Length != 8)
                                {
                                    throw new FormatException();
                                }

                                writer.WriteValue(
                                    BitConverter.ToInt64(kv.Value, 0).ToString(CultureInfo.InvariantCulture));
                                break;
                            }

                            case FieldType.Float32:
                            {
                                if (kv.Value.Length != 4)
                                {
                                    throw new FormatException();
                                }

                                writer.WriteValue(
                                    BitConverter.ToSingle(kv.Value, 0).ToString(CultureInfo.InvariantCulture));
                                break;
                            }

                            case FieldType.Float64:
                            {
                                if (kv.Value.Length != 8)
                                {
                                    throw new FormatException();
                                }

                                writer.WriteValue(
                                    BitConverter.ToDouble(kv.Value, 0).ToString(CultureInfo.InvariantCulture));
                                break;
                            }

                            case FieldType.String:
                            {
                                if (kv.Value.Length < 1)
                                {
                                    throw new FormatException();
                                }

                                if (kv.Value[kv.Value.Length - 1] != 0)
                                {
                                    throw new FormatException();
                                }

                                writer.WriteString(Encoding.UTF8.GetString(kv.Value, 0, kv.Value.Length - 1));
                                break;
                            }

                            default:
                            {
                                throw new NotSupportedException();
                            }
                        }
                    }

                    writer.WriteEndElement();
                }
            }

            if (node.Children.Count > 0)
            {
                foreach (var child in node.Children)
                {
                    Configuration.ObjectDefinition childObjectDef = null;

                    if (classDef != null)
                    {
                        var nestedClassDef = classDef.GetNestedClassDefinition(child.TypeHash);
                        if (nestedClassDef != null)
                        {
                            childObjectDef = new Configuration.ObjectDefinition(nestedClassDef.Name,
                                                                                nestedClassDef.Hash,
                                                                                nestedClassDef,
                                                                                null);
                        }
                    }

                    if (childObjectDef == null && objectDef != null)
                    {
                        childObjectDef = objectDef.GetNestedObjectDefinition(child.TypeHash);
                    }

                    WriteNode(writer, child, childObjectDef);
                }
            }

            writer.WriteEndElement();
        }
    }
}
