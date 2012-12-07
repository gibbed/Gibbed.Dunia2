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
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml.Serialization;

namespace Gibbed.Dunia2.ConvertObjectBinary
{
    public class Configuration
    {
        private readonly List<ClassDefinition> _ClassDefinitions = new List<ClassDefinition>();
        private readonly List<ObjectFileDefinition> _ObjectFileDefinitions = new List<ObjectFileDefinition>();

        private Configuration()
        {
        }

        public ClassDefinition GetClassDefinition(string name)
        {
            return this._ClassDefinitions.SingleOrDefault(cd => cd.Name == name);
        }

        public ObjectFileDefinition GetObjectFileDefinition(string name)
        {
            return
                this._ObjectFileDefinitions.SingleOrDefault(
                    ofd => ofd.Name.ToLowerInvariant() == name.ToLowerInvariant());
        }

        public static Configuration Load(ProjectData.Project project)
        {
            var config = new Configuration();
            config.LoadClassDefinitions(project);
            config.LoadObjectFileDefinitions(project);
            return config;
        }

        private void LoadClassDefinitions(ProjectData.Project project)
        {
            var rawClassDefs = new List<RawClassDefinition>();

            foreach (var inputPath in GetClassDefinitionPaths(project))
            {
                using (var input = File.OpenRead(inputPath))
                {
                    var serializer = new XmlSerializer(typeof(RawClassDefinition), new[] {typeof(RawFieldDefinition)});
                    var rawClassDef = (RawClassDefinition)serializer.Deserialize(input);
                    rawClassDef.Path = inputPath;
                    rawClassDefs.Add(rawClassDef);
                }
            }

            foreach (var rawClassDef in rawClassDefs)
            {
                var classDef = LoadClassDefinition(rawClassDefs, rawClassDef);
                if (this._ClassDefinitions.Any(cd => cd.Name == classDef.Name) == true)
                {
                    throw new InvalidOperationException();
                }
                this._ClassDefinitions.Add(classDef);
            }
        }

        private ClassDefinition LoadClassDefinition(List<RawClassDefinition> rawClassDefs,
                                                    RawClassDefinition rawClassDef)
        {
            if (string.IsNullOrEmpty(rawClassDef.Name) == true)
            {
                throw new InvalidOperationException();
            }

            var name = rawClassDef.Name;
            var fieldDefs = new List<FieldDefinition>();
            do
            {
                foreach (var rawFieldDef in rawClassDef.Fields)
                {
                    var fieldDef = LoadFieldDefinition(rawFieldDef);
                    if (fieldDefs.Any(fd => fd.Hash == fieldDef.Hash))
                    {
                        throw new InvalidOperationException();
                    }
                    fieldDefs.Add(fieldDef);
                }

                if (string.IsNullOrEmpty(rawClassDef.Inherit) == false)
                {
                    rawClassDef = rawClassDefs.FirstOrDefault(rcd => rcd.Name == rawClassDef.Inherit);
                    if (rawClassDef == null)
                    {
                        throw new InvalidOperationException();
                    }
                }
                else
                {
                    rawClassDef = null;
                }
            }
            while (rawClassDef != null);

            return new ClassDefinition(name, fieldDefs);
        }

        private FieldDefinition LoadFieldDefinition(RawFieldDefinition rawFieldDef)
        {
            return new FieldDefinition(rawFieldDef.Name, rawFieldDef.Hash, rawFieldDef.Type);
        }

        private static string[] GetClassDefinitionPaths(ProjectData.Project project)
        {
            return Directory.GetFiles(project.ListsPath, "*.binaryclass.xml", SearchOption.AllDirectories);
        }

        public class ClassDefinition
        {
            public string Name { get; private set; }
            public ReadOnlyCollection<FieldDefinition> Fields { get; private set; }

            public ClassDefinition(string name, IList<FieldDefinition> fieldDefs)
            {
                this.Name = name;
                this.Fields = new ReadOnlyCollection<FieldDefinition>(fieldDefs);
            }

            public FieldDefinition this[string name]
            {
                get { return this[FileFormats.CRC32.Hash(name)]; }
            }

            public FieldDefinition this[uint hash]
            {
                get { return this.Fields.FirstOrDefault(fd => fd.Hash == hash); }
            }
        }

        public class FieldDefinition
        {
            public string Name { get; private set; }
            public uint Hash { get; private set; }
            public FieldType Type { get; private set; }

