using System.IO;
using System.Text;

namespace AITSFChsPatchCreate
{
    unsafe public class BinaryWriterExtended : BinaryWriter
    {
        public BinaryWriterExtended(Stream output) : base(output) { }

        public long Position
        {
            get => BaseStream.Position;
            set => BaseStream.Position = value;
        }

        public void WriteBE(short value)
        {
            var _buffer = new byte[] {
                (byte)(value >> 8),
                (byte)value
            };
            OutStream.Write(_buffer, 0, 2);
        }

        public void WriteBE(ushort value)
        {
            var _buffer = new byte[] {
                (byte)(value >> 8),
                (byte)value
            };
            OutStream.Write(_buffer, 0, 2);
        }

        public void WriteBE(int value)
        {
            var _buffer = new byte[] {
                (byte)(value >> 24),
                (byte)(value >> 16),
                (byte)(value >> 8),
                (byte)value
            };
            OutStream.Write(_buffer, 0, 4);
        }

        public void WriteBE(uint value)
        {
            var _buffer = new byte[] {
                (byte)(value >> 24),
                (byte)(value >> 16),
                (byte)(value >> 8),
                (byte)value
            };
            OutStream.Write(_buffer, 0, 4);
        }

        public void WriteBE(long value)
        {
            var _buffer = new byte[] {
                (byte)(value >> 56),
                (byte)(value >> 48),
                (byte)(value >> 40),
                (byte)(value >> 32),
                (byte)(value >> 24),
                (byte)(value >> 16),
                (byte)(value >> 8),
                (byte)value
            };
            OutStream.Write(_buffer, 0, 8);
        }

        public void WriteBE(ulong value)
        {
            var _buffer = new byte[] {
                (byte)(value >> 56),
                (byte)(value >> 48),
                (byte)(value >> 40),
                (byte)(value >> 32),
                (byte)(value >> 24),
                (byte)(value >> 16),
                (byte)(value >> 8),
                (byte)value
            };
            OutStream.Write(_buffer, 0, 8);
        }

        public void WriteBE(float value)
        {
            uint num = *(uint*)&value;
            var _buffer = new byte[] {
                (byte)(num >> 24),
                (byte)(num >> 16),
                (byte)(num >> 8),
                (byte)num
            };
            OutStream.Write(_buffer, 0, 4);
        }

        public void WriteBE(double value)
        {
            ulong num = *(ulong*)&value;
            var _buffer = new byte[] {
                (byte)(num >> 56),
                (byte)(num >> 48),
                (byte)(num >> 40),
                (byte)(num >> 32),
                (byte)(num >> 24),
                (byte)(num >> 16),
                (byte)(num >> 8),
                (byte)num
            };
            OutStream.Write(_buffer, 0, 8);
        }

        public void WriteAlignedString(string value)
        {
            Write(value.Length);
            var bytes = Encoding.UTF8.GetBytes(value);
            OutStream.Write(bytes, 0, bytes.Length);
            AlignStream(4);
        }

        public void WriteStringToNull(string value)
        {
            var bytes = Encoding.UTF8.GetBytes(value);
            OutStream.Write(bytes, 0, bytes.Length);
            OutStream.WriteByte(0);
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
                if (BaseStream.Length >= BaseStream.Position + alignment - mod)
                {
                    BaseStream.Position += alignment - mod;
                }
                else
                {
                    BaseStream.Write(new byte[alignment - mod], 0, (int)(alignment - mod));
                }
            }
        }
    }
}
