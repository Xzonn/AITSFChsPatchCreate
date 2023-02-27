using System.IO;

namespace AITSFChsPatchCreate
{
    public class PositionXorWriter : BinaryWriter
    {
        public long Position
        {
            get
            {
                return BaseStream.Position;
            }
            set
            {
                BaseStream.Position = value;
            }
        }
        public readonly long BasePosition;
        public readonly long StartPosition;

        public PositionXorWriter(Stream stream, long basePosition, long startPosition) : base(stream)
        {
            BasePosition = basePosition;
            StartPosition = startPosition;
        }

        public void Encrypt(byte[] buffer, int offset, int count, long pos)
        {
            for (int i = offset; i < count; i++, pos++)
            {
                if (pos >= StartPosition)
                {
                    buffer[i] ^= (byte)(pos & 0xff);
                }
            }
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            long pos = Position - BasePosition;
            Encrypt(buffer, offset, count, pos);
            BaseStream.Write(buffer, offset, count);
        }
    }
}
