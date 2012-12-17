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
using System.Linq;
using System.Text;
using System.Xml;
using Gibbed.Dunia2.FileFormats;
using Gibbed.Dunia2.BinaryObjectInfo;
using Gibbed.Dunia2.BinaryObjectInfo.Definitions;

namespace Gibbed.Dunia2.BinaryObjectInfo
{
    public static class FieldTypeDeserializers
    {
        public static object Deserialize(FieldType fieldType, byte[] data)
        {
            switch (fieldType)
            {
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

                    return data[0] != 0;
                }

                case FieldType.UInt8:
                {
                    if (data == null ||
                        data.Length != 1)
                    {
                        throw new FormatException("field type UInt8 requires 1 byte");
                    }

                    return data[0];
                }

                case FieldType.Int8:
                {
                    if (data == null ||
                        data.Length != 1)
                    {
                        throw new FormatException("field type Int8 requires 1 byte");
                    }

                    return (sbyte)data[0];
                }

                case FieldType.UInt16:
                {
                    if (data == null ||
                        data.Length != 2)
                    {
                        throw new FormatException("field type UInt16 requires 2 bytes");
                    }

                    return BitConverter.ToUInt16(data, 0);
                }

                case FieldType.Int16:
                {
                    if (data == null ||
                        data.Length != 2)
                    {
                        throw new FormatException("field type Int16 requires 2 bytes");
                    }

                    return BitConverter.ToInt16(data, 0);
                }

                case FieldType.UInt32:
                {
                    if (data == null ||
                        data.Length != 4)
                    {
                        throw new FormatException("field type UInt32 requires 4 bytes");
                    }

                    return BitConverter.ToUInt32(data, 0);
                }

                case FieldType.Int32:
                {
                    if (data == null ||
                        data.Length != 4)
                    {
                        throw new FormatException("field type Int32 requires 4 bytes");
                    }

                    return BitConverter.ToInt32(data, 0);
                }

                case FieldType.UInt64:
                {
                    if (data == null ||
                        data.Length != 8)
                    {
                        throw new FormatException("field type UInt64 requires 8 bytes");
                    }

                    return BitConverter.ToUInt64(data, 0);
                }

                case FieldType.Int64:
                {
                    if (data == null ||
                        data.Length != 8)
                    {
                        throw new FormatException("field type Int64 requires 8 bytes");
                    }

                    return BitConverter.ToInt64(data, 0);
                }

                case FieldType.Float32:
                {
                    if (data == null ||
                        data.Length != 4)
                    {
                        throw new FormatException("field type Float32 requires 4 bytes");
                    }

                    return BitConverter.ToSingle(data, 0);
                }

                case FieldType.Float64:
                {
                    if (data == null ||
                        data.Length != 8)
                    {
                        throw new FormatException("field type Float64 requires 8 bytes");
                    }

                    return BitConverter.ToDouble(data, 0);
                }

                case FieldType.Vector2:
                {
                    if (data == null ||
                        data.Length != 8)
                    {
                        throw new FormatException("field type Vector2 requires 8 bytes");
                    }

                    return new Vector2
                    {
                        X = BitConverter.ToSingle(data, 0),
                        Y = BitConverter.ToSingle(data, 4),
                    };
                }

                case FieldType.Vector3:
                {
                    if (data == null ||
                        data.Length != 12)
                    {
                        throw new FormatException("field type Vector3 requires 12 bytes");
                    }

                    return new Vector3
                    {
                        X = BitConverter.ToSingle(data, 0),
                        Y = BitConverter.ToSingle(data, 4),
                        Z = BitConverter.ToSingle(data, 8),
                    };
                }

                case FieldType.Vector4:
                {
                    if (data == null ||
                        data.Length != 16)
                    {
                        throw new FormatException("field type Vector4 requires 16 bytes");
                    }

                    return new Vector4
                    {
                        X = BitConverter.ToSingle(data, 0),
                        Y = BitConverter.ToSingle(data, 4),
                        Z = BitConverter.ToSingle(data, 8),
                        W = BitConverter.ToSingle(data, 12),
                    };
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

                    return Encoding.UTF8.GetString(data, 0, data.Length - 1);
                }

                case FieldType.Enum:
                {
                    if (data == null ||
                        data.Length != 4)
                    {
                        throw new FormatException("field type Enum requires 4 bytes");
                    }

                    return BitConverter.ToInt32(data, 0);
                }

                case FieldType.Hash32:
                {
                    if (data == null ||
                        data.Length != 4)
                    {
                        throw new FormatException("field type Hash32 requires 4 bytes");
                    }

                    return BitConverter.ToUInt32(data, 0);
                }

                case FieldType.Hash64:
                {
                    if (data == null ||
                        data.Length != 8)
                    {
                        throw new FormatException("field type Hash64 requires 8 bytes");
                    }

                    return BitConverter.ToUInt64(data, 0);
                }

                case FieldType.Id32:
                {
                    if (data == null ||
                        data.Length != 4)
                    {
                        throw new FormatException("field type Id32 requires 4 bytes");
                    }

                    return BitConverter.ToUInt32(data, 0);
                }

                case FieldType.Id64:
                {
                    if (data == null ||
                        data.Length != 8)
                    {
                        throw new FormatException("field type Id64 requires 8 bytes");
                    }

                    return BitConverter.ToUInt64(data, 0);
                }

