using Emgu.CV;
using Emgu.CV.Structure;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Drawing;
using System.IO;
using Color = Microsoft.Xna.Framework.Color;
using Point = System.Drawing.Point;
using Rectangle = Microsoft.Xna.Framework.Rectangle;

namespace MonogameTriangulation
{
    /// <summary>
    /// This is the main type for your game.
    /// </summary>
    public class TheGame : Game
    {
        private GraphicsDeviceManager graphics;
        private SpriteBatch spriteBatch;
        private Texture2D cam;
        private SmartFramerate fps = new SmartFramerate(5);
        private Texture2D frames;
        private int _seed = 0;
        private bool prev = false;
        private VideoCapture capture;
        private VertexPositionColor[] vertices;
        private VertexPositionColor[] tris;
        private Vector3[] points;
        private BasicEffect basic;

        private Random rng = new Random();
        private OpenSimplexNoise noise = new OpenSimplexNoise();

        private float Width => GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width;

        private float Height => GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height;

        private float offset = 0f;

        public TheGame()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
        }

        protected override void Initialize()
        {
            //Color c = new Color(1, 2, 3);
            //Console.WriteLine(c.PackedValue & 0xff);
            //Console.WriteLine((c.PackedValue & 0xff00) >> 0x08);
            //Console.WriteLine((c.PackedValue & 0xff0000) >> 0x10);
            //Console.WriteLine((c.PackedValue & 0xff000000) >> 0x18);
            //ComputeUtility.Run();
            basic = new BasicEffect(GraphicsDevice)
            {
                VertexColorEnabled = true,
                View = Matrix.CreateLookAt(Vector3.Backward * 500, Vector3.Zero, Vector3.Up),
                Projection = Matrix.CreateOrthographic(Width, Height, 1f, 10000f)
            };
            base.Initialize();
            graphics.PreferredBackBufferWidth = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width;
            graphics.PreferredBackBufferHeight = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height;
            graphics.IsFullScreen = true;
            IsFixedTimeStep = false;
            graphics.ApplyChanges();
        }

        protected override void LoadContent()
        {
            spriteBatch = new SpriteBatch(GraphicsDevice);

            int halfwidth = (int)(Width / 2f);
            int halfheight = (int)(Height / 2f);
            points = new Vector3[200];
            for (int i = 0; i < points.Length; i++) points[i] = new Vector3(rng.Next(-halfwidth, halfwidth), rng.Next(-halfheight, halfheight), 0);
            points[0] = new Vector3(-halfwidth, -halfheight, 0);
            points[1] = new Vector3(halfwidth, -halfheight, 0);
            points[2] = new Vector3(-halfwidth, halfheight, 0);
            points[3] = new Vector3(halfwidth, halfheight, 0);
            vertices = points.ToPrimitives(Color.OrangeRed, 7.5f);
            capture = new VideoCapture();
            Mat img = null;
            while (img == null) img = capture.QueryFrame();
            Bitmap bmp = img.ToBitmap();
            cam = bmp.ToTexture(GraphicsDevice);
            bmp.Dispose();
            img.Dispose();
        }

        protected override void UnloadContent()
        {
        }

        protected override void Update(GameTime gameTime)
        {
            KeyboardState k = Keyboard.GetState();
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || k.IsKeyDown(Keys.Escape)) Exit();
            if (k.IsKeyDown(Keys.K)) basic.World *= Matrix.CreateScale(.99f);
            if (k.IsKeyDown(Keys.I)) basic.World *= Matrix.CreateScale(1 / .99f);
            if (k.IsKeyDown(Keys.J)) basic.World *= Matrix.CreateRotationZ(0.04f);
            if (k.IsKeyDown(Keys.L)) basic.World *= Matrix.CreateRotationZ(-0.04f);
            if (k.IsKeyDown(Keys.W)) basic.World *= Matrix.CreateTranslation(Vector3.Down * 3);
            if (k.IsKeyDown(Keys.S)) basic.World *= Matrix.CreateTranslation(Vector3.Up * 3);
            if (k.IsKeyDown(Keys.A)) basic.World *= Matrix.CreateTranslation(Vector3.Right * 3);
            if (k.IsKeyDown(Keys.D)) basic.World *= Matrix.CreateTranslation(Vector3.Left * 3);
            if (k.IsKeyDown(Keys.NumPad5)) ++_seed;
            bool p = k.IsKeyDown(Keys.NumPad6);
            if (!prev && p) ++_seed;
            prev = p;
            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            Mat img = capture.QueryFrame();
            if (img != null)
            {
                CvInvoke.Flip(img, img, Emgu.CV.CvEnum.FlipType.Horizontal);
                CvInvoke.Flip(img, img, Emgu.CV.CvEnum.FlipType.Vertical);
                Bitmap bmp = img.ToBitmap();
                cam.Dispose();
                cam = bmp.ToTexture(GraphicsDevice) ?? cam;
                bmp.Dispose();
            }
            img?.Dispose();
#if DEBUG
            Console.SetCursorPosition(1, 3);
#endif

            //tris = Utility.BowyerWatsonSample(ref cam, 512, 3, _seed);
            Vector2[] rpoints = Utility.RandomPoints(cam.Width, cam.Height, 200, _seed, new Vector2(0, 0));
            Delaunator del = new Delaunator(rpoints);
            tris = del.ToMesh(0);
            //tris = del.ToMesh(cam, 3);

            GraphicsDevice.RasterizerState = new RasterizerState
            {
                CullMode = CullMode.None
            };

            GraphicsDevice.Clear(Color.Navy * 0.4f);
#if DEBUG
            fps.Update(gameTime.ElapsedGameTime.TotalSeconds);
            Console.SetCursorPosition(1, 1);
            Console.Write($"{fps.Framerate,10:0.0}");
#endif
            foreach (EffectPass pass in basic.CurrentTechnique.Passes)
            {
                pass.Apply();
                GraphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleList, tris, 0, tris.Length / 3);
                //GraphicsDevice.DrawUserPrimitives(PrimitiveType.LineList, lines, 0, lines.Length / 2);
                //GraphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleList, vertices, 0, vertices.Length / 3);
            }

            base.Draw(gameTime);
        }
    }
}
