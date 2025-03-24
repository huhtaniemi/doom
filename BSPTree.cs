using System.Numerics;
using DOOM.WAD;
using static DOOM.Renderer;
using static DOOM.WAD.WADFile;
using static DOOM.WAD.WADFileTypes;

namespace DOOM
{
    public class BSP(SegHandler seg_handler)
    {
        SegHandler seg_handler = seg_handler;

        private const ushort NF_SUBSECTOR = WADFileTypes.NF_SUBSECTOR;
        public const float FOV = 90;
        public const float H_FOV = FOV / 2;
        public const float SCALE = 2.5f;
        public const float WIDTH = (320f * SCALE);
        public const float HEIGHT = (200f * SCALE);
        public const float H_WIDTH = WIDTH / 2;
        public const float H_HEIGHT = HEIGHT / 2;
        public static float SCREEN_DIST = H_WIDTH / MathF.Tan((MathF.PI / 180f) * H_FOV);

        public ushort root_node_id = ushort.MaxValue;
        public bool is_traverse_bsp = true;

        public int GetSubSectorHeight(MapData map, Player player)
        {
            int sub_sector_id = root_node_id;
            while (sub_sector_id < NF_SUBSECTOR)
            {
                var node = map.nodes[sub_sector_id];

                bool is_on_back = IsOnBackSide(player, node);
                if (is_on_back)
                    sub_sector_id = node.child_id_left;
                else
                    sub_sector_id = node.child_id_right;
            }
            return map.seg_front_sidedef_sector(sub_sector_id - NF_SUBSECTOR).height_floor;

            /*
            var sub_sector = map.subsectors[sub_sector_id - NF_SUBSECTOR];
            var seg = map.segs[sub_sector.seg_id_first];

            var seg_linedef = map.linedefs[seg.linedef_id];
            var seg_front_sidedef = map.sidedefs[seg.direction > 0
                ? seg_linedef.sidedef_id_back
                : seg_linedef.sidedef_id_front
            ];
            return map.sectors[seg_front_sidedef.sector_id].height_floor;
            */
        }

        static float radians(float angle)
             => MathF.PI / 180f * angle;

        public static int AngleToX(float angle)
            => (int)(SCREEN_DIST - MathF.Tan(radians(angle)) * H_WIDTH);

        public bool AddSegmentToFov(Player player, vertex v1, vertex v2, out (int x1, int x2, float rwAngle1) result)
        {
            result = (0, 0, 0);

            float angle1 = PointToAngle(player, new Vector2(v1.pos_x, v1.pos_y));
            float angle2 = PointToAngle(player, new Vector2(v2.pos_x, v2.pos_y));

            float span = Norm(angle1 - angle2);
            // backface culling
            if (span >= 180.0f)
                return false;

            // needed for further calculations
            float rwAngle1 = angle1;

            angle1 -= player.angle;
            angle2 -= player.angle;

            float span1 = Norm(angle1 + H_FOV);
            if (span1 > FOV)
            {
                if (span1 >= span + FOV)
                    return false;
                // clipping
                angle1 = H_FOV;
            }

            float span2 = Norm(H_FOV - angle2);
            if (span2 > FOV)
            {
                if (span2 >= span + FOV)
                    return false;
                // clipping
                angle2 = -H_FOV;
            }

            int x1 = AngleToX(angle1);
            int x2 = AngleToX(angle2);
            result = (x1, x2, rwAngle1);

            return true;
        }

        public static float Norm(float angle)
            => ((angle % 360f) + 360f) % 360f;

