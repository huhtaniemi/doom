using System;
using System.Text;
using System.Runtime.InteropServices;
using static DOOM.WAD.WADFileTypes;
using System.Runtime.CompilerServices;
using static DOOM.WAD.WADFileTypes.helper;

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


        // Subsector Identifier is the 16th bit which
        // indicate if the node ID is a subsector.
        // 0x8000 in binary 1000000000000000
        public const ushort NF_SUBSECTOR = 0x8000;

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


        // map types

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
        public struct Palette
        {
            [StructLayout(LayoutKind.Sequential, Pack = 1)]
            public struct Color
            {
                public byte r, g, b;
                public const int SIZE = 3;
                public static implicit operator System.Drawing.Color(Color obj)
                    => System.Drawing.Color.FromArgb(obj.r, obj.g, obj.b);
            }
            public const int SIZE = Color.SIZE * 256;
            //public Color[256] colors;
        }


        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct thing
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
            //[MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
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
            public helper.Sfn<helper.Sfnbyte8> tex_upper;
            public helper.Sfn<helper.Sfnbyte8> tex_lower;
            public helper.Sfn<helper.Sfnbyte8> tex_middle;
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
            public ushort seg_id_first;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct node
        {
            [StructLayout(LayoutKind.Sequential, Pack = 1)]
            public struct bounding_box
            {
                public short top, bottom, left, right;
            }
            // coordinate of partition line start
            public short partition_x;
            public short partition_y;
            // change in n from start to end of partition line
            public short partition_dx;
            public short partition_dy;
            public bounding_box right; // front
            public bounding_box left; // back
            public ushort child_id_right; //front_child_id
            public ushort child_id_left; //back_child_id
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct sector
        {
            public short height_floor;
            public short height_ceiling;
            public helper.Sfn<helper.Sfnbyte8> tex_floor;
            public helper.Sfn<helper.Sfnbyte8> tex_ceiling;
            public ushort light_level;
            public ushort sector_type;
            public ushort tag;
        }


        // texture types

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct pnames
        {
            public readonly uint count;
            //public helper.Sfn<helper.Sfnbyte8>[count] names;
            public uint size { get => 8 * count; }
        }


        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct PatchHeader
        {
            public ushort width;
            public ushort height;
            public short offset_left;
            public short offset_top;
            //uint [width x I] offset_column;
        }

        public struct PatchColumn
        {
            [StructLayout(LayoutKind.Sequential, Pack = 1)]
            public struct Header
            {
                public byte top_delta;
                public byte length;
                //public byte padding_pre; // - unused
                //byte [length x B] data;
                //public byte padding_post; // - unused
            }
            public Header header;
            //public byte padding_pre; // - unused
            //byte [length x B] data;
            //public byte padding_post; // - unused
            public byte[] data;
        }


        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct TextureHeader
        {
            public uint texture_count; // numtextures
            //public uint[numtextures] texture_offset; // offset[numtextures]
            //public TextureData[numtextures] mtexture[];
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct TextureData // maptexture
        {
            public helper.Sfn<helper.Sfnbyte8> name;
            public uint flags; // masked
            public ushort width;
            public ushort height;
            public uint column_dir; //  - unused
            public ushort patch_count; // patchcount
            // public mappatch[patchcount] patch_maps; // pathces
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct TexturePatch // mappatch
        {
            public short offset_x; // originx
            public short offset_y; // originy
            public ushort p_name_index; // patch
            public ushort step_dir; // - unused
            public ushort color_map; // - unused
        }
    }

    public class WADFile : IDisposable
    {
        private readonly BaseReader r;
        private readonly wadinfo header;
        private readonly filelump pnlump;

        internal WADFile(BaseReader reader)
        {
            this.r = reader;
            this.header = MemoryMarshal.Cast<byte, wadinfo>(
                r.GetBytes(0, (uint)Marshal.SizeOf<wadinfo>()))[0];

            var lump_idx = GetIndexByName(this.filelumps, "PNAMES");
            this.pnlump = this.filelumps[lump_idx];
            //var header = GetRefData<pnames>(pnamesheader.filepos, (uint)Marshal.SizeOf<pnames>())[0];
        }

        public void Dispose()
            => r?.Dispose();

        private ReadOnlySpan<T> GetRefData<T>(uint offset, uint size) where T : struct
            => MemoryMarshal.Cast<byte, T>(r.GetBytes(offset, size));

        private ReadOnlySpan<T> GetLumpData<T>(filelump lump_info) where T : struct
            => GetRefData<T>(lump_info.filepos, lump_info.size);

        protected ReadOnlySpan<filelump> filelumps
            => GetRefData<filelump>(header.infotableofs, header.numlumps * (uint)Marshal.SizeOf<filelump>());

        private int GetIndexByName(ReadOnlySpan<filelump> filelumps, string name)
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

        /*
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

        */

        protected ReadOnlySpan<Sfn<Sfnbyte8>> pnames
            => GetRefData<Sfn<Sfnbyte8>>(pnlump.filepos + 4, pnlump.size - 4);

        protected List<WADFile.Patch> patches
            => pnames.ToArray()
                .Where(name => GetIndexByName(filelumps, name.ToString().ToUpper()) != -1)
                .Select(name => GetPatch(name.ToString().ToUpper()))
                .ToList();

        // properties

        public ref struct MapData
        {
            public ReadOnlySpan<thing> things;
            public ReadOnlySpan<linedef> linedefs;
            public ReadOnlySpan<sidedef> sidedefs;
            public ReadOnlySpan<vertex> vertexes;
            public ReadOnlySpan<seg> segs;
            public ReadOnlySpan<subsector> subsectors;
            public ReadOnlySpan<node> nodes;
            public ReadOnlySpan<sector> sectors;

            public MapData(WADFile wad, string map_name)
                : this(wad, wad.GetIndexByName(wad.filelumps, map_name)) { }

            public MapData(WADFile wad, int lump_idx)
            {
                var filelumps = wad.filelumps;
                things = wad.GetLumpData<thing>(filelumps[lump_idx + (int)EMAPLUMPSINDEX.eTHINGS]);
                linedefs = wad.GetLumpData<linedef>(filelumps[lump_idx + (int)EMAPLUMPSINDEX.eLINEDEFS]);
                sidedefs = wad.GetLumpData<sidedef>(filelumps[lump_idx + (int)EMAPLUMPSINDEX.eSIDEDDEFS]);
                vertexes = wad.GetLumpData<vertex>(filelumps[lump_idx + (int)EMAPLUMPSINDEX.eVERTEXES]);
                segs = wad.GetLumpData<seg>(filelumps[lump_idx + (int)EMAPLUMPSINDEX.eSEAGS]);
                subsectors = wad.GetLumpData<subsector>(filelumps[lump_idx + (int)EMAPLUMPSINDEX.eSSECTORS]);
                nodes = wad.GetLumpData<node>(filelumps[lump_idx + (int)EMAPLUMPSINDEX.eNODES]);
                sectors = wad.GetLumpData<sector>(filelumps[lump_idx + (int)EMAPLUMPSINDEX.eSECTORS]);
            }


            // sidedefs
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public readonly sector sidedef_sector(sidedef s) => sectors[s.sector_id];

            // linedefs
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public readonly sidedef linedef_front_sidedef(linedef l) => sidedefs[l.sidedef_id_front];

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public readonly sidedef? linedef_back_sidedef(linedef l)
                => l.sidedef_id_back == 0xFFFF ? null : sidedefs[l.sidedef_id_back];

            // segs
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public readonly vertex seg_start_vertex(seg s) => vertexes[s.vertex_id_start];
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public readonly vertex seg_end_vertex(seg s) => vertexes[s.vertex_id_end];

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public readonly linedef seg_linedef(seg s) => linedefs[s.linedef_id];

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public readonly float seg_angle(seg s)
            {
                //return ((int)s.slope_angle << 16) * 8.38190317e-8f;
                var a = ((int)s.slope_angle << 16) * 8.38190317e-8f;
                if (a < 0)
                    return (a + 360);
                else return a;

            }

            public readonly sidedef? seg_front_sidedef(seg s) => s.direction > 0
                    ? linedef_back_sidedef(seg_linedef(s))
                    : linedef_front_sidedef(seg_linedef(s));

            public readonly sidedef? seg_back_sidedef(seg s) => s.direction > 0
                    ? linedef_front_sidedef(seg_linedef(s))
                    : linedef_back_sidedef(seg_linedef(s));

            public readonly sector seg_front_sector(seg s) => sidedef_sector(seg_front_sidedef(s)??new());

            public readonly sector? seg_back_sector(seg s)
                => (seg_linedef(s).flags & (ushort)linedef.FLAGS.ML_TWOSIDED) > 0
                ? sidedef_sector(seg_back_sidedef(s) ?? new()) : null;

            // subsector
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public readonly seg subsec_seg_first(subsector ss) => segs[ss.seg_id_first];


            // segs combo
            public readonly sector seg_front_sidedef_sector(int sub_sector_id)
            {
                var sub_sector = subsectors[sub_sector_id];
                //var seg = segs[sub_sector.seg_id_first];
                var seg = subsec_seg_first(sub_sector);

                var linedef = linedefs[seg.linedef_id];
                var seg_front_sidedef = seg.direction > 0
                    ? sidedefs[linedef.sidedef_id_back] // linedef_back_sidedef(linedef)
                    : sidedefs[linedef.sidedef_id_front]; // linedef_front_sidedef(linedef)
                //var seg_front_sidedef = this.seg_front_sidedef(seg);

                //return sectors[seg_front_sidedef.sector_id];
                return sidedef_sector(seg_front_sidedef);
            }
        }

        public MapData GetMapData(string map_name = "E1M1")
        {
            var filelumps = this.filelumps; // new helper.refData<filelump>(r, header.infotableofs, header.numlumps);
            var lump_idx = GetIndexByName(filelumps, map_name);

            return new(){
                things = GetLumpData<thing>(filelumps[lump_idx + (int)EMAPLUMPSINDEX.eTHINGS]),
                linedefs = GetLumpData<linedef>(filelumps[lump_idx + (int)EMAPLUMPSINDEX.eLINEDEFS]),
                sidedefs = GetLumpData<sidedef>(filelumps[lump_idx + (int)EMAPLUMPSINDEX.eSIDEDDEFS]),
                vertexes = GetLumpData<vertex>(filelumps[lump_idx + (int)EMAPLUMPSINDEX.eVERTEXES]),
                segs = GetLumpData<seg>(filelumps[lump_idx + (int)EMAPLUMPSINDEX.eSEAGS]),
                subsectors = GetLumpData<subsector>(filelumps[lump_idx + (int)EMAPLUMPSINDEX.eSSECTORS]),
                nodes = GetLumpData<node>(filelumps[lump_idx + (int)EMAPLUMPSINDEX.eNODES]),
                sectors = GetLumpData<sector>(filelumps[lump_idx + (int)EMAPLUMPSINDEX.eSECTORS])
            };
        }

        public struct Palette
        {
            internal const int SIZE = WADFileTypes.Palette.SIZE;
            private readonly WADFileTypes.Palette.Color[] colors;
            public readonly WADFileTypes.Palette.Color this[int index] => colors[index];
            public Palette(ReadOnlySpan<WADFileTypes.Palette.Color> data)
            {
                if (data.Length != 256)
                    throw new IndexOutOfRangeException();
                colors = [..data];
            }
        }

        public WADFile.Palette GetPalette(int index)
        {
            var lump_idx = GetIndexByName(this.filelumps, "PLAYPAL");
            var filelump = this.filelumps[lump_idx];
            //var palettes = GetRefData<Palette>(filelump.filepos, filelump.size);
            //return palettes[index];
            if (filelump.size < ((uint)index + 1) * Palette.SIZE)
                throw new IndexOutOfRangeException();
            var colors = GetRefData<WADFileTypes.Palette.Color>(filelump.filepos + ((uint)index * Palette.SIZE), Palette.SIZE);
            return new(colors);
        }

        public struct Patch
        {
            public readonly string names;
            public readonly PatchHeader header;
            private readonly List<PatchColumn> columns;
            private readonly Palette palette;
            public readonly Bitmap image;

            public Patch(string name, PatchHeader header, List<PatchColumn> columns, Palette palette, float scale=1.0f)
            {
                this.names = name;
                this.header = header;
                this.columns = columns;
                this.palette = palette;
                this.image = GetImage();
                if (scale != 1.0f)
                    this.image = new Bitmap(this.image,
                        (int)(image.Width * scale), (int)(image.Height * scale));
            }

            Color COLOR_KEY = Color.FromArgb(152, 0, 136);
            private readonly Bitmap GetImage()
            {
                Bitmap image = new Bitmap(header.width, header.height);
                using (Graphics g = Graphics.FromImage(image))
                {
                    g.Clear(COLOR_KEY);
                }
                image.MakeTransparent(COLOR_KEY);

                int ix = 0;
                foreach (var column in columns)
                {
                    if (column.header.top_delta == 0xFF)
                    {
                        ix += 1;
                        continue;
                    }
                    for (int iy = 0; iy < column.header.length; iy++)
                    {
                        image.SetPixel(ix, iy + column.header.top_delta, palette[column.data[iy]]);
                    }
                }
                return image;
            }
        }

        public WADFile.Patch GetPatch(string name, float scale = 1f)
        {
            var lump_idx = GetIndexByName(this.filelumps, name);
            if (lump_idx < 0)
                throw new Exception($"unable to locate path {name,-8} in lumps directory!");
            var filelump = this.filelumps[lump_idx];
            var patch_offset = filelump.filepos;
            var patch_header = GetRefData<PatchHeader>(patch_offset, (uint)Marshal.SizeOf<PatchHeader>())[0];
            var column_offsets = GetRefData<uint>(
                (patch_offset + (uint)Marshal.SizeOf<PatchHeader>()), (uint)patch_header.width * 4);

            List<PatchColumn> patch_columns = [];
            foreach (var column_offset in column_offsets)
            {
                var offset = patch_offset + column_offset;
                while (true)
                {
                    var column_header = GetRefData<PatchColumn.Header>(offset, (uint)Marshal.SizeOf<PatchColumn.Header>())[0];
                    if (column_header.top_delta == 0xFF)
                    {
                        patch_columns.Add(new PatchColumn()
                        {
                            header = new PatchColumn.Header() { top_delta = 0xFF }
                        });
                        break;
                    }
                    patch_columns.Add(new PatchColumn()
                    {
                        header = column_header,
                        data = [.. GetRefData<byte>(offset + 2 + 1, (uint)column_header.length)]
                    });
                    offset += 4 + (uint)column_header.length;
                }
            }
            return new(name, patch_header, patch_columns, this.GetPalette(0), scale);
        }

        public Dictionary<string, WADFile.Patch> GetSprites()
        {
            Dictionary<string,WADFile.Patch> sprites = [];

            var lump_idx_first = GetIndexByName(this.filelumps, "S_START") + 1;
            var lump_idx_last = GetIndexByName(this.filelumps, "S_END");

            foreach(var idx in Enumerable.Range(lump_idx_first, lump_idx_last-lump_idx_first))
            {
                var spritename = this.filelumps[idx].name;
                sprites.Add(spritename, GetPatch(spritename, BSP.SCALE));
            }
            return sprites;
        }


        public struct Texture
        {
            public readonly TextureData tex_header;
            public readonly List<TexturePatch> tex_patches;
            public readonly Bitmap image;
            public readonly byte[,,] image_array;

            public Texture(TextureData tex_header, List<TexturePatch> tex_patches, List<WADFile.Patch> patches)
            {
                this.tex_header = tex_header;
                this.tex_patches = tex_patches;
                this.image = GetImage(patches);
                //pg.surfarray.array3d()
                this.image_array = ConvertTo3DArray(this.image);
            }

            Color COLOR_KEY = Color.FromArgb(152, 0, 136);
            private readonly Bitmap GetImage(List<WADFile.Patch> patches)
            {
                Bitmap image = new(tex_header.width, tex_header.height);
                using (Graphics g = Graphics.FromImage(image))
                {
                    g.Clear(COLOR_KEY);
                }
                image.MakeTransparent(COLOR_KEY);

                foreach (var patchMap in tex_patches)
                {
                    var patch = patches[patchMap.p_name_index];
                    using (Graphics g = Graphics.FromImage(image))
                    {
                        g.DrawImage(patch.image, patchMap.offset_x, patchMap.offset_y);
                    }
                }
                return image;
            }

            private readonly byte[,,] ConvertTo3DArray(Bitmap image)
            {
                var (width, height) = (image.Width, image.Height);
                byte[,,] result = new byte[width, height, 3];
                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        Color pixel = image.GetPixel(x, y);
                        result[x, y, 0] = pixel.R;
                        result[x, y, 1] = pixel.G;
                        result[x, y, 2] = pixel.B;
                    }
                }
                return result;
            }
        }

        public Dictionary<string,WADFile.Texture> GetTextures(string name = "TEXTURE1")
        {
            var patches = this.patches;
            var lump_idx = GetIndexByName(this.filelumps, name);
            var lump_offset = this.filelumps[lump_idx].filepos;
            var header = GetRefData<TextureHeader>(lump_offset, (uint)Marshal.SizeOf<TextureHeader>())[0];
            var texture_offsets = GetRefData<uint>(
                (lump_offset + (uint)Marshal.SizeOf<TextureHeader>()), (uint)header.texture_count * 4);
            Dictionary<string, Texture> map_textures = [];
            foreach (var texture_offset in texture_offsets)
            {
                var offset = lump_offset + texture_offset;
                var tex_map = GetRefData<TextureData>(offset, (uint)Marshal.SizeOf<TextureData>())[0];
                var tex_patches = GetRefData<TexturePatch>(offset + (uint)Marshal.SizeOf<TextureData>(),
                    tex_map.patch_count * (uint)Marshal.SizeOf<TexturePatch>());
                map_textures.Add(tex_map.name, new(tex_map, [.. tex_patches], patches));
            }
            return map_textures;
        }

        public struct TextureFlat
        {
            readonly Size size = new( 64, 64 );
            private byte[] flatdata;
            private Palette palette;
            public readonly Bitmap image;
            public readonly byte[,,] image_array;

            public TextureFlat(byte[] data, Palette palette)
            {
                this.flatdata = data;
                this.palette = palette;
                this.image = GetImage();
                //pg.surfarray.array3d()
                this.image_array = ConvertTo3DArray(this.image);
            }

            private readonly Bitmap GetImage()
            {
                Bitmap image = new(size.Width, size.Height);
                for (int i = 0; i < flatdata.Length; i++)
                {
                    int ix = i % 64;
                    int iy = i / 64;
                    Color color = palette[flatdata[i]];
                    image.SetPixel(ix, iy, color);
                }
                return image;
            }

            private readonly byte[,,] ConvertTo3DArray(Bitmap image)
            {
                var (width, height) = (image.Width, image.Height);
                byte[,,] result = new byte[width, height, 3];
                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        Color pixel = image.GetPixel(x, y);
                        result[x, y, 0] = pixel.R;
                        result[x, y, 1] = pixel.G;
                        result[x, y, 2] = pixel.B;
                    }
                }
                return result;
            }
        }

        public Dictionary<string, WADFile.TextureFlat> GetTexturesFlats(Palette palette)
        {
            var lump_idx_first = GetIndexByName(this.filelumps, "F_START") + 1;
            var lump_idx_last = GetIndexByName(this.filelumps, "F_END");

            Dictionary<string, WADFile.TextureFlat> flat_textures = [];
            foreach (var idx in Enumerable.Range(lump_idx_first, lump_idx_last - lump_idx_first))
            {
                var lump = this.filelumps[idx];
                var data = GetRefData<byte>(lump.filepos, (uint)lump.size);
                flat_textures.Add(lump.name, new([..data], palette));
            }
            return flat_textures;
        }

        // DEBUG

        public void TEST()
        {
            //foreach (ref readonly var filelump in filelumps)
            {
                //Console.WriteLine($"{filelump.name,-8} - offset {filelump.filepos,-7} - size {filelump.size}");
            }

            List<WADFile.Patch> patches = [];
            foreach (ref readonly var name in this.pnames)
            {
                //Console.WriteLine($"{name,-8}");
                try
                {
                    var patch = GetPatch(name.ToString().ToUpper());
                    patches.Add(patch);
                    //Console.WriteLine($"{name,-8} {patch.header.width}x{patch.header.height}");
                }
                catch (Exception e)
                {
                    //Console.WriteLine(e.Message);
                }
            }
            var tmp = GetTextures("TEXTURE1");
            //var tmp = GetTextures("TEXTURE2", patches);

            foreach (var (name, patch) in GetSprites())
            {
                //Console.WriteLine($"{name,-8} {patch.header.width}x{patch.header.height}");
            }

            var tmp2 = GetTexturesFlats(this.GetPalette(0));

            var map_name = "E1M1";
            //*
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
                    //VERTEXES.Add(new(vex.pos_x, vex.pos_y));
                    VERTEXES.Add(vex);
                }
            }
            // */

            {
                ReadOnlySpan<filelump> filelumps2
                    = GetRefData<filelump>(header.infotableofs, header.numlumps * (uint)Marshal.SizeOf<filelump>());
                var lump_index2 = GetIndexByName(filelumps2, map_name);
                LINEDEFS = [..
                    GetLumpData<linedef>(filelumps2[lump_index2 + (int)EMAPLUMPSINDEX.eLINEDEFS])
                ];
                VERTEXES = [..
                    GetLumpData<vertex>(filelumps2[lump_index2 + (int)EMAPLUMPSINDEX.eVERTEXES])
                ];
            }

        }

        List<linedef> LINEDEFS = [];
        public List<linedef> GetLINEDEFS()
        {
            return LINEDEFS;
        }

        List<vertex> VERTEXES = [];
        public List<vertex> GetVERTEXES()
        {
            return VERTEXES;
        }

    }
}
