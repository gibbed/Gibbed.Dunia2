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

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using Gibbed.Dunia2.BinaryObjectInfo;
using Gibbed.Dunia2.BinaryObjectInfo.Definitions;
using Gibbed.Dunia2.FileFormats;

namespace Gibbed.Dunia2.ConvertBinaryObject
{
    internal static class Exporting
    {
        public static void Export(ObjectFileDefinition objectFileDef,
                                  string outputPath,
                                  InfoManager infoManager,
                                  BinaryObjectFile bof)
        {
            var settings = new XmlWriterSettings
            {
                Encoding = Encoding.UTF8,
                Indent = true,
                CheckCharacters = false,
                OmitXmlDeclaration = false
            };

            var objectDef = objectFileDef != null &&
                            objectFileDef.Object != null &&
                            objectFileDef.Object.Hash == bof.Root.NameHash
                                ? objectFileDef.Object
                                : null;

            using (var writer = XmlWriter.Create(outputPath, settings))
            {
                writer.WriteStartDocument();
                WriteNode(infoManager, writer, bof.Root, objectDef);
                writer.WriteEndDocument();
            }
        }

        internal const uint EntityLibrariesHash = 0xBCDD10B4u; // crc32(EntityLibraries)
        internal const uint EntityLibraryHash = 0xE0BDB3DBu; // crc32(EntityLibrary)
        internal const uint NameHash = 0xFE11D138u; // crc32(Name);
        internal const uint EntityLibraryItemHash = 0x256A1FF9u; // unknown source name
        internal const uint DisLibItemIdHash = 0x8EDB0295u; // crc32(disLibItemId)
        internal const uint EntityHash = 0x0984415Eu; // crc32(Entity)
        internal const uint LibHash = 0xA90F3BCC; // crc32(lib)
        internal const uint LibItemHash = 0x72DE4948; // unknown source name
        internal const uint TextHidNameHash = 0x9D8873F8; // crc32(text_hidName);

