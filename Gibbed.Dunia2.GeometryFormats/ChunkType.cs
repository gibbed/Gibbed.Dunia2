using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Gibbed.Dunia2.GeometryFormats
{
    internal enum ChunkType : uint
    {
        // ReSharper disable InconsistentNaming
        Root = 0x00000000,

        MaterialReference = 0x524D544C, // 'RMTL'
        SKEL = 0x534B454C, // 'SKEL'
        Nodes = 0x4E4F4445, // 'NODE'
        SKID = 0x534B4944, // 'SKID'
        SKND = 0x534B4E44, // 'SKND'
        Cluster = 0x434C5553, // 'CLUS'
        LevelOfDetails = 0x04C4F4453, // 'LODS'
        BoundingBox = 0x42424F58, // 'BBOX'
        BoundingSphere = 0x42535048, // 'BSPH'
        LevelOfDetailInfo = 0x004C4F44, // 'LOD\0'
        PCMP = 0x50434D50,
        UCMP = 0x55434D50,
        MaterialDescriptor = 0x444D544C, // 'DMTL'

        //O2BM = 0x4F32424D,
        //IKDA = 0x494B4441,
        // ReSharper restore InconsistentNaming
    }
}
