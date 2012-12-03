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
    public class Archive
    {
        public const uint Signature = 0x4D334346; // 'FC3M'

        public uint Version;
        public CompressedData Data;
        public CompressedData Header;
        public CompressedData Descriptor;

        public void Serialize(Stream output, Endian endian)
        {
            throw new NotImplementedException();
        }

        public void Deserialize(Stream input, Endian endian)
        {
            var baseOffset = input.Position;

            var magic = input.ReadValueU32(endian);
            if (magic != Signature)
            {
                throw new FormatException();
            }

            this.Version = input.ReadValueU32(endian);
            if (this.Version != 1)
            {
                throw new FormatException();
            }

            uint offsetData = input.ReadValueU32(endian);
            uint offsetHeader = input.ReadValueU32(endian);
            uint offsetDescriptor = input.ReadValueU32(endian);

            if (offsetData != 20)
            {
                throw new FormatException();
            }

            this.Data = new CompressedData();
            this.Data.Deserialize(input, endian);

            if (baseOffset + offsetHeader != input.Position)
            {
                throw new FormatException();
            }

            this.Header = new CompressedData();
            this.Header.Deserialize(input, endian);

            if (baseOffset + offsetDescriptor != input.Position)
            {
                throw new FormatException();
            }

            this.Descriptor = new CompressedData();
            this.Descriptor.Deserialize(input, endian);
        }
    }
}
