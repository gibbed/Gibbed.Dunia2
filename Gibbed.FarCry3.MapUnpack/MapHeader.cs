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
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Gibbed.FarCry3.MapUnpack
{
    [JsonObject(MemberSerialization.OptIn)]
    public class MapHeader
    {
        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        [JsonProperty(PropertyName = "creator")]
        public string Creator { get; set; }

        [JsonProperty(PropertyName = "author")]
        public string Author { get; set; }

        [JsonProperty(PropertyName = "map_id")]
        public MapId MapId { get; set; }

        [JsonProperty(PropertyName = "version_id")]
        public Guid VersionId { get; set; }

        [JsonProperty(PropertyName = "time_modified")]
        public DateTime TimeModified { get; set; }

        [JsonProperty(PropertyName = "time_created")]
        public DateTime TimeCreated { get; set; }

        [JsonProperty(PropertyName = "map_size")]
        [JsonConverter(typeof(StringEnumConverter))]
        public FileFormats.CustomMap.MapSize MapSize { get; set; }

        [JsonProperty(PropertyName = "player_range")]
        [JsonConverter(typeof(StringEnumConverter))]
        public FileFormats.CustomMap.PlayerRange PlayerRange { get; set; }

        [JsonProperty(PropertyName = "unknown2")]
        public float Unknown2 { get; set; }

        [JsonProperty(PropertyName = "unknown3")]
        public float Unknown3 { get; set; }

        [JsonProperty(PropertyName = "unknown4")]
        public float Unknown4 { get; set; }

        [JsonProperty(PropertyName = "unknown5")]
        public long Unknown5 { get; set; }

        [JsonProperty(PropertyName = "unknown7")]
        public long Unknown7 { get; set; }

        [JsonProperty(PropertyName = "unknown16")]
        public uint Unknown16 { get; set; }

        [JsonProperty(PropertyName = "unknown17")]
        public byte Unknown17 { get; set; }
    }
}
