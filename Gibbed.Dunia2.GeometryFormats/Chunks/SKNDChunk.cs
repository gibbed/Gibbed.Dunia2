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
    public class SKNDChunk : IChunk
    {
        #region Fields
        private readonly List<UnknownData0> _Unknown0 = new List<UnknownData0>();
        private readonly List<ClusterChunk> _Unknown1 = new List<ClusterChunk>();
        #endregion

        ChunkType IChunk.Type
        {
            get { return ChunkType.SKND; }
        }

        #region Properties
        public List<UnknownData0> Unknown0
        {
            get { return this._Unknown0; }
        }

        public List<ClusterChunk> Unknown1
        {
            get { return this._Unknown1; }
        }
        #endregion

        void IChunk.Serialize(IChunk parent, Stream output, Endian endian)
        {
            throw new NotImplementedException();
        }

        void IChunk.Deserialize(IChunk parent, Stream input, Endian endian)
        {
            var count = input.ReadValueU32(endian);

            this.Unknown0.Clear();
            this.Unknown1.Clear();
            for (uint i = 0; i < count; i++)
            {
                var unknown = new UnknownData0();

                unknown.Unknown00 = input.ReadValueF32(endian);
                unknown.Unknown04 = input.ReadValueF32(endian);
                unknown.Unknown08 = input.ReadValueF32(endian);
                unknown.Unknown0C = input.ReadValueF32(endian);
                unknown.Unknown10 = input.ReadValueF32(endian);
                unknown.Unknown14 = input.ReadValueF32(endian);
                unknown.Unknown18 = input.ReadValueF32(endian);
                unknown.Unknown1C = input.ReadValueF32(endian);
                unknown.Unknown20 = input.ReadValueF32(endian);
                unknown.Unknown24 = input.ReadValueF32(endian);
                unknown.Unknown28 = input.ReadValueF32(endian);
                unknown.Unknown2C = input.ReadValueU32(endian);
                unknown.Unknown30 = input.ReadValueU32(endian);

                var length = input.ReadValueU32(endian);
                unknown.Name = input.ReadString(length);

                input.Seek(1, SeekOrigin.Current); // skip null

                this.Unknown0.Add(unknown);
            }
        }

        void IChunk.AddChild(IChunk child)
        {
            if ((child is ClusterChunk) == false)
            {
                throw new ArgumentException("child");
            }

            this.Unknown1.Add((ClusterChunk)child);
        }

        IEnumerable<IChunk> IChunk.GetChildren()
        {
            return this.Unknown1;
        }

        IChunk IChunkFactory.CreateChunk(ChunkType type)
        {
            switch (type)
            {
                case ChunkType.Cluster:
                {
                    return new ClusterChunk();
                }
            }

            throw new NotSupportedException();
        }

        public class UnknownData0
        {
            public float Unknown00;
            public float Unknown04;
            public float Unknown08;
            public float Unknown0C;
            public float Unknown10;
            public float Unknown14;
            public float Unknown18;
            public float Unknown1C;
            public float Unknown20;
            public float Unknown24;
            public float Unknown28;
            public uint Unknown2C;
            public uint Unknown30;
            public string Name;
        }
    }
}
