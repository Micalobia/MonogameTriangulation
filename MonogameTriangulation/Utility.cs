

using Emgu.CV;
using Emgu.CV.Structure;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Color = Microsoft.Xna.Framework.Color;
using Rectangle = System.Drawing.Rectangle;


namespace MonogameTriangulation
{
    public static partial class Utility
    {
        public static float Entropy(params int[] arr)
        {
            Dictionary<int, int> probs = new Dictionary<int, int>();
            for (int i = 0; i < arr.Length; i++) probs[arr[i]] = 0;
            for (int i = 0; i < arr.Length; i++) ++probs[arr[i]];
            int total = probs.Sum(x => x.Value);
            float[] ps = probs.Select(x => (float)x.Value / total).ToArray();
            float p = 0;
            for (int i = 0; i < ps.Length; i++) p -= (float)(ps[i] * Math.Log(ps[i], arr.Length));
            return p;
        }

        public static float RandomFloat(this Random r, float inclusiveMin, float exclusiveMax) => (float)(r.NextDouble() * (exclusiveMax - inclusiveMin) + inclusiveMin);

        public static Vector2[] RandomPoints(float width, float height, int pointcount, int? seed, Vector2? center)
        {
            if (width < 0) throw new ArgumentException("Width cannot be negative", "width");
            if (height < 0) throw new ArgumentException("Height cannot be negative", "height");
            if (pointcount < 4) throw new ArgumentException("Not enough points", "pointcount");
            Random rng;
            if (seed == null) rng = new Random();
            else rng = new Random(seed ?? 0);
            Vector2[] ret = new Vector2[pointcount];
            float cx = center?.X ?? width / 2f;
            float cy = center?.Y ?? height / 2f;
            float left = -cx;
            float right = left + width;
            float top = -cy;
            float bottom = top + height;
            ret[0] = new Vector2(left, top);
            ret[1] = new Vector2(right, top);
            ret[2] = new Vector2(left, bottom);
            ret[3] = new Vector2(right, bottom);
            for (int i = 4; i < ret.Length; i++) ret[i] = new Vector2(rng.RandomFloat(left, right), rng.RandomFloat(top, bottom));
            return ret;
        }


