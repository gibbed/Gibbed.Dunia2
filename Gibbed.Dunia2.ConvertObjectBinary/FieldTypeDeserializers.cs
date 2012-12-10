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
using System.Globalization;
using System.IO;
using System.Text;
using System.Xml;
using Gibbed.Dunia2.FileFormats;

namespace Gibbed.Dunia2.ConvertObjectBinary
{
    internal static class FieldTypeDeserializers
    {
        public static void Deserialize(XmlWriter writer, Configuration.FieldDefinition fieldDef, byte[] data)
        {
            switch (fieldDef.Type)
            {
                case FieldType.BinHex:
                {
                    writer.WriteBinHex(data, 0, data.Length);
                    break;
                }

                case FieldType.Boolean:
                {
                    if (data == null ||
                        data.Length != 1)
                    {
                        throw new FormatException("field type Boolean requires 1 byte");
                    }

                    if (data[0] != 0 &&
                        data[0] != 1)
                    {
                        throw new FormatException("invalid value for field type Boolean");
                    }

                    var value = data[0] != 0;
                    writer.WriteString(value.ToString(CultureInfo.InvariantCulture));
                    break;
                }

                case FieldType.UInt8:
                {
                    if (data == null ||
                        data.Length != 1)
                    {
                        throw new FormatException("field type UInt8 requires 1 byte");
                    }

                    var value = data[0];
                    writer.WriteString(value.ToString(CultureInfo.InvariantCulture));
                    break;
                }

                case FieldType.Int8:
                {
                    if (data == null ||
                        data.Length != 1)
                    {
                        throw new FormatException("field type Int8 requires 1 byte");
                    }

                    var value = (sbyte)data[0];
                    writer.WriteString(value.ToString(CultureInfo.InvariantCulture));
                    break;
                }

                case FieldType.UInt16:
                {
                    if (data == null ||
                        data.Length != 2)
                    {
                        throw new FormatException("field type UInt16 requires 2 bytes");
                    }

                    var value = BitConverter.ToUInt16(data, 0);
                    writer.WriteString(value.ToString(CultureInfo.InvariantCulture));
                    break;
                }

                case FieldType.Int16:
                {
                    if (data == null ||
                        data.Length != 2)
                    {
                        throw new FormatException("field type Int16 requires 2 bytes");
                    }

                    var value = BitConverter.ToInt16(data, 0);
                    writer.WriteString(value.ToString(CultureInfo.InvariantCulture));
                    break;
                }

                case FieldType.UInt32:
                {
                    if (data == null ||
                        data.Length != 4)
                    {
                        throw new FormatException("field type UInt32 requires 4 bytes");
                    }

                    var value = BitConverter.ToUInt32(data, 0);
                    writer.WriteString(value.ToString(CultureInfo.InvariantCulture));
                    break;
                }

                case FieldType.Int32:
                {
                    if (data == null ||
                        data.Length != 4)
                    {
                        throw new FormatException("field type Int32 requires 4 bytes");
                    }

                    var value = BitConverter.ToInt32(data, 0);
                    writer.WriteString(value.ToString(CultureInfo.InvariantCulture));
                    break;
                }

                case FieldType.UInt64:
                {
                    if (data == null ||
                        data.Length != 8)
                    {
                        throw new FormatException("field type UInt64 requires 8 bytes");
                    }

                    var value = BitConverter.ToUInt64(data, 0);
                    writer.WriteString(value.ToString(CultureInfo.InvariantCulture));
                    break;
                }

                case FieldType.Int64:
                {
                    if (data == null ||
                        data.Length != 8)
                    {
                        throw new FormatException("field type Int64 requires 8 bytes");
                    }

                    var value = BitConverter.ToInt64(data, 0);
                    writer.WriteString(value.ToString(CultureInfo.InvariantCulture));
                    break;
                }

                case FieldType.Float32:
                {
                    if (data == null ||
                        data.Length != 4)
                    {
                        throw new FormatException("field type Float32 requires 4 bytes");
                    }

                    var value = BitConverter.ToSingle(data, 0);
                    writer.WriteString(value.ToString(CultureInfo.InvariantCulture));
                    break;
                }

                case FieldType.Float64:
                {
                    if (data == null ||
                        data.Length != 8)
                    {
                        throw new FormatException("field type Float64 requires 8 bytes");
                    }

                    var value = BitConverter.ToDouble(data, 0);
                    writer.WriteString(value.ToString(CultureInfo.InvariantCulture));
                    break;
                }

                case FieldType.Vector2:
                {
                    if (data == null ||
                        data.Length != 8)
                    {
                        throw new FormatException("field type Vector2 requires 8 bytes");
                    }

                    var x = BitConverter.ToSingle(data, 0);
                    var y = BitConverter.ToSingle(data, 4);

                    writer.WriteString(string.Format("{0},{1}",
                                                     x.ToString(CultureInfo.InvariantCulture),
                                                     y.ToString(CultureInfo.InvariantCulture)));
                    break;
                }

                case FieldType.Vector3:
                {
                    if (data == null ||
                        data.Length != 12)
                    {
                        throw new FormatException("field type Vector3 requires 12 bytes");
                    }

                    var x = BitConverter.ToSingle(data, 0);
                    var y = BitConverter.ToSingle(data, 4);
                    var z = BitConverter.ToSingle(data, 8);

                    writer.WriteString(string.Format("{0},{1},{2}",
                                                     x.ToString(CultureInfo.InvariantCulture),
                                                     y.ToString(CultureInfo.InvariantCulture),
                                                     z.ToString(CultureInfo.InvariantCulture)));
                    break;
                }

                case FieldType.Vector4:
                {
                    if (data == null ||
                        data.Length != 16)
                    {
                        throw new FormatException("field type Vector4 requires 16 bytes");
                    }

                    var x = BitConverter.ToSingle(data, 0);
                    var y = BitConverter.ToSingle(data, 4);
                    var z = BitConverter.ToSingle(data, 8);
                    var w = BitConverter.ToSingle(data, 12);

                    writer.WriteString(string.Format("{0},{1},{2},{3}",
                                                     x.ToString(CultureInfo.InvariantCulture),
                                                     y.ToString(CultureInfo.InvariantCulture),
                                                     z.ToString(CultureInfo.InvariantCulture),
                                                     w.ToString(CultureInfo.InvariantCulture)));
                    break;
                }

                case FieldType.String:
                {
                    if (data == null ||
                        data.Length < 1)
                    {
                        throw new FormatException("field type String requires at least 1 byte");
                    }

                    if (data[data.Length - 1] != 0)
                    {
                        throw new FormatException("invalid trailing byte value for field type String");
                    }

                    var value = Encoding.UTF8.GetString(data, 0, data.Length - 1);
                    writer.WriteString(value);
                    break;
                }

                case FieldType.Hash32:
                {
                    if (data == null ||
                        data.Length != 4)
                    {
                        throw new FormatException("field type Hash32 requires 4 bytes");
                    }

                    var value = BitConverter.ToUInt32(data, 0);
                    writer.WriteString(value.ToString("X8", CultureInfo.InvariantCulture));
                    break;
                }

                case FieldType.Hash64:
                {
                    if (data == null ||
                        data.Length != 8)
                    {
                        throw new FormatException("field type Hash64 requires 4 bytes");
                    }

                    var value = BitConverter.ToUInt64(data, 0);
                    writer.WriteString(value.ToString("X16", CultureInfo.InvariantCulture));
                    break;
                }

                case FieldType.Id32:
                {
                    if (data == null ||
                        data.Length != 4)
                    {
                        throw new FormatException("field type Id32 requires 4 bytes");
                    }

                    var value = BitConverter.ToUInt32(data, 0);
                    writer.WriteString(value.ToString(CultureInfo.InvariantCulture));
                    break;
                }

                case FieldType.Id64:
                {
                    if (data == null ||
                        data.Length != 8)
                    {
                        throw new FormatException("field type Id64 requires 8 bytes");
                    }

                    var value = BitConverter.ToUInt64(data, 0);
                    writer.WriteString(value.ToString(CultureInfo.InvariantCulture));
                    break;
                }

                case FieldType.Rml:
                {
                    if (data == null ||
                        data.Length < 5)
                    {
                        throw new FormatException("field type Rml requires at least 5 bytes");
                    }

                    var rez = new XmlResourceFile();
                    using (var input = new MemoryStream(data))
                    {
                        rez.Deserialize(input);
                    }

                    writer.WriteStartElement("rml");
                    ConvertXml.Program.WriteNode(writer, rez.Root);
                    writer.WriteEndElement();
                    break;
                }

                default:
                {
                    throw new NotSupportedException("unsupported field type");
                }
            }
        }
    }
}
