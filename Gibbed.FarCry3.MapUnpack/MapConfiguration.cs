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

using Newtonsoft.Json;

namespace Gibbed.FarCry3.MapUnpack
{
    [JsonObject(MemberSerialization.OptIn)]
    public class MapConfiguration
    {
        public MapConfiguration()
        {
            this.Header = new MapHeader();
        }

        [JsonProperty(PropertyName = "header")]
        public MapHeader Header { get; set; }

        [JsonProperty(PropertyName = "snapshot_path")]
        public string SnapshotPath { get; set; }

        [JsonProperty(PropertyName = "extra_snapshot_path")]
        public string ExtraSnapshotPath { get; set; }

        [JsonProperty(PropertyName = "data")]
        public MapData Data { get; set; }
    }
}
