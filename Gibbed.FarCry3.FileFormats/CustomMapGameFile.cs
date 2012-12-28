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

namespace Gibbed.FarCry3.FileFormats
{
    public class CustomMapGameFile
    {
        public const uint Version = 19;
        public const uint Signature = 0xD2FD0A6B; // crc32(CCustomMapGameFile)

        public CustomMapGameFileHeader Header;
        public Snapshot Snapshot;
        public Snapshot ExtraSnapshot;
        public CustomMap.Data Data;
        public CustomMap.Archive Archive;

        public void Serialize(Stream output)
        {
            throw new NotImplementedException();
        }

        public void Deserialize(Stream input)
        {
            var version = input.ReadValueU32(Endian.Little);
            if (version != Version &&
                version.Swap() != Version)
            {
                throw new FormatException("unsupported file version");
            }
            var endian = version == Version ? Endian.Little : Endian.Big;

            var magic = input.ReadValueU32(endian);
            if (magic != Signature)
            {
                throw new FormatException("bad magic");
            }

            this.Header = new CustomMapGameFileHeader();
            this.Header.Deserialize(input, endian);

            this.Snapshot = new Snapshot();
            this.Snapshot.Deserialize(input, endian);

            /* PS3 map files have an extra snapshot which has a larger resolution,
             * need to figure out how to adequately detect this (and get a sample
             * of a 360 map file. */
            if (endian == Endian.Big)
            {
                this.ExtraSnapshot = new Snapshot();
                this.ExtraSnapshot.Deserialize(input, endian);
            }

            this.Data = new CustomMap.Data();
            this.Data.Deserialize(input, endian);

            this.Archive = new CustomMap.Archive();
            this.Archive.Deserialize(input, endian);
        }
    }
}
