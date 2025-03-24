using System;
using System.Drawing.Imaging;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using DOOM.WAD;
using static DOOM.Renderer;
using static DOOM.WAD.WADFile;
using static DOOM.WAD.WADFileTypes;

namespace DOOM
{
    public class ViewRenderer
    {
        // [B, G, R, B, G, R, B, G, R, ...]
        public byte[] framebuffer;
        public Bitmap framebuffer_bitmap;
        private BitmapData framebuffer_bitmapData = new();
        private readonly Dictionary<string, Color> colors = [];
        //private readonly
            public Dictionary<string, WADFile.Texture> textures { get; set; }
        //private readonly
            public WADFile.Palette palette { get; set; }
        //private readonly
            public Dictionary<string, WADFile.Patch> sprites { get; set; } = [];
        private Player player;

        public ViewRenderer(Size size,
            Dictionary<string, WADFile.Texture> textures, WADFile.Palette palette, Player player)
        {
            this.textures = textures;
            this.palette = palette;
            this.player = player;

            framebuffer_bitmap = new Bitmap(size.Width, size.Height, PixelFormat.Format24bppRgb);
            var bitsPerPixel = Bitmap.GetPixelFormatSize(framebuffer_bitmap.PixelFormat);
            int stride = ((size.Width * bitsPerPixel + 31) / 32) * 4;
            framebuffer = new byte[stride * size.Height];
        }
        public Color GetColor(string tex, int lightLevel)
        {
            string key = tex + lightLevel.ToString();
            if (!colors.ContainsKey(key))
            {
                int texId = tex.GetHashCode();
                float l = lightLevel / 255f;
                Random random = new(texId);
                /*
                int rngMin = 50;
                int rngMax = 256;
                int r = (int)(random.Next(rngMin, rngMax) * l);
                int g = (int)(random.Next(rngMin, rngMax) * l);
                int b = (int)(random.Next(rngMin, rngMax) * l);
                Color color = Color.FromArgb(r, g, b);
                */
                Color color = palette[random.Next(0, 256)];
                colors[key] = Color.FromArgb((int)(color.R * l), (int)(color.G * l), (int)(color.B * l));
            }
            return colors[key];
        }

        public void DrawVLine(int x, int y1, int y2, string tex, int light)
        {
            if (y1 < y2)
                DrawColumn(x, y1, y2, GetColor(tex, light));
        }

        public void DrawColumn(int x, int y1, int y2, Color c)
        {
            for (int iy = y1; iy <= y2; iy++)
            {
                var index = (iy * (int)BSP.WIDTH + x) * 3;
                framebuffer[index + 0] = c.R; // Red
                framebuffer[index + 1] = c.G; // Green
                framebuffer[index + 2] = c.B; // Blue
            }
        }

        public void UpdateBitmap()
        {
            framebuffer_bitmap.LockBits(new(Point.Empty, framebuffer_bitmap.Size),
                ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb, framebuffer_bitmapData);
            Marshal.Copy(framebuffer, 0, framebuffer_bitmapData.Scan0, framebuffer.Length);
            framebuffer_bitmap.UnlockBits(framebuffer_bitmapData);
        }

        public readonly string skyId = "F_SKY1";
        public readonly string skyTexName = "SKY1";
        public WADFile.Texture skyTex;// = textures["SKY1"]; // skyTexName
        private readonly float skyInvScale = 160 / BSP.HEIGHT;
        private readonly float skyTexAlt = 100;

        public void DrawFlat(string texId, float lightLevel, int x, int y1, int y2, float worldZ)
        {
            if (y1 < y2)
            {
                if (texId == skyId)
                {
                    float texColumn = 2.2f * (player.angle + SegHandler.x_to_angle[x]);
                    DrawWallCol(skyTexName, texColumn, x, y1, y2, skyTexAlt, skyInvScale, 255); //1.0f
                }
                else
                {
                    //DrawVLine(x, y1, y2, texId, (int)lightLevel);
                    DrawFlatCol(texId, x, y1, y2, lightLevel, worldZ, player.angle, player.pos.X, player.pos.Y);
                }
            }
        }

