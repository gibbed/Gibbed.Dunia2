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
    public class Snapshot : ICloneable
    {
        public uint Width;
        public uint Height;
        public uint BytesPerPixel;
        public uint BitsPerComponent;
        public byte[] Data;

        public void Serialize(Stream output, Endian endian)
        {
            output.WriteValueU32(this.Width, endian);
            output.WriteValueU32(this.Height, endian);
            output.WriteValueU32(this.BytesPerPixel, endian);
            output.WriteValueU32(this.BitsPerComponent, endian);

            var size = (this.BitsPerComponent *
                        this.BytesPerPixel *
                        this.Height *
                        this.Width) / 8;

            if (size != this.Data.Length)
            {
                throw new InvalidOperationException();
            }

            output.WriteBytes(this.Data);

            output.WriteValueU32(0, endian); // unknown6
        }

        public void Deserialize(Stream input, Endian endian)
        {
            this.Width = input.ReadValueU32(endian);
            this.Height = input.ReadValueU32(endian);
            this.BytesPerPixel = input.ReadValueU32(endian);
            this.BitsPerComponent = input.ReadValueU32(endian);

            var size = (this.Width *
                        this.Height *
                        this.BytesPerPixel *
                        this.BitsPerComponent) / 8;
            this.Data = input.ReadBytes(size);

            var unknown6 = input.ReadValueU32(endian);
            for (uint i = 0; i < unknown6; i++)
            {
                throw new NotSupportedException();
                // two strings prefixed by a uint length?
            }
        }

        public object Clone()
        {
            return new Snapshot()
            {
                Width = this.Width,
                Height = this.Height,
                BytesPerPixel = this.BytesPerPixel,
                BitsPerComponent = this.BitsPerComponent,
                Data = this.Data != null ? (byte[])this.Data.Clone() : null,
            };
        }
    }
}