        public bool CheckBBox(Player player, node.bounding_box bbox)
        {
            var a = new Vector2(bbox.left, bbox.bottom);
            var b = new Vector2(bbox.left, bbox.top);
            var c = new Vector2(bbox.right, bbox.top);
            var d = new Vector2(bbox.right, bbox.bottom);

            float px = player.pos.X;
            float py = player.pos.Y;

            var bboxSides = new List<(Vector2, Vector2)>();

            if (px < bbox.left)
            {
                if (py > bbox.top)
                    bboxSides.AddRange([(b, a), (c, b)]);
                else if (py < bbox.bottom)
                    bboxSides.AddRange([(b, a),(a, d)]);
                else
                    bboxSides.AddRange([(b, a)]);
            }
            else if (px > bbox.right)
            {
                if (py > bbox.top)
                    bboxSides.AddRange([(c, b),(d, c)]);
                else if (py < bbox.bottom)
                    bboxSides.AddRange([(a, d),(d, c)]);
                else
                    bboxSides.AddRange([(d, c)]);
            }
            else
            {
                if (py > bbox.top)
                    bboxSides.AddRange([(c, b)]);
                else if (py < bbox.bottom)
                    bboxSides.AddRange([(a, d)]);
                else
                    return true;
            }

            foreach (var (v1, v2) in bboxSides)
            {
                float angle1 = PointToAngle(player, v1);
                float angle2 = PointToAngle(player, v2);

                float span = Norm(angle1 - angle2);

                angle1 -= player.angle;
                float span1 = Norm(angle1 + H_FOV);
                if (span1 > FOV)
                    if (span1 >= span + FOV)
                        continue;
                return true;
            }
            return false;
        }

        public float PointToAngle(Player player, Vector2 vec)
        {
            var delta = vec - player.pos;
            return (MathF.Atan2(delta.Y, delta.X) * (180.0f / MathF.PI)) % 360;
        }

        public void RenderBspNode(MapData map, Player player, Graphics g, ushort node_id,
            Action<seg, ushort> DrawSeg, Action<node.bounding_box> DrawBox)
        {
            if (is_traverse_bsp == false)
                return;

            // if ((bspnum & NF_SUBSECTOR) != 0)
            //   sub_sector_id = (bspnum == -1) ? 0 : (bspnum & ~NF_SUBSECTOR);
            if (node_id >= NF_SUBSECTOR)
            {
                var sub_sector_id = node_id - NF_SUBSECTOR;
                RenderSubSector(map, g, (ushort)sub_sector_id, player, DrawSeg);
                return;
            }

            var node = map.nodes[node_id];

            var OnBackSide  = IsOnBackSide(player, node);
            if (OnBackSide)
            {
                RenderBspNode(map, player, g, node.child_id_left, DrawSeg, DrawBox);
                if (CheckBBox(player, node.right)) // front
                {
                    DrawBox(node.right);
                    RenderBspNode(map, player, g, node.child_id_right, DrawSeg, DrawBox);
                }
            }
            else
            {
                RenderBspNode(map, player, g, node.child_id_right, DrawSeg, DrawBox);
                if (CheckBBox(player, node.left)) // back
                {
                    DrawBox(node.left);
                    RenderBspNode(map, player, g, node.child_id_left, DrawSeg, DrawBox);
                }
            }
        }

        private void RenderSubSector(MapData map, Graphics g, ushort subSectorId, Player player, Action<seg, ushort> DrawSeg)
        {
            var subSector = map.subsectors[subSectorId];

            for (int i = 0; i < subSector.seg_count; i++)
            {
                var seg = map.segs[subSector.seg_id_first + i];
                (int x1, int x2, float rwAngle1) result;
                if (AddSegmentToFov(player, map.vertexes[seg.vertex_id_start], map.vertexes[seg.vertex_id_end], out result))
                {
                    DrawSeg(seg, subSectorId);
                    seg_handler.ClassifySegment(map, seg, result.x1, result.x2, result.rwAngle1, ref is_traverse_bsp);
                }
            }
        }

        private bool IsOnBackSide(Player player, node node)
        {
            float dx = player.pos.X - node.partition_x;
            float dy = player.pos.Y - node.partition_y;
            return dx * node.partition_dy - dy * node.partition_dx <= 0.0f;
        }
    }

}
