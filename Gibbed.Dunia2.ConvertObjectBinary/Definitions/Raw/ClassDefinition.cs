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
using System.Xml.Serialization;

namespace Gibbed.Dunia2.ConvertObjectBinary.Definitions.Raw
{
    [XmlRoot("class")]
    public class ClassDefinition
    {
        private string _Path;
        private string _Name;
        private uint? _Hash;
        private List<InheritDefinition> _Inherit;
        private List<FieldDefinition> _FieldDefinitions = new List<FieldDefinition>();
        private List<ClassDefinition> _NestedClassDefinitions = new List<ClassDefinition>();
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

                throw new LoadException("tried to get null hash value");
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
        public List<InheritDefinition> Inherit
        {
            get { return this._Inherit; }
            set { this._Inherit = value; }
        }

        [XmlElement("merge")]
        public List<InheritDefinition> Merge
        {
            get { return this._Inherit; }
            set { this._Inherit = value; }
        }

        [XmlElement("field")]
        public List<FieldDefinition> FieldDefinitions
        {
            get { return this._FieldDefinitions; }
            set { this._FieldDefinitions = value; }
        }

        [XmlElement("object")]
        public List<ClassDefinition> NestedClassDefinitions
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
}
