using System.IO;

namespace AITSFChsPatchCreate
{
    public class PositionXorReader : BinaryReader
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

        public PositionXorReader(Stream stream, long basePosition, long startPosition) : base(stream)
        {
            BasePosition = basePosition;
            StartPosition = startPosition;
        }

        public void Decrypt(byte[] buffer, int offset, int count, long pos)
        {
            for (int i = offset; i < count; i++, pos++)
            {
                if (pos >= StartPosition)
                {
                    buffer[i] ^= (byte)(pos & 0xff);
                }
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            long pos = Position - BasePosition;
            int read = BaseStream.Read(buffer, offset, count);
            Decrypt(buffer, offset, count, pos);
            return read;
        }
    }
}
