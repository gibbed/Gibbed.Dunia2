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
using System.Linq;
using Gibbed.IO;
using ICSharpCode.SharpZipLib.Zip.Compression.Streams;

namespace Gibbed.FarCry3.FileFormats.CustomMap
{
    public class CompressedData : ICloneable
    {
        private readonly List<Block> _Blocks = new List<Block>();

        public byte[] Data { get; set; }

        public List<Block> Blocks
        {
            get { return _Blocks; }
        }

        public void Deserialize(Stream input, Endian endian)
        {
            var offset = input.ReadValueU32(endian);

            var length = offset - 4;
            this.Data = new byte[length];
            if (input.Read(this.Data, 0, this.Data.Length) != this.Data.Length)
            {
                throw new FormatException();
            }

            var blockCount = input.ReadValueU32(endian);
            this._Blocks.Clear();
            for (uint i = 0; i < blockCount; i++)
            {
                var block = new Block();
                block.VirtualOffset = input.ReadValueU32(endian);
                block.FileOffset = input.ReadValueU32(endian);
                block.IsCompressed = (block.FileOffset & 0x80000000) != 0;
                block.FileOffset &= 0x7FFFFFFF;
                this._Blocks.Add(block);
            }

            if (this._Blocks.Count == 0)
            {
                throw new FormatException();
            }

            if (this._Blocks.First().FileOffset != 4)
            {
                throw new FormatException();
            }

            if (this._Blocks.Last().FileOffset != 4 + this.Data.Length)
            {
                throw new FormatException();
            }
        }

        public void Serialize(Stream output, Endian endian)
        {
            output.WriteValueS32(4 + this.Data.Length, endian);
            output.Write(this.Data, 0, this.Data.Length);

            output.WriteValueS32(this._Blocks.Count, endian);
            foreach (var block in this._Blocks)
            {
                output.WriteValueU32(block.VirtualOffset, endian);

                uint foic = 0;
                foic |= block.FileOffset;
                foic &= 0x7FFFFFFF;
                foic |= (block.IsCompressed == true ? 1u : 0u) << 31;

                output.WriteValueU32(foic, endian);
            }
        }

        public struct Block
        {
            public uint VirtualOffset;
            public uint FileOffset;
            public bool IsCompressed;
        }

        public MemoryStream Unpack()
        {
            var memory = new MemoryStream();

            using (var data = new MemoryStream(this.Data))
            {
                for (int i = 0; i + 1 < this._Blocks.Count; i++)
                {
                    var block = this._Blocks[i + 0];
                    var next = this._Blocks[i + 1];

                    var size = next.VirtualOffset - block.VirtualOffset;

                    data.Seek(block.FileOffset - 4, SeekOrigin.Begin);

                    memory.Seek(block.VirtualOffset, SeekOrigin.Begin);

                    if (block.IsCompressed == true)
                    {
                        var zlib = new InflaterInputStream(data);
                        memory.WriteFromStream(zlib, size);
                    }
                    else
                    {
                        memory.WriteFromStream(data, size);
                    }
                }
            }

            memory.Position = 0;
            return memory;
        }

        public object Clone()
        {
            var clone = new CompressedData()
            {
                Data = this.Data != null ? (byte[])this.Data.Clone() : null,
            };
            clone._Blocks.AddRange(this._Blocks);
            return clone;
        }
    }
}
