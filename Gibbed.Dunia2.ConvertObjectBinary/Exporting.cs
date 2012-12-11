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
using System.Text;
using System.Xml;
using Gibbed.Dunia2.FileFormats;

namespace Gibbed.Dunia2.ConvertObjectBinary
{
    internal static class Exporting
    {
        public static void Export(Configuration.ObjectFileDefinition objectFileDef,
                                  string outputPath,
                                  Configuration config,
                                  BinaryObjectFile bof)
        {
            var settings = new XmlWriterSettings();
            settings.Encoding = Encoding.UTF8;
            settings.Indent = true;
            settings.CheckCharacters = false;
            settings.OmitXmlDeclaration = false;

            var objectDef = objectFileDef != null ? objectFileDef.ObjectDefinition : null;

            using (var writer = XmlWriter.Create(outputPath, settings))
            {
                writer.WriteStartDocument();
                WriteNode(config, writer, bof.Root, objectDef);
                writer.WriteEndDocument();
            }
        }

        internal const uint EntityLibrariesHash = 0xBCDD10B4u; // crc32(EntityLibraries)
        internal const uint EntityLibraryHash = 0xE0BDB3DBu; // crc32(EntityLibrary)
        internal const uint NameHash = 0xFE11D138u; // crc32(Name);
        internal const uint EntityLibraryItemHash = 0x256A1FF9u; // unknown source name
        internal const uint DisLibItemIdHash = 0x8EDB0295u; // crc32(disLibItemId)
        internal const uint EntityHash = 0x0984415Eu; // crc32(Entity)

        public static void MultiExport(Configuration.ObjectFileDefinition objectFileDef,
                                       string basePath,
                                       string outputPath,
                                       Configuration config,
                                       BinaryObjectFile bof)
        {
            var settings = new XmlWriterSettings();
            settings.Encoding = Encoding.UTF8;
            settings.Indent = true;
            settings.CheckCharacters = false;
            settings.OmitXmlDeclaration = false;

            var objectDef = objectFileDef != null ? objectFileDef.ObjectDefinition : null;

            Configuration.ClassDefinition classDef = null;
            if (objectDef != null)
            {
                classDef = objectDef.ClassDefinition;
            }

            using (var writer = XmlWriter.Create(outputPath, settings))
            {
                writer.WriteStartDocument();

                var root = bof.Root;
                {
                    writer.WriteStartElement("object");
                    writer.WriteAttributeString("name", "EntityLibraries");

                    var libraryNames = new Dictionary<string, int>();

                    Directory.CreateDirectory(basePath);

                    foreach (var library in root.Children)
                    {
                        var libraryObjectDef = Helpers.GetChildObjectDefinition(objectDef, classDef, library.NameHash);

                        var libraryName = FieldTypeDeserializers.Deserialize<string>(FieldType.String,
                                                                                     library.Fields[NameHash]);
                        var unsanitizedLibraryName = libraryName;

                        libraryName = libraryName.Replace('/', Path.DirectorySeparatorChar);
                        libraryName = libraryName.Replace('\\', Path.DirectorySeparatorChar);

                        if (libraryNames.ContainsKey(libraryName) == false)
                        {
                            libraryNames.Add(libraryName, 1);
                        }
                        else
                        {
                            libraryName = string.Format("{0} ({1})", libraryName, ++libraryNames[libraryName]);
                        }

                        var libraryPath = Path.Combine(libraryName, "@library.xml");

                        writer.WriteStartElement("object");
                        writer.WriteAttributeString("external", libraryPath);
                        writer.WriteEndElement();

                        libraryPath = Path.Combine(basePath, libraryPath);

                        var itemNames = new Dictionary<string, int>();

                        var libraryParentPath = Path.GetDirectoryName(libraryPath);
                        if (string.IsNullOrEmpty(libraryParentPath) == false)
                        {
                            Directory.CreateDirectory(libraryParentPath);
                        }

                        using (var libraryWriter = XmlWriter.Create(libraryPath, settings))
                        {
                            libraryWriter.WriteStartDocument();
                            libraryWriter.WriteStartElement("object");
                            libraryWriter.WriteAttributeString("name", "EntityLibrary");

                            libraryWriter.WriteStartElement("field");
                            libraryWriter.WriteAttributeString("name", "Name");
                            libraryWriter.WriteAttributeString("type", "String");
                            libraryWriter.WriteString(unsanitizedLibraryName);
                            libraryWriter.WriteEndElement();

                            foreach (var item in library.Children)
                            {
                                var itemObjectDef = Helpers.GetChildObjectDefinition(libraryObjectDef,
                                                                                     libraryObjectDef != null
                                                                                         ? libraryObjectDef.
                                                                                               ClassDefinition
                                                                                         : null,
                                                                                     item.NameHash);

                                var itemName = FieldTypeDeserializers.Deserialize<string>(FieldType.String,
                                                                                          item.Fields[NameHash]);
                                itemName = itemName.Replace('/', Path.DirectorySeparatorChar);
                                itemName = itemName.Replace('\\', Path.DirectorySeparatorChar);

                                if (itemNames.ContainsKey(itemName) == false)
                                {
                                    itemNames.Add(itemName, 1);
                                }
                                else
                                {
                                    itemName = string.Format("{0} ({1})", itemName, ++itemNames[itemName]);
                                }

                                var itemPath = itemName + ".xml";

                                libraryWriter.WriteStartElement("object");
                                libraryWriter.WriteAttributeString("external", itemPath);
                                libraryWriter.WriteEndElement();

                                itemPath = Path.Combine(basePath, libraryName, itemPath);

                                var itemParentPath = Path.GetDirectoryName(itemPath);
                                if (string.IsNullOrEmpty(itemParentPath) == false)
                                {
                                    Directory.CreateDirectory(itemParentPath);
                                }

                                using (var itemWriter = XmlWriter.Create(itemPath, settings))
                                {
                                    itemWriter.WriteStartDocument();
                                    WriteNode(config,
                                              itemWriter,
                                              item,
                                              itemObjectDef);
                                    itemWriter.WriteEndDocument();
                                }
                            }

                            libraryWriter.WriteEndDocument();
                        }
                    }

                    writer.WriteEndElement();
                }

                writer.WriteEndDocument();
            }
        }

