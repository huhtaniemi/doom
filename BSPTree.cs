using System.Numerics;
using DOOM.WAD;
using static DOOM.WAD.WADFile;
using static DOOM.WAD.WADFileTypes;

namespace DOOM
{
    public class BSP
    {
        private const ushort NF_SUBSECTOR = 0x8000; // 2**15 = 32768
        public const float FOV = 90;
        public const float H_FOV = FOV / 2;

        public void RenderBspNode(MapData map, int bspnum, Vector2 player, Action<seg, int> DrawSeg)
        {
            // if ((bspnum & NF_SUBSECTOR) != 0)
            if (bspnum >= NF_SUBSECTOR)
            {
                //sub_sector_id = (bspnum == -1) ? 0 : (bspnum & ~NF_SUBSECTOR);
                RenderSubSector(map, bspnum - NF_SUBSECTOR, player, DrawSeg);
                return;
            }

            var node = map.nodes[bspnum];
            if (IsOnBackSide(player,node))
            {
                RenderBspNode(map, node.child_id_left, player, DrawSeg);
                RenderBspNode(map, node.child_id_right, player, DrawSeg);
            }
            else
            {
                RenderBspNode(map, node.child_id_right, player, DrawSeg);
                RenderBspNode(map, node.child_id_left, player, DrawSeg);
            }
        }

        private void RenderSubSector(MapData map, int subSectorId, Vector2 player, Action<seg, int> DrawSeg)
        {
            var subSector = map.subsectors[subSectorId];

            for (int i = 0; i < subSector.seg_count; i++)
            {
                var seg = map.segs[subSector.first_seg_id + i];
                DrawSeg(seg, subSectorId);
            }
        }

        private bool IsOnBackSide(Vector2 player, node node)
        {
            float dx = player.X - node.partition_x;
            float dy = player.Y - node.partition_y;
            return dx * node.partition_dy - dy * node.partition_dx <= 0.0f;
        }
    }

}
