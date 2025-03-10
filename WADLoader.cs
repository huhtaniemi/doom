using System.Runtime.InteropServices;
using System.Collections;
using static DOOM.WAD.WADFile;
using System.Data;

namespace DOOM.WAD
{
    internal abstract class BaseReader(string filepath) : IDisposable
    {
        protected string filepath = filepath;

        public virtual void Dispose() { }

        public abstract ReadOnlySpan<byte> GetBytes(uint offset, uint count);
    }

    public static class WADLoader
    {
        private class StreamReader : BaseReader
        {
            protected BinaryReader stream;

            public StreamReader(string filepath) : base(filepath)
            {
                stream = new(File.Open(this.filepath, FileMode.Open));
            }
            public override void Dispose()
            {
                stream?.Dispose();
            }

            public override ReadOnlySpan<byte> GetBytes(uint offset, uint count)
            {
                stream.BaseStream.Position = offset;
                return stream.ReadBytes((int)count);
            }
        }

        private class BufferReader : BaseReader
        {
            private readonly byte[] buffer;

            public BufferReader(string filepath) : base(filepath)
            {
                using var stream = File.Open(this.filepath, FileMode.Open);
                buffer = new byte[stream.Length];
                stream.Read(buffer);
            }

            public override void Dispose()
            {
                //buffer = [];
            }

            public override ReadOnlySpan<byte> GetBytes(uint offset, uint count)
            {
                return buffer.AsSpan((int)offset, (int)count);
            }
        }

        public static WADFile Open(string filepath, bool buffered = true)
        {
            return new WADFile(buffered
                ? new BufferReader(filepath)
                : new StreamReader(filepath));
        }
    }
}