            public FieldDefinition(string name, uint hash, FieldType type)
            {
                this.Name = name;
                this.Hash = hash;
                this.Type = type;
            }
        }

        [XmlRoot("class")]
        public class RawClassDefinition
        {
            private string _Path;
            private string _Name;
            private string _Inherit;
            private List<RawFieldDefinition> _Fields = new List<RawFieldDefinition>();

            [XmlIgnore]
            public string Path
            {
                get { return this._Path; }
                set { this._Path = value; }
            }

            [XmlAttribute("name")]
            public string Name
            {
                get { return this._Name; }
                set { this._Name = value; }
            }

            [XmlAttribute("inherit")]
            public string Inherit
            {
                get { return this._Inherit; }
                set { this._Inherit = value; }
            }

            [XmlElement("field")]
            public List<RawFieldDefinition> Fields
            {
                get { return this._Fields; }
                set { this._Fields = value; }
            }
        }

        [XmlRoot("field")]
        public class RawFieldDefinition
        {
            private string _Name;
            private uint? _Hash;
            private FieldType _Type = FieldType.BinHex;

            [XmlAttribute("name")]
            public string Name
            {
                get { return this._Name; }
                set
                {
                    if (this._Hash.HasValue == true &&
                        FileFormats.CRC32.Hash(value) != this._Hash.Value)
                    {
                        throw new InvalidOperationException();
                    }

                    this._Name = value;
                }
            }

            [XmlIgnore]
            public uint Hash
            {
                get
                {
                    if (this._Hash.HasValue == true)
                    {
                        return this._Hash.Value;
                    }

                    if (this._Name != null)
                    {
                        var hash = FileFormats.CRC32.Hash(this._Name);
                        this._Hash = hash;
                        return hash;
                    }

                    throw new InvalidOperationException();
                }

                set
                {
                    if (this._Name != null &&
                        FileFormats.CRC32.Hash(this._Name) != value)
                    {
                        throw new InvalidOperationException();
                    }

                    this._Hash = value;
                }
            }

            [XmlAttribute("hash")]
            public string HashString
            {
                get { return this.Hash.ToString("X8"); }
                set { this.Hash = uint.Parse(value, NumberStyles.AllowHexSpecifier); }
            }

            [XmlAttribute("type")]
            public FieldType Type
            {
                get { return this._Type; }
                set { this._Type = value; }
            }
        }

        private void LoadObjectFileDefinitions(ProjectData.Project project)
        {
            var rawObjectFileDefs = new List<RawObjectFileDefinition>();

            foreach (var inputPath in GetObjectFilePaths(project))
            {
                using (var input = File.OpenRead(inputPath))
                {
                    var serializer = new XmlSerializer(typeof(RawObjectFileDefinition),
                                                       new[] {typeof(RawObjectDefinition)});
                    var rawObjectFileDef = (RawObjectFileDefinition)serializer.Deserialize(input);
                    rawObjectFileDef.Path = inputPath;
                    rawObjectFileDefs.Add(rawObjectFileDef);
                }
            }

            foreach (var rawObjectFileDef in rawObjectFileDefs)
            {
                var objectFileDef = LoadObjectFileDefinition(rawObjectFileDef);
                if (this._ObjectFileDefinitions.Any(ofd => ofd.Name == objectFileDef.Name) == true)
                {
                    throw new InvalidOperationException();
                }
                this._ObjectFileDefinitions.Add(objectFileDef);
            }
        }

        private ObjectFileDefinition LoadObjectFileDefinition(RawObjectFileDefinition rawObjectFileDef)
        {
            if (string.IsNullOrEmpty(rawObjectFileDef.Name) == true)
            {
                throw new InvalidOperationException();
            }

            var name = rawObjectFileDef.Name;
            var objectDefs = new List<ObjectDefinition>();
            foreach (var rawObjectDef in rawObjectFileDef.ObjectDefinitions)
            {
                var objectDef = LoadObjectDefinition(rawObjectDef);
                if (objectDefs.Any(fd => fd.Hash == objectDef.Hash))
                {
                    throw new InvalidOperationException();
                }
                objectDefs.Add(objectDef);
            }
            return new ObjectFileDefinition(name, objectDefs);
        }