        public static Bitmap ToBitmap(this Mat frame)
        {
            Bitmap bmp = new Bitmap(frame.Width, frame.Height, PixelFormat.Format32bppArgb);
            BitmapData data = bmp.LockBits(new Rectangle(0, 0, frame.Width, frame.Height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
            Image<Rgba, byte> image = new Image<Rgba, byte>(data.Width, data.Height, data.Stride, data.Scan0);
            image.ConvertFrom(frame.ToImage<Rgba, byte>());
            image.Dispose();
            bmp.UnlockBits(data);
            return bmp;
        }

        public static VertexPositionColor[] ToMesh(this Delaunator d, int? seed)
        {
            List<VertexPositionColor> ret = new List<VertexPositionColor>();
            Random rng;
            if (seed == null) rng = new Random();
            else rng = new Random(seed ?? 0);
            d.ForEachTriangle(add);
            return ret.ToArray();

            void add(ITriangle t)
            {
                Vector2 g = Vector2.Zero;
                foreach (Vector2 v in t.Points) g += v;
                g /= 3f;
                foreach (Vector2 v in t.Points) ret.Add(new VertexPositionColor(new Vector3(v, 0), FromHue(180f / (float)Math.PI * (float)Math.Atan2(g.Y - 450f / 2f, g.X - 800f / 2f))));
            }
        }

        public static Color RandomColor(this Random r) => new Color(r.Next(255), r.Next(255), r.Next(255));
        public static Color RandomHue(this Random r) => FromHue((float)r.NextDouble() * 360f);

        public static unsafe Texture2D ToTexture(this Bitmap bmp, GraphicsDevice device)
        {
            Texture2D tex = new Texture2D(device, bmp.Width, bmp.Height, false, SurfaceFormat.Color);
            Color[] col = new Color[bmp.Width * bmp.Height];
            Rectangle rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
            BitmapData data = bmp.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            uint* ptr = (uint*)data.Scan0;
            for (int i = 0; i < col.Length; i++, ptr++) col[i] = new Color(*ptr);
            bmp.UnlockBits(data);
            tex.SetData(col);
            return tex;
        }

        public static VertexPositionColor[] ToPrimitives(this IEnumerable<Vector3> vectors, Color color, float size)
        {
            float half = size / 2f;
            VertexPositionColor[] ret = new VertexPositionColor[vectors.Count() * 6];

            int j = 0;
            foreach (Vector3 v in vectors)
            {
                Vector3 topleft = v + new Vector3(-half, -half, 0);
                Vector3 topRight = v + new Vector3(half, -half, 0);
                Vector3 bottomLeft = v + new Vector3(-half, half, 0);
                Vector3 bottomRight = v + new Vector3(half, half, 0);
                ret[j].Position = topleft;
                ret[j + 1].Position = topRight;
                ret[j + 2].Position = bottomLeft;
                ret[j + 3].Position = bottomRight;
                ret[j + 4].Position = topRight;
                ret[j + 5].Position = bottomLeft;
                j += 6;
            }
            for (int i = 0; i < ret.Length; i++) ret[i].Color = color;
            return ret;
        }

        private struct Triangle : IEquatable<Triangle>
        {
            public int a;
            public int b;
            public int c;
            public Triangle(int a, int b, int c)
            {
                this.a = a;
                this.b = b;
                this.c = c;
            }
            public bool Has(int d) => a == d || b == d || c == d;
            public bool Has(Edge e) => Has(ref e);
            public bool Has(ref Edge e)
            {
                Edge[] ed = Edges;
                if (e.Is(ed[0])) return true;
                if (e.Is(ed[1])) return true;
                if (e.Is(ed[2])) return true;
                return false;
            }
            public Edge[] Edges => new Edge[] { new Edge(a, b), new Edge(b, c), new Edge(c, a) };
            public override string ToString()
            {
                string s = a.ToString();
                s += ",";
                s += b.ToString();
                s += ",";
                s += c.ToString();
                return s;
                //a.ToString() + "," + b.ToString() + "," + c.ToString();
            }
            public override bool Equals(object obj) => obj is Triangle && Equals((Triangle)obj);
            public bool Equals(Triangle other) => this == other;

            public override int GetHashCode()
            {
                int hashCode = 1474027755;
                hashCode = hashCode * -1521134295 + a.GetHashCode();
                hashCode = hashCode * -1521134295 + b.GetHashCode();
                hashCode = hashCode * -1521134295 + c.GetHashCode();
                return hashCode;
            }

            public static bool operator ==(Triangle a, Triangle b) => b.Has(a.a) && b.Has(a.b) && b.Has(a.c);
            public static bool operator !=(Triangle a, Triangle b) => !(a == b);
        }

        private struct Edge
        {
            public int a;
            public int b;
            public Edge(int a, int b)
            {
                this.a = a;
                this.b = b;
            }
            public bool Is(Edge e) => e.a == a && e.b == b || e.a == b && e.b == a;
            public override string ToString() => $"{a},{b}";
        }

        private struct CircumFunc
        {
            public Vector2 CirCen;
            public Vector2 TruCen;
            public float CirSqr;
            public float TruSqr;
            public CircumFunc(Vector2 a, Vector2 b, Vector2 c)
            {
                CirCen = circumcenter(a, b, c);
                TruCen = (a + b + c) / 3f;
                CirSqr = (a - CirCen).LengthSquared();
                TruSqr = (a - TruCen).LengthSquared();
            }
            public CircumFunc(List<Vector2> vecs, Triangle t) : this(vecs[t.a], vecs[t.b], vecs[t.c]) { }
            public CircumFunc(Vector2[] vecs, Triangle t) : this(vecs[t.a], vecs[t.b], vecs[t.c]) { }
            public unsafe CircumFunc(Vector2* vecs, Triangle t) : this(vecs[t.a], vecs[t.b], vecs[t.c]) { }
            public bool EvalCir(Vector2 i) => (CirCen - i).LengthSquared() < CirSqr;
            public bool EvalTru(Vector2 i) => (TruCen - i).LengthSquared() < TruSqr;
            public override string ToString() => $"{CirCen}; {CirSqr}";
        }

        public static VertexPositionColor[] BowyerWatson(this IEnumerable<Vector2> vectors)
        {
            //vectors = vectors.OrderBy(x => Math.Atan2(x.Y, x.X)).ToArray();
            List<Vector2> vecs = new List<Vector2>() { new Vector2(-8000, 8000), new Vector2(8000, 8000), new Vector2(0, -8000) };
            vecs.AddRange(vectors);
            List<Triangle> tris = new List<Triangle>();
            List<CircumFunc> trifuncs = new List<CircumFunc>();
            tris.Add(new Triangle(0, 1, 3));
            tris.Add(new Triangle(1, 2, 3));
            tris.Add(new Triangle(2, 0, 3));
            foreach (Triangle t in tris) trifuncs.Add(new CircumFunc(vecs, t));
            DateTime start = DateTime.Now;
            for (int i = 4; i < vecs.Count; i++)
            {
                List<Triangle> badTris = new List<Triangle>();
                for (int j = 0; j < tris.Count; j++)
                    if (trifuncs[j].EvalCir(vecs[i]))
                        badTris.Add(tris[j]);
                List<Edge> polygon = new List<Edge>();
                for (int j = 0; j < badTris.Count; j++)
                {
                    Edge[] edges = badTris[j].Edges;
                    for (int k = 0; k < 3; k++)
                    {
                        if (!badTris.Any(x => x != badTris[j] && x.Has(edges[k]))) polygon.Add(edges[k]);
                    }
                }
                for (int j = 0; j < badTris.Count; j++)
                {
                    int k = tris.IndexOf(badTris[j]);
                    tris.RemoveAt(k);
                    trifuncs.RemoveAt(k);
                }
                for (int j = 0; j < polygon.Count; j++)
                {
                    Triangle t = new Triangle(polygon[j].a, polygon[j].b, i);
                    tris.Add(t);
                    trifuncs.Add(new CircumFunc(vecs, t));
                }
            }
            tris.RemoveAll(x => x.Has(0));
            tris.RemoveAll(x => x.Has(1));
            tris.RemoveAll(x => x.Has(2));
            List<VertexPositionColor> ret = new List<VertexPositionColor>();
            float mag = new Vector2(vectors.Max(x => Math.Abs(x.X)), vectors.Max(x => Math.Abs(x.Y))).Length();
            for (int i = 0; i < tris.Count; i++)
            {
                Triangle t = tris[i];

                //Vector3 g = (vecs[t.a] + vecs[t.b] + vecs[t.c]) / 3f;
                Vector2 g = circumcenter(vecs[t.a], vecs[t.b], vecs[t.c]);
                float scale = g.Length() / mag;
                Color a = FromHue(MathHelper.ToDegrees((float)Math.Atan2(vecs[t.a].Y, vecs[t.a].X)));
                Color b = FromHue(MathHelper.ToDegrees((float)Math.Atan2(vecs[t.b].Y, vecs[t.b].X)));
                Color c = FromHue(MathHelper.ToDegrees((float)Math.Atan2(vecs[t.c].Y, vecs[t.c].X)));
                Color d = FromHue(MathHelper.ToDegrees((float)Math.Atan2(g.Y, g.X)));
                d = Color.Lerp(d, d * 0.5f, scale);
                ret.Add(new VertexPositionColor(new Vector3(vecs[t.a], 0), d));
                ret.Add(new VertexPositionColor(new Vector3(vecs[t.b], 0), d));
                ret.Add(new VertexPositionColor(new Vector3(vecs[t.c], 0), d));
            }
#if DEBUG
            Console.Write((DateTime.Now - start).TotalMilliseconds);
            Console.Write('\r');
#endif  
            return ret.ToArray();
        }

        private static bool InTri(Vector2 a, Vector2 b, Vector2 c, Vector2 p)
        {
            float d1, d2, d3;
            bool has_neg, has_pos;

            d1 = sign(p, a, b);
            d2 = sign(p, b, c);
            d3 = sign(p, c, a);

            has_neg = d1 < 0 || d2 < 0 || d3 < 0;
            has_pos = d1 > 0 || d2 > 0 || d3 > 0;

            return !(has_neg && has_pos);
        }

        //private static Color Average(this Color[] cols, Triangle t, List<Vector2> vecs, )

        private static Color Average(this Color[] cols, Triangle t, List<Vector2> vecs, CircumFunc func, int width, int res)
        {
            Vector2[] points = new Vector2[] { vecs[t.a], vecs[t.b], vecs[t.c] };
            int left = (int)points.Min(x => x.X);
            int top = (int)points.Min(x => x.Y);
            int right = (int)points.Max(x => x.X);
            int bottom = (int)points.Max(x => x.Y);
            int br = bottom * width + right;
            int r = 0, g = 0, b = 0, count = 0;
            int toggle = 0;
            for (int i = top; i < bottom; ++i)
            {
                for (int j = left; j < right; ++j)
                {
                    if (++toggle % res != 0) continue;
                    if (func.EvalTru(new Vector2(j, i)))
                    {
                        Color c = cols[i * width + j];
                        r += c.R;
                        g += c.G;
                        b += c.B;
                        ++count;
                    }
                }
            }
            if (count == 0) return cols[(int)func.TruCen.Y * width + (int)func.TruCen.X];
            r /= count;
            g /= count;
            b /= count;
            return new Color(r, g, b);
        }

        private static Color Average(this Color[] cols, Triangle t, Vector2[] vecs, int width, int res)
        {
            Vector2[] points = new Vector2[] { vecs[t.a], vecs[t.b], vecs[t.c] };
            int left = (int)points.Min(x => x.X);
            int top = (int)points.Min(x => x.Y);
            int right = (int)points.Max(x => x.X);
            int bottom = (int)points.Max(x => x.Y);
            int r = 0, g = 0, b = 0, count = 0;
            Parallel.For(top, bottom, i =>
            {
                if (i % res != 0) return;
                Parallel.For(left, right, j =>
                {
                    if (j % res != 0) return;
                    Vector2 v = new Vector2(j, i);
                    if (InTri(points[0], points[1], points[2], v))
                    {
                        Color c = cols[i * width + j];
                        r += c.R;
                        g += c.G;
                        b += c.B;
                        ++count;
                    }
                });
            });
            if (count == 0)
            {
                int x = (int)points.Average(_ => _.X);
                int y = (int)points.Average(_ => _.Y);
                return cols[y * width + x];
            }
            r /= count;
            g /= count;
            b /= count;
            return new Color(r, g, b);
        }

        private static Color Average(this Color[] cols, Triangle t, List<Vector2> vecs, int width, int res)
        {
            Vector2[] points = new Vector2[] { vecs[t.a], vecs[t.b], vecs[t.c] };
            int left = (int)points.Min(x => x.X);
            int top = (int)points.Min(x => x.Y);
            int right = (int)points.Max(x => x.X);
            int bottom = (int)points.Max(x => x.Y);
            int r = 0, g = 0, b = 0, count = 0;
            for (int i = top; i < bottom; i += res)
            {
                for (int j = left; j < right; j += res)
                {
                    Vector2 v = new Vector2(j, i);
                    if (InTri(points[0], points[1], points[2], v))
                    {
                        Color c = cols[i * width + j];
                        r += c.R;
                        g += c.G;
                        b += c.B;
                        ++count;
                    }
                }
            }
            if (count == 0) return Color.Black;
            r /= count;
            g /= count;
            b /= count;
            return new Color(r, g, b);
        }
        private static unsafe Color Average(Color* cols, Triangle t, List<Vector2> vecs, int width, int res)
        {
            Vector2[] points = new Vector2[] { vecs[t.a], vecs[t.b], vecs[t.c] };
            int left = (int)points.Min(x => x.X);
            int top = (int)points.Min(x => x.Y);
            int right = (int)points.Max(x => x.X);
            int bottom = (int)points.Max(x => x.Y);
            int br = bottom * width + right;
            int r = 0, g = 0, b = 0, count = 0;
            for (int i = top; i < bottom; i += res)
            {
                for (int j = left; j < right; j += res)
                {
                    Vector2 v = new Vector2(j, i);
                    if (InTri(points[0], points[1], points[2], v))
                    {
                        Color c = cols[i * width + j];
                        r += c.R;
                        g += c.G;
                        b += c.B;
                        ++count;
                    }
                }
            }
            if (count == 0) return Color.Black;
            r /= count;
            g /= count;
            b /= count;
            return new Color(r, g, b);
        }

        public static unsafe VertexPositionColor[] BowyerWatsonSample(ref Texture2D texture, int pointCount, int res = 1, int seed = 0)
        {
            Random rng = new Random(seed);
            OpenSimplexNoise noise = new OpenSimplexNoise(seed);
            Vector2[] vecs = new Vector2[pointCount + 3];
            vecs[0] = new Vector2(-8000, 8000);
            vecs[1] = new Vector2(8000, 8000);
            vecs[2] = new Vector2(0, -8000);
            vecs[3] = Vector2.Zero;
            vecs[4] = new Vector2(texture.Width, 0);
            vecs[5] = new Vector2(0, texture.Height);
            vecs[6] = new Vector2(texture.Width, texture.Height);
            for (int i = 7; i < pointCount; i++)
                vecs[i] = new Vector2(rng.Next(0, texture.Width), rng.Next(0, texture.Height));
            int tricount = pointCount * 2 + 1;
            List<Triangle> tris = new List<Triangle>(tricount);
            List<CircumFunc> trifuncs = new List<CircumFunc>(tricount);
            int edgeCount = pointCount * 3 + 3;
            tris.Add(new Triangle(0, 1, 3));
            tris.Add(new Triangle(1, 2, 3));
            tris.Add(new Triangle(2, 0, 3));
            foreach (Triangle t in tris) trifuncs.Add(new CircumFunc(vecs, t));
            Stopwatch sw = Stopwatch.StartNew();
            for (int i = 4; i < vecs.Length; i++)
            {
                List<Triangle> badTris = new List<Triangle>(tricount);
                for (int j = 0; j < tris.Count; j++)
                    if (trifuncs[j].EvalCir(vecs[i]))
                        badTris.Add(tris[j]);
                List<Edge> polygon = new List<Edge>();
                for (int j = 0; j < badTris.Count; j++)
                {
                    Edge[] edges = badTris[j].Edges;
                    for (int k = 0; k < 3; k++)
                    {
                        if (!badTris.Any(x => x != badTris[j] && x.Has(edges[k]))) polygon.Add(edges[k]);
                    }
                }
                for (int j = 0; j < badTris.Count; j++)
                {
                    int k = tris.IndexOf(badTris[j]);
                    tris.RemoveAt(k);
                    trifuncs.RemoveAt(k);
                }
                for (int j = 0; j < polygon.Count; j++)
                {
                    Triangle t = new Triangle(polygon[j].a, polygon[j].b, i);
                    tris.Add(t);
                    trifuncs.Add(new CircumFunc(vecs, t));
                }
            }
            for (int i = tris.Count - 1; i >= 0; --i)
            {
                if (tris[i].Has(0) || tris[i].Has(1) || tris[i].Has(2))
                {
                    tris.RemoveAt(i);
                    trifuncs.RemoveAt(i);
                }
            }
            VertexPositionColor[] ret = new VertexPositionColor[tris.Count * 3];
            int width = texture.Width;
            Color[] cols = new Color[width * texture.Height];
            texture.GetData(cols);
            Vector2 half = new Vector2(width / 2, texture.Height / 2);
            Parallel.For(0, tris.Count, i =>
                {
                    Triangle t = tris[i];
                    Color d = Average(cols, t, vecs, width, res);
                    ret[3 * i] = new VertexPositionColor(new Vector3(vecs[t.a] - half, 0), d);
                    ret[3 * i + 1] = new VertexPositionColor(new Vector3(vecs[t.b] - half, 0), d);
                    ret[3 * i + 2] = new VertexPositionColor(new Vector3(vecs[t.c] - half, 0), d);
                });
            sw.Stop();
            millis += sw.ElapsedMilliseconds;
            ++ticks;
#if DEBUG
            Console.Write($"{millis / ticks,10}");
#endif
            return ret;
        }

        //public static VertexPositionColor[] Sample(this Delaunator del, Texture2D tex)
        //{
        //    Color[] cols = new Color[tex.Width * tex.Height];
        //    tex.GetData(cols);
        //    Parallel.For(0, del.Triangles.Length / 3, i =>
        //    {
        //        Vector2[] points = del.GetTrianglePoints(i);
        //        Average(point)
        //    });
        //}

        public static long millis = 0;
        public static long ticks = 0;

        public static Color FromHue(float value)
        {
            value += 180f;
            value %= 360;
            int id = (int)(value / 60);
            value %= 60;
            value /= 60;
            switch (id)
            {
                case 0: return Color.Lerp(Color.Red, Color.Yellow, value);
                case 1: return Color.Lerp(Color.Yellow, Color.Green, value);
                case 2: return Color.Lerp(Color.Green, Color.Aqua, value);
                case 3: return Color.Lerp(Color.Aqua, Color.Blue, value);
                case 4: return Color.Lerp(Color.Blue, Color.Magenta, value);
                default: return Color.Lerp(Color.Magenta, Color.Red, value);
            }
        }

        public static VertexPositionColor[] BasicTriangulation(this Vector2[] vectors)
        {
            Random rng = new Random(0);
            vectors = vectors.OrderBy(x => Math.Atan2(x.Y, x.X)).ToArray();
            List<VertexPositionColor> ret = new List<VertexPositionColor>();
            DateTime start = DateTime.Now;
            for (int i = 0; i < vectors.Length; i++)
            {
                for (int j = i + 1; j < vectors.Length; j++)
                    for (int k = j + 1; k < vectors.Length; k++)
                    {
                        bool valid = true;
                        Vector2 a = vectors[i], b = vectors[j], c = vectors[k];
                        Vector2 cen = circumcenter(a, b, c);
                        float s = (a - cen).LengthSquared();
                        for (int n = 0; n < vectors.Length; n++)
                        {
                            if (n == i) continue;
                            if (n == j) continue;
                            if (n == k) continue;
                            if ((cen - vectors[n]).LengthSquared() <= s)
                            {
                                valid = false;
                                break;
                            }
                        }
                        if (valid)
                        {
                            Color _c = new Color(rng.Next(256), rng.Next(256), rng.Next(256));
                            ret.Add(new VertexPositionColor(new Vector3(a, 0), new Color(rng.Next(256), rng.Next(256), rng.Next(256))));
                            ret.Add(new VertexPositionColor(new Vector3(b, 0), new Color(rng.Next(256), rng.Next(256), rng.Next(256))));
                            ret.Add(new VertexPositionColor(new Vector3(c, 0), new Color(rng.Next(256), rng.Next(256), rng.Next(256))));
                            //rind += 3;
                        }
                    }
            }
#if DEBUG
            Console.Write((DateTime.Now - start).TotalMilliseconds);
            Console.Write('\r');
#endif
            return ret.ToArray();
        }

        private static Vector2 circumcenter(Vector2 a, Vector2 b, Vector2 c)
        {
            float dx = b.X - a.X;
            float dy = b.Y - a.Y;
            float ex = c.X - a.X;
            float ey = c.Y - a.Y;
            float bl = dx * dx + dy * dy;
            float cl = ex * ex + ey * ey;
            float d = 0.5f / (dx * ey - dy * ex);
            float x = a.X + (ey * bl - dy * cl) * d;
            float y = a.Y + (dx * cl - ex * bl) * d;
            return new Vector2(x, y);
        }

        private static bool inCircle(Vector3 a, Vector3 b, Vector3 c, Vector3 n)
        {
            Vector3 _a = a - n;
            Vector3 _b = b - n;
            Vector3 _c = c - n;
            return
                _a.LengthSquared() * (_b.X * _c.X - _c.X * _b.Y) -
                _b.LengthSquared() * (_a.X * _c.X - _c.X * _a.Y) +
                _c.LengthSquared() * (_a.X * _b.X - _b.X * _a.Y) > 0;
        }

        private static bool ccw(Vector3 a, Vector3 b, Vector3 c) => (b.X - a.X) * (c.Y - a.Y) - (c.X - a.X) * (b.Y - a.Y) > 0;
        private static float sign(Vector2 a, Vector2 b, Vector2 c) => (a.X - c.X) * (b.Y - c.Y) - (b.X - c.X) * (a.Y - c.Y);
    }

    internal class SmartFramerate
    {
        private double _currentFrametimes;
        private double _weight;
        private int _numerator;

        public double Framerate => _numerator / _currentFrametimes;

        public SmartFramerate(int oldFrameWeight)
        {
            _numerator = oldFrameWeight;
            _weight = oldFrameWeight / (oldFrameWeight - 1d);
        }

        public void Update(double timeSinceLastFrame)
        {
            _currentFrametimes = _currentFrametimes / _weight;
            _currentFrametimes += timeSinceLastFrame;
        }
    }
}
