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
    internal static class ObjectFileDefinitionLoader
    {
        public static DefinitionLookup<ObjectFileDefinition> Load(ProjectData.Project project,
                                                                  DefinitionLookup<ClassDefinition> classDefs)
        {
            var rawObjectFileDefs = new List<Raw.ObjectFileDefinition>();

            var serializer = new XmlSerializer(typeof(Raw.ObjectFileDefinition));
            foreach (var inputPath in GetObjectFilePaths(project))
            {
                using (var input = File.OpenRead(inputPath))
                {
                    Raw.ObjectFileDefinition rawObjectFileDef;

                    try
                    {
                        rawObjectFileDef = (Raw.ObjectFileDefinition)serializer.Deserialize(input);
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

                    rawObjectFileDef.Path = inputPath;
                    rawObjectFileDefs.Add(rawObjectFileDef);
                }
            }

            var objectFileDefs = new List<ObjectFileDefinition>();
            foreach (var rawObjectFileDef in rawObjectFileDefs)
            {
                var objectFileDef = GetObjectFileDefinition(rawObjectFileDef, classDefs);
                if (objectFileDefs.Any(ofd => ofd.Name == objectFileDef.Name) == true)
                {
                    throw new LoadException(string.Format("duplicate binary object file '{0}'", objectFileDef.Name));
                }
                objectFileDefs.Add(objectFileDef);
            }
            return new DefinitionLookup<ObjectFileDefinition>(objectFileDefs);
        }

        private static ObjectFileDefinition GetObjectFileDefinition(Raw.ObjectFileDefinition rawObjectFileDef,
                                                                    DefinitionLookup<ClassDefinition> classDefs)
        {
            if (string.IsNullOrEmpty(rawObjectFileDef.Name) == true)
            {
                throw new LoadException("binary object file missing name?");
            }

            var name = rawObjectFileDef.Name;
            var objectDef = rawObjectFileDef.ObjectDefinition != null
                                ? GetObjectDefinition(rawObjectFileDef.ObjectDefinition, classDefs)
                                : null;
            return new ObjectFileDefinition(name, objectDef);
        }

        private static ObjectDefinition GetObjectDefinition(Raw.ObjectDefinition rawObjectDef,
                                                            DefinitionLookup<ClassDefinition> classDefs)
        {
            var name = rawObjectDef.Name;
            var hash = rawObjectDef.Hash;
            ClassDefinition classDef = null;
            var classFieldName = rawObjectDef.ClassFieldName;
            var classFieldHash = rawObjectDef.ClassFieldHash;

            var className = rawObjectDef.ClassDefinition;
            if (string.IsNullOrEmpty(className) == false)
            {
                classDef = classDefs[className];
                if (classDef == null)
                {
                    throw new LoadException(string.Format("could not find definition for binary class '{0}'",
                                                          className));
                }
            }

            var objectDefs = new List<ObjectDefinition>();
            foreach (var childObjectDef in rawObjectDef.ObjectDefinitions)
            {
                var childDef = GetObjectDefinition(childObjectDef, classDefs);
                if (objectDefs.Any(fd => fd.Hash == childDef.Hash))
                {
                    if (string.IsNullOrEmpty(rawObjectDef.Name) == false)
                    {
                        if (string.IsNullOrEmpty(childDef.Name) == false)
                        {
                            throw new LoadException(string.Format("duplicate object '{1}' in binary object '{0}'",
                                                                  childDef.Name,
                                                                  rawObjectDef.Name));
                        }

                        throw new LoadException(string.Format("duplicate object '{1:X8}' in binary object '{0}'",
                                                              childDef.Hash,
                                                              rawObjectDef.Name));
                    }

                    if (string.IsNullOrEmpty(childDef.Name) == false)
                    {
                        throw new LoadException(string.Format("duplicate object '{1}' in binary object '{0:X8}'",
                                                              childDef.Name,
                                                              rawObjectDef.Hash));
                    }

                    throw new LoadException(string.Format("duplicate object '{1:X8}' in binary object '{0:X8}'",
                                                          childDef.Hash,
                                                          rawObjectDef.Hash));
                }
                objectDefs.Add(childDef);
            }
            return new ObjectDefinition(name, hash, classDef, classFieldName, classFieldHash, objectDefs);
        }

        private static string[] GetObjectFilePaths(ProjectData.Project project)
        {
            return Directory.GetFiles(project.ListsPath, "*.binaryobjectfile.xml", SearchOption.AllDirectories);
        }
    }
}
