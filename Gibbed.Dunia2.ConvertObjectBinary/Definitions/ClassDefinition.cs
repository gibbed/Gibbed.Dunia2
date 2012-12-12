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
using System.Collections.ObjectModel;
using System.Linq;

namespace Gibbed.Dunia2.ConvertObjectBinary.Definitions
{
    public class ClassDefinition : IDefinition
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
}
