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

namespace Gibbed.Dunia2.FileFormats
{
    public class BinaryObject
    {
        public long Position;
        public uint TypeHash;
        public readonly Dictionary<uint, byte[]> Values = new Dictionary<uint, byte[]>();
        public readonly List<BinaryObject> Children = new List<BinaryObject>();

        public static BinaryObject Deserialize(Stream input, List<BinaryObject> pointers, Endian endian)
        {
            long position = input.Position;

            bool isOffset;
            var childCount = input.ReadCount(out isOffset, endian);

            if (isOffset == true)
            {
                return pointers[(int)childCount];
            }

            var child = new BinaryObject();
            child.Position = position;
            pointers.Add(child);

            child.Deserialize(input, childCount, pointers, endian);
            return child;
        }

        private void Deserialize(Stream input,
                                 uint childCount,
                                 List<BinaryObject> pointers,
                                 Endian endian)
        {
            long position;
            bool isOffset;

            this.TypeHash = input.ReadValueU32(endian);

            var valueCount = input.ReadCount(out isOffset, endian);
            if (isOffset == true)
            {
                throw new NotImplementedException();
            }

            this.Values.Clear();
            for (var i = 0; i < valueCount; i++)
            {
                var nameHash = input.ReadValueU32(endian);
                byte[] value;

                uint size;
                position = input.Position;

                size = input.ReadCount(out isOffset, endian);
                if (isOffset == true)
                {
                    input.Seek(position - size, SeekOrigin.Begin);

                    size = input.ReadCount(out isOffset, endian);
                    if (isOffset == true)
                    {
                        throw new FormatException();
                    }

                    value = new byte[size];
                    input.Read(value, 0, value.Length);

                    input.Seek(position, SeekOrigin.Begin);
                    input.ReadCount(out isOffset, endian);
                }
                else
                {
                    value = new byte[size];
                    input.Read(value, 0, value.Length);
                }

                this.Values.Add(nameHash, value);
            }

            this.Children.Clear();
            for (var i = 0; i < childCount; i++)
            {
                this.Children.Add(Deserialize(input, pointers, endian));
            }
        }

        public void Serialize(Stream output,
                              ref uint totalObjectCount,
                              ref uint totalValueCount,
                              Endian endian)
        {
            totalObjectCount += (uint)this.Children.Count;
            totalValueCount += (uint)this.Values.Count;

            output.WriteCount(this.Children.Count, false, endian);

            output.WriteValueU32(this.TypeHash, endian);

            output.WriteCount(this.Values.Count, false, endian);
            foreach (var kv in this.Values)
            {
                output.WriteValueU32(kv.Key, endian);
                output.WriteCount(kv.Value.Length, false, endian);
                output.Write(kv.Value, 0, kv.Value.Length);
            }

            foreach (var child in this.Children)
            {
                child.Serialize(output,
                                ref totalObjectCount,
                                ref totalValueCount,
                                endian);
            }
        }
    }
}
