using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TriangulateCamera
{
    public static partial class RandomExtensions
    {
        private const float TAU = (float)Utility.TAU;
        public static float NextFloat(this Random self) => (float)self.NextDouble();
        public static float NextFloat(this Random self, float exclusiveMax) => self.NextFloat() * exclusiveMax;
        public static float NextFloat(this Random self, float inclusiveMin, float exclusiveMax) => self.NextFloat() * (exclusiveMax - inclusiveMin) + inclusiveMin;
        public static double NextDouble(this Random self, double exclusiveMax) => self.NextDouble() * exclusiveMax;
        public static double NextDouble(this Random self, double inclusiveMin, double exclusiveMax) => self.NextDouble() * (exclusiveMax - inclusiveMin) + inclusiveMin;
        public static Color NextColor(this Random self) => new Color(self.Next(256), self.Next(256), self.Next(256));
        public static Color NextHue(this Random self) => Utility.FromHue(self.NextFloat(360f));
        public static Vector2 NextDirection(this Random self) => self.NextFloat(TAU).ToDirection();
        public static Vector2 NextPoint(this Random self, float width, float height) => new Vector2(self.NextFloat(width), self.NextFloat(height));
        public static Vector2 NextPoint(this Random self, float width, float height, float centerX, float centerY)
        {
            float left = -centerX;
            float right = left + width;
            float top = -centerY;
            float bottom = top + height;
            return new Vector2(self.NextFloat(left, right), self.NextFloat(top, bottom));
        }
        public static Vector2[] NextPoints(this Random self, float width, float height, int count, bool includeCorners, bool centered = true) =>
            self.NextPoints(width, height, count, includeCorners, centered ? width / 2f : 0f, centered ? height / 2f : 0f);
        public static Vector2[] NextPoints(this Random self, float width, float height, int count, bool includeCorners, float centerX, float centerY)
        {
            if (includeCorners && count < 4) throw new ArgumentOutOfRangeException("Not enough points to include corners");
            int start = includeCorners ? 4 : 0;
            Vector2[] ret = new Vector2[count];
            float left = -centerX;
            float right = left + width;
            float top = -centerY;
            float bottom = top + height;
            if (includeCorners)
            {
                ret[0] = new Vector2(left, top);
                ret[1] = new Vector2(right, top);
                ret[2] = new Vector2(left, bottom);
                ret[3] = new Vector2(right, bottom);
            }
            for (int i = start; i < ret.Length; ++i) ret[i] = new Vector2(self.NextFloat(left, right), self.NextFloat(top, bottom));
            return ret;
        }
        public static Vector2[] NextPoissonPoints(this Random self, float width, float height, float r, int k, float centerX, float centerY)
        {
            List<Vector2> ret = new List<Vector2>();
            List<Vector2> active = new List<Vector2>();
            Vector2 p0 = self.NextPoint(width, height, centerX, centerY);
            ret.Add(p0);
            active.Add(p0);
            float sqr = r * r;
            float r2 = r * 2f;
            float left = -centerX;
            float right = left + width;
            float top = -centerY;
            float bottom = top + height;
            while (active.Count > 0)
            {
                int index = self.Next(active.Count);
                Vector2 current = active[index];
                bool added = false;
                for (int i = 0; i < k; ++i)
                {
                    Vector2 p = self.NextDirection() * self.NextFloat(r, r2) + current;
                    if (p.X > right || p.X < left || p.Y > bottom || p.Y < top) continue;
                    bool invalid = false;
                    for (int j = 0; j < ret.Count; ++j)
                    {
                        if (invalid = ret[j].SquareDistanceFrom(p) < sqr) break;
                    }
                    if (!invalid)
                    {
                        added = true;
                        ret.Add(p);
                        active.Add(p);
                    }
                }
                if (!added) active.RemoveAt(index);
            }
            return ret.ToArray();
        }
        public static Vector2[] NextPoissonPointsGrid(this Random self, float width, float height, float r, int k)
        {
            PoissonGrid grid = new PoissonGrid(width, height, r);
            List<int> active = new List<int> { 0 };
            if (!grid.AddPoint(self.NextPoint(width, height))) throw new Exception("What");
            float rr = 2f * r;
            while(active.Count > 0)
            {
                int _i = self.Next(active.Count);
                int i = active[_i];
                bool dead = true;
                for (int n = 0; n < k; n++)
                {
                    Vector2 next = grid[i] + self.NextDirection() * self.NextFloat(r, rr);
                    bool add = grid.AddPoint(next);
                    if(add)
                    {
                        dead = false;
                        active.Add(grid.Length - 1);
                    }
                }
                if (dead) active.RemoveAt(_i);
            }
            return grid.GetPoints();
        }

        #region Triangles
        public static VertexPositionColor[] GetRandomColoredTriangles(this Random self, Triangulator triangulator)
        {
            if (triangulator is null) throw new ArgumentNullException("self");
            self = self ?? new Random();
            VertexPositionColor[] ret = new VertexPositionColor[triangulator.Length];
            for (int i = 0; i < ret.Length; ++i)
            {
                ret[i].Position = triangulator[i].ToVector3();
                ret[i].Color = self.NextColor();
            }
            return ret;
        }
        public static VertexPositionColor[] GetRandomHuedTriangles(this Random self, Triangulator triangulator)
        {
            self = self ?? new Random();
            VertexPositionColor[] ret = new VertexPositionColor[triangulator.Length];
            for (int i = 0; i < ret.Length; ++i)
            {
                ret[i].Position = triangulator[i].ToVector3();
                ret[i].Color = self.NextHue();
            }
            return ret;
        }
        #endregion
    }
}
