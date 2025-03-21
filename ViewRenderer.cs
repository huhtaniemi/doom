using System;
using System.Runtime.CompilerServices;
using DOOM.WAD;
using static DOOM.Renderer;
using static DOOM.WAD.WADFile;

namespace DOOM
{
    public class ViewRenderer(byte[] framebuffer, Action<Graphics, Pen, int, int, int> fn,
        Dictionary<string, WADFile.Texture> textures, Palette palette, Player player)
    {
        public Action<Graphics, Pen, int, int, int> DrawLine { get; } = fn;

        public byte[] framebuffer = framebuffer;
        private readonly Dictionary<string, Color> colors = [];
        //private readonly
            public Dictionary<string, WADFile.Texture> textures { get; set; } = textures;
        //private readonly
            public Palette palette { get; set; } = palette;
        //private readonly
            public Dictionary<string, WADFile.Patch> sprites { get; set; } = [];

        private Player player = player;

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

        public void DrawVLine(Graphics g, int x, int y1, int y2, string tex, int light)
        {
            if (y1 < y2)
            {
                var c = GetColor(tex, light);
                DrawColumn(x, y1, y2, c);
                /*
                using (var pen = new Pen(c))
                {
                    DrawLine(g, pen, x, y1, y2);
                    //g.DrawLine(pen, x, y1, x, y2);
                }
                */
            }
        }

        public void DrawSprite(Graphics g)
        {
            var bitmap = sprites["SHTGA0"].image;
            int pos_x = (int)BSP.H_WIDTH - bitmap.Width / 2;
            int pos_y = (int)BSP.HEIGHT - bitmap.Height;
            g.DrawImage(bitmap, pos_x, pos_y);
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int Mod(int a, int b) => (a % b + b) % b;

        public void DrawWallCol(Bitmap tex, float texCol, int x, int y1, int y2, float texAlt, float invScale, float lightLevel)
        {
            if (y1 < y2)
            {
                var (texW, texH) = (tex.Width, tex.Height);
                int texColInt = Mod((int)texCol, texW);
                float texY = texAlt + (y1 - BSP.H_HEIGHT) * invScale;

                float l = lightLevel / 255f;
                for (int iy = y1; iy <= y2; iy++)
                {
                    var tex_col = tex.GetPixel(texColInt, Mod((int)texY, texH));
                    var index = (iy * (int)BSP.WIDTH + x) * 3;
                    framebuffer[index + 0] = (byte)(tex_col.R * l); // Red
                    framebuffer[index + 1] = (byte)(tex_col.G * l); // Green
                    framebuffer[index + 2] = (byte)(tex_col.B * l); // Blue
                    texY += invScale;
                }
            }
        }
    }
}

