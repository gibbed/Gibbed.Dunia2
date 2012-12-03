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
using ICSharpCode.SharpZipLib.Zip.Compression;
using ICSharpCode.SharpZipLib.Zip.Compression.Streams;

namespace Gibbed.Dunia2.FileFormats.Big
{
    public static class EntryDecompression
    {
        public static void Decompress(Entry entry, Stream input, Stream output)
        {
            input.Seek(entry.Offset, SeekOrigin.Begin);

            if (entry.CompressionScheme == CompressionScheme.None)
            {
                output.WriteFromStream(input, entry.CompressedSize);
            }
            else if (entry.CompressionScheme == CompressionScheme.LZO1x)
            {
                DecompressLzo(entry, input, output);
            }
            else if (entry.CompressionScheme == CompressionScheme.Zlib)
            {
                DecompressZlib(entry, input, output);
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        private static void DecompressLzo(Entry entry, Stream input, Stream output)
        {
            input.Seek(entry.Offset, SeekOrigin.Begin);

            var compressedData = new byte[entry.CompressedSize];
            if (input.Read(compressedData, 0, compressedData.Length) != compressedData.Length)
            {
                throw new EndOfStreamException();
            }

            var uncompressedData = new byte[entry.UncompressedSize];
            int actualUncompressedLength = uncompressedData.Length;

            var result = LZO.Decompress(compressedData,
                                        0,
                                        compressedData.Length,
                                        uncompressedData,
                                        0,
                                        ref actualUncompressedLength);
            if (result != LZO.ErrorCode.Success)
            {
                throw new FormatException(string.Format("LZO decompression failure ({0})", result));
            }

            if (actualUncompressedLength != uncompressedData.Length)
            {
                throw new FormatException("LZO decompression failure (uncompressed size mismatch)");
            }

            output.Write(uncompressedData, 0, uncompressedData.Length);
        }

        private static void DecompressZlib(Entry entry, Stream input, Stream output)
        {
            if (entry.CompressedSize < 16)
            {
                throw new FormatException();
            }

            var sizes = new ushort[8];
            for (int i = 0; i < 8; i++)
            {
                sizes[i] = input.ReadValueU16(Endian.Little);
            }

            var blockCount = sizes[0];
            var maximumUncompressedBlockSize = 16 * (sizes[1] + 1);

            long left = entry.UncompressedSize;
            for (int i = 0, c = 2; i < blockCount; i++, c++)
            {
                if (c == 8)
                {
                    for (int j = 0; j < 8; j++)
                    {
                        sizes[j] = input.ReadValueU16(Endian.Little);
                    }

                    c = 0;
                }

                uint compressedBlockSize = sizes[c];
                if (compressedBlockSize != 0)
                {
                    var uncompressedBlockSize = i + 1 < blockCount
                                                    ? Math.Min(maximumUncompressedBlockSize, left)
                                                    : left;
                    //var uncompressedBlockSize = Math.Min(maximumUncompressedBlockSize, left);

                    using (var temp = input.ReadToMemoryStream(compressedBlockSize))
                    {
                        var zlib = new InflaterInputStream(temp, new Inflater(true));
                        output.WriteFromStream(zlib, uncompressedBlockSize);
                        left -= uncompressedBlockSize;
                    }

                    var padding = (16 - (compressedBlockSize % 16)) % 16;
                    if (padding > 0)
                    {
                        input.Seek(padding, SeekOrigin.Current);
                    }
                }
                else
                {
                    throw new NotImplementedException();
                }
            }

            if (left > 0)
            {
                Console.WriteLine("WAT");
            }
        }
    }
}
