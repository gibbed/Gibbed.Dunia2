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
using System.Text;
using Gibbed.IO;

namespace Gibbed.FarCry3.FileFormats
{
    internal static class Helpers
    {
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

        public static void WriteMungedGuid(Stream output, Guid value, Endian endian)
        {
            var bytes = value.ToByteArray();

            var a = BitConverter.ToUInt32(bytes, 0);
            var b = BitConverter.ToUInt16(bytes, 4);
            var c = BitConverter.ToUInt16(bytes, 6);
            var d = bytes[8];
            var e = bytes[9];
            var f = bytes[10];
            var g = bytes[11];
            var h = bytes[12];
            var i = bytes[13];
            var j = bytes[14];
            var k = bytes[15];

            ulong high = 0ul;
            high |= (((ulong)a << 32) & 0xFFFFFFFF00000000ul);
            high |= (((ulong)b << 16) & 0x00000000FFFF0000ul);
            high |= (((ulong)c << 0) & 0x000000000000FFFFul);

            ulong low = 0ul;
            low |= (((ulong)d << 56) & 0xFF00000000000000ul);
            low |= (((ulong)e << 48) & 0x00FF000000000000ul);
            low |= (((ulong)f << 40) & 0x0000FF0000000000ul);
            low |= (((ulong)g << 32) & 0x000000FF00000000ul);
            low |= (((ulong)h << 24) & 0x00000000FF000000ul);
            low |= (((ulong)i << 16) & 0x0000000000FF0000ul);
            low |= (((ulong)j << 8) & 0x000000000000FF00ul);
            low |= (((ulong)k << 0) & 0x00000000000000FFul);

            output.WriteValueU64(high, endian);
            output.WriteValueU64(low, endian);
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

        public static string ReadString(this Stream stream, Endian endian)
        {
            var length = stream.ReadValueU32(endian);
            if (length == 0)
            {
                return "";
            }

            if (length > 1024 * 1024)
            {
                throw new FormatException("somehow I doubt there is a >1MB string to be read");
            }

            var bytes = new byte[length];
            stream.Read(bytes, 0, bytes.Length);
            return Encoding.UTF8.GetString(bytes, 0, bytes.Length);
        }

        public static void WriteString(this Stream stream, string value, Endian endian)
        {
            var bytes = Encoding.UTF8.GetBytes(value ?? "");
            stream.WriteValueS32(bytes.Length, endian);
            stream.WriteBytes(bytes);
        }
    }
}
