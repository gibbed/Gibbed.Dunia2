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
using System.IO;
using Gibbed.IO;

namespace Gibbed.Dunia2.FileFormats
{
    public class SubFatFile
    {
        public readonly List<Big.Entry> Entries = new List<Big.Entry>();

        public void Serialize(FileStream output)
        {
            throw new NotImplementedException();
        }

        public void Deserialize(Stream input, BigFile fat)
        {
            var entrySerializer = fat.EntrySerializer;

            uint entryCount = input.ReadValueU32(Endian.Little);
            for (uint i = 0; i < entryCount; i++)
            {
                Big.Entry entry;
                entrySerializer.Deserialize(input, fat.Endian, out entry);
                this.Entries.Add(entry);
            }

            foreach (var entry in this.Entries)
            {
                BigFile.SanityCheckEntry(entry, fat.Platform);
            }
        }
    }
}
