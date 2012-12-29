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
    public class CustomMapGameFile : ICloneable
    {
        public const uint Version = 19;
        public const uint Signature = 0xD2FD0A6B; // crc32(CCustomMapGameFile)

        public Endian Endian;
        public CustomMapGameFileHeader Header = new CustomMapGameFileHeader();
        public Snapshot Snapshot = new Snapshot();
        public Snapshot ExtraSnapshot = null;
        public CustomMap.Data Data = new CustomMap.Data();
        public CustomMap.Archive Archive = new CustomMap.Archive();

        public void Serialize(Stream output)
        {
            var endian = this.Endian;

            output.WriteValueU32(Version, endian);
            output.WriteValueU32(Signature, endian);
            
            this.Header.Serialize(output, endian);
            this.Snapshot.Serialize(output, endian);

            if (endian == Endian.Big)
            {
                this.ExtraSnapshot.Serialize(output, endian);
            }

            this.Data.Serialize(output, endian);
            this.Archive.Serialize(output, endian);
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

            this.Endian = endian;

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

        public object Clone()
        {
            return new CustomMapGameFile()
            {
                Endian = this.Endian,
                Header = this.Header != null ? (CustomMapGameFileHeader)this.Header.Clone() : null,
                Snapshot = this.Snapshot != null ? (Snapshot)this.Snapshot.Clone() : null,
                ExtraSnapshot = this.ExtraSnapshot != null ? (Snapshot)this.ExtraSnapshot.Clone() : null,
                Data = this.Data != null ? (CustomMap.Data)this.Data.Clone() : null,
                Archive = this.Archive != null ? (CustomMap.Archive)this.Archive.Clone() : null,
            };
        }
    }
}
