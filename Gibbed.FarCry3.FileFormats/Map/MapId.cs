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
using System.IO;
using Gibbed.IO;

namespace Gibbed.FarCry3.FileFormats.Map
{
    public struct MapId
    {
        public Guid Guid;
        public uint Unknown2;
        public uint Unknown3;

        public void Serialize(Stream output, Endian endian)
        {
            Helpers.WriteMungedGuid(output, this.Guid, endian);
            output.WriteValueU32(this.Unknown2, endian);
            output.WriteValueU32(this.Unknown3, endian);
        }

        public static MapId Deserialize(Stream input, Endian endian)
        {
            MapId id;
            id.Guid = Helpers.ReadMungedGuid(input, endian);
            id.Unknown2 = input.ReadValueU32(endian);
            id.Unknown3 = input.ReadValueU32(endian);
            return id;
        }
    }
}