        private ObjectDefinition LoadObjectDefinition(RawObjectDefinition rawObjectDef)
        {
            var name = rawObjectDef.Name;
            var hash = rawObjectDef.Hash;
            ClassDefinition classDef = null;

            var className = rawObjectDef.ClassDefinition;
            if (string.IsNullOrEmpty(className) == false)
            {
                classDef = this.GetClassDefinition(className);
                if (classDef == null)
                {
                    throw new InvalidOperationException();
                }
            }

            var objectDefs = new List<ObjectDefinition>();
            foreach (var childObjectDef in rawObjectDef.ObjectDefinitions)
            {
                var childDef = LoadObjectDefinition(childObjectDef);
                if (objectDefs.Any(fd => fd.Hash == childDef.Hash))
                {
                    throw new InvalidOperationException();
                }
                objectDefs.Add(childDef);
            }
            return new ObjectDefinition(name, hash, classDef, objectDefs);
        }

        private static string[] GetObjectFilePaths(ProjectData.Project project)
        {
            return Directory.GetFiles(project.ListsPath, "*.binaryobjectfile.xml", SearchOption.AllDirectories);
        }

        public class ObjectFileDefinition
        {
            public string Name { get; private set; }
            public ReadOnlyCollection<ObjectDefinition> ObjectDefinitions { get; private set; }

            public ObjectFileDefinition(string name, IList<ObjectDefinition> objectDefs)
            {
                this.Name = name;
                this.ObjectDefinitions = new ReadOnlyCollection<ObjectDefinition>(objectDefs);
            }
        }

        public class ObjectDefinition
        {
            public string Name { get; private set; }
            public uint Hash { get; private set; }
            public ClassDefinition ClassDefinition { get; private set; }
            public ReadOnlyCollection<ObjectDefinition> ObjectDefinitions { get; private set; }

            public ObjectDefinition(string name, uint hash, ClassDefinition classDef, IList<ObjectDefinition> objectDefs)
            {
                this.Name = name;
                this.Hash = hash;
                this.ClassDefinition = classDef;
                this.ObjectDefinitions = new ReadOnlyCollection<ObjectDefinition>(objectDefs);
            }
        }

        [XmlRoot("file")]
        public class RawObjectFileDefinition
        {
            private string _Path;
            private string _Name;
            private List<RawObjectDefinition> _ObjectDefinitions = new List<RawObjectDefinition>();

            [XmlIgnore]
            public string Path
            {
                get { return this._Path; }
                set { this._Path = value; }
            }

            [XmlAttribute("name")]
            public string Name
            {
                get { return this._Name; }
                set { this._Name = value; }
            }

            [XmlElement("object")]
            public List<RawObjectDefinition> ObjectDefinitions
            {
                get { return this._ObjectDefinitions; }
                set { this._ObjectDefinitions = value; }
            }
        }

        public class RawObjectDefinition
        {
            private string _Name;
            private uint? _Hash;
            private string _ClassDefinition;
            private List<RawObjectDefinition> _ObjectDefinitions = new List<RawObjectDefinition>();

            [XmlAttribute("name")]
            public string Name
            {
                get { return this._Name; }
                set
                {
                    if (this._Hash.HasValue == true &&
                        FileFormats.CRC32.Hash(value) != this._Hash.Value)
                    {
                        throw new InvalidOperationException();
                    }

                    this._Name = value;
                }
            }

            [XmlIgnore]
            public uint Hash
            {
                get
                {
                    if (this._Hash.HasValue == true)
                    {
                        return this._Hash.Value;
                    }

                    if (this._Name != null)
                    {
                        var hash = FileFormats.CRC32.Hash(this._Name);
                        this._Hash = hash;
                        return hash;
                    }

                    throw new InvalidOperationException();
                }

                set
                {
                    if (this._Name != null &&
                        FileFormats.CRC32.Hash(this._Name) != value)
                    {
                        throw new InvalidOperationException();
                    }

                    this._Hash = value;
                }
            }

            [XmlAttribute("hash")]
            public string HashString
            {
                get { return this.Hash.ToString("X8"); }
                set { this.Hash = uint.Parse(value, NumberStyles.AllowHexSpecifier); }
            }

            [XmlAttribute("class")]
            public string ClassDefinition
            {
                get { return this._ClassDefinition; }
                set { this._ClassDefinition = value; }
            }

            [XmlElement("object")]
            public List<RawObjectDefinition> ObjectDefinitions
            {
                get { return this._ObjectDefinitions; }
                set { this._ObjectDefinitions = value; }
            }
        }
    }
}
