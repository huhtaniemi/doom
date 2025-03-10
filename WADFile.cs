using System;
using System.Text;
using System.Runtime.InteropServices;
using static DOOM.WAD.WADFileTypes;
using System.Numerics;

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
        public struct Thing
        {
            [Flags]
            public enum FLAGS
            {
                skill_level_1_2 = 0x0001,	// Thing is on skill levels 1 & 2
                skill_level_3   = 0x0002,	// Thing is on skill level 3
                skill_level_4_5 = 0x0004,	// Thing is on skill levels 4 & 5
                ambush          = 0x0008,	// Thing is waiting in ambush. Commonly known as "deaf" flag.
                //In fact, it does not render monsters deaf per se.
                not_in_single_player = 0x0010// Thing is not in single player
            }
            public short pos_x;
            public short pos_y;
            public ushort angle_facing;
            public ushort type;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
            //public FLAGS flags;
            public ushort flags;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct linedef
        {
            [Flags]
            public enum FLAGS
            {
                ML_BLOCKING         = 0x0001, // blocks players and monsters
                ML_BLOCKMONSTERS    = 0x0002, // blocks monsters
                ML_TWOSIDED         = 0x0004, // two sided
                ML_DONTPEGTOP       = 0x0008, // upper texture is unpegged
                ML_DONTPEGBOTTOM    = 0x0010, // 	lower texture is unpegged
                ML_SECRET           = 0x0020, // secret (shows as one-sided on automap), and monsters cannot open if it is a door (type 1)
                ML_SOUNDBLOCK       = 0x0040, // blocks sound
                ML_DONTDRAW         = 0x0050, // never shows on automap
                ML_MAPPED           = 0x0100  // always shows on automap
            }
            public ushort vertex_id_start;
            public ushort vertex_id_end;
            //[MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
            //public FLAGS flags;
            public ushort flags;
            public ushort line_type;
            public ushort sector_tag;
            public ushort sidedef_id_front;
            public ushort sidedef_id_back;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct sidedef
        {
            public short offset_x;
            public short offset_y;
            public long tex_upper; // helper.Sfn<helper.Sfnbyte8>
            public long tex_lower;
            public long tex_middle;
            public ushort sector_id;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct vertex
        {
            public short pos_x;
            public short pos_y;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct seg
        {
            public ushort vertex_id_start;
            public ushort vertex_id_end;
            public ushort slope_angle;
            public ushort linedef_id;
            public ushort direction;
            public ushort offset;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct subsector
        {
            public ushort seg_count;
            public ushort first_seg_id;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct node
        {
            public struct bounding_box
            {
                public short top, bottom, left, right;
            }
            // coordinate of partition line start
            public short partition_x;
            public short partition_y;
            // change in n from start to end of partition line
            public short partition_x_change;
            public short partition_y_change;
            public bounding_box right;
            public bounding_box left;
            public ushort child_id_right;
            public ushort child_id_left;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct sector
        {
            public short height_floor;
            public short height_ceiling;
            public long tex_floor; // helper.Sfn<helper.Sfnbyte8>
            public long tex_ceiling;
            public ushort light_level;
            public ushort sector_type;
            public ushort tag;
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


        private int GetIndexByName(helper.refData<filelump> filelumps, string name )
        {
            var lump_index = 0;
            foreach (ref readonly var filelump in filelumps)
            {
                if (filelump.name == name)
                    return lump_index;
                lump_index++;
            }
            return -1;
        }

        private helper.refData<T> GetLumpData<T>(filelump lump_info) where T : struct
        {
            return new helper.refData<T>(r, lump_info.filepos, lump_info.size / (uint)Marshal.SizeOf<T>());
        }


        // properties

        public ref struct MapData
        {
            public helper.refData<linedef> linedefs;
            public helper.refData<vertex> vertexes;

            public MapData(helper.refData<linedef> linedefs, helper.refData<vertex> vertexes)
            {
                this.linedefs = linedefs;
                this.vertexes = vertexes;
            }
        }

        public MapData GetMapData(string map_name = "E1M1")
        {
            var filelumps = new helper.refData<filelump>(r, header.infotableofs, header.numlumps);
            var lump_idx = GetIndexByName(filelumps, map_name);

            var linedefs = GetLumpData<linedef>(filelumps[lump_idx + (int)EMAPLUMPSINDEX.eLINEDEFS]);
            var vertexes = GetLumpData<vertex>(filelumps[lump_idx + (int)EMAPLUMPSINDEX.eVERTEXES]);

            return new(linedefs, vertexes);
        }


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
                var lump_info = filelumps[lump_index + (int)EMAPLUMPSINDEX.eLINEDEFS];
                var linedefs = new helper.refData<linedef>(r, lump_info.filepos, lump_info.size/14);
                foreach (ref readonly var line in linedefs)
                {
                    LINEDEFS.Add(line);
                }
            }

            {
                var lump_info = filelumps[lump_index + (int)EMAPLUMPSINDEX.eVERTEXES];
                var vertexes = new helper.refData<vertex>(r, lump_info.filepos, lump_info.size/4);
                foreach (ref readonly var vex in vertexes)
                {
                    VERTEXES.Add(new(vex.pos_x, vex.pos_y));
                }
            }

        }

        List<linedef> LINEDEFS = [];
        public List<linedef> GetLINEDEFS()
        {
            return LINEDEFS;
        }

        List<Vector2> VERTEXES = [];
        public List<Vector2> GetVERTEXES()
        {
            return VERTEXES;
        }

    }
}
