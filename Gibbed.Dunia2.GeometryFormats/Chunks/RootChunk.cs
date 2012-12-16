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
    public class RootChunk : IChunk
    {
        #region Fields
        private MaterialReferenceChunk _MaterialReference;
        private readonly List<MaterialDescriptorChunk> _MaterialDescriptors = new List<MaterialDescriptorChunk>();
        #endregion

        ChunkType IChunk.Type
        {
            get { return ChunkType.Root; }
        }

        void IChunk.Serialize(IChunk parent, Stream output, Endian endian)
        {
        }

        void IChunk.Deserialize(IChunk parent, Stream input, Endian endian)
        {
        }

        private static void SetChild<TType>(IChunk child, ref TType value)
            where TType : class, IChunk
        {
            if (child is TType)
            {
                if (value != null)
                {
                    throw new InvalidOperationException();
                }

                value = (TType)child;
            }
        }

        void IChunk.AddChild(IChunk child)
        {
            SetChild(child, ref this._MaterialReference);
            /*
            SetChild(child, ref this.Nodes);
            SetChild(child, ref this.O2BM);
            SetChild(child, ref this.SKID);
            SetChild(child, ref this.SKND);
            SetChild(child, ref this.LODs);
            SetChild(child, ref this.BoundingBox);
            SetChild(child, ref this.BSPH);
            SetChild(child, ref this.LOD);
            SetChild(child, ref this.PCMP);
            SetChild(child, ref this.UCMP);
            SetChild(child, ref this.IKDA);
            */

            var materialDescriptor = child as MaterialDescriptorChunk;
            if (materialDescriptor != null)
            {
                this._MaterialDescriptors.Add(materialDescriptor);
            }
        }

        private static void GetChild(List<IChunk> blocks, IChunk value)
        {
            if (value != null)
            {
                blocks.Add(value);
            }
        }

        IEnumerable<IChunk> IChunk.GetChildren()
        {
            var children = new List<IChunk>();
            GetChild(children, this._MaterialReference);
            /*
            GetChild(children, this.Nodes);
            GetChild(children, this.O2BM);
            GetChild(children, this.SKID);
            GetChild(children, this.SKND);
            GetChild(children, this.LODs);
            GetChild(children, this.BoundingBox);
            GetChild(children, this.BSPH);
            GetChild(children, this.LOD);
            GetChild(children, this.PCMP);
            GetChild(children, this.UCMP);
            GetChild(children, this.IKDA);
            */
            children.AddRange(this._MaterialDescriptors);
            return children;
        }

        IChunk IChunkFactory.CreateChunk(ChunkType type)
        {
            switch (type)
            {
                case ChunkType.MaterialReference:
                {
                    return new MaterialReferenceChunk();
                }

                case ChunkType.SKEL:
                {
                    return new SKELChunk();
                }

                case ChunkType.Nodes:
                {
                    return new NodesChunk();
                }

                case ChunkType.SKID:
                {
                    return new SKIDChunk();
                }

                case ChunkType.SKND:
                {
                    return new SKNDChunk();
                }

                case ChunkType.LevelOfDetails:
                {
                    return new LevelOfDetailsChunk();
                }

                case ChunkType.BoundingBox:
                {
                    return new BoundingBoxChunk();
                }

                case ChunkType.BoundingSphere:
                {
                    return new BoundingSphereChunk();
                }

                case ChunkType.LevelOfDetailInfo:
                {
                    return new LevelOfDetailInfoChunk();
                }

                case ChunkType.PCMP:
                {
                    return new PCMPChunk();
                }

                case ChunkType.UCMP:
                {
                    return new UCMPChunk();
                }

                /*case ChunkType.MaterialDescriptor:
                {
                    return new MaterialDescriptorChunk();
                }*/
            }

            throw new NotSupportedException();
        }
    }
}
