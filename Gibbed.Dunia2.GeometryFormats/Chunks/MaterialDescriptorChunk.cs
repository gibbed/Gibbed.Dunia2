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

namespace Gibbed.Dunia2.GeometryFormats.Chunks
{
    // Refer to common\engine\providerdescriptors.xml
    public class MaterialDescriptorChunk : IChunk
    {
        #region Fields
        private string _Name;
        private string _Unknown1;
        private string _Unknown2;
        private readonly Dictionary<string, string> _TextureProperties = new Dictionary<string, string>();
        private readonly Dictionary<string, float> _Float1Properties = new Dictionary<string, float>();
        private readonly Dictionary<string, Float2> _Float2Properties = new Dictionary<string, Float2>();
        private readonly Dictionary<string, Float3> _Float3Properties = new Dictionary<string, Float3>();
        private readonly Dictionary<string, Float4> _Float4Properties = new Dictionary<string, Float4>();
        private readonly Dictionary<string, int> _IntProperties = new Dictionary<string, int>();
        private readonly Dictionary<string, bool> _BoolProperties = new Dictionary<string, bool>();
        #endregion

        ChunkType IChunk.Type
        {
            get { return ChunkType.MaterialDescriptor; }
        }

        #region Properties
        public string Name
        {
            get { return this._Name; }
            set { this._Name = value; }
        }

        public string Unknown1
        {
            get { return this._Unknown1; }
            set { this._Unknown1 = value; }
        }

        public string Unknown2
        {
            get { return this._Unknown2; }
            set { this._Unknown2 = value; }
        }

        public Dictionary<string, string> TextureProperties
        {
            get { return this._TextureProperties; }
        }

        public Dictionary<string, float> Float1Properties
        {
            get { return this._Float1Properties; }
        }

        public Dictionary<string, Float2> Float2Properties
        {
            get { return this._Float2Properties; }
        }

        public Dictionary<string, Float3> Float3Properties
        {
            get { return this._Float3Properties; }
        }

        public Dictionary<string, Float4> Float4Properties
        {
            get { return this._Float4Properties; }
        }

        public Dictionary<string, int> IntProperties
        {
            get { return this._IntProperties; }
        }

        public Dictionary<string, bool> BoolProperties
        {
            get { return this._BoolProperties; }
        }
        #endregion

        void IChunk.Serialize(IChunk parent, Stream output, Endian endian)
        {
            throw new NotImplementedException();
        }

        void IChunk.Deserialize(IChunk parent, Stream input, Endian endian)
        {
            uint length;
            int count;

            length = input.ReadValueU32(endian);
            this._Name = input.ReadString(length);
            input.Seek(1, SeekOrigin.Current); // skip null

            length = input.ReadValueU32(endian);
            this.Unknown1 = input.ReadString(length);
            input.Seek(1, SeekOrigin.Current); // skip null

            length = input.ReadValueU32(endian);
            this.Unknown2 = input.ReadString(length);
            input.Seek(1, SeekOrigin.Current); // skip null

            this.TextureProperties.Clear();
            count = input.ReadValueS32(endian);
            for (int i = 0; i < count; i++)
            {
                length = input.ReadValueU32(endian);
                var value = input.ReadString(length);
                input.Seek(1, SeekOrigin.Current); // skip null

                length = input.ReadValueU32(endian);
                var key = input.ReadString(length);
                input.Seek(1, SeekOrigin.Current); // skip null

                this.TextureProperties[key] = value;
            }

            this.Float1Properties.Clear();
            count = input.ReadValueS32(endian);
            for (int i = 0; i < count; i++)
            {
                length = input.ReadValueU32(endian);
                var key = input.ReadString(length);
                input.Seek(1, SeekOrigin.Current); // skip null

                this.Float1Properties[key] = input.ReadValueF32(endian);
            }

            this.Float2Properties.Clear();
            count = input.ReadValueS32(endian);
            for (int i = 0; i < count; i++)
            {
                length = input.ReadValueU32(endian);
                var key = input.ReadString(length);
                input.Seek(1, SeekOrigin.Current); // skip null

                var value = new Float2();
                value.X = input.ReadValueF32(endian);
                value.Y = input.ReadValueF32(endian);

                this.Float2Properties[key] = value;
            }

            this.Float3Properties.Clear();
            count = input.ReadValueS32(endian);
            for (int i = 0; i < count; i++)
            {
                length = input.ReadValueU32(endian);
                var key = input.ReadString(length);
                input.Seek(1, SeekOrigin.Current); // skip null

                var value = new Float3();
                value.X = input.ReadValueF32(endian);
                value.Y = input.ReadValueF32(endian);
                value.Z = input.ReadValueF32(endian);

                this.Float3Properties[key] = value;
            }

            this.Float4Properties.Clear();
            count = input.ReadValueS32(endian);
            for (int i = 0; i < count; i++)
            {
                length = input.ReadValueU32(endian);
                var key = input.ReadString(length);
                input.Seek(1, SeekOrigin.Current); // skip null

                var value = new Float4();
                value.X = input.ReadValueF32(endian);
                value.Y = input.ReadValueF32(endian);
                value.Z = input.ReadValueF32(endian);
                value.W = input.ReadValueF32(endian);

                this.Float4Properties[key] = value;
            }

            this.IntProperties.Clear();
            count = input.ReadValueS32(endian);
            for (int i = 0; i < count; i++)
            {
                length = input.ReadValueU32(endian);
                var key = input.ReadString(length);
                input.Seek(1, SeekOrigin.Current); // skip null

                this.IntProperties[key] = input.ReadValueS32(endian);
            }

            this.BoolProperties.Clear();
            count = input.ReadValueS32(endian);
            for (int i = 0; i < count; i++)
            {
                length = input.ReadValueU32(endian);
                var key = input.ReadString(length);
                input.Seek(1, SeekOrigin.Current); // skip null

                this.BoolProperties[key] = input.ReadValueB8();
            }
        }

        void IChunk.AddChild(IChunk child)
        {
            throw new NotSupportedException();
        }

        IEnumerable<IChunk> IChunk.GetChildren()
        {
            throw new NotSupportedException();
        }

        IChunk IChunkFactory.CreateChunk(ChunkType type)
        {
            throw new NotSupportedException();
        }
    }
}
