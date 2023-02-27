using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace AITSFChsPatchCreate
{
    public class BinaryReaderExtended : BinaryReader
    {
        public BinaryReaderExtended(Stream input) : base(input) { }

        public long Position
        {
            get => BaseStream.Position;
            set => BaseStream.Position = value;
        }

        public short ReadInt16BE()
        {
            var data = ReadBytes(2);
            Array.Reverse(data);
            return BitConverter.ToInt16(data, 0);
        }

        public int ReadInt32BE()
        {
            var data = ReadBytes(4);
            Array.Reverse(data);
            return BitConverter.ToInt32(data, 0);
        }

        public long ReadInt64BE()
        {
            var data = ReadBytes(8);
            Array.Reverse(data);
            return BitConverter.ToInt64(data, 0);
        }

        public ushort ReadUInt16BE()
        {
            var data = ReadBytes(2);
            Array.Reverse(data);
            return BitConverter.ToUInt16(data, 0);
        }

        public uint ReadUInt32BE()
        {
            var data = ReadBytes(4);
            Array.Reverse(data);
            return BitConverter.ToUInt32(data, 0);
        }

        public ulong ReadUInt64BE()
        {
            var data = ReadBytes(8);
            Array.Reverse(data);
            return BitConverter.ToUInt64(data, 0);
        }

        public float ReadSingleBE()
        {
            var data = ReadBytes(4);
            Array.Reverse(data);
            return BitConverter.ToSingle(data, 0);
        }

        public double ReadDoubleBE()
        {
            var data = ReadBytes(8);
            Array.Reverse(data);
            return BitConverter.ToDouble(data, 0);
        }

        public string ReadAlignedString()
        {
            var length = ReadInt32();
            if (length > 0 && length <= BaseStream.Length - BaseStream.Position)
            {
                var stringData = ReadBytes(length);
                var result = Encoding.UTF8.GetString(stringData);
                AlignStream(4);
                return result;
            }
            return "";
        }

        public string ReadStringToNull(int maxLength = 32767)
        {
            var bytes = new List<byte>();
            int count = 0;
            while (BaseStream.Position != BaseStream.Length && count < maxLength)
            {
                var b = ReadByte();
                if (b == 0)
                {
                    break;
                }
                bytes.Add(b);
                count++;
            }
            return Encoding.UTF8.GetString(bytes.ToArray());
        }

        public void AlignStream()
        {
            AlignStream(4);
        }

        public void AlignStream(int alignment)
        {
            var pos = BaseStream.Position;
            var mod = pos % alignment;
            if (mod != 0)
            {
                BaseStream.Position += alignment - mod;
            }
        }
    }
}