        public static void MultiExportEntityLibrary(ObjectFileDefinition objectFileDef,
                                                    string basePath,
                                                    string outputPath,
                                                    InfoManager infoManager,
                                                    BinaryObjectFile bof)
        {
            var settings = new XmlWriterSettings
            {
                Encoding = Encoding.UTF8,
                Indent = true,
                CheckCharacters = false,
                OmitXmlDeclaration = false
            };

            var objectDef = objectFileDef != null ? objectFileDef.Object : null;

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
                        var libraryDef = objectDef != null
                                             ? objectDef.GetObjectDefinition(library.NameHash, null)
                                             : null;

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
                                var itemDef = libraryDef != null
                                                  ? libraryDef.GetObjectDefinition(item.NameHash, null)
                                                  : null;

                                var itemName =
                                    FieldTypeDeserializers.Deserialize<string>(FieldType.String,
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
                                    WriteNode(infoManager,
                                              itemWriter,
                                              item,
                                              itemDef);
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

        public static bool IsSuitableForEntityLibraryMultiExport(BinaryObjectFile bof)
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
                if (library.Fields.Keys.SequenceEqual(nameSeq) == false)
                {
                    return false;
                }

                if (library.Children.Any(sc => sc.NameHash != EntityLibraryItemHash) == true)
                {
                    return false;
                }

                foreach (var item in library.Children)
                {
                    if (item.Fields.Keys.OrderBy(h => h).SequenceEqual(idAndNameSeq) == false)
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

        public static void MultiExportLibrary(ObjectFileDefinition objectFileDef,
                                              string basePath,
                                              string outputPath,
                                              InfoManager infoManager,
                                              BinaryObjectFile bof)
        {
            var settings = new XmlWriterSettings
            {
                Encoding = Encoding.UTF8,
                Indent = true,
                CheckCharacters = false,
                OmitXmlDeclaration = false
            };

            var objectDef = objectFileDef != null ? objectFileDef.Object : null;

            using (var writer = XmlWriter.Create(outputPath, settings))
            {
                writer.WriteStartDocument();

                var root = bof.Root;
                {
                    writer.WriteStartElement("object");
                    writer.WriteAttributeString("name", "lib");

                    Directory.CreateDirectory(basePath);

                    var itemNames = new Dictionary<string, int>();

                    foreach (var item in root.Children)
                    {
                        var itemDef = objectDef != null
                                          ? objectDef.GetObjectDefinition(item.NameHash, null)
                                          : null;

                        var itemName =
                            FieldTypeDeserializers.Deserialize<string>(FieldType.String,
                                                                       item.Fields[TextHidNameHash]);
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

                        writer.WriteStartElement("object");
                        writer.WriteAttributeString("external", itemPath);
                        writer.WriteEndElement();

                        itemPath = Path.Combine(basePath, itemPath);

                        var itemParentPath = Path.GetDirectoryName(itemPath);
                        if (string.IsNullOrEmpty(itemParentPath) == false)
                        {
                            Directory.CreateDirectory(itemParentPath);
                        }

                        using (var itemWriter = XmlWriter.Create(itemPath, settings))
                        {
                            itemWriter.WriteStartDocument();
                            WriteNode(infoManager,
                                      itemWriter,
                                      item,
                                      itemDef);
                            itemWriter.WriteEndDocument();
                        }
                    }
                }

                writer.WriteEndDocument();
            }
        }

        public static bool IsSuitableForLibraryMultiExport(BinaryObjectFile bof)
        {
            return bof.Root.Fields.Count == 0 &&
                   bof.Root.NameHash == LibHash &&
                   bof.Root.Children.Any(c => c.NameHash != LibItemHash ||
                                              c.Fields.ContainsKey(TextHidNameHash) == false) == false;
        }

        private static void WriteNode(InfoManager infoManager,
                                      XmlWriter writer,
                                      BinaryObject node,
                                      ClassDefinition def)
        {
            var originalDef = def;

            if (def != null &&
                def.ClassFieldHash.HasValue == true)
            {
                if (node.Fields.ContainsKey(def.ClassFieldHash.Value) == true)
                {
                    var hash = FieldTypeDeserializers.Deserialize<uint>(FieldType.UInt32,
                                                                        node.Fields[def.ClassFieldHash.Value]);
                    def = infoManager.GetClassDefinition(hash);
                }
            }

            writer.WriteStartElement("object");

            if (originalDef != null && originalDef.Name != null && originalDef.Hash == node.NameHash)
            {
                writer.WriteAttributeString("name", originalDef.Name);
            }
            else if (def != null && def.Name != null && def.Hash == node.NameHash)
            {
                writer.WriteAttributeString("name", def.Name);
            }
            else
            {
                writer.WriteAttributeString("hash", node.NameHash.ToString("X8"));
            }

            if (node.Fields != null)
            {
                foreach (var kv in node.Fields)
                {
                    writer.WriteStartElement("field");

                    var fieldDef = def != null ? def.GetFieldDefinition(kv.Key, node) : null;

                    if (fieldDef != null && fieldDef.Name == "selStimType" &&
                        node.Fields.Count > 1)
                    {
                    }

                    if (fieldDef != null && fieldDef.Name != null && fieldDef.Hash == kv.Key)
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
                        FieldTypeDeserializers.Deserialize(writer, fieldDef, kv.Value);
                    }

                    writer.WriteEndElement();
                }
            }

            if (def == null || def.DynamicNestedClasses == false)
            {
                foreach (var childNode in node.Children)
                {
                    var childDef = def != null ? def.GetObjectDefinition(childNode.NameHash, node) : null;
                    WriteNode(infoManager, writer, childNode, childDef);
                }
            }
            else if (def.DynamicNestedClasses == true)
            {
                foreach (var childNode in node.Children)
                {
                    var childDef = infoManager.GetClassDefinition(childNode.NameHash);
                    WriteNode(infoManager, writer, childNode, childDef);
                }
            }

            writer.WriteEndElement();
        }
    }
}