        public void DrawFlatCol(string texId, int x, int y1, int y2, float lightLevel, float worldZ, float playerAngle, float playerX, float playerY)
        {
            var flatTex = textures[texId];

            float playerDirX = MathF.Cos((MathF.PI / 180) * playerAngle);
            float playerDirY = MathF.Sin((MathF.PI / 180) * playerAngle);

            float l = lightLevel / 255f;
            for (int iy = y1; iy <= y2; iy++)
            {
                float z = BSP.H_WIDTH * worldZ / (BSP.H_HEIGHT - iy);

                float px = playerDirX * z + playerX;
                float py = playerDirY * z + playerY;

                float leftX = -playerDirY * z + px;
                float leftY = playerDirX * z + py;
                float rightX = playerDirY * z + px;
                float rightY = -playerDirX * z + py;

                float dx = (rightX - leftX) / BSP.WIDTH;
                float dy = (rightY - leftY) / BSP.WIDTH;

                int tx = (int)(leftX + dx * x) & 63;
                int ty = (int)(leftY + dy * x) & 63;

                /*
                var tex_col = flatTex.image.GetPixel(tx, ty);
                var index = (iy * (int)BSP.WIDTH + x) * 3;
                framebuffer[index + 0] = (byte)(tex_col.R * l); // Red
                framebuffer[index + 1] = (byte)(tex_col.G * l); // Green
                framebuffer[index + 2] = (byte)(tex_col.B * l); // Blue
                //*/

                var tex_col = flatTex.image_array.AsSpan(ty*flatTex.width*3 + tx * 3, 3);
                var frame = framebuffer.AsSpan((iy * (int)BSP.WIDTH + x) * 3, 3);
                frame[0] = (byte)(tex_col[0] * l); // Red
                frame[1] = (byte)(tex_col[1] * l); // Green
                frame[2] = (byte)(tex_col[2] * l); // Blue
                //tex_col.CopyTo(frame);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int Mod(int a, int b) => (a % b + b) % b;

        public void DrawWallCol(string texture_id, float texCol, int x, int y1, int y2, float texAlt, float invScale, float lightLevel)
        {
            if (y1 < y2)
            {
                var tex = textures[texture_id];
                var (texW, texH) = (tex.width, tex.height);
                int texColInt = Mod((int)texCol, texW);
                float texY = texAlt + (y1 - BSP.H_HEIGHT) * invScale;

                float l = lightLevel / 255f;
                for (int iy = y1; iy <= y2; iy++)
                {
                    /*
                    var tex_col = tex.image.GetPixel(texColInt, Mod((int)texY, texH));
                    var index = (iy * (int)BSP.WIDTH + x) * 3;
                    framebuffer[index + 0] = (byte)(tex_col.R * l); // Red
                    framebuffer[index + 1] = (byte)(tex_col.G * l); // Green
                    framebuffer[index + 2] = (byte)(tex_col.B * l); // Blue
                    //*/

                    var tx = texColInt; var ty = Mod((int)texY, texH);
                    var tex_col = tex.image_array.AsSpan(ty * tex.width * 3 + tx * 3, 3);
                    var frame = framebuffer.AsSpan((iy * (int)BSP.WIDTH + x) * 3, 3);
                    frame[0] = (byte)(tex_col[0] * l); // Red
                    frame[1] = (byte)(tex_col[1] * l); // Green
                    frame[2] = (byte)(tex_col[2] * l); // Blue
                    //tex_col.CopyTo(frame);

                    texY += invScale;
                }
            }
        }

        public void DrawSprite(Graphics g)
        {
            var bitmap = sprites["SHTGA0"].image;
            int pos_x = (int)BSP.H_WIDTH - bitmap.Width / 2;
            int pos_y = (int)BSP.HEIGHT - bitmap.Height;
            g.DrawImage(bitmap, pos_x, pos_y);
        }

        public void DrawSeg(seg seg, int id)
        {
            return;
            /*
            var v1 = VERTEXES[seg.vertex_id_start];
            var v2 = VERTEXES[seg.vertex_id_end];
            g.DrawLine(Pens.Green, RemapX(v1.X), RemapY(v1.Y), RemapX(v2.X), RemapY(v2.Y));
            //*/
        }

        public void DrawBBox(node.bounding_box bbox, Color color)
        {
            return;
            /*
            float x = RemapX(bbox.left);
            float y = RemapY(bbox.top);
            float w = RemapX(bbox.right) - x;
            float h = RemapY(bbox.bottom) - y;
            g.DrawRectangle(new Pen(color, 0.5f), x, y, w, h);
            //*/
        }

        public void DrawNode(Graphics g, int nodeId)
        {
            /*
            var node = NODES[nodeId];
            var bboxFront = node.right; //front;
            var bboxBack = node.left; //back;
            DrawBBox(g, bboxFront, Color.Green);
            DrawBBox(g, bboxBack, Color.Red);
            var (x1, y1) = (RemapX(node.partition_x), RemapY(node.partition_y));
            float x2 = RemapX(node.partition_x + node.partition_dx);
            float y2 = RemapY(node.partition_y + node.partition_dy);
            g.DrawLine(new Pen(Color.Blue, 4), x1, y1, x2, y2);
            //*/
        }
    }
}

