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

using System.IO;
using Gibbed.IO;

namespace Gibbed.Dunia2.FileFormats.Big
{
    internal class EntrySerializerV9 : IEntrySerializer
    {
        // hhhhhhhh hhhhhhhh hhhhhhhh hhhhhhhh
        // hhhhhhhh hhhhhhhh hhhhhhhh hhhhhhhh
        // uuuuuuuu uuuuuuuu uuuuuuuu uuuuuuss
        // oooooooo oooooooo oooooooo oooooooo
        // oocccccc cccccccc cccccccc cccccccc

        // [h] hash = 64 bits
        // [u] uncompressed size = 30 bits
        // [s] compression scheme = 2 bits
        // [o] offset = 34 bits
        // [c] compressed size = 30 bits

        public void Serialize(Stream output, Entry entry, Endian endian)
        {
            var a = (uint)((entry.NameHash & 0xFFFFFFFF00000000ul) >> 32);
            var b = (uint)((entry.NameHash & 0x00000000FFFFFFFFul) >> 0);
            
            uint c = 0;
            c |= ((entry.UncompressedSize << 2) & 0xFFFFFFFCu);
            c |= (uint)(((byte)entry.CompressionScheme << 0) & 0x00000003u);

            uint d = (uint)((entry.Offset & 0X00000003FFFFFFFCL) >> 2);

            uint e = 0;
            e |= (uint)((entry.Offset & 0X0000000000000003L) << 30);
            e |= (entry.CompressedSize & 0x3FFFFFFFu) << 0;

            output.WriteValueU32(a, endian);
            output.WriteValueU32(b, endian);
            output.WriteValueU32(c, endian);
            output.WriteValueU32(d, endian);
            output.WriteValueU32(e, endian);
        }

        public void Deserialize(Stream input, Endian endian, out Entry entry)
        {
            var a = input.ReadValueU32(endian);
            var b = input.ReadValueU32(endian);
            var c = input.ReadValueU32(endian);
            var d = input.ReadValueU32(endian);
            var e = input.ReadValueU32(endian);

            entry.NameHash = b;
            entry.NameHash |= ((ulong)a) << 32;
            entry.UncompressedSize = (c & 0xFFFFFFFCu) >> 2;
            entry.CompressionScheme = (CompressionScheme)((c & 0x00000003u) >> 0);
            entry.Offset = (long)d << 2;
            entry.Offset |= ((e & 0xC0000000u) >> 30);
            entry.CompressedSize = (uint)((e & 0x3FFFFFFFul) >> 0);
        }
    }
}
