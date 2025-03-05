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

        // common abstraction

        public abstract Span<byte> GetBytes(uint offset, uint count);

        public abstract ref WADFileTypes.info ReadHeaderData(uint offset = 0);

        public abstract ref WADFileTypes.filelump ReadDirectoryData(uint offset, uint count = 1);
    }

    public static class WADLoader
    {
        private class StreamReader : BaseReader
        {
            protected BinaryReader reader;

            public StreamReader(string filepath) : base(filepath)
            {
                reader = new(File.Open(this.filepath, FileMode.Open));
            }
            public override void Dispose()
            {
                reader?.Dispose();
            }

            public override Span<byte> GetBytes(uint offset, uint count)
            {
                reader.BaseStream.Position = offset;
                return reader.ReadBytes((int)count);
            }

            public override ref WADFileTypes.info ReadHeaderData(uint offset = 0)
            {
                reader.BaseStream.Position = offset;
                return ref MemoryMarshal.Cast<byte, WADFileTypes.info>(
                    reader.ReadBytes(Marshal.SizeOf<WADFileTypes.info>()))[0];
                //return new(
                //    idname: reader.ReadBytes(4),// offset + 0
                //    count: reader.ReadUInt32(), // offset + 4
                //    offset: reader.ReadUInt32() // offset + 8
                //);
            }

            public override ref WADFileTypes.filelump ReadDirectoryData(uint offset, uint count = 1)
            {
                reader.BaseStream.Position = offset;
                return ref MemoryMarshal.Cast<byte, WADFileTypes.filelump>(
                    reader.ReadBytes(Marshal.SizeOf<WADFileTypes.filelump>() * (int)count))[0];
                //return new(
                //    offset: reader.ReadUInt32(),// offset + 0
                //    size: reader.ReadUInt32(),  // offset + 4
                //    name: reader.ReadBytes(8)   // offset + 8
                //);
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

            public override Span<byte> GetBytes(uint offset, uint count)
            {
                return buffer.AsSpan((int)offset, (int)count);
            }

            public override ref WADFileTypes.info ReadHeaderData(uint offset = 0)
            {
                return ref MemoryMarshal.Cast<byte, WADFileTypes.info>(
                    buffer.AsSpan((int)offset, Marshal.SizeOf<WADFileTypes.info>()))[0];
            }

            public override ref WADFileTypes.filelump ReadDirectoryData(uint offset, uint count = 1)
            {
                return ref MemoryMarshal.Cast<byte, WADFileTypes.filelump>(
                    buffer.AsSpan((int)offset, (int)count * Marshal.SizeOf<WADFileTypes.filelump>()))[0];
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
