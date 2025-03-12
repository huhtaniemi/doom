using System;
using System.Diagnostics;
using System.Numerics;

using DOOM.WAD;
using static DOOM.WAD.WADFile;
using static DOOM.WAD.WADFileTypes;

using ModernGL;
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
        public class Player
        {
            public Vector2 pos { get; set; }
            public float angle { get; set; }

            const float PLAYER_SPEED = 0.3f;
            const float PLAYER_ROT_SPEED = 0.12f;

            public void Control(float dt)
            {
                float speed = PLAYER_SPEED * dt;
                float rotSpeed = PLAYER_ROT_SPEED * dt;

                if (Keyboard.IsKeyDown(Keys.Left))
                    angle += rotSpeed;
                if (Keyboard.IsKeyDown(Keys.Right))
                    angle -= rotSpeed;

                Vector2 inc = Vector2.Zero;
                if (Keyboard.IsKeyDown(Keys.A))
                    inc += new Vector2(0, speed);
                if (Keyboard.IsKeyDown(Keys.D))
                    inc += new Vector2(0, -speed);
                if (Keyboard.IsKeyDown(Keys.W))
                    inc += new Vector2(speed, 0);
                if (Keyboard.IsKeyDown(Keys.S))
                    inc += new Vector2(-speed, 0);

                const float DIAG_MOVE_CORR = 0.7071f; // 1/sqrt(2) for diagonal movement correction
                if (inc.X != 0 && inc.Y != 0)
                    inc *= DIAG_MOVE_CORR;

                inc = RotateInPlace(inc, angle);
                pos += inc;
            }

            //public static void RotateInPlace(ref this Vector2 vector, float angleDegrees)
            public Vector2 RotateInPlace(Vector2 vector, float angleDegrees)
            {
                float angleRadians = (MathF.PI / 180f) * angleDegrees;
                float cos = MathF.Cos(angleRadians);
                float sin = MathF.Sin(angleRadians);
                return new(vector.X * cos - vector.Y * sin, vector.X * sin + vector.Y * cos);
            }

            public static class Keyboard
            {
                private static HashSet<Keys> keys = new HashSet<Keys>();
                public static void KeyDown(Keys key) => keys.Add(key);
                public static void KeyUp(Keys key) => keys.Remove(key);
                public static bool IsKeyDown(Keys key) => keys.Contains(key);
            }
        }

        private System.Windows.Forms.Timer renderTimer;
        private Stopwatch stopwatch;
        Player player = new();
        private int fps;
        private int frameCount;
        private Point lastMousePosition;

        public Renderer()
        {
            this.DoubleBuffered = true;
            this.Width = 320*5;
            this.Height = 200*5 + 40;

            renderTimer = new System.Windows.Forms.Timer();
            renderTimer.Interval = 16; // ~60 FPS
            renderTimer.Tick += RenderTimer_Tick;

            stopwatch = new Stopwatch();
            stopwatch.Start();

            this.KeyDown += MainForm_KeyDown;
            this.MouseMove += MainForm_MouseMove;
            this.MouseDown += MainForm_MouseDown;
            this.Load += MainForm_Load;

            KeyDown += (sender, e) => Player.Keyboard.KeyDown(e.KeyCode);
            KeyUp += (sender, e) => Player.Keyboard.KeyUp(e.KeyCode);
        }

        List<thing> THINGS = [];
        List<linedef> LINEDEFS = [];
        List<Vector2> VERTEXES = [];
        List<node> NODES = [];

        BSP bsp = new();
        WADFile? wadloader;
        internal MapData map => wadloader.GetMapData("E1M1");

        short minX = short.MaxValue,
            maxX = short.MinValue;
        short minY = short.MaxValue,
            maxY = short.MinValue;

        float RemapX(float n, float out_min = 30, float out_max = 0)
        {
            out_max = out_max == 0 ? this.Width - 30 : out_max;
            return (float)(Math.Max(minX, Math.Min(n, maxX)) - minX) * (out_max - out_min) / (maxX - minX) + out_min;
        }

        float RemapY(float n, float out_min = 30, float out_max = 0)
        {
            var HEIGHT = this.Height - 30;
            out_max = out_max == 0 ? HEIGHT - 30 : out_max;
            return HEIGHT - (float)(Math.Max(minY, Math.Min(n, maxY)) - minY) * (out_max - out_min) / (maxY - minY) - out_min;
        }

        private void MainForm_Load(object? sender, EventArgs e)
        {
            wadloader = WADLoader.Open("DOOM1.WAD", buffered: true);
            wadloader.TEST();

            //LINEDEFS = wadloader.GetLINEDEFS();
            //VERTEXES = wadloader.GetVERTEXES();
            //var map = wadloader.GetMapData("E1M2");

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


            THINGS = [.. map.things];

            player.pos = new(THINGS[0].pos_x, THINGS[0].pos_y);
            player.angle = THINGS[0].angle_facing;

            //foreach (var line in map.linedefs)
            //    LINEDEFS.Add(line);
            LINEDEFS = [.. map.linedefs];

            foreach (var v in map.vertexes)
                VERTEXES.Add(new Vector2((v.pos_x), (v.pos_y)));

            NODES = [.. map.nodes];

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

            player.Control((float)renderTimer.Interval);

            DrawLinedefs(g);
            DrawPlayerPos(g);

            // draw_node(node_id = self.engine.bsp.root_node_id)
            var root_node_id = map.nodes.Length - 1;
            //DrawNode(g, root_node_id);
            bsp.RenderBspNode(map, player, (ushort)root_node_id, (seg, id) => DrawSeg(g, seg, id), (bbox) => DrawBBox(g, bbox, Color.Aquamarine));
        }

        public void DrawSeg(Graphics g, seg seg, int id)
        {
            var v1 = VERTEXES[seg.vertex_id_start];
            var v2 = VERTEXES[seg.vertex_id_end];
            g.DrawLine(Pens.Green, RemapX(v1.X), RemapY(v1.Y), RemapX(v2.X), RemapY(v2.Y));
        }


        public void DrawBBox(Graphics g, node.bounding_box bbox, Color color)
        {
            float x = RemapX(bbox.left);
            float y = RemapY(bbox.top);
            float w = RemapX(bbox.right) - x;
            float h = RemapY(bbox.bottom) - y;
            g.DrawRectangle(new Pen(color, 0.5f), x, y, w, h);
        }

        public void DrawNode(Graphics g, int nodeId)
        {
            var node = NODES[nodeId];
            var bboxFront = node.right; //front;
            var bboxBack = node.left; //back;
            DrawBBox(g, bboxFront, Color.Green);
            DrawBBox(g, bboxBack, Color.Red);
            var (x1, y1) = (RemapX(node.partition_x), RemapY(node.partition_y));
            float x2 = RemapX(node.partition_x + node.partition_dx);
            float y2 = RemapY(node.partition_y + node.partition_dy);
            g.DrawLine(new Pen(Color.Blue, 4), x1, y1, x2, y2);
        }

        public void DrawLinedefs(Graphics g)
        {
            foreach (var line in LINEDEFS)
            {
                var (p1, p2) = (
                    VERTEXES[line.vertex_id_start],
                    VERTEXES[line.vertex_id_end]
                );
                g.DrawLine(Pens.Red, RemapX(p1.X), RemapY(p1.Y), RemapX(p2.X), RemapY(p2.Y));
            }
            /*
            foreach (var v in VERTEXES)
                g.DrawEllipse(Pens.White, RemapX(v.X)-1, RemapY(v.Y)-1, 2, 2);
            */
        }

        public void DrawPlayerPos(Graphics g)
        {
            float x = RemapX(player.pos.X);
            float y = RemapY(player.pos.Y);
            DrawFov(g, x, y);
            g.FillEllipse(new SolidBrush(Color.Orange), x - 8, y - 8, 16, 16);
        }

        public void DrawFov(Graphics g, float px, float py)
        {
            float angle = -player.angle + 90;
            float sinA1 = MathF.Sin((MathF.PI / 180f) * (angle - BSP.H_FOV));
            float cosA1 = MathF.Cos((MathF.PI / 180f) * (angle - BSP.H_FOV));
            float sinA2 = MathF.Sin((MathF.PI / 180f) * (angle + BSP.H_FOV));
            float cosA2 = MathF.Cos((MathF.PI / 180f) * (angle + BSP.H_FOV));

            float lenRay = this.Height;
            float x = player.pos.X;
            float y = player.pos.Y;

            var (x1, y1) = (RemapX(x + lenRay * sinA1), RemapY(y + lenRay * cosA1));
            var (x2, y2) = (RemapX(x + lenRay * sinA2), RemapY(y + lenRay * cosA2));
            g.DrawLine(Pens.Yellow, px, py, x1, y1);
            g.DrawLine(Pens.Yellow, px, py, x2, y2);
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
