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
using System.Linq;

namespace Gibbed.Dunia2.BinaryObjectInfo.Definitions
{
    public class ClassDefinition : IDefinition
    {
        public string Name { get; internal set; }
        public uint Hash { get; internal set; }

        public ReadOnlyCollection<FriendDefinition> Friends { get; internal set; }
        public ReadOnlyCollection<FieldDefinition> Fields { get; internal set; }
        public ReadOnlyCollection<ClassDefinition> Objects { get; internal set; }

        public bool DynamicNestedClasses { get; internal set; }
        public string ClassFieldName { get; internal set; }
        public uint? ClassFieldHash { get; internal set; }

        public FieldDefinition GetFieldDefinition(string name, IEnumerable<FileFormats.BinaryObject> chain)
        {
            return this.GetFieldDefinition(FileFormats.CRC32.Hash(name), chain);
        }

        public FieldDefinition GetFieldDefinition(uint hash, IEnumerable<FileFormats.BinaryObject> chain)
        {
            var def = this.Fields.FirstOrDefault(fd => fd.Hash == hash);
            if (def != null)
            {
                return def;
            }

            foreach (var friend in this.Friends)
            {
                if (string.IsNullOrEmpty(friend.ConditionField) == false)
                {
                    if (chain == null)
                    {
                        throw new NotSupportedException();
                    }

                    byte[] fieldData;
                    var hasFieldData = GetFieldData(friend.ConditionField, chain, out fieldData);
                    if (hasFieldData == false)
                    {
                        continue;
                    }

                    var conditionData = FieldTypeSerializers.Serialize(friend.ConditionType, friend.ConditionValue);
                    if (fieldData.SequenceEqual(conditionData) == false)
                    {
                        continue;
                    }
                }

                def = friend.Class.GetFieldDefinition(hash, chain);
                if (def != null)
                {
                    return def;
                }
            }

            return null;
        }

        public ClassDefinition GetObjectDefinition(string name, IEnumerable<FileFormats.BinaryObject> chain)
        {
            return this.GetObjectDefinition(FileFormats.CRC32.Hash(name), chain);
        }

        public ClassDefinition GetObjectDefinition(uint hash, IEnumerable<FileFormats.BinaryObject> chain)
        {
            var def = this.Objects.FirstOrDefault(fd => fd.Hash == hash);
            if (def != null)
            {
                return def;
            }

            foreach (var friend in this.Friends)
            {
                if (string.IsNullOrEmpty(friend.ConditionField) == false)
                {
                    if (chain == null)
                    {
                        throw new NotSupportedException();
                    }

                    byte[] fieldData;
                    var hasFieldData = GetFieldData(friend.ConditionField, chain, out fieldData);
                    if (hasFieldData == false)
                    {
                        continue;
                    }

                    var conditionData = FieldTypeSerializers.Serialize(friend.ConditionType, friend.ConditionValue);
                    if (fieldData.SequenceEqual(conditionData) == false)
                    {
                        continue;
                    }
                }

                def = friend.Class.GetObjectDefinition(hash, chain);
                if (def != null)
                {
                    return def;
                }
            }

            return null;
        }

        private static bool GetFieldData(string path, IEnumerable<FileFormats.BinaryObject> chain, out byte[] data)
        {
            data = null;

            if (chain == null)
            {
                return false;
            }

            int skip = 0;
            int i;
            for (i = 0; i < path.Length; i++)
            {
                if (path[i] == '^')
                {
                    skip++;
                }
                else
                {
                    break;
                }
            }

            var node = chain.Reverse().Skip(skip).FirstOrDefault();
            if (node == null)
            {
                return false;
            }

            var parts = path.Substring(i).Split('.');

            var children = parts.Take(parts.Length - 1);
            var last = parts.Last();

            foreach (var child in children)
            {
                node = node.Children.FirstOrDefault(c => c.NameHash == FileFormats.CRC32.Hash(child));
                if (node == null)
                {
                    return false;
                }
            }

            var hash = FileFormats.CRC32.Hash(last);
            if (node.Fields.ContainsKey(hash) == false)
            {
                return false;
            }

            data = node.Fields[hash];
            if (data == null)
            {
                return false;
            }

            return true;
        }
    }
}
