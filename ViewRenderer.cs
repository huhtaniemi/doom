using DOOM.WAD;
using System;
using static DOOM.WAD.WADFile;

namespace DOOM
{
    public class ViewRenderer(Action<Graphics, Pen, int, int, int> fn, Palette palette)
    {
        public Action<Graphics, Pen, int, int, int> DrawLine { get; } = fn;

        private readonly Dictionary<string, Color> colors = [];
        //private readonly
            public Palette palette = palette;

        public Dictionary<string, WADFile.Patch> sprites = [];

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
                using (Pen pen = new(GetColor(tex, light)))
                {
                    //DrawLine(pen, x, y1, y2);
                    g.DrawLine(pen, x, y1, x, y2);
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
    }
}

