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
using System.Globalization;
using System.IO;
using System.Xml.XPath;
using Gibbed.Dunia2.FileFormats;

namespace Gibbed.Dunia2.ConvertObjectBinary
{
    internal static class Importing
    {
        public static BinaryObject Import(Configuration config,
                                          Configuration.ObjectDefinition objectDef,
                                          string basePath,
                                          XPathNavigator nav)
        {
            return ReadNodeInternal(config, objectDef, basePath, nav);
        }

        private static void LoadNameAndHash(XPathNavigator node, out string name, out uint hash)
        {
            var _name = node.GetAttribute("name", "");
            var _hash = node.GetAttribute("hash", "");

            if (string.IsNullOrWhiteSpace(_name) == true &&
                string.IsNullOrWhiteSpace(_hash) == true)
            {
                throw new FormatException();
            }

            name = string.IsNullOrWhiteSpace(_name) == false ? _name : null;
            hash = name != null ? CRC32.Hash(name) : uint.Parse(_hash, NumberStyles.AllowHexSpecifier);
        }

        private static BinaryObject ReadNode(Configuration config,
                                             Configuration.ObjectDefinition objectDef,
                                             Configuration.ClassDefinition classDef,
                                             string basePath,
                                             XPathNavigator nav)
        {
            string className;
            uint classNameHash;

            LoadNameAndHash(nav, out className, out classNameHash);

            if (classDef == null || classDef.DynamicNestedClasses == false)
            {
                var childObjectDef = Helpers.GetChildObjectDefinition(objectDef, classDef, classNameHash);
                return ReadNodeInternal(config, childObjectDef, basePath, nav);
            }

            if (classDef.DynamicNestedClasses == true)
            {
                Configuration.ObjectDefinition childObjectDef = null;

                var nestedClassDef = config.GetClassDefinition(classNameHash);
                if (nestedClassDef != null)
                {
                    childObjectDef = new Configuration.ObjectDefinition(nestedClassDef.Name,
                                                                        nestedClassDef.Hash,
                                                                        nestedClassDef,
                                                                        null,
                                                                        null,
                                                                        null);
                }

                return ReadNodeInternal(config, childObjectDef, basePath, nav);
            }

            throw new InvalidOperationException();
        }

        private static BinaryObject ReadNodeInternal(Configuration config,
                                                     Configuration.ObjectDefinition objectDef,
                                                     string basePath,
                                                     XPathNavigator nav)
        {
            string className;
            uint classNameHash;

            LoadNameAndHash(nav, out className, out classNameHash);

            Configuration.ClassDefinition classDef = null;

            if (objectDef != null &&
                objectDef.ClassFieldHash.HasValue == true)
            {
                var hash = GetClassDefinitionByField(objectDef.ClassFieldName, objectDef.ClassFieldHash, nav);
                if (hash.HasValue == false)
                {
                    throw new InvalidOperationException();
                }

                classDef = config.GetClassDefinition(hash.Value);
            }

            if (classDef == null &&
                objectDef != null)
            {
                classDef = objectDef.ClassDefinition;

                if (classDef != null &&
                    classDef.ClassFieldHash.HasValue == true)
                {
                    var hash = GetClassDefinitionByField(classDef.ClassFieldName, classDef.ClassFieldHash, nav);
                    if (hash.HasValue == false)
                    {
                        throw new InvalidOperationException();
                    }

                    classDef = config.GetClassDefinition(hash.Value);
                }
            }

            var parent = new BinaryObject();
            parent.NameHash = classNameHash;

            var fields = nav.Select("field");
            while (fields.MoveNext() == true)
            {
                if (fields.Current == null)
                {
                    throw new InvalidOperationException();
                }

                string fieldName;
                uint fieldNameHash;

                LoadNameAndHash(fields.Current, out fieldName, out fieldNameHash);

                FieldType fieldType;
                var fieldTypeName = fields.Current.GetAttribute("type", "");
                if (Enum.TryParse(fieldTypeName, true, out fieldType) == false)
                {
                    throw new InvalidOperationException();
                }

                var fieldDef = classDef != null ? classDef.GetFieldDefinition(fieldNameHash) : null;
                byte[] data = FieldTypeSerializers.Serialize(fieldDef, fieldType, fields.Current);
                parent.Fields.Add(fieldNameHash, data);
            }

            var children = nav.Select("object");
            while (children.MoveNext() == true)
            {
                parent.Children.Add(LoadNode(config, objectDef, classDef, basePath, children.Current));
            }

            return parent;
        }

        private static uint? GetClassDefinitionByField(string classFieldName, uint? classFieldHash, XPathNavigator nav)
        {
            uint? hash = null;

            if (string.IsNullOrEmpty(classFieldName) == false)
            {
                var fieldByName = nav.SelectSingleNode("field[@name=\"" + classFieldName + "\"]");
                if (fieldByName != null)
                {
                    uint temp;
                    if (
                        uint.TryParse(fieldByName.Value,
                                      NumberStyles.AllowHexSpecifier,
                                      CultureInfo.InvariantCulture,
                                      out temp) == false)
                    {
                        throw new InvalidOperationException();
                    }
                    hash = temp;
                }
            }

            if (hash.HasValue == false)
            {
                var fieldByHash =
                    nav.SelectSingleNode("field[@hash=\"" +
                                         classFieldHash.Value.ToString("X8", CultureInfo.InvariantCulture) +
                                         "\"]");
                if (fieldByHash == null)
                {
                    uint temp;
                    if (
                        uint.TryParse(fieldByHash.Value,
                                      NumberStyles.AllowHexSpecifier,
                                      CultureInfo.InvariantCulture,
                                      out temp) == false)
                    {
                        throw new InvalidOperationException();
                    }
                    hash = temp;
                }
            }

            return hash;
        }

        private static BinaryObject LoadNode(Configuration config,
                                             Configuration.ObjectDefinition objectDef,
                                             Configuration.ClassDefinition classDef,
                                             string basePath,
                                             XPathNavigator node)
        {
            var external = node.GetAttribute("external", "");
            if (string.IsNullOrWhiteSpace(external) == true)
            {
                return ReadNode(config, objectDef, classDef, basePath, node);
            }

            var inputPath = Path.Combine(basePath, external);
            using (var input = File.OpenRead(inputPath))
            {
                //Console.WriteLine("Loading object from '{0}'...", Path.GetFileName(inputPath));

                var doc = new XPathDocument(input);
                var nav = doc.CreateNavigator();

                var root = nav.SelectSingleNode("/object");
                if (root == null)
                {
                    throw new InvalidOperationException();
                }

                return ReadNode(config, objectDef, classDef, Path.GetDirectoryName(inputPath), root);
            }
        }
    }
}
