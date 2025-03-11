using System;
using System.Diagnostics;
using System.Numerics;
using System.Security.Cryptography.Pkcs;
using System.Threading.Tasks;

using DOOM.WAD;

using ModernGL;
using static DOOM.WAD.WADFileTypes;
using static ModernGL.glContext;

//glContext ctx;

//ctx = moderngl.create_context();
//ctx.enable(EnableFlags.DEPTH_TEST | EnableFlags.CULL_FACE | EnableFlags.BLEND);
//ctx.set_clearcolor(System.Drawing.Color.CornflowerBlue);


namespace DOOM
{

    public class Canvas : Form
    {
        public Canvas(Size size, string title, PaintEventHandler renderer)
        {
            this.DoubleBuffered = true;
            this.Size = size;
            this.Text = title;
            this.Paint += renderer;
        }
    }

    public partial class Renderer : Form
    {
        private System.Windows.Forms.Timer renderTimer;
        private Stopwatch stopwatch;
        private float angleX;
        private float angleY;
        private int fps;
        private int frameCount;
        private Point lastMousePosition;

        public Renderer()
        {
            this.DoubleBuffered = true;
            this.Width = 320*5;
            this.Height = 200*5;

            renderTimer = new System.Windows.Forms.Timer();
            renderTimer.Interval = 16; // ~60 FPS
            renderTimer.Tick += RenderTimer_Tick;

            stopwatch = new Stopwatch();
            stopwatch.Start();

            this.KeyDown += MainForm_KeyDown;
            this.MouseMove += MainForm_MouseMove;
            this.MouseDown += MainForm_MouseDown;
            this.Load += MainForm_Load;
        }

        List<linedef> LINEDEFS = [];
        List<Vector2> VERTEXES = [];

        private void MainForm_Load(object? sender, EventArgs e)
        {
            var wadloader = WADLoader.Open("DOOM1.WAD", buffered: true);
            wadloader.TEST();

            //LINEDEFS = wadloader.GetLINEDEFS();
            //VERTEXES = wadloader.GetVERTEXES();

            var map = wadloader.GetMapData("E1M2");

            short minX = short.MaxValue,
                maxX = short.MinValue;
            short minY = short.MaxValue,
                maxY = short.MinValue;
            /*
            VERTEXES.ForEach(v => {
                minX = short.Min(minX, (short)v.X);
                maxX = short.Max(maxX, (short)v.X);
                minY = short.Min(minY, (short)v.Y);
                maxY = short.Max(maxY, (short)v.Y);
            });
            */

            foreach (var v in map.vertexes)
            {
                minX = short.Min(minX, (short)v.pos_x);
                maxX = short.Max(maxX, (short)v.pos_x);
                minY = short.Min(minY, (short)v.pos_y);
                maxY = short.Max(maxY, (short)v.pos_y);
            }

            int WIDTH = Width, HEIGHT = Height;


            float RemapX(float n, float out_min = 30, float out_max = 0)
            {
                out_max = out_max == 0 ? WIDTH - 30 : out_max;
                return (Math.Max(minX, Math.Min(n, maxX)) - minX) * (out_max - out_min) / (maxX - minX) + out_min;
            };

            float RemapY(float n, float out_min = 30, float out_max = 0)
            {
                out_max = out_max == 0 ? HEIGHT - 30 : out_max;
                return HEIGHT - (Math.Max(minY, Math.Min(n, maxY)) - minY) * (out_max - out_min) / (maxY - minY) - out_min;
            };

            /*
            VERTEXES = [.. VERTEXES.Select(v => {
                return new Vector2(RemapX(v.X), RemapY(v.Y));
            })];
            */

            foreach (var line in map.linedefs)
                LINEDEFS.Add(line);

            foreach (var v in map.vertexes)
                VERTEXES.Add(new Vector2(RemapX(v.pos_x), RemapY(v.pos_y)));

            renderTimer.Start();
        }

        private void RenderTimer_Tick(object? sender, EventArgs e)
        {
            this.Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;

            g.Clear(Color.Black);

            frameCount++;
            if (stopwatch.ElapsedMilliseconds >= 1000)
            {
                fps = frameCount;
                frameCount = 0;
                stopwatch.Restart();
            }
            g.DrawString($"FPS: {fps}", this.Font, Brushes.White, 10, 10);

            //for (int i = 0; i < vertices.Length; i++)
            //{
            //    vertices[i] = RotateX(vertices[i], angleX);
            //    vertices[i] = RotateY(vertices[i], angleY);
            //}

            // draw_linedefs
            var idx = 0;
            foreach (var line in LINEDEFS)
            {
                idx++;
                var (p1, p2) = (
                    VERTEXES[line.vertex_id_start],
                    VERTEXES[line.vertex_id_end]
                );
                g.DrawLine(Pens.Orange, p1.X, p1.Y, p2.X, p2.Y);
            }

            foreach (var vex in VERTEXES)
                g.DrawEllipse(Pens.White, vex.X-1, vex.Y-1, 2, 2);
        }

        private void MainForm_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                this.Close();
            }
        }

        private void MainForm_MouseMove(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                angleX += (e.Y - lastMousePosition.Y) * 0.5f;
                angleY += (e.X - lastMousePosition.X) * 0.5f;
                lastMousePosition = e.Location;
                this.Invalidate();
            }
        }

        private void MainForm_MouseDown(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                lastMousePosition = e.Location;
            }
        }
    }

}