                default:
                {
                    throw new NotSupportedException("unsupported field type");
                }
            }
        }

        public static TType Deserialize<TType>(FieldType fieldType, byte[] data)
        {
            return (TType)Deserialize(fieldType, data);
        }

        public static void Deserialize(XmlWriter writer,
                                       FieldDefinition fieldDef,
                                       byte[] data)
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
                    var value = Deserialize<bool>(fieldDef.Type, data);
                    writer.WriteString(value.ToString(CultureInfo.InvariantCulture));
                    break;
                }

                case FieldType.UInt8:
                {
                    var value = Deserialize<byte>(fieldDef.Type, data);
                    writer.WriteString(value.ToString(CultureInfo.InvariantCulture));
                    break;
                }

                case FieldType.Int8:
                {
                    var value = Deserialize<sbyte>(fieldDef.Type, data);
                    writer.WriteString(value.ToString(CultureInfo.InvariantCulture));
                    break;
                }

                case FieldType.UInt16:
                {
                    var value = Deserialize<ushort>(fieldDef.Type, data);
                    writer.WriteString(value.ToString(CultureInfo.InvariantCulture));
                    break;
                }

                case FieldType.Int16:
                {
                    var value = Deserialize<short>(fieldDef.Type, data);
                    writer.WriteString(value.ToString(CultureInfo.InvariantCulture));
                    break;
                }

                case FieldType.UInt32:
                {
                    var value = Deserialize<uint>(fieldDef.Type, data);
                    writer.WriteString(value.ToString(CultureInfo.InvariantCulture));
                    break;
                }

                case FieldType.Int32:
                {
                    var value = Deserialize<int>(fieldDef.Type, data);
                    writer.WriteString(value.ToString(CultureInfo.InvariantCulture));
                    break;
                }

                case FieldType.UInt64:
                {
                    var value = Deserialize<ulong>(fieldDef.Type, data);
                    writer.WriteString(value.ToString(CultureInfo.InvariantCulture));
                    break;
                }

                case FieldType.Int64:
                {
                    var value = Deserialize<long>(fieldDef.Type, data);
                    writer.WriteString(value.ToString(CultureInfo.InvariantCulture));
                    break;
                }

                case FieldType.Float32:
                {
                    var value = Deserialize<float>(fieldDef.Type, data);
                    writer.WriteString(value.ToString(CultureInfo.InvariantCulture));
                    break;
                }

                case FieldType.Float64:
                {
                    var value = Deserialize<double>(fieldDef.Type, data);
                    writer.WriteString(value.ToString(CultureInfo.InvariantCulture));
                    break;
                }

                case FieldType.Vector2:
                {
                    var value = Deserialize<Vector2>(fieldDef.Type, data);
                    writer.WriteString(string.Format("{0},{1}",
                                                     value.X.ToString(CultureInfo.InvariantCulture),
                                                     value.Y.ToString(CultureInfo.InvariantCulture)));
                    break;
                }

                case FieldType.Vector3:
                {
                    var value = Deserialize<Vector3>(fieldDef.Type, data);
                    writer.WriteString(string.Format("{0},{1},{2}",
                                                     value.X.ToString(CultureInfo.InvariantCulture),
                                                     value.Y.ToString(CultureInfo.InvariantCulture),
                                                     value.Z.ToString(CultureInfo.InvariantCulture)));
                    break;
                }

                case FieldType.Vector4:
                {
                    var value = Deserialize<Vector4>(fieldDef.Type, data);
                    writer.WriteString(string.Format("{0},{1},{2},{3}",
                                                     value.X.ToString(CultureInfo.InvariantCulture),
                                                     value.Y.ToString(CultureInfo.InvariantCulture),
                                                     value.Z.ToString(CultureInfo.InvariantCulture),
                                                     value.W.ToString(CultureInfo.InvariantCulture)));
                    break;
                }

                case FieldType.String:
                {
                    var value = Deserialize<string>(fieldDef.Type, data);
                    writer.WriteString(value);
                    break;
                }

                case FieldType.Enum:
                {
                    var value = Deserialize<int>(fieldDef.Type, data);

                    if (fieldDef.Enum != null)
                    {
                        var enumDef = fieldDef.Enum.Elements.FirstOrDefault(ed => ed.Value == value);
                        if (enumDef != null)
                        {
                            writer.WriteString(enumDef.Name);
                            break;
                        }
                    }

                    writer.WriteString(value.ToString(CultureInfo.InvariantCulture));
                    break;
                }

                case FieldType.Hash32:
                {
                    var value = Deserialize<uint>(fieldDef.Type, data);
                    writer.WriteString(value.ToString("X8", CultureInfo.InvariantCulture));
                    break;
                }

                case FieldType.Hash64:
                {
                    var value = Deserialize<ulong>(fieldDef.Type, data);
                    writer.WriteString(value.ToString("X16", CultureInfo.InvariantCulture));
                    break;
                }

                case FieldType.Id32:
                {
                    var value = Deserialize<uint>(fieldDef.Type, data);
                    writer.WriteString(value.ToString(CultureInfo.InvariantCulture));
                    break;
                }

                case FieldType.Id64:
                {
                    var value = Deserialize<ulong>(fieldDef.Type, data);
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
