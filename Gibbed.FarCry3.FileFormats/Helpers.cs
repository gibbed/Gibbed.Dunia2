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

namespace Gibbed.FarCry3.FileFormats
{
    internal static class Helpers
    {
        public static void WriteMungedGuid(Stream output, Guid value, Endian endian)
        {
            throw new NotImplementedException();
        }

        public static Guid ReadMungedGuid(Stream input, Endian endian)
        {
            var high = input.ReadValueU64(Endian.Little);
            var low = input.ReadValueU64(Endian.Little);

            var a = (uint)((high & 0xFFFFFFFF00000000ul) >> 32);
            var b = (ushort)((high & 0x00000000FFFF0000ul) >> 16);
            var c = (ushort)((high & 0x000000000000FFFFul) >> 0);

            var d = (byte)((low & 0xFF00000000000000ul) >> 56);
            var e = (byte)((low & 0x00FF000000000000ul) >> 48);
            var f = (byte)((low & 0x0000FF0000000000ul) >> 40);
            var g = (byte)((low & 0x000000FF00000000ul) >> 32);
            var h = (byte)((low & 0x00000000FF000000ul) >> 24);
            var i = (byte)((low & 0x0000000000FF0000ul) >> 16);
            var j = (byte)((low & 0x000000000000FF00ul) >> 8);
            var k = (byte)((low & 0x00000000000000FFul) >> 0);

            return new Guid(a, b, c, d, e, f, g, h, i, j, k);
        }

        public static void WriteTime(Stream output, CustomMap.Time time, Endian endian)
        {
            output.WriteValueS32(time.Second, endian);
            output.WriteValueS32(time.Minute, endian);
            output.WriteValueS32(time.Hour, endian);
            output.WriteValueS32(time.DayOfMonth, endian);
            output.WriteValueS32(time.Month, endian);
            output.WriteValueS32(time.Year, endian);
            output.WriteValueS32(time.DayOfWeek, endian);
            output.WriteValueS32(time.DayOfYear, endian);
            output.WriteValueS32(time.IsDaylightsSavingTime, endian);
        }

        public static CustomMap.Time ReadTime(Stream input, Endian endian)
        {
            CustomMap.Time time;
            time.Second = input.ReadValueS32(endian);
            time.Minute = input.ReadValueS32(endian);
            time.Hour = input.ReadValueS32(endian);
            time.DayOfMonth = input.ReadValueS32(endian);
            time.Month = input.ReadValueS32(endian);
            time.Year = input.ReadValueS32(endian);
            time.DayOfWeek = input.ReadValueS32(endian);
            time.DayOfYear = input.ReadValueS32(endian);
            time.IsDaylightsSavingTime = input.ReadValueS32(endian);

            if (time.IsDaylightsSavingTime != 0)
            {
                throw new FormatException();
            }

            return time;
        }
    }
}
