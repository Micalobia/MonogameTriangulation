using Emgu.CV;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;

namespace TriangulateCamera
{
    public class MainGame : Game
    {
        private const int FRAME_SAMPLES = 30;
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;
        private FramerateTracker _frameTracker;
        private BasicEffect _basic;
        private int _seed = 0;
        private int _frameCount = FRAME_SAMPLES;
        private Mat _curFrame = null;
        private Vector3 camoff;
        private int bigcounter = -3000;

        public VideoCapture Camera;

        public Mat NextFrame => Camera.QueryFrame();



        public int ViewWidth => GraphicsDevice.Viewport.Width;
        public int ViewHeight => GraphicsDevice.Viewport.Height;
        public int ScreenWidth => GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width;
        public int ScreenHeight => GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height;
        public int ViewCenterX => ViewWidth >> 1;
        public int ViewCenterY => ViewHeight >> 1;

        public MainGame()
        {
            _graphics = new GraphicsDeviceManager(this)
            {
                IsFullScreen = false,
                PreferredBackBufferWidth = ScreenWidth,
                PreferredBackBufferHeight = ScreenHeight,
                SynchronizeWithVerticalRetrace = false
            };
            Window.IsBorderless = true;
            _graphics.ApplyChanges();
            Content.RootDirectory = "Content";
        }

        protected override void Initialize()
        {
            base.Initialize();
            _frameTracker = new FramerateTracker(FRAME_SAMPLES);
            camoff = new Vector3(320, 240, 0);
            _basic = new BasicEffect(GraphicsDevice)
            {
                VertexColorEnabled = true,
                View = Matrix.CreateLookAt(Vector3.Backward + camoff, Vector3.Zero + camoff, Vector3.Up),
                Projection = Matrix.CreateOrthographic(854, 480, 1f, 10000f)
            };
            IsFixedTimeStep = false;
            Camera = new VideoCapture();
        }
        protected override void LoadContent() => _spriteBatch = new SpriteBatch(GraphicsDevice);

        protected override void UnloadContent()
        {
        }

        protected override void Update(GameTime gameTime)
        {
            KeyboardState k = Keyboard.GetState();
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || k.IsKeyDown(Keys.Escape))
                Exit();
            float time = (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (k.IsKeyDown(Keys.K)) _basic.World *= Matrix.CreateTranslation(-camoff) * Matrix.CreateScale(.99f) * Matrix.CreateTranslation(camoff);
            if (k.IsKeyDown(Keys.I)) _basic.World *= Matrix.CreateTranslation(-camoff) * Matrix.CreateScale(1f / .99f) * Matrix.CreateTranslation(camoff);
            if (k.IsKeyDown(Keys.J)) _basic.World *= Matrix.CreateTranslation(-camoff) * Matrix.CreateRotationZ(1f * time) * Matrix.CreateTranslation(camoff);
            if (k.IsKeyDown(Keys.L)) _basic.World *= Matrix.CreateTranslation(-camoff) * Matrix.CreateRotationZ(-1f * time) * Matrix.CreateTranslation(camoff);
            if (k.IsKeyDown(Keys.W)) _basic.World *= Matrix.CreateTranslation(Vector3.Down * 100 * time);
            if (k.IsKeyDown(Keys.S)) _basic.World *= Matrix.CreateTranslation(Vector3.Up * 100 * time);
            if (k.IsKeyDown(Keys.A)) _basic.World *= Matrix.CreateTranslation(Vector3.Right * 100 * time);
            if (k.IsKeyDown(Keys.D)) _basic.World *= Matrix.CreateTranslation(Vector3.Left * 100 * time);


            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            ++bigcounter;
            Random rng = new Random(_seed);

            _frameTracker.Update(gameTime);
            if (FRAME_SAMPLES - _frameCount++ == 0)
            {
                _frameCount = 0;
                Console.SetCursorPosition(0, 0);
                Console.Write($"{_frameTracker.Framerate,6:0.00}");
                Console.SetCursorPosition(0, 1);
                Console.Write($"{1f / gameTime.ElapsedGameTime.TotalSeconds,6:0.00}");
            }
            GraphicsDevice.Clear(Color.Navy * 0.8f);
            //Vector2[] randomPoints = rng.NextPoissonPointsGrid(ViewWidth, ViewHeight, 40f, 10);
            Mat frame = NextFrame;
            CvInvoke.Flip(frame, frame, Emgu.CV.CvEnum.FlipType.Horizontal);
            CvInvoke.Flip(frame, frame, Emgu.CV.CvEnum.FlipType.Vertical);
            Vector2[] randomPoints = rng.NextPoints(frame.Width, frame.Height, 8192, true, false);
            Triangulator tri = new Triangulator(randomPoints);
            VertexPositionColor[] tris = tri.Examine(ref frame, 4);
            frame.Dispose();
            //VertexPositionColor[] tris = rng.GetRandomHuedTriangles(tri);
            foreach (EffectPass pass in _basic.CurrentTechnique.Passes)
            {
                pass.Apply();
                int n = Math.Min(tris.Length, Math.Max(3, 200000000)) / 3;
                VertexPositionColor[] slice = tris.Slice(0, 3 * n);
                GraphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleList, slice, 0, n);
            }
            base.Draw(gameTime);
        }
    }
}
