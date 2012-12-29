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
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using Gibbed.IO;

namespace Gibbed.Dunia2.FileFormats
{
    public class BigFile
    {
        public const uint Signature = 0x46415432; // 'FAT2'

        public Endian Endian
        {
            get
            {
                return (this.Platform == Big.Platform.PC || this.Platform == Big.Platform.Any)
                           ? Endian.Little
                           : Endian.Big;
            }
        }

        public int Version;
        public Big.Platform Platform;
        // ReSharper disable InconsistentNaming
        public uint Unknown74;
        // ReSharper restore InconsistentNaming
        public readonly List<Big.Entry> Entries = new List<Big.Entry>();
        public readonly List<Big.SubFatEntry> SubFats = new List<Big.SubFatEntry>();

        public void Serialize(Stream output)
        {
            var version = this.Version;

            if (version < 2 || version > 9)
            {
                throw new FormatException("unsupported file version");
            }

            if (this.Platform != Big.Platform.Any &&
                this.Platform != Big.Platform.PC &&
                this.Platform != Big.Platform.X360 &&
                this.Platform != Big.Platform.PS3)
            {
                throw new FormatException("unsupported/invalid platform");
            }

            if (IsValidPlatform(this.Platform, this.Unknown74) == false)
            {
                throw new FormatException("invalid platform settings");
            }

            var endian = this.Endian;

            output.WriteValueU32(Signature, Endian.Little);
            output.WriteValueS32(version, Endian.Little);

            if (version >= 3)
            {
                var platform = ((uint)this.Platform) & 0xFF;
                platform |= this.Unknown74 << 8;
                output.WriteValueU32(platform, Endian.Little);
            }

            if (version < 9)
            {
                throw new InvalidOperationException();
            }

            if (version >= 9)
            {
                output.WriteValueS32(this.SubFats.Sum(sf => sf.Entries.Count), Endian.Little); // subfat total entry count
                output.WriteValueS32(this.SubFats.Count, Endian.Little); // subfat count
            }

            if (version >= _EntrySerializers.Count ||
                _EntrySerializers[version] == null)
            {
                throw new InvalidOperationException("entry serializer is missing");
            }
            var entrySerializer = _EntrySerializers[version];

            output.WriteValueS32(this.Entries.Count, Endian.Little);
            foreach (var entry in this.Entries)
            {
                entrySerializer.Serialize(output, entry, endian);
            }

            output.WriteValueU32(0, Endian.Little);

            if (version >= 7)
            {
                output.WriteValueU32(0, Endian.Little);
            }

            foreach (var subfat in this.SubFats)
            {
                output.WriteValueS32(subfat.Entries.Count);
                foreach (var entry in subfat.Entries)
                {
                    entrySerializer.Serialize(output, entry, endian);
                }
            }
        }

