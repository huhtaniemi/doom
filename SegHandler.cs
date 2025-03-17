using System;
using System.Numerics;
using DOOM.WAD;
using static DOOM.Renderer;
using static DOOM.WAD.WADFile;
using static DOOM.WAD.WADFileTypes;

namespace DOOM
{
    public class SegHandler
    {
        public const float MAX_SCALE = 64.0f;
        public const float MIN_SCALE = 0.00390625f;

        private ViewRenderer view_renderer;
        private Player player;

        public HashSet<int> screen_range = [];
        public List<int> upper_clip = [];
        public List<int> lower_clip = [];
        public readonly List<float> x_to_angle;

        public SegHandler(ViewRenderer view_renderer, Player player)
        {
            this.view_renderer = view_renderer;
            //this.wad_data = engine.wad_data;
            this.player = player;

            //for (int i = 0; i <= BSP.WIDTH; i++)
            //{
            //    this.x_to_angle.Add(MathF.Atan((BSP.H_WIDTH - i) / BSP.SCREEN_DIST) * (180 / MathF.PI));
            //}
            this.x_to_angle = [.. Enumerable
                .Range(0, (int)BSP.WIDTH + 1)
                .Select(i => MathF.Atan((BSP.H_WIDTH - i) / BSP.SCREEN_DIST) * (180 / MathF.PI))
            ];
        }

        public void Update()
        {
            this.screen_range = [.. Enumerable.Range(0, (int)BSP.WIDTH)];
            this.upper_clip = [.. Enumerable.Repeat(-1, (int)BSP.WIDTH)];
            this.lower_clip = [.. Enumerable.Repeat((int)BSP.HEIGHT, (int)BSP.WIDTH)];
        }


        public float ScaleFromGlobalAngle(int x, float rw_normal_angle, float rw_distance)
        {
            float x_angle = x_to_angle[x];
            float num = BSP.SCREEN_DIST * MathF.Cos(MathF.PI / 180f * (rw_normal_angle - x_angle - player.angle));
            float den = rw_distance * MathF.Cos(MathF.PI / 180f * x_angle);

            float scale = num / den;
            scale = MathF.Min(MAX_SCALE, MathF.Max(MIN_SCALE, scale));
            return scale;
        }


        public seg seg;
        public float rw_angle1;

