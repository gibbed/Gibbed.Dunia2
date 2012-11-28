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

namespace Gibbed.Dunia2.FileFormats.Big
{
    internal class EntrySerializerV5 : IEntrySerializer
    {
        // hhhhhhhh hhhhhhhh hhhhhhhh hhhhhhhh
        // uuuuuuuu uuuuuuuu uuuuuuuu uuuuuuss
        // oocccccc cccccccc cccccccc cccccccc
        // oooooooo oooooooo oooooooo oooooooo

        // [h] hash = 32 bits
        // [u] uncompressed size = 30 bits
        // [s] compression scheme = 2 bits
        // [o] offset = 34 bits
        // [c] compressed size = 30 bits

        public void Serialize(Stream output, Entry entry, Endian endian)
        {
            throw new NotImplementedException();
        }

        public void Deserialize(Stream input, Endian endian, out Entry entry)
        {
            var a = input.ReadValueU32(Endian.Little);
            var b = input.ReadValueU32(Endian.Little);
            var c = input.ReadValueU32(Endian.Little);
            var d = input.ReadValueU32(Endian.Little);

            entry.NameHash = a;
            entry.UncompressedSize = (b & 0xFFFFFFFCu) >> 2;
            entry.CompressionScheme = (CompressionScheme)((b & 0x00000003u) >> 0);
            entry.Offset = (long)d << 2;
            entry.Offset |= ((c & 0xC0000000u) >> 30);
            entry.CompressedSize = (uint)((c & 0x3FFFFFFFul) >> 0);

            throw new NotImplementedException();
        }
    }
}
