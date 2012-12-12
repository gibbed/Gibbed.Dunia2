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
}
