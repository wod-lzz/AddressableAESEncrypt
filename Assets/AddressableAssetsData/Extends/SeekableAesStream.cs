using System;
using System.IO;


namespace UnityEngine.ResourceManagement.ResourceProviders
{
    public class SeekableAesStream : Stream
    {
        public static readonly byte[] AesKey = new byte[32]
        {
            0x10, 0x31, 0xFF, 0xAD,
            0xE4, 0xF1, 0xF2, 0xA7,
            0x18, 0x32, 0x1A, 0x7B,
            0xCC, 0xE3, 0x1C, 0x3A,
            0x4E, 0x25, 0x32, 0x78,
            0x15, 0x33, 0xF6, 0x1F,
            0x5D, 0x6B, 0x3A, 0x13,
            0x10, 0xF2, 0xEE, 0xF5
        };

        public static readonly byte[] CNonce = new byte[12]
        {
            0x1A, 0xB3, 0xCF, 0xDE,
            0xE0, 0xAA, 0xB3, 0x45,
            0xB8, 0x73, 0x12, 0x89
        };

        private const int EscapeLength = 256;
        private static readonly uint[] Sigma =
        {
            0x61707865,
            0x3320646e,
            0x79622d32,
            0x6b206574
        };

        private readonly Stream baseStream;
        public bool autoDisposeBaseStream { get; set; } = true;

        public SeekableAesStream(Stream baseStream)
        {
            this.baseStream = baseStream ?? throw new ArgumentNullException(nameof(baseStream));
        }

        public static void TransformFileHeaderInPlace(string bundlePath)
        {
            using var stream = new FileStream(bundlePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            TransformHeaderInPlace(stream);
        }

        public static void TransformHeaderInPlace(Stream stream)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));
            if (!stream.CanRead || !stream.CanWrite || !stream.CanSeek)
                throw new NotSupportedException("Stream must support read, write, and seek for in-place header transform.");

            long originalPosition = stream.Position;
            try
            {
                stream.Position = 0;
                int headerLength = (int)Math.Min(EscapeLength, stream.Length);
                if (headerLength <= 0)
                    return;

                byte[] headerBuffer = new byte[headerLength];
                int readLength = stream.Read(headerBuffer, 0, headerLength);
                if (readLength <= 0)
                    return;

                ApplyCipher(headerBuffer, 0, readLength, 0);

                stream.Position = 0;
                stream.Write(headerBuffer, 0, readLength);
                stream.Flush();
            }
            finally
            {
                stream.Position = originalPosition;
            }
        }

        private static void ApplyCipher(byte[] buffer, int offset, int count, long streamPos)
        {
            if (count <= 0 || streamPos >= EscapeLength)
                return;

            int remaining = (int)Math.Min(EscapeLength - streamPos, count);
            long currentPosition = streamPos;
            int currentOffset = offset;
            byte[] keyStreamBlock = new byte[64];

            while (remaining > 0)
            {
                uint blockIndex = (uint)(currentPosition / 64);
                int innerOffset = (int)(currentPosition % 64);
                int bytesThisRound = Math.Min(64 - innerOffset, remaining);

                GenerateKeyStreamBlock(blockIndex, keyStreamBlock);

                for (int i = 0; i < bytesThisRound; i++)
                    buffer[currentOffset + i] ^= keyStreamBlock[innerOffset + i];

                currentOffset += bytesThisRound;
                currentPosition += bytesThisRound;
                remaining -= bytesThisRound;
            }
        }

        private static void GenerateKeyStreamBlock(uint counter, byte[] output)
        {
            uint[] state = new uint[16];
            uint[] workingState = new uint[16];

            state[0] = Sigma[0];
            state[1] = Sigma[1];
            state[2] = Sigma[2];
            state[3] = Sigma[3];

            for (int i = 0; i < 8; i++)
                state[4 + i] = ToUInt32(AesKey, i * 4);

            state[12] = counter;
            state[13] = ToUInt32(CNonce, 0);
            state[14] = ToUInt32(CNonce, 4);
            state[15] = ToUInt32(CNonce, 8);

            Array.Copy(state, workingState, state.Length);

            for (int round = 0; round < 10; round++)
            {
                QuarterRound(ref workingState[0], ref workingState[4], ref workingState[8], ref workingState[12]);
                QuarterRound(ref workingState[1], ref workingState[5], ref workingState[9], ref workingState[13]);
                QuarterRound(ref workingState[2], ref workingState[6], ref workingState[10], ref workingState[14]);
                QuarterRound(ref workingState[3], ref workingState[7], ref workingState[11], ref workingState[15]);

                QuarterRound(ref workingState[0], ref workingState[5], ref workingState[10], ref workingState[15]);
                QuarterRound(ref workingState[1], ref workingState[6], ref workingState[11], ref workingState[12]);
                QuarterRound(ref workingState[2], ref workingState[7], ref workingState[8], ref workingState[13]);
                QuarterRound(ref workingState[3], ref workingState[4], ref workingState[9], ref workingState[14]);
            }

            for (int i = 0; i < 16; i++)
                WriteUInt32(workingState[i] + state[i], output, i * 4);
        }

        private static uint ToUInt32(byte[] source, int offset)
        {
            return (uint)(source[offset]
                | (source[offset + 1] << 8)
                | (source[offset + 2] << 16)
                | (source[offset + 3] << 24));
        }

        private static void WriteUInt32(uint value, byte[] destination, int offset)
        {
            destination[offset] = (byte)value;
            destination[offset + 1] = (byte)(value >> 8);
            destination[offset + 2] = (byte)(value >> 16);
            destination[offset + 3] = (byte)(value >> 24);
        }

        private static void QuarterRound(ref uint a, ref uint b, ref uint c, ref uint d)
        {
            a += b;
            d ^= a;
            d = RotateLeft(d, 16);

            c += d;
            b ^= c;
            b = RotateLeft(b, 12);

            a += b;
            d ^= a;
            d = RotateLeft(d, 8);

            c += d;
            b ^= c;
            b = RotateLeft(b, 7);
        }

        private static uint RotateLeft(uint value, int count)
        {
            return (value << count) | (value >> (32 - count));
        }

        public override bool CanRead { get { return baseStream.CanRead; } }
        public override bool CanSeek { get { return baseStream.CanSeek; } }
        public override bool CanWrite { get { return baseStream.CanWrite; } }
        public override long Length { get { return baseStream.Length; } }
        public override long Position { get { return baseStream.Position; } set { baseStream.Position = value; } }
        public override void Flush() { baseStream.Flush(); }
        public override void SetLength(long value) { baseStream.SetLength(value); }
        public override long Seek(long offset, SeekOrigin origin) { return baseStream.Seek(offset, origin); }

        public override int Read(byte[] buffer, int offset, int count)
        {
            var streamPos = Position;
            var ret = baseStream.Read(buffer, offset, count);
            ApplyCipher(buffer, offset, ret, streamPos);
            return ret;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (count <= 0)
                return;

            long streamPos = Position;
            if (streamPos >= EscapeLength)
            {
                baseStream.Write(buffer, offset, count);
                return;
            }

            byte[] tempBuffer = new byte[count];
            Buffer.BlockCopy(buffer, offset, tempBuffer, 0, count);
            ApplyCipher(tempBuffer, 0, count, streamPos);
            baseStream.Write(tempBuffer, 0, count);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (autoDisposeBaseStream)
                    baseStream?.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}

