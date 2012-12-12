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
using System.Xml;
using System.Xml.Serialization;

namespace Gibbed.Dunia2.ConvertObjectBinary.Definitions.Loaders
{
    internal static class ClassDefinitionLoader
    {
        public static DefinitionLookup<ClassDefinition> Load(ProjectData.Project project)
        {
            var rawClassDefs = new List<Raw.ClassDefinition>();

            var serializer = new XmlSerializer(typeof(Raw.ClassDefinition));
            foreach (var inputPath in GetClassDefinitionPaths(project))
            {
                using (var input = File.OpenRead(inputPath))
                {
                    Raw.ClassDefinition rawClassDef;

                    try
                    {
                        rawClassDef = (Raw.ClassDefinition)serializer.Deserialize(input);
                    }
                    catch (InvalidOperationException e)
                    {
                        var ie = e.InnerException as XmlException;
                        if (ie != null)
                        {
                            throw new XmlLoadException(inputPath, ie.Message, ie);
                        }
                        throw e;
                    }

                    rawClassDef.Path = inputPath;
                    rawClassDefs.Add(rawClassDef);
                }
            }

            var classDefs = new List<ClassDefinition>();
            foreach (var rawClassDef in rawClassDefs)
            {
                var classDef = GetClassDefinition(rawClassDefs, rawClassDef);
                if (classDefs.Any(cd => cd.Name == classDef.Name ||
                                        cd.Hash == classDef.Hash) == true)
                {
                    throw new LoadException(string.Format("duplicate binary class '{0}'", classDef.Name));
                }
                classDefs.Add(classDef);
            }

            return new DefinitionLookup<ClassDefinition>(classDefs);
        }

        private static ClassDefinition GetClassDefinition(List<Raw.ClassDefinition> rawClassDefs,
                                                          Raw.ClassDefinition rawClassDef)
        {
            var name = rawClassDef.Name;
            var hash = rawClassDef.Hash;
            var fieldDefs = new List<FieldDefinition>();
            var nestedClassDefs = new List<ClassDefinition>();
            var dynamicNestedClasses = rawClassDef.DynamicNestedClasses;
            var classFieldName = rawClassDef.ClassFieldName;
            var classFieldHash = rawClassDef.ClassFieldHash;

            MergeClassDefinition(rawClassDef.Name, rawClassDefs, rawClassDef, fieldDefs, nestedClassDefs);

            return new ClassDefinition(name,
                                       hash,
                                       fieldDefs,
                                       nestedClassDefs,
                                       dynamicNestedClasses,
                                       classFieldName,
                                       classFieldHash);
        }

        private static void MergeClassDefinition(string rootClassName,
                                                 List<Raw.ClassDefinition> rawClassDefs,
                                                 Raw.ClassDefinition rawClassDef,
                                                 List<FieldDefinition> fieldDefs,
                                                 List<ClassDefinition> nestedClassDefs)
        {
            foreach (var rawFieldDef in rawClassDef.FieldDefinitions)
            {
                var fieldDef = GetFieldDefinition(rawFieldDef);
                if (fieldDefs.Any(fd => fd.Hash == fieldDef.Hash))
                {
                    throw new LoadException(
                        string.Format("duplicate field '{1}' from '{2}' in binary class '{0}'",
                                      rootClassName,
                                      rawFieldDef.Name,
                                      rawClassDef.Name));
                }
                fieldDefs.Add(fieldDef);
            }

            foreach (var rawNestedClassDef in rawClassDef.NestedClassDefinitions)
            {
                var objectDef = GetClassDefinition(rawClassDefs, rawNestedClassDef);
                if (nestedClassDefs.Any(fd => fd.Hash == objectDef.Hash))
                {
                    throw new LoadException(
                        string.Format("duplicate nested class '{1}' from '{2}' in binary class '{0}'",
                                      rootClassName,
                                      rawNestedClassDef.Name,
                                      rawClassDef.Name));
                }
                nestedClassDefs.Add(objectDef);
            }

            foreach (var inherit in rawClassDef.Inherit)
            {
                if (string.IsNullOrEmpty(inherit.Name) == true)
                {
                    throw new LoadException(
                        string.Format("null inherit name from '{1}' in binary class '{0}'",
                                      rootClassName,
                                      rawClassDef.Name));
                }

                var inheritRawClassDef = rawClassDefs.FirstOrDefault(rcd => rcd.Name == inherit.Name);
                if (inheritRawClassDef == null)
                {
                    throw new LoadException(
                        string.Format("could not find definition for binary class '{0}'",
                                      inherit.Name));
                }

                MergeClassDefinition(rootClassName, rawClassDefs, inheritRawClassDef, fieldDefs, nestedClassDefs);
            }
        }

        private static FieldDefinition GetFieldDefinition(Raw.FieldDefinition rawFieldDef)
        {
            EnumDefinition enumDef;

            if (rawFieldDef.EnumDefinition == null)
            {
                enumDef = null;

                if (rawFieldDef.Type == FieldType.Enum)
                {
                    Console.WriteLine("Warning: enum definition for field '{0}' is missing.", rawFieldDef.Name);
                }
            }
            else
            {
                if (rawFieldDef.Type != FieldType.Enum)
                {
                    throw new LoadException(string.Format(
                        "field '{0}' specified an enum but isn't an enum",
                        rawFieldDef.Name));
                }

                enumDef = GetEnumDefinition(rawFieldDef.EnumDefinition);
            }

            return new FieldDefinition(rawFieldDef.Name, rawFieldDef.Hash, rawFieldDef.Type, enumDef);
        }

        private static EnumDefinition GetEnumDefinition(Raw.EnumDefinition rawEnumDef)
        {
            var name = rawEnumDef.Name;

            var elementDefs = new List<EnumElementDefinition>();
            foreach (var rawElementDef in rawEnumDef.ElementDefinitions)
            {
                if (elementDefs.Any(ed => ed.Name == rawElementDef.Name) == true)
                {
                    throw new LoadException(
                        string.Format("duplicate element name '{1}' in binary enum '{0}'",
                                      rawEnumDef.Name,
                                      rawElementDef.Name));
                }

                if (elementDefs.Any(ed => ed.Value == rawElementDef.Value) == true)
                {
                    throw new LoadException(string.Format(
                        "duplicate element value '{1}' in binary enum '{0}'", rawEnumDef.Name, rawElementDef.Value));
                }

                elementDefs.Add(new EnumElementDefinition(rawElementDef.Name, rawElementDef.Value));
            }

            return new EnumDefinition(name, elementDefs);
        }

        private static string[] GetClassDefinitionPaths(ProjectData.Project project)
        {
            return Directory.GetFiles(project.ListsPath, "*.binaryclass.xml", SearchOption.AllDirectories);
        }
    }
}
