using System;
using System.Text;
using System.Runtime.InteropServices;
using static DOOM.WAD.WADFileTypes;

#pragma warning disable CS8981

namespace DOOM.WAD
{
    public class WADFileTypes
    {
        public struct helper
        {
            public interface ISfnBytes
            {
                public byte[] Bytes();
            }

            // shortfilename, dosfilename
            public readonly struct Sfn<T>(T val) where T : ISfnBytes
            {
                private readonly T val = val;
                public static implicit operator string(Sfn<T> obj)
                {
                    var bytes = obj.val.Bytes();
                    return new(Encoding.ASCII.GetChars(bytes, 0, bytes.Count(b => b != 0)));
                }
                public override readonly string ToString() => this;
            }


            [StructLayout(LayoutKind.Sequential, Pack = 1)]
            public struct Sfnbyte4(byte[] b) : ISfnBytes
            {
                private readonly byte
                    b0 = b[0], b1 = b[1], b2 = b[2], b3 = b[3];
                public readonly byte[] Bytes()
                    => [b0, b1, b2, b3];
            }

            [StructLayout(LayoutKind.Sequential, Pack = 1)]
            public struct Sfnbyte8(byte[] b) : ISfnBytes
            {
                private readonly byte
                    b0 = b[0], b1 = b[1], b2 = b[2], b3 = b[3],
                    b4 = b[4], b5 = b[5], b6 = b[6], b7 = b[7];
                public readonly byte[] Bytes()
                    => [b0, b1, b2, b3, b4, b5, b6, b7];
            }


            public ref struct refData<T> where T : struct
            {
                private readonly ReadOnlySpan<byte> bytespan;
                private readonly ReadOnlySpan<T> typespan;

                internal refData(BaseReader r, uint offset, uint count)
                {
                    var sz = (uint)Marshal.SizeOf<T>();
                    this.bytespan = r.GetBytes(offset, count * sz);
                    this.typespan = MemoryMarshal.Cast<byte, T>(bytespan);
                }

                public ref readonly T this[int index] => ref typespan[index];

                public readonly ReadOnlySpan<T>.Enumerator GetEnumerator() => typespan.GetEnumerator();
            }

        }


        public enum EMAPLUMPSINDEX
        {
            eName,
            eTHINGS,
            eLINEDEFS,
            eSIDEDDEFS,
            eVERTEXES,
            eSEAGS,
            eSSECTORS,
            eNODES,
            eSECTORS,
            eREJECT,
            eBLOCKMAP,
            eCOUNT
        }


        // types

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct wadinfo()
        {
            private helper.Sfn<helper.Sfnbyte4> _id;
            public readonly string identification => _id;
            public UInt32 numlumps;
            public UInt32 infotableofs;
        };

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct filelump
        {
            public UInt32 filepos;
            public UInt32 size;
            private helper.Sfn<helper.Sfnbyte8> _name;
            public readonly string name => _name;
        };


        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct vertex
        {
            public short pos_x;
            public short pos_y;
        }

    }

    public class WADFile : IDisposable
    {
        private readonly BaseReader r;
        private readonly wadinfo header;

        internal WADFile(BaseReader reader)
        {
            this.r = reader;
            this.header = MemoryMarshal.Cast<byte, wadinfo>(
                r.GetBytes(0, (uint)Marshal.SizeOf<wadinfo>()))[0];
        }

        public void Dispose()
            => r?.Dispose();


        protected helper.refData<filelump> filelumps
            => new(r, header.infotableofs, header.numlumps);


        // properties



        // DEBUG

        public void TEST()
        {
            var map_name = "E1M1";
            var lump_index = 0;
            foreach (ref readonly var filelump in filelumps)
            {
                if (filelump.name == map_name)
                {
                    Console.WriteLine($"Index: {lump_index}, Name: {filelump.name}");
                    break;
                }
                lump_index++;
            }


            {
                var lump_info = filelumps[lump_index + (int)EMAPLUMPSINDEX.eVERTEXES];
                var vertexes = new helper.refData<vertex>(r, lump_info.filepos, lump_info.size);
                foreach (ref readonly var vex in vertexes)
                {
                    Console.WriteLine($"vertexe: {vex}");
                }
            }
        }

    }
}
