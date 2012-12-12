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
    public class ObjectDefinition
    {
        private string _Name;
        private uint? _Hash;
        private string _ClassDefinition;
        private string _ClassFieldName;
        private uint? _ClassFieldHash;
        private List<ObjectDefinition> _ObjectDefinitions = new List<ObjectDefinition>();

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
        public List<ObjectDefinition> ObjectDefinitions
        {
            get { return this._ObjectDefinitions; }
            set { this._ObjectDefinitions = value; }
        }
    }
}
