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

namespace Gibbed.Dunia2.GeometryFormats
{
    public class GeometryResourceFile : IChunkFactory
    {
        private const uint _Signature = 0x4D455348; // 'MESH'
        public ushort MajorVersion;
        public ushort MinorVersion;
        public uint Unknown08;
        public Chunks.RootChunk Root;

        private const uint _MetadataSignature = 0x4D455441; // 'META'
        public byte[] Metadata;

        public void Deserialize(Stream input)
        {
            if (input.Position + 32 > input.Length)
            {
                throw new EndOfStreamException("not enough data for mesh header");
            }

            if (input.ReadValueU32(Endian.Little) != _Signature)
            {
                throw new FormatException();
            }
            var endian = Endian.Little;

            this.MajorVersion = input.ReadValueU16(endian);
            if (this.MajorVersion != 52)
            {
                throw new FormatException();
            }

            this.MinorVersion = input.ReadValueU16(endian);
            this.Unknown08 = input.ReadValueU32(endian);

            this.Root = (Chunks.RootChunk)DeserializeBlock(null, this, input, endian);

            this.ReadMetadata(input, endian);
        }

        private void ReadMetadata(Stream input, Endian endian)
        {
            this.Metadata = null;

            if (input.Length < 12)
            {
                return;
            }

            input.Seek(-12, SeekOrigin.End);

            var magic = input.ReadValueU32(endian);
            if (magic != _MetadataSignature)
            {
                return;
            }

            var offset = input.ReadValueU32(endian);
            var length = input.ReadValueU32(endian);

            if (input.Length < offset + length)
            {
                throw new EndOfStreamException();
            }

            input.Seek(offset, SeekOrigin.Begin);
            this.Metadata = input.ReadBytes(length);
        }

        IChunk IChunkFactory.CreateChunk(ChunkType type)
        {
            return type != ChunkType.Root ? null : new Chunks.RootChunk();
        }

        private static IChunk DeserializeBlock(IChunk parent,
                                               IChunkFactory factory,
                                               Stream input,
                                               Endian endian)
        {
            var baseOffset = input.Position;

            var type = (ChunkType)input.ReadValueU32(endian);
            var block = factory.CreateChunk(type);
            if (block == null || block.Type != type)
            {
                throw new FormatException();
            }

            var unknown04 = input.ReadValueU32(endian);
            var size = input.ReadValueU32(endian);
            var dataSize = input.ReadValueU32(endian);
            var childCount = input.ReadValueU32(endian);

            if (dataSize > size)
            {
                throw new FormatException();
            }

            var childOffset = input.Position;
            var childEnd = childOffset + (size - dataSize - 20);
            var blockOffset = childEnd;
            var blockEnd = blockOffset + dataSize;

            if (blockEnd != baseOffset + size)
            {
                throw new FormatException();
            }

            input.Seek(blockOffset, SeekOrigin.Begin);
            block.Deserialize(parent, input, endian);

            if (input.Position != blockEnd)
            {
                throw new FormatException();
            }

            input.Seek(childOffset, SeekOrigin.Begin);
            for (uint i = 0; i < childCount; i++)
            {
                block.AddChild(DeserializeBlock(block, block, input, endian));
            }

            if (input.Position != childEnd)
            {
                throw new FormatException();
            }

            input.Seek(blockEnd, SeekOrigin.Begin);
            return block;
        }
    }
}
