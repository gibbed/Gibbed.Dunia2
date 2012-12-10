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

        public ClassDefinition GetClassDefinition(uint hash)
        {
            return this._ClassDefinitions.SingleOrDefault(cd => cd.Hash == hash);
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

            var serializer = new XmlSerializer(typeof(RawClassDefinition));
            foreach (var inputPath in GetClassDefinitionPaths(project))
            {
                using (var input = File.OpenRead(inputPath))
                {
                    var rawClassDef = (RawClassDefinition)serializer.Deserialize(input);
                    rawClassDef.Path = inputPath;
                    rawClassDefs.Add(rawClassDef);
                }
            }

            foreach (var rawClassDef in rawClassDefs)
            {
                var classDef = LoadClassDefinition(rawClassDefs, rawClassDef);
                if (this._ClassDefinitions.Any(cd => cd.Name == classDef.Name ||
                                                     cd.Hash == classDef.Hash) == true)
                {
                    throw new ConfigurationLoadException(string.Format("duplicate binary class '{0}'", classDef.Name));
                }
                this._ClassDefinitions.Add(classDef);
            }
        }

        private ClassDefinition LoadClassDefinition(List<RawClassDefinition> rawClassDefs,
                                                    RawClassDefinition rawClassDef)
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

        private void MergeClassDefinition(string rootClassName,
                                          List<RawClassDefinition> rawClassDefs,
                                          RawClassDefinition rawClassDef,
                                          List<FieldDefinition> fieldDefs,
                                          List<ClassDefinition> nestedClassDefs)
        {
            foreach (var rawFieldDef in rawClassDef.FieldDefinitions)
            {
                var fieldDef = LoadFieldDefinition(rawFieldDef);
                if (fieldDefs.Any(fd => fd.Hash == fieldDef.Hash))
                {
                    throw new ConfigurationLoadException(
                        string.Format("duplicate field '{1}' from '{2}' in binary class '{0}'",
                                      rootClassName,
                                      rawFieldDef.Name,
                                      rawClassDef.Name));
                }
                fieldDefs.Add(fieldDef);
            }

            foreach (var rawNestedClassDef in rawClassDef.NestedClassDefinitions)
            {
                var objectDef = this.LoadClassDefinition(rawClassDefs, rawNestedClassDef);
                if (nestedClassDefs.Any(fd => fd.Hash == objectDef.Hash))
                {
                    throw new ConfigurationLoadException(
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
                    throw new ConfigurationLoadException(
                        string.Format("null inherit name from '{1}' in binary class '{0}'",
                                      rootClassName,
                                      rawClassDef.Name));
                }

                var inheritRawClassDef = rawClassDefs.FirstOrDefault(rcd => rcd.Name == inherit.Name);
                if (inheritRawClassDef == null)
                {
                    throw new ConfigurationLoadException(
                        string.Format("could not find definition for binary class '{0}'",
                                      inherit.Name));
                }

                MergeClassDefinition(rootClassName, rawClassDefs, inheritRawClassDef, fieldDefs, nestedClassDefs);
            }
        }

        private FieldDefinition LoadFieldDefinition(RawFieldDefinition rawFieldDef)
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
                    throw new ConfigurationLoadException(string.Format(
                        "field '{0}' specified an enum but isn't an enum",
                        rawFieldDef.Name));
                }

                enumDef = LoadEnumDefinition(rawFieldDef.EnumDefinition);
            }

            return new FieldDefinition(rawFieldDef.Name, rawFieldDef.Hash, rawFieldDef.Type, enumDef);
        }

        private EnumDefinition LoadEnumDefinition(RawEnumDefinition rawEnumDef)
        {
            var name = rawEnumDef.Name;

            var elementDefs = new List<EnumElementDefinition>();
            foreach (var rawElementDef in rawEnumDef.ElementDefinitions)
            {
                if (elementDefs.Any(ed => ed.Name == rawElementDef.Name) == true)
                {
                    throw new ConfigurationLoadException(
                        string.Format("duplicate element name '{1}' in binary enum '{0}'",
                                      rawEnumDef.Name,
                                      rawElementDef.Name));
                }

                if (elementDefs.Any(ed => ed.Value == rawElementDef.Value) == true)
                {
                    throw new ConfigurationLoadException(string.Format(
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

        public class ClassDefinition
        {
            public string Name { get; private set; }
            public uint Hash { get; private set; }
            public ReadOnlyCollection<FieldDefinition> FieldDefinitions { get; private set; }
            public ReadOnlyCollection<ClassDefinition> NestedClassDefinitions { get; private set; }
            public bool DynamicNestedClasses { get; private set; }
            public string ClassFieldName { get; private set; }
            public uint? ClassFieldHash { get; private set; }

            public ClassDefinition(string name,
                                   uint hash,
                                   IList<FieldDefinition> fieldDefs,
                                   IList<ClassDefinition> nestedClassDefs,
                                   bool dynamicNestedClasses,
                                   string classFieldName,
                                   uint? classFieldHash)
            {
                this.Name = name;
                this.Hash = hash;
                this.FieldDefinitions = new ReadOnlyCollection<FieldDefinition>(fieldDefs);
                this.NestedClassDefinitions = new ReadOnlyCollection<ClassDefinition>(nestedClassDefs);
                this.DynamicNestedClasses = dynamicNestedClasses;
                this.ClassFieldName = classFieldName;
                this.ClassFieldHash = classFieldHash;
            }

            public FieldDefinition GetFieldDefinition(string name)
            {
                return this.GetFieldDefinition(FileFormats.CRC32.Hash(name));
            }

            public FieldDefinition GetFieldDefinition(uint hash)
            {
                return this.FieldDefinitions.FirstOrDefault(fd => fd.Hash == hash);
            }

            public ClassDefinition GetNestedClassDefinition(string name)
            {
                return this.GetNestedClassDefinition(FileFormats.CRC32.Hash(name));
            }

            public ClassDefinition GetNestedClassDefinition(uint hash)
            {
                return this.NestedClassDefinitions.FirstOrDefault(fd => fd.Hash == hash);
            }
        }

        public class FieldDefinition
        {
            public string Name { get; private set; }
            public uint Hash { get; private set; }
            public FieldType Type { get; private set; }
            public EnumDefinition EnumDefinition { get; private set; }

            public FieldDefinition(string name, uint hash, FieldType type, EnumDefinition enumDef)
            {
                this.Name = name;
                this.Hash = hash;
                this.Type = type;
                this.EnumDefinition = enumDef;
            }
        }

        public class EnumDefinition
        {
            public string Name { get; private set; }
            public List<EnumElementDefinition> ElementDefinitions { get; private set; }

            public EnumDefinition(string name, List<EnumElementDefinition> elementDefs)
            {
                this.Name = name;
                this.ElementDefinitions = elementDefs ?? new List<EnumElementDefinition>();
            }
        }

        public class EnumElementDefinition
        {
            public string Name { get; private set; }
            public int Value { get; private set; }

            public EnumElementDefinition(string name, int value)
            {
                this.Name = name;
                this.Value = value;
            }
        }

        [XmlRoot("class")]
        public class RawClassDefinition
        {
            private string _Path;
            private string _Name;
            private uint? _Hash;
            private List<RawInheritDefinition> _Inherit;
            private List<RawFieldDefinition> _FieldDefinitions = new List<RawFieldDefinition>();
            private List<RawClassDefinition> _NestedClassDefinitions = new List<RawClassDefinition>();
            private bool _DynamicNestedClasses;
            private string _ClassFieldName;
            private uint? _ClassFieldHash;

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

                    throw new ConfigurationLoadException("tried to get null hash value");
                }

                set
                {
                    if (this._Name != null)
                    {
                        var hash = FileFormats.CRC32.Hash(this._Name);
                        if (hash != value)
                        {
                            throw new InvalidOperationException(string.Format("hash mismatch for '{0}': {1} vs {2}",
                                                                              this._Name,
                                                                              hash,
                                                                              value));
                        }
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

            [XmlElement("inherit")]
            public List<RawInheritDefinition> Inherit
            {
                get { return this._Inherit; }
                set { this._Inherit = value; }
            }

            [XmlElement("merge")]
            public List<RawInheritDefinition> Merge
            {
                get { return this._Inherit; }
                set { this._Inherit = value; }
            }

            [XmlElement("field")]
            public List<RawFieldDefinition> FieldDefinitions
            {
                get { return this._FieldDefinitions; }
                set { this._FieldDefinitions = value; }
            }

            [XmlElement("object")]
            public List<RawClassDefinition> NestedClassDefinitions
            {
                get { return this._NestedClassDefinitions; }
                set { this._NestedClassDefinitions = value; }
            }

            [XmlAttribute("dynamic_nested_classes")]
            public bool DynamicNestedClasses
            {
                get { return this._DynamicNestedClasses; }
                set { this._DynamicNestedClasses = value; }
            }

            [XmlIgnore]
            public uint? ClassFieldHash
            {
                get
                {
                    if (this._ClassFieldHash.HasValue == true)
                    {
                        return this._ClassFieldHash.Value;
                    }

                    if (this._ClassFieldName != null)
                    {
                        var hash = FileFormats.CRC32.Hash(this._ClassFieldName);
                        this._ClassFieldHash = hash;
                        return hash;
                    }

                    return null;
                }

                set
                {
                    if (this._ClassFieldName != null)
                    {
                        var hash = FileFormats.CRC32.Hash(this._ClassFieldName);
                        if (hash != value)
                        {
                            throw new InvalidOperationException(string.Format("hash mismatch for '{0}': {1} vs {2}",
                                                                              this._ClassFieldName,
                                                                              hash,
                                                                              value));
                        }
                    }

                    this._ClassFieldHash = value;
                }
            }

            [XmlAttribute("class_field_hash")]
            public string ClassFieldHashString
            {
                set { this.ClassFieldHash = uint.Parse(value, NumberStyles.AllowHexSpecifier); }
            }

            [XmlAttribute("class_field_name")]
            public string ClassFieldName
            {
                get { return this._ClassFieldName; }
                set
                {
                    if (this._ClassFieldHash.HasValue == true &&
                        FileFormats.CRC32.Hash(value) != this._ClassFieldHash.Value)
                    {
                        throw new InvalidOperationException();
                    }

                    this._ClassFieldName = value;
                }
            }
        }

        public class RawInheritDefinition
        {
            private string _Name;

            [XmlAttribute("name")]
            public string Name
            {
                get { return this._Name; }
                set { this._Name = value; }
            }
        }

        public class RawFieldDefinition
        {
            private string _Name;
            private uint? _Hash;
            private FieldType _Type = FieldType.BinHex;
            private RawEnumDefinition _EnumDefinition;

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

            [XmlElement("enum")]
            public RawEnumDefinition EnumDefinition
            {
                get { return this._EnumDefinition; }
                set { this._EnumDefinition = value; }
            }
        }

        public class RawEnumDefinition
        {
            private string _Path;
            private string _Name;
            private List<RawEnumElementDefinition> _ElementDefinitions = new List<RawEnumElementDefinition>();

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

            [XmlElement("element")]
            public List<RawEnumElementDefinition> ElementDefinitions
            {
                get { return this._ElementDefinitions; }
                set { this._ElementDefinitions = value; }
            }
        }

        public class RawEnumElementDefinition
        {
            private string _Name;
            private int _Value;

            [XmlAttribute("name")]
            public string Name
            {
                get { return this._Name; }
                set { this._Name = value; }
            }

            //[XmlAttribute("value")]
            [XmlText]
            public int Value
            {
                get { return this._Value; }
                set { this._Value = value; }
            }
        }

        private void LoadObjectFileDefinitions(ProjectData.Project project)
        {
            var rawObjectFileDefs = new List<RawObjectFileDefinition>();

            var serializer = new XmlSerializer(typeof(RawObjectFileDefinition));
            foreach (var inputPath in GetObjectFilePaths(project))
            {
                using (var input = File.OpenRead(inputPath))
                {
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
            var objectDef = rawObjectFileDef.ObjectDefinition != null
                                ? this.LoadObjectDefinition(rawObjectFileDef.ObjectDefinition)
                                : null;
            return new ObjectFileDefinition(name, objectDef);
        }

        private ObjectDefinition LoadObjectDefinition(RawObjectDefinition rawObjectDef)
        {
            var name = rawObjectDef.Name;
            var hash = rawObjectDef.Hash;
            ClassDefinition classDef = null;
            var classFieldName = rawObjectDef.ClassFieldName;
            var classFieldHash = rawObjectDef.ClassFieldHash;

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
            return new ObjectDefinition(name, hash, classDef, classFieldName, classFieldHash, objectDefs);
        }

        private static string[] GetObjectFilePaths(ProjectData.Project project)
        {
            return Directory.GetFiles(project.ListsPath, "*.binaryobjectfile.xml", SearchOption.AllDirectories);
        }

        public class ObjectFileDefinition
        {
            public string Name { get; private set; }
            public ObjectDefinition ObjectDefinition { get; private set; }

            public ObjectFileDefinition(string name, ObjectDefinition objectDef)
            {
                this.Name = name;
                this.ObjectDefinition = objectDef;
            }
        }

        public class ObjectDefinition
        {
            public string Name { get; private set; }
            public uint Hash { get; private set; }
            public ClassDefinition ClassDefinition { get; private set; }
            public string ClassFieldName;
            public uint? ClassFieldHash;
            public ReadOnlyCollection<ObjectDefinition> ObjectDefinitions { get; private set; }

            public ObjectDefinition(string name,
                                    uint hash,
                                    ClassDefinition classDef,
                                    string classFieldName,
                                    uint? classFieldHash,
                                    IList<ObjectDefinition> objectDefs)
            {
                this.Name = name;
                this.Hash = hash;
                this.ClassDefinition = classDef;
                this.ClassFieldName = classFieldName;
                this.ClassFieldHash = classFieldHash;
                this.ObjectDefinitions = new ReadOnlyCollection<ObjectDefinition>(objectDefs ?? new ObjectDefinition[0]);
            }

            public ObjectDefinition GetNestedObjectDefinition(string name)
            {
                return this.GetNestedObjectDefinition(FileFormats.CRC32.Hash(name));
            }

            public ObjectDefinition GetNestedObjectDefinition(uint hash)
            {
                return this.ObjectDefinitions.FirstOrDefault(fd => fd.Hash == hash);
            }
        }

        [XmlRoot("file")]
        public class RawObjectFileDefinition
        {
            private string _Path;
            private string _Name;
            private RawObjectDefinition _ObjectDefinition;

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
            public RawObjectDefinition ObjectDefinition
            {
                get { return this._ObjectDefinition; }
                set { this._ObjectDefinition = value; }
            }
        }

        public class RawObjectDefinition
        {
            private string _Name;
            private uint? _Hash;
            private string _ClassDefinition;
            private string _ClassFieldName;
            private uint? _ClassFieldHash;
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

            [XmlIgnore]
            public uint? ClassFieldHash
            {
                get
                {
                    if (this._ClassFieldHash.HasValue == true)
                    {
                        return this._ClassFieldHash.Value;
                    }

                    if (this._ClassFieldName != null)
                    {
                        var hash = FileFormats.CRC32.Hash(this._ClassFieldName);
                        this._ClassFieldHash = hash;
                        return hash;
                    }

                    return null;
                }

                set
                {
                    if (this._ClassFieldName != null &&
                        FileFormats.CRC32.Hash(this._ClassFieldName) != value)
                    {
                        throw new InvalidOperationException();
                    }

                    this._ClassFieldHash = value;
                }
            }

            [XmlAttribute("class_field_hash")]
            public string ClassFieldHashString
            {
                set { this.ClassFieldHash = uint.Parse(value, NumberStyles.AllowHexSpecifier); }
            }

            [XmlAttribute("class_field_name")]
            public string ClassFieldName
            {
                get { return this._ClassFieldName; }
                set
                {
                    if (this._ClassFieldHash.HasValue == true &&
                        FileFormats.CRC32.Hash(value) != this._ClassFieldHash.Value)
                    {
                        throw new InvalidOperationException();
                    }

                    this._ClassFieldName = value;
                }
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