        public void DrawSolidWallRange(MapData map, Graphics g, int x1, int x2)
        {
            var seg = this.seg;
            var front_sector = map.seg_front_sector(seg);
            var line = map.seg_linedef(seg);
            var side = map.linedef_front_sidedef(map.seg_linedef(seg));
            var renderer = view_renderer;
            var upper_clip = this.upper_clip;
            var lower_clip = this.lower_clip;

            var wall_texture = map.linedef_front_sidedef(map.seg_linedef(seg)).tex_middle;
            var ceil_texture = front_sector.tex_ceiling;
            var floor_texture = front_sector.tex_floor;
            var light_level = front_sector.light_level;

            float world_front_z1 = front_sector.height_ceiling - player.height;
            float world_front_z2 = front_sector.height_floor - player.height;

            bool b_draw_wall = side.tex_middle != "-";
            bool b_draw_ceil = world_front_z1 > 0;
            bool b_draw_floor = world_front_z2 < 0;

            float rw_normal_angle = map.seg_angle(seg) + 90;
            float offset_angle = rw_normal_angle - this.rw_angle1;

            float hypotenuse = Vector2.Distance(player.pos,
                new Vector2(map.seg_start_vertex(seg).pos_x, map.seg_start_vertex(seg).pos_y));
            float rw_distance = hypotenuse * MathF.Cos(MathF.PI / 180f * offset_angle);

            float rw_scale1 = ScaleFromGlobalAngle(x1, rw_normal_angle, rw_distance);
            float rw_scale_step = 0;
            if (x1 < x2)
            {
                float scale2 = ScaleFromGlobalAngle(x2, rw_normal_angle, rw_distance);
                rw_scale_step = (scale2 - rw_scale1) / (x2 - x1);
            }

            float wall_y1 = BSP.H_HEIGHT - world_front_z1 * rw_scale1;
            float wall_y1_step = -rw_scale_step * world_front_z1;

            float wall_y2 = BSP.H_HEIGHT - world_front_z2 * rw_scale1;
            float wall_y2_step = -rw_scale_step * world_front_z2;

            for (int x = x1; x <= x2; x++)
            {
                float draw_wall_y1 = wall_y1 - 1;
                float draw_wall_y2 = wall_y2;

                if (b_draw_ceil)
                {
                    int cy1 = upper_clip[x] + 1;
                    int cy2 = (int)MathF.Min(draw_wall_y1 - 1, lower_clip[x] - 1);
                    renderer.DrawVLine(g, x, cy1, cy2, ceil_texture, light_level);
                }

                if (b_draw_wall)
                {
                    int wy1 = (int)MathF.Max(draw_wall_y1, upper_clip[x] + 1);
                    int wy2 = (int)MathF.Min(draw_wall_y2, lower_clip[x] - 1);
                    renderer.DrawVLine(g, x, wy1, wy2, wall_texture, light_level);
                }

                if (b_draw_floor)
                {
                    int fy1 = (int)MathF.Max(draw_wall_y2 + 1, upper_clip[x] + 1);
                    int fy2 = lower_clip[x] - 1;
                    renderer.DrawVLine(g, x, fy1, fy2, floor_texture, light_level);
                }

                wall_y1 += wall_y1_step;
                wall_y2 += wall_y2_step;
            }
        }

