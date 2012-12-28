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

        O2BM = 0x4F32424D, // 'O2BM'
        IKDA = 0x494B4441, // 'IKDA'

        // ReSharper restore InconsistentNaming
    }
}
