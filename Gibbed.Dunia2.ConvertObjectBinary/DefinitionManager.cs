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
using System.Linq;
using Gibbed.Dunia2.ConvertObjectBinary.Definitions;
using Gibbed.Dunia2.ConvertObjectBinary.Definitions.Loaders;

namespace Gibbed.Dunia2.ConvertObjectBinary
{
    internal class DefinitionManager
    {
        public DefinitionLookup<ClassDefinition> ClassDefinitions { get; private set; }
        public DefinitionLookup<ObjectFileDefinition> ObjectFileDefinitions { get; private set; }

        private DefinitionManager()
        {
        }

        public ClassDefinition GetClassDefinition(string name)
        {
            return this.ClassDefinitions[name];
        }

        public ClassDefinition GetClassDefinition(uint hash)
        {
            return this.ClassDefinitions[hash];
        }

        public ObjectFileDefinition GetObjectFileDefinition(string name)
        {
            if (name == null)
            {
                throw new ArgumentNullException("name");
            }

            name = name.ToLowerInvariant();
            return this.ObjectFileDefinitions.Items
                .FirstOrDefault(i => i.Name.ToLowerInvariant() == name);
        }

        public static DefinitionManager Load(ProjectData.Project project)
        {
            var manager = new DefinitionManager();
            manager.ClassDefinitions = ClassDefinitionLoader.Load(project);
            manager.ObjectFileDefinitions = ObjectFileDefinitionLoader.Load(project, manager.ClassDefinitions);
            return manager;
        }
    }
}