        public void Deserialize(Stream input)
        {
            var magic = input.ReadValueU32(Endian.Little);
            if (magic != Signature)
            {
                throw new FormatException("bad magic");
            }

            var version = input.ReadValueS32(Endian.Little);
            if (version < 2 || version > 9)
            {
                throw new FormatException("unsupported file version");
            }
            this.Version = version;

            var platform = Big.Platform.Invalid;
            uint unknown74 = 0;

            if (version >= 3)
            {
                unknown74 = input.ReadValueU32(Endian.Little); // ###### ##
                platform = (Big.Platform)(unknown74 & 0xFF);
                unknown74 >>= 8;
            }

            if (platform != Big.Platform.Any &&
                platform != Big.Platform.PC &&
                platform != Big.Platform.X360 &&
                platform != Big.Platform.PS3)
            {
                throw new FormatException("unsupported/invalid platform");
            }

            if (IsValidPlatform(platform, unknown74) == false)
            {
                throw new FormatException("invalid platform settings");
            }

            this.Platform = platform;
            var endian = this.Endian;

            int subfatTotalEntryCount = 0;
            int subfatCount = 0;

            if (version >= 9)
            {
                subfatTotalEntryCount = input.ReadValueS32(Endian.Little);
                if (subfatTotalEntryCount < 0)
                {
                    throw new FormatException("invalid subfat first header entry index");
                }

                subfatCount = input.ReadValueS32(Endian.Little);
                if (subfatCount < 0)
                {
                    throw new FormatException("invalid subfat count");
                }
            }

            var entrySerializer = this.EntrySerializer;

            uint entryCount = input.ReadValueU32(Endian.Little);
            for (uint i = 0; i < entryCount; i++)
            {
                Big.Entry entry;
                entrySerializer.Deserialize(input, endian, out entry);
                this.Entries.Add(entry);
            }

            uint unknown1Count = input.ReadValueU32(Endian.Little);
            for (uint i = 0; i < unknown1Count; i++)
            {
                throw new NotSupportedException();
                input.ReadBytes(16);
            }

            if (version >= 7)
            {
                uint unknown2Count = input.ReadValueU32(Endian.Little);
                for (uint i = 0; i < unknown2Count; i++)
                {
                    throw new NotSupportedException();
                    input.ReadBytes(16);
                }
            }

            for (int i = 0; i < subfatCount; i++)
            {
                var subFat = new Big.SubFatEntry();
                uint subfatEntryCount = input.ReadValueU32(Endian.Little);
                for (uint j = 0; j < subfatEntryCount; j++)
                {
                    Big.Entry entry;
                    entrySerializer.Deserialize(input, endian, out entry);
                    subFat.Entries.Add(entry);
                }
                this.SubFats.Add(subFat);
            }

            var subfatComputedTotalEntryCount = this.SubFats.Sum(sf => sf.Entries.Count);
            if (subfatCount > 0 &&
                subfatTotalEntryCount != subfatComputedTotalEntryCount)
            {
                throw new FormatException("subfat total entry count mismatch");
            }

            this.Version = version;
            this.Platform = platform;
            this.Unknown74 = unknown74;

            foreach (var entry in this.Entries)
            {
                SanityCheckEntry(entry, platform);
            }

            foreach (var subFat in this.SubFats)
            {
                foreach (var entry in subFat.Entries)
                {
                    SanityCheckEntry(entry, platform);
                }
            }
        }

        internal static void SanityCheckEntry(Big.Entry entry, Big.Platform platform)
        {
            if (entry.CompressionScheme == Big.CompressionScheme.None)
            {
                if (platform != Big.Platform.X360 &&
                    entry.UncompressedSize != 0)
                {
                    throw new FormatException("got entry with no compression with a non-zero uncompressed size");
                }
            }
            else if (entry.CompressionScheme == Big.CompressionScheme.LZO1x ||
                     entry.CompressionScheme == Big.CompressionScheme.Zlib)
            {
                if (entry.CompressedSize == 0 &&
                    entry.UncompressedSize > 0)
                {
                    throw new FormatException(
                        "got entry with compression with a zero compressed size and a non-zero uncompressed size");
                }
            }
            else
            {
                throw new FormatException("got entry with unsupported compression scheme");
            }
        }

        private static bool IsValidPlatform(Big.Platform platform, uint unknown74)
        {
            if (platform == Big.Platform.Any &&
                unknown74 != 0)
            {
                return false;
            }

            if (platform == Big.Platform.PC &&
                ( /*unknown5C < 0 ||*/ unknown74 > 3))
            {
                return false;
            }

            if (platform == Big.Platform.X360 &&
                (unknown74 < 1 || unknown74 > 4))
            {
                return false;
            }

            if (platform == Big.Platform.PS3 &&
                (unknown74 < 1 || unknown74 > 4))
            {
                return false;
            }

            return true;
        }

        internal Big.IEntrySerializer EntrySerializer
        {
            get
            {
                if (this.Version >= _EntrySerializers.Count ||
                _EntrySerializers[this.Version] == null)
                {
                    throw new InvalidOperationException("entry serializer is missing");
                }
                return _EntrySerializers[this.Version];
            }
        }

        private static readonly ReadOnlyCollection<Big.IEntrySerializer> _EntrySerializers;

        static BigFile()
        {
            _EntrySerializers = new ReadOnlyCollection<Big.IEntrySerializer>(new Big.IEntrySerializer[]
            {
                null, // 0
                new Big.EntrySerializerV1(), // 1
                new Big.EntrySerializerV1(), // 2
                new Big.EntrySerializerV1(), // 3
                new Big.EntrySerializerV1(), // 4
                new Big.EntrySerializerV5(), // 5
                new Big.EntrySerializerV6V8(), // 6
                new Big.EntrySerializerV7(), // 7
                new Big.EntrySerializerV6V8(), // 8
                new Big.EntrySerializerV9(), // 9
            });
        }
    }
}
