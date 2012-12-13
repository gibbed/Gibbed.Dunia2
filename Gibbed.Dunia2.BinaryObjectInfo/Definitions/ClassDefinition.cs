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

        public FieldDefinition GetFieldDefinition(string name, FileFormats.BinaryObject conditionNode)
        {
            return this.GetFieldDefinition(FileFormats.CRC32.Hash(name), conditionNode);
        }

        public FieldDefinition GetFieldDefinition(uint hash, FileFormats.BinaryObject conditionNode)
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
                    if (conditionNode == null)
                    {
                        throw new NotSupportedException();
                    }

                    var conditionValue = GetFieldValue(friend.ConditionField, conditionNode);
                    if (conditionValue.HasValue == false ||
                        conditionValue.Value != friend.ConditionValue)
                    {
                        continue;
                    }
                }

                def = friend.Class.GetFieldDefinition(hash, conditionNode);
                if (def != null)
                {
                    return def;
                }
            }

            return null;
        }

        public ClassDefinition GetObjectDefinition(string name, FileFormats.BinaryObject conditionNode)
        {
            return this.GetObjectDefinition(FileFormats.CRC32.Hash(name), conditionNode);
        }

        public ClassDefinition GetObjectDefinition(uint hash, FileFormats.BinaryObject conditionNode)
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
                    if (conditionNode == null)
                    {
                        throw new NotSupportedException();
                    }

                    var conditionValue = GetFieldValue(friend.ConditionField, conditionNode);

                    if (conditionValue.HasValue == false)
                    {
                        continue;
                    }

                    if (conditionValue.Value != friend.ConditionValue)
                    {
                        continue;
                    }
                }

                def = friend.Class.GetObjectDefinition(hash, conditionNode);
                if (def != null)
                {
                    return def;
                }
            }

            return null;
        }

        private static int? GetFieldValue(string path, FileFormats.BinaryObject node)
        {
            if (node == null)
            {
                return null;
            }

            int i;
            for (i = 0; i < path.Length; i++)
            {
                if (path[i] == '^')
                {
                    node = node.Parent;
                    if (node == null)
                    {
                        return null;
                    }
                }
                else
                {
                    break;
                }
            }

            var parts = path.Substring(i).Split('.');

            var children = parts.Take(parts.Length - 1);
            var last = parts.Last();

            foreach (var child in children)
            {
                node = node.Children.FirstOrDefault(c => c.NameHash == FileFormats.CRC32.Hash(child));
                if (node == null)
                {
                    return null;
                }
            }

            var hash = FileFormats.CRC32.Hash(last);
            if (node.Fields.ContainsKey(hash) == false)
            {
                return null;
            }

            var field = node.Fields[hash];
            if (field == null ||
                field.Length != 4)
            {
                return null;
            }

            return BitConverter.ToInt32(field, 0);
        }
    }
}
