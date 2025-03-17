using System;

namespace DOOM
{
    public class ViewRenderer
    {
        private readonly Dictionary<string, Color> colors = [];

        public Color GetColor(string tex, int lightLevel)
        {
            string key = tex + lightLevel.ToString();
            if (!colors.ContainsKey(key))
            {
                int texId = tex.GetHashCode();
                float l = lightLevel / 255f;
                Random random = new Random(texId);
                int rngMin = 50;
                int rngMax = 256;
                int r = (int)(random.Next(rngMin, rngMax) * l);
                int g = (int)(random.Next(rngMin, rngMax) * l);
                int b = (int)(random.Next(rngMin, rngMax) * l);
                Color color = Color.FromArgb(r, g, b);
                colors[key] = color;
            }
            return colors[key];
        }

        public void DrawVLine(Graphics g, int x, int y1, int y2, string tex, int light)
        {
            if (y1 < y2)
            {
                using (Pen pen = new(GetColor(tex, light)))
                {
                    g.DrawLine(pen, x, y1, x, y2);
                }
            }
        }
    }
}

