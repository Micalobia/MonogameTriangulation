using Emgu.CV;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TriangulateCamera
{
    public static partial class Utility
    {
        public const double PI = Math.PI;
        public const double TAU = Math.PI * 2d;
        public const double HALFPI = Math.PI / 2f;
        public static float SquareDistanceFrom(this Vector2 self, Vector2 from)
        {
            float x = self.X - from.X;
            float y = self.Y - from.Y;
            return x * x + y * y;
        }
        public static float SquareDistanceFrom(this Vector2 self, float fromX, float fromY)
        {
            float x = self.X - fromX;
            float y = self.Y - fromY;
            return x * x + y * y;
        }

        public static Color FromHue(float value)
        {
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
        public static Vector3 ToVector3(this Vector2 self) => new Vector3(self.X, self.Y, 0f);
        public static Vector2 ToVector2(this Vector3 self) => new Vector2(self.X, self.Y);
        public static Vector2 Left(this Vector4 self) => new Vector2(self.X, self.Y);
        public static Vector2 Right(this Vector4 self) => new Vector2(self.Z, self.W);
        //public static Vector2 ToDirection(this float self) => new Vector2((float)Math.Cos(self), (float)Math.Sin(self));
        public static Vector2 ToDirection(this float self)
        {
            float sin = (float)Math.Sin(self);
            float cos = (float)Math.Cos(self);
            return new Vector2(cos, sin);
        }

        public static T Clamped<T>(this T self, T min, T max) where T : IComparable<T>
        {
            T ret = self;
            if (ret.CompareTo(max) > 0) ret = max;
            else if (ret.CompareTo(min) < 0) ret = min;
            return ret;
        }

        public static T[] Slice<T>(this T[] self, int inclusiveLeft, int exclusiveRight)
        {
            int count = exclusiveRight - inclusiveLeft;
            T[] ret = new T[count];
            for (int i = 0, j = inclusiveLeft; i < count; i++, j++)
                ret[i] = self[j];
            return ret;
        }

        private static float Dot(float x, float y, float u, float v) => x * u + y * v;
        private static float Sqr(float x, float y) => x * x + y * y;

        public static unsafe VertexPositionColor[] Examine(this Triangulator self, ref Mat mat, int accPow)
        {
            int pow;
            if (accPow == 0) pow = 0;
            else pow = (1 << accPow) - 1;
            byte[,,] scan0 = (byte[,,])mat.GetData();

            VertexPositionColor[] ret = new VertexPositionColor[self.Length];
            //for (int i = 0; i < self.Length; i += 3)
            //    i++;
            Parallel.For(0, self.Length / 3, i =>
            {
                Vector4 bounds = Bounds(self[3 * i], self[3 * i + 1], self[3 * i + 2]);
                int redBin = 0, greenBin = 0, blueBin = 0, count = 0;
                for (int j = (int)bounds.X; j < bounds.Z; ++j)
                {
                    for (int k = (int)bounds.Y; k < bounds.W; ++k)
                    {
                        if (((j + k) & pow) == 0) continue;
                        if (new Vector2(j, k).InTriangle(self[3 * i], self[3 * i + 1], self[3 * i + 2]))
                        {
                            redBin += scan0[k, j, 2];
                            greenBin += scan0[k, j, 1];
                            blueBin += scan0[k, j, 0];
                            ++count;
                        }
                    }
                }
                if (count == 0) count = 1;
                Color c = new Color(redBin / count, greenBin / count, blueBin / count);
                ret[3 * i] = new VertexPositionColor(self[3 * i].ToVector3(), c);
                ret[3 * i + 1] = new VertexPositionColor(self[3 * i + 1].ToVector3(), c);
                ret[3 * i + 2] = new VertexPositionColor(self[3 * i + 2].ToVector3(), c);
            });
            return ret;
        }

        public static Vector4 Bounds(params Vector2[] args)
        {
            float left = float.PositiveInfinity;
            float right = float.NegativeInfinity;
            float top = float.PositiveInfinity;
            float bottom = float.NegativeInfinity;
            foreach (Vector2 arg in args)
            {
                if (arg.X < left) left = arg.X;
                if (arg.X > right) right = arg.X;
                if (arg.Y < top) top = arg.Y;
                if (arg.Y > bottom) bottom = arg.Y;
            }
            return new Vector4(left, top, right, bottom);
        }
        public static bool InBounds(this Vector4 self, Vector2 point) => point.X > self.X && point.Y > self.Y && point.X < self.Z && point.Y < self.W;

        public static bool InTriangle(float u, float v, float x0, float y0, float x1, float y1, float x2, float y2)
        {
            float v0x = x2 - x0;
            float v0y = y2 - y0;
            float v1x = x1 - x0;
            float v1y = y1 - y0;
            float v2x = u - x0;
            float v2y = v - y0;
            float len0 = v0x * v0x + v0y * v0y;
            float len1 = v1x * v1x + v1y * v1y;
            float dot0 = v0x * v1x + v0y * v1y;
            float dot1 = v0x * v2x + v0y * v2y;
            float dot2 = v1x * v2x + v1y * v2y;
            float invDem = len0 * len1 - dot0 * dot0;
            if (invDem == 0) return false;
            invDem = 1 / invDem;
            float h = (len1 * dot1 - dot0 * dot2) * invDem;
            if (h < 0) return false;
            float k = (len0 * dot2 - dot0 * dot1) * invDem;
            return k >= 0 || h + k < 1;
        }

        public static bool InTriangle(this Vector2 self, Vector2 a, Vector2 b, Vector2 c)
        {
            Vector2 v0 = c - a;
            Vector2 v1 = b - a;
            Vector2 v2 = self - a;
            float len0 = v0.LengthSquared();
            float len1 = v1.LengthSquared();
            float dot0 = Vector2.Dot(v0, v1);
            float dot1 = Vector2.Dot(v0, v2);
            float dot2 = Vector2.Dot(v1, v2);
            float invDem = len0 * len1 - dot0 * dot0;
            if (invDem == 0) return false;
            invDem = 1 / invDem;
            float u = (len1 * dot1 - dot0 * dot2) * invDem;
            if (u < 0) return false;
            float v = (len0 * dot2 - dot0 * dot1) * invDem;
            return v >= 0 || u + v < 1;
        }
    }
}