        public void DrawPortalWallRange(MapData map, Graphics g, int x1, int x2)
        {
            var seg = this.seg;
            var front_sector = map.seg_front_sector(seg);
            var back_sector = map.seg_back_sector(seg);
            var line = map.seg_linedef(seg);
            var side = map.linedef_front_sidedef(map.seg_linedef(seg));
            var renderer = view_renderer;
            var upper_clip = this.upper_clip;
            var lower_clip = this.lower_clip;

            var upper_wall_texture = side.tex_upper;
            var lower_wall_texture = side.tex_lower;
            var tex_ceil_id = front_sector.tex_ceiling;
            var tex_floor_id = front_sector.tex_floor;
            var light_level = front_sector.light_level;

            float world_front_z1 = front_sector.height_ceiling - player.height;
            float world_back_z1 = (float)(back_sector?.height_ceiling - player.height);
            float world_front_z2 = front_sector.height_floor- player.height;
            float world_back_z2 = (float)(back_sector?.height_floor - player.height);

            bool b_draw_upper_wall = side.tex_upper != "-" && world_back_z1 < world_front_z1;
            bool b_draw_ceil = world_front_z1 >= 0;

            bool b_draw_lower_wall = side.tex_lower != "-" && world_back_z2 > world_front_z2;
            bool b_draw_floor = world_front_z2 <= 0;

            if (!b_draw_upper_wall && !b_draw_ceil && !b_draw_lower_wall && !b_draw_floor)
                return;

            float rw_normal_angle = map.seg_angle(seg) + 90;
            float offset_angle = rw_normal_angle - this.rw_angle1;

            float hypotenuse = Vector2.Distance(player.pos,
                new Vector2(map.seg_start_vertex(seg).pos_x, map.seg_start_vertex(seg).pos_y));
            float rw_distance = hypotenuse * MathF.Cos(MathF.PI / 180f * offset_angle);

            float rw_scale = ScaleFromGlobalAngle(x1, rw_normal_angle, rw_distance);
            float rw_scale_step = 0;
            if (x2 > x1)
            {
                float scale2 = ScaleFromGlobalAngle(x2, rw_normal_angle, rw_distance);
                rw_scale_step = (scale2 - rw_scale) / (x2 - x1);
            }

            float wall_y1 = BSP.H_HEIGHT - world_front_z1 * rw_scale;
            float wall_y1_step = -rw_scale_step * world_front_z1;
            float wall_y2 = BSP.H_HEIGHT - world_front_z2 * rw_scale;
            float wall_y2_step = -rw_scale_step * world_front_z2;

            float portal_y1 = wall_y2;
            float portal_y1_step = wall_y2_step;
            if (b_draw_upper_wall && world_back_z1 > world_front_z2)
            {
                portal_y1 = BSP.H_HEIGHT - world_back_z1 * rw_scale;
                portal_y1_step = -rw_scale_step * world_back_z1;
            }

            float portal_y2 = wall_y1;
            float portal_y2_step = wall_y1_step;
            if (b_draw_lower_wall && world_back_z2 < world_front_z1)
            {
                portal_y2 = BSP.H_HEIGHT - world_back_z2 * rw_scale;
                portal_y2_step = -rw_scale_step * world_back_z2;
            }

            for (int x = x1; x <= x2; x++)
            {
                float draw_wall_y1 = wall_y1 - 1;
                float draw_wall_y2 = wall_y2;

                if (b_draw_upper_wall)
                {
                    float draw_upper_wall_y1 = wall_y1 - 1;
                    float draw_upper_wall_y2 = portal_y1;

                    if (b_draw_ceil)
                    {
                        int cy1 = upper_clip[x] + 1;
                        int cy2 = (int)MathF.Min(draw_wall_y1 - 1, lower_clip[x] - 1);
                        renderer.DrawVLine(g, x, cy1, cy2, tex_ceil_id, light_level);
                    }

                    int wy1 = (int)MathF.Max(draw_upper_wall_y1, upper_clip[x] + 1);
                    int wy2 = (int)MathF.Min(draw_upper_wall_y2, lower_clip[x] - 1);
                    renderer.DrawVLine(g, x, wy1, wy2, upper_wall_texture, light_level);

                    if (upper_clip[x] < wy2)
                        upper_clip[x] = wy2;

                    portal_y1 += portal_y1_step;
                }

                if (b_draw_ceil)
                {
                    int cy1 = upper_clip[x] + 1;
                    int cy2 = (int)MathF.Min(draw_wall_y1 - 1, lower_clip[x] - 1);
                    renderer.DrawVLine(g, x, cy1, cy2, tex_ceil_id, light_level);

                    if (upper_clip[x] < cy2)
                        upper_clip[x] = cy2;
                }

                if (b_draw_lower_wall)
                {
                    if (b_draw_floor)
                    {
                        int fy1 = (int)MathF.Max(draw_wall_y2 + 1, upper_clip[x] + 1);
                        int fy2 = lower_clip[x] - 1;
                        renderer.DrawVLine(g, x, fy1, fy2, tex_floor_id, light_level);
                    }

                    float draw_lower_wall_y1 = portal_y2 - 1;
                    float draw_lower_wall_y2 = wall_y2;

                    int wy1 = (int)MathF.Max(draw_lower_wall_y1, upper_clip[x] + 1);
                    int wy2 = (int)MathF.Min(draw_lower_wall_y2, lower_clip[x] - 1);
                    renderer.DrawVLine(g, x, wy1, wy2, lower_wall_texture, light_level);

                    if (lower_clip[x] > wy1)
                        lower_clip[x] = wy1;

                    portal_y2 += portal_y2_step;
                }

                if (b_draw_floor)
                {
                    int fy1 = (int)MathF.Max(draw_wall_y2 + 1, upper_clip[x] + 1);
                    int fy2 = lower_clip[x] - 1;
                    renderer.DrawVLine(g, x, fy1, fy2, tex_floor_id, light_level);

                    if (lower_clip[x] > draw_wall_y2 + 1)
                        lower_clip[x] = fy1;
                }

                wall_y1 += wall_y1_step;
                wall_y2 += wall_y2_step;
            }
        }

