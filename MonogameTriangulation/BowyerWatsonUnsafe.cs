using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonogameTriangulation
{
    public static partial class Utility
    {
        public static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
        {
            foreach (T item in source) action(item);
        }
        public static VertexPositionColor[] ToMesh(this Delaunator d, Texture2D tex, int res = 1)
        {
            List<VertexPositionColor> ret = new List<VertexPositionColor>();
            Color[] cols = new Color[tex.Width * tex.Height];
            tex.GetData(cols);
            d.ForEachTriangle(add);
            return ret.ToArray();

            void add(ITriangle t)
            {
                Color c = Average(cols, t, tex.Width, res);
                foreach (Vector2 v in t.Points) ret.Add(new VertexPositionColor(new Vector3(v, 0), c));
            }
        }

        public static Color Average(Color[] cols, ITriangle t, int width, int res)
        {
            Vector2[] points = t.Points.ToArray();
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
    }
}
