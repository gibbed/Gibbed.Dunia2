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
using Gibbed.IO;

namespace Gibbed.Dunia2.FileFormats
{
    public class BigFile
    {
        public const uint Signature = 0x46415432; // 'FAT2'

        public int Version;
        public Big.Platform Platform;
        public uint Unknown5C;
        public uint Unknown8C;
        public uint Unknown90;
        public readonly List<Big.Entry> Entries = new List<Big.Entry>();

        public void Deserialize(Stream input)
        {
            var magic = input.ReadValueU32(Endian.Little);
            if (magic != Signature)
            {
                throw new FormatException("bad magic");
            }

            var version = input.ReadValueS32(Endian.Little); // 58
            if (version < 2 || version > 9)
            {
                throw new FormatException("unsupported file version");
            }

            var platform = Big.Platform.Invalid;
            uint unknown5C = 0;

            if (version >= 3)
            {
                unknown5C = input.ReadValueU32(Endian.Little); // ###### ##
                platform = (Big.Platform)(unknown5C & 0xFF);
                unknown5C >>= 8;
            }

            if (platform != Big.Platform.PC &&
                platform != Big.Platform.X360 &&
                platform != Big.Platform.PS3)
            {
                throw new FormatException("unsupported/invalid platform");
            }

            if (platform == Big.Platform.PC &&
                ( /*unknown5C < 0 ||*/ unknown5C > 3))
            {
                throw new FormatException("@5C is invalid for PC platform");
            }

            if (platform == Big.Platform.X360 &&
                (unknown5C < 1 || unknown5C > 4))
            {
                throw new FormatException("@5C is invalid for X360 platform");
            }

            if (platform == Big.Platform.PS3 &&
                (unknown5C < 1 || unknown5C > 4))
            {
                throw new FormatException("@5C is invalid for PS3 platform");
            }

            var endian = platform == Big.Platform.PC ? Endian.Little : Endian.Big;

            uint unknown8C = 0;
            uint unknown90 = 0;
            if (version >= 9)
            {
                unknown8C = input.ReadValueU32(Endian.Little);
                unknown90 = input.ReadValueU32(Endian.Little);
            }

            if (version >= _EntrySerializers.Count ||
                _EntrySerializers[version] == null)
            {
                throw new InvalidOperationException("entry serializer is missing");
            }
            var entrySerializer = _EntrySerializers[version];

            uint entryCount = input.ReadValueU32(Endian.Little);
            for (uint i = 0; i < entryCount; i++)
            {
                Big.Entry entry;
                entrySerializer.Deserialize(input, endian, out entry);
                this.Entries.Add(entry);
            }

            // TODO: there's more data...

            this.Version = version;
            this.Platform = platform;
            this.Unknown5C = unknown5C;
            this.Unknown8C = unknown8C;
            this.Unknown90 = unknown90;
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