        public void ClipPortalWalls(MapData map, Graphics g, int x_start, int x_end)
        {
            HashSet<int> curr_wall = [.. Enumerable.Range(x_start, x_end-x_start)];
            //var curr_wall = new HashSet<int>();
            //for (int i = x_start; i < x_end; i++)
            //{
            //    curr_wall.Add(i);
            //}

            var intersection = new HashSet<int>(curr_wall);
            intersection.IntersectWith(screen_range);

            if (intersection.Count > 0)
            {
                if (intersection.SetEquals(curr_wall))
                {
                    DrawPortalWallRange(map, g, x_start, x_end - 1);
                }
                else
                {
                    var arr = new List<int>(intersection);
                    arr.Sort();
                    int x = arr[0];
                    for (int i = 1; i < arr.Count; i++)
                    {
                        int x1 = arr[i - 1];
                        int x2 = arr[i];
                        if (x2 - x1 > 1)
                        {
                            DrawPortalWallRange(map, g, x, x1);
                            x = x2;
                        }
                    }
                    DrawPortalWallRange(map, g, x, arr[arr.Count - 1]);
                }
            }
        }

        public void ClipSolidWalls(MapData map, Graphics g, int x_start, int x_end, ref bool bsp_is_traverse_bsp)
        {
            if (screen_range.Count > 0)
            {
                HashSet<int> curr_wall = [.. Enumerable.Range(x_start, x_end-x_start)];
                //var curr_wall = new HashSet<int>();
                //for (int i = x_start; i < x_end; i++)
                //{
                //    curr_wall.Add(i);
                //}

                var intersection = new HashSet<int>(curr_wall);
                intersection.IntersectWith(screen_range);

                if (intersection.Count > 0)
                {
                    if (intersection.SetEquals(curr_wall))
                    {
                        DrawSolidWallRange(map, g, x_start, x_end - 1);
                    }
                    else
                    {
                        var arr = new List<int>(intersection);
                        arr.Sort();
                        int x = arr[0];
                        for (int i = 1; i < arr.Count; i++)
                        {
                            int x1 = arr[i - 1];
                            int x2 = arr[i];
                            if (x2 - x1 > 1)
                            {
                                DrawSolidWallRange(map, g, x, x1);
                                x = x2;
                            }
                        }
                        DrawSolidWallRange(map, g, x, arr[arr.Count - 1]);
                    }
                    screen_range.ExceptWith(intersection);
                }
            }
            else
            {
                bsp_is_traverse_bsp = false;
            }
        }

        public void ClassifySegment(MapData map, Graphics g, seg segment, int x1, int x2, float rw_angle1, ref bool bsp_is_traverse_bsp)
        {
            this.seg = segment;
            this.rw_angle1 = rw_angle1;

            if (x1 == x2)
                return;

            var back_sector = map.seg_back_sector(segment);
            var front_sector = map.seg_front_sector(segment);

            if (back_sector == null)
            {
                ClipSolidWalls(map, g, x1, x2, ref bsp_is_traverse_bsp);
                return;
            }

            if (front_sector.height_ceiling != back_sector?.height_ceiling ||
                front_sector.height_floor != back_sector?.height_floor)
            {
                ClipPortalWalls(map, g, x1, x2);
                return;
            }

            if (back_sector?.tex_ceiling == front_sector.tex_ceiling &&
                back_sector?.tex_floor == front_sector.tex_floor &&
                back_sector?.light_level == front_sector.light_level &&
                //this.seg.linedef.front_sidedef.middle_texture == "-")
                map.linedef_front_sidedef(map.seg_linedef(this.seg)).tex_middle == "-")
            {
                return;
            }

            ClipPortalWalls(map, g, x1, x2);
        }
    }
}
