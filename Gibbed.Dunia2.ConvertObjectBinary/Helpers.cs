using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Gibbed.Dunia2.ConvertObjectBinary
{
    internal static class Helpers
    {
        public static Definitions.ObjectDefinition GetChildObjectDefinition(Definitions.ObjectDefinition objectDef,
                                                                              Definitions.ClassDefinition classDef,
                                                                              uint typeHash)
        {
            Definitions.ObjectDefinition childObjectDef = null;

            if (classDef != null)
            {
                var nestedClassDef = classDef.GetNestedClassDefinition(typeHash);
                if (nestedClassDef != null)
                {
                    childObjectDef = new Definitions.ObjectDefinition(nestedClassDef.Name,
                                                                        nestedClassDef.Hash,
                                                                        nestedClassDef,
                                                                        null,
                                                                        null,
                                                                        null);
                }
            }

            if (childObjectDef == null && objectDef != null)
            {
                childObjectDef = objectDef.GetNestedObjectDefinition(typeHash);
            }

            return childObjectDef;
        }
    }
}
