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
using System.Text;
using Gibbed.IO;

namespace Test
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            /*
            using (var input = File.OpenRead(@"T:\Games\Steam\steamapps\common\Far Cry 3\modding\18108\patch\__UNKNOWN\gfx\53A14FB06434D0C9.xbg"))
            {
                var grf = new Gibbed.Dunia2.GeometryFormats.GeometryResourceFile();
                grf.Deserialize(input);
            }
            */

            /*
            var bof = new Gibbed.Dunia2.FileFormats.BinaryObjectFile();
            using (var input = File.OpenRead("ige_boot.fcb"))
            {
                bof.Deserialize(input);
            }

            HandleNode(bof.Root);
            */

            var butt = ulong.Parse("7670070514511").Swap().ToString("X16");
            var a = Gibbed.Dunia2.FileFormats.CRC32.Hash("NomadObject").ToString("X8");
            var b = Gibbed.Dunia2.FileFormats.CRC32.Hash("nomadObject").ToString("X8");
            var c = Gibbed.Dunia2.FileFormats.CRC32.Hash("objectTemplate").ToString("X8");
            var d = Gibbed.Dunia2.FileFormats.CRC32.Hash("shopping_Item").ToString("X8");
        }

        private static readonly byte[] _Prefix = Encoding.UTF8.GetBytes(@"ui\");

        private static void HandleNode(Gibbed.Dunia2.FileFormats.BinaryObject node)
        {
            foreach (var kv in node.Fields)
            {
                if (kv.Value != null && kv.Value.Take(_Prefix.Length).SequenceEqual(_Prefix) == true)
                {
                    var line = Encoding.UTF8.GetString(kv.Value, 0, kv.Value.Length - 1);
                    Console.WriteLine(line);
                }
            }

            foreach (var child in node.Children)
            {
                HandleNode(child);
            }
        }
    }
}
