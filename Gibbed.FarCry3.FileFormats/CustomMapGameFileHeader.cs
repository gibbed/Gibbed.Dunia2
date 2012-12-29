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
using Gibbed.FarCry3.FileFormats.CustomMap;

namespace Gibbed.FarCry3.FileFormats
{
    public class CustomMapGameFileHeader : ICloneable
    {
        public float Unknown2;
        public float Unknown3;
        public float Unknown4;
        public long Unknown5;
        public string Creator;
        public long Unknown7;
        public string Author;
        public string Name;
        public MapId MapId;
        public Guid VersionId;
        public DateTime TimeModified;
        public DateTime TimeCreated;
        public MapSize MapSize = MapSize.Invalid;
        public PlayerRange PlayerRange = PlayerRange.TwoToFour;
        public uint Unknown16;
        public byte Unknown17;

        public void Serialize(Stream output, Endian endian)
        {
            output.WriteValueF32(this.Unknown2, endian);
            output.WriteValueF32(this.Unknown3, endian);
            output.WriteValueF32(this.Unknown4, endian);

            output.WriteValueS64(this.Unknown5, endian);
            output.WriteString(this.Creator, endian);
            output.WriteValueS64(this.Unknown7, endian);
            output.WriteString(this.Author, endian);
            output.WriteString(this.Name, endian);
            this.MapId.Serialize(output, endian);
            Helpers.WriteMungedGuid(output, this.VersionId, endian);
            Helpers.WriteTime(output, (Time)this.TimeModified, endian);
            Helpers.WriteTime(output, (Time)this.TimeCreated, endian);
            output.WriteValueEnum<MapSize>(this.MapSize, endian);
            output.WriteValueEnum<PlayerRange>(this.PlayerRange, endian);
            output.WriteValueU32(this.Unknown16, endian);
            output.WriteValueU8(this.Unknown17);
        }

        public void Deserialize(Stream input, Endian endian)
        {
            this.Unknown2 = input.ReadValueF32(endian);
            this.Unknown3 = input.ReadValueF32(endian);
            this.Unknown4 = input.ReadValueF32(endian);

            if (this.Unknown2.Equals(0.0f) == false ||
                this.Unknown3.Equals(0.0f) == false ||
                this.Unknown4.Equals(0.0f) == false)
            {
                throw new FormatException();
            }

            this.Unknown5 = input.ReadValueS64(endian);
            this.Creator = input.ReadString(endian);
            this.Unknown7 = input.ReadValueS64(endian);
            this.Author = input.ReadString(endian);
            this.Name = input.ReadString(endian);
            this.MapId = MapId.Deserialize(input, endian);
            this.VersionId = Helpers.ReadMungedGuid(input, endian);
            this.TimeModified = (DateTime)Helpers.ReadTime(input, endian);
            this.TimeCreated = (DateTime)Helpers.ReadTime(input, endian);
            this.MapSize = input.ReadValueEnum<MapSize>(endian);
            this.PlayerRange = input.ReadValueEnum<PlayerRange>(endian);
            this.Unknown16 = input.ReadValueU32(endian);
            this.Unknown17 = input.ReadValueU8();
        }

        public object Clone()
        {
            return new CustomMapGameFileHeader()
            {
                Unknown2 = this.Unknown2,
                Unknown3 = this.Unknown3,
                Unknown4 = this.Unknown4,
                Unknown5 = this.Unknown5,
                Creator = this.Creator,
                Unknown7 = this.Unknown7,
                Author = this.Author,
                Name = this.Name,
                MapId = this.MapId,
                VersionId = this.VersionId,
                TimeModified = this.TimeModified,
                TimeCreated = this.TimeCreated,
                MapSize = this.MapSize,
                PlayerRange = this.PlayerRange,
                Unknown16 = this.Unknown16,
                Unknown17 = this.Unknown17,
            };
        }
    }
}