        public static bool IsSuitableForMultiExport(BinaryObjectFile bof)
        {
            if (bof.Root.Fields.Count != 0 ||
                bof.Root.NameHash != EntityLibrariesHash ||
                bof.Root.Children.Any(c => c.NameHash != EntityLibraryHash) == true)
            {
                return false;
            }

            var nameSeq = new[] {NameHash};
            var idAndNameSeq = new[] {DisLibItemIdHash, NameHash};

            foreach (var library in bof.Root.Children)
            {
                if (Enumerable.SequenceEqual(library.Fields.Keys, nameSeq) == false)
                {
                    return false;
                }

                if (library.Children.Any(sc => sc.NameHash != EntityLibraryItemHash) == true)
                {
                    return false;
                }

                foreach (var item in library.Children)
                {
                    if (Enumerable.SequenceEqual(item.Fields.Keys.OrderBy(h => h), idAndNameSeq) == false)
                    {
                        return false;
                    }

                    if (item.Children.Any(sc => sc.NameHash != EntityHash) == true)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private static void WriteNode(Configuration config,
                                      XmlWriter writer,
                                      BinaryObject node,
                                      Configuration.ObjectDefinition objectDef)
        {
            Configuration.ClassDefinition classDef = null;

            if (objectDef != null &&
                objectDef.ClassFieldHash.HasValue == true)
            {
                if (node.Fields.ContainsKey(objectDef.ClassFieldHash.Value) == true)
                {
                    var bytes = node.Fields[objectDef.ClassFieldHash.Value];
                    var hash = BitConverter.ToUInt32(bytes, 0);
                    classDef = config.GetClassDefinition(hash);
                }
            }

            if (classDef == null &&
                objectDef != null)
            {
                classDef = objectDef.ClassDefinition;

                if (classDef != null &&
                    classDef.ClassFieldHash.HasValue == true)
                {
                    if (node.Fields.ContainsKey(classDef.ClassFieldHash.Value) == true)
                    {
                        var bytes = node.Fields[classDef.ClassFieldHash.Value];
                        var hash = BitConverter.ToUInt32(bytes, 0);
                        classDef = config.GetClassDefinition(hash);
                    }
                }
            }

            writer.WriteStartElement("object");

            if (classDef != null && classDef.Name != null && classDef.Hash == node.NameHash)
            {
                writer.WriteAttributeString("name", classDef.Name);
            }
            else if (objectDef != null && objectDef.Name != null && objectDef.Hash == node.NameHash)
            {
                writer.WriteAttributeString("name", objectDef.Name);
            }
            else
            {
                writer.WriteAttributeString("hash", node.NameHash.ToString("X8"));
            }

            if (node.Fields != null &&
                node.Fields.Count > 0)
            {
                foreach (var kv in node.Fields)
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

                        if (fieldDef.Type == FieldType.Enum &&
                            fieldDef.EnumDefinition != null)
                        {
                            writer.WriteAttributeString("enum", fieldDef.EnumDefinition.Name);
                        }

                        FieldTypeDeserializers.Deserialize(writer, fieldDef, kv.Value);
                    }

                    writer.WriteEndElement();
                }
            }

            if (node.Children.Count > 0)
            {
                if (classDef == null || classDef.DynamicNestedClasses == false)
                {
                    foreach (var child in node.Children)
                    {
                        var childObjectDef = Helpers.GetChildObjectDefinition(objectDef, classDef, child.NameHash);
                        WriteNode(config, writer, child, childObjectDef);
                    }
                }
                else if (classDef.DynamicNestedClasses == true)
                {
                    foreach (var child in node.Children)
                    {
                        Configuration.ObjectDefinition childObjectDef = null;

                        var nestedClassDef = config.GetClassDefinition(child.NameHash);
                        if (nestedClassDef != null)
                        {
                            childObjectDef = new Configuration.ObjectDefinition(nestedClassDef.Name,
                                                                                nestedClassDef.Hash,
                                                                                nestedClassDef,
                                                                                null,
                                                                                null,
                                                                                null);
                        }
                        else
                        {
                            //Console.WriteLine("Wanted a dynamic class with has {0:X8}", child.TypeHash);
                        }

                        WriteNode(config, writer, child, childObjectDef);
                    }
                }
            }

            writer.WriteEndElement();
        }
    }
}
