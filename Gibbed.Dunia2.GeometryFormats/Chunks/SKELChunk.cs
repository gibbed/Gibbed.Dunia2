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

namespace Gibbed.Dunia2.GeometryFormats.Chunks
{
    public class SKELChunk : IChunk
    {
        #region Fields
        private uint _Index;
        #endregion

        ChunkType IChunk.Type
        {
            get { return ChunkType.SKEL; }
        }

        #region Properties
        public uint Index
        {
            get { return this._Index; }
            set { this._Index = value; }
        }
        #endregion

        void IChunk.Serialize(IChunk parent, Stream output, Endian endian)
        {
            throw new NotImplementedException();
        }

        void IChunk.Deserialize(IChunk parent, Stream input, Endian endian)
        {
            this.Index = input.ReadValueU32(endian);
        }

        void IChunk.AddChild(IChunk child)
        {
            throw new NotSupportedException();
        }

        IEnumerable<IChunk> IChunk.GetChildren()
        {
            throw new NotSupportedException();
        }

        IChunk IChunkFactory.CreateChunk(ChunkType type)
        {
            throw new NotSupportedException();
        }
    }
}
