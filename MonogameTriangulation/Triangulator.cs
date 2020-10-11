using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MonogameTriangulation
{
    public class Delaunator
    {
        private readonly float EPSILON = float.Epsilon;
        private readonly int[] EDGE_STACK = new int[512];

        public int[] Triangles { get; private set; }
        public int[] Halfedges { get; private set; }
        public Vector2[] Points { get; private set; }

        private readonly int hashSize;
        private readonly int[] hullPrev;
        private readonly int[] hullNext;
        private readonly int[] hullTri;
        private readonly int[] hullHash;

        private float cx;
        private float cy;

        private int trianglesLen;
        private readonly int hullStart;
        private readonly int hullSize;
        private readonly int[] hull;

        public Delaunator(Vector2[] points)
        {
            if (points.Length < 3)
            {
                throw new ArgumentOutOfRangeException("Need at least 3 points");
            }

            Points = points;

            int n = points.Length;
            int maxTriangles = 2 * n - 5;

            Triangles = new int[maxTriangles * 3];

            Halfedges = new int[maxTriangles * 3];
            hashSize = (int)Math.Ceiling(Math.Sqrt(n));

            hullPrev = new int[n];
            hullNext = new int[n];
            hullTri = new int[n];
            hullHash = new int[hashSize];

            int[] ids = new int[n];

            float minX = float.PositiveInfinity;
            float minY = float.PositiveInfinity;
            float maxX = float.NegativeInfinity;
            float maxY = float.NegativeInfinity;

            for (int i = 0; i < n; i++)
            {
                float x = points[i].X;
                float y = points[i].Y;
                if (x < minX) minX = x;
                if (y < minY) minY = y;
                if (x > maxX) maxX = x;
                if (y > maxY) maxY = y;
                ids[i] = i;
            }

            float cx = (minX + maxX) / 2;
            float cy = (minY + maxY) / 2;

            float minDist = float.PositiveInfinity;
            int i0 = 0;
            int i1 = 0;
            int i2 = 0;

            // pick a seed point close to the center
            for (int i = 0; i < n; i++)
            {
                float d = Dist(cx, cy, points[i].X, points[i].Y);
                if (d < minDist)
                {
                    i0 = i;
                    minDist = d;
                }
            }
            float i0x = points[i0].X;
            float i0y = points[i0].Y;

            minDist = float.PositiveInfinity;

            // find the point closest to the seed
            for (int i = 0; i < n; i++)
            {
                if (i == i0) continue;
                float d = Dist(i0x, i0y, points[i].X, points[i].Y);
                if (d < minDist && d > 0)
                {
                    i1 = i;
                    minDist = d;
                }
            }

            float i1x = points[i1].X;
            float i1y = points[i1].Y;

            float minRadius = float.PositiveInfinity;

            // find the third point which forms the smallest circumcircle with the first two
            for (int i = 0; i < n; i++)
            {
                if (i == i0 || i == i1) continue;
                float r = Circumradius(i0x, i0y, i1x, i1y, points[i].X, points[i].Y);
                if (r < minRadius)
                {
                    i2 = i;
                    minRadius = r;
                }
            }
            float i2x = points[i2].X;
            float i2y = points[i2].Y;

            if (minRadius == float.PositiveInfinity)
            {
                throw new Exception("No Delaunay triangulation exists for this input.");
            }

            if (Orient(i0x, i0y, i1x, i1y, i2x, i2y))
            {
                int i = i1;
                float x = i1x;
                float y = i1y;
                i1 = i2;
                i1x = i2x;
                i1y = i2y;
                i2 = i;
                i2x = x;
                i2y = y;
            }

            Vector2 center = Circumcenter(i0x, i0y, i1x, i1y, i2x, i2y);
            this.cx = center.X;
            this.cy = center.Y;

            float[] dists = new float[n];
            for (int i = 0; i < n; i++)
            {
                dists[i] = Dist(points[i].X, points[i].Y, center.X, center.Y);
            }

            // sort the points by distance from the seed triangle circumcenter
            Quicksort(ids, dists, 0, n - 1);

            // set up the seed triangle as the starting hull
            hullStart = i0;
            hullSize = 3;

            hullNext[i0] = hullPrev[i2] = i1;
            hullNext[i1] = hullPrev[i0] = i2;
            hullNext[i2] = hullPrev[i1] = i0;

            hullTri[i0] = 0;
            hullTri[i1] = 1;
            hullTri[i2] = 2;

            hullHash[HashKey(i0x, i0y)] = i0;
            hullHash[HashKey(i1x, i1y)] = i1;
            hullHash[HashKey(i2x, i2y)] = i2;

            trianglesLen = 0;
            AddTriangle(i0, i1, i2, -1, -1, -1);

            float xp = 0;
            float yp = 0;

            for (int k = 0; k < ids.Length; k++)
            {
                int i = ids[k];
                float x = points[i].X;
                float y = points[i].Y;

                // skip near-duplicate points
                if (k > 0 && Math.Abs(x - xp) <= EPSILON && Math.Abs(y - yp) <= EPSILON) continue;
                xp = x;
                yp = y;

                // skip seed triangle points
                if (i == i0 || i == i1 || i == i2) continue;

                // find a visible edge on the convex hull using edge hash
                int start = 0;
                for (int j = 0; j < hashSize; j++)
                {
                    int key = HashKey(x, y);
                    start = hullHash[(key + j) % hashSize];
                    if (start != -1 && start != hullNext[start]) break;
                }


                start = hullPrev[start];
                int e = start;
                int q = hullNext[e];

                while (!Orient(x, y, points[e].X, points[e].Y, points[q].X, points[q].Y))
                {
                    e = q;
                    if (e == start)
                    {
                        e = int.MaxValue;
                        break;
                    }

                    q = hullNext[e];
                }

                if (e == int.MaxValue) continue; // likely a near-duplicate point; skip it

                // add the first triangle from the point
                int t = AddTriangle(e, i, hullNext[e], -1, -1, hullTri[e]);

                // recursively flip triangles from the point until they satisfy the Delaunay condition
                hullTri[i] = Legalize(t + 2);
                hullTri[e] = t; // keep track of boundary triangles on the hull
                hullSize++;

                // walk forward through the hull, adding more triangles and flipping recursively
                int next = hullNext[e];
                q = hullNext[next];

                while (Orient(x, y, points[next].X, points[next].Y, points[q].X, points[q].Y))
                {
                    t = AddTriangle(next, i, q, hullTri[i], -1, hullTri[next]);
                    hullTri[i] = Legalize(t + 2);
                    hullNext[next] = next; // mark as removed
                    hullSize--;
                    next = q;

                    q = hullNext[next];
                }

                // walk backward from the other side, adding more triangles and flipping
                if (e == start)
                {
                    q = hullPrev[e];

                    while (Orient(x, y, points[q].X, points[q].Y, points[e].X, points[e].Y))
                    {
                        t = AddTriangle(q, i, e, -1, hullTri[e], hullTri[q]);
                        Legalize(t + 2);
                        hullTri[q] = t;
                        hullNext[e] = e; // mark as removed
                        hullSize--;
                        e = q;

                        q = hullPrev[e];
                    }
                }

                // update the hull indices
                hullStart = hullPrev[i] = e;
                hullNext[e] = hullPrev[next] = i;
                hullNext[i] = next;

                // save the two new edges in the hash table
                hullHash[HashKey(x, y)] = i;
                hullHash[HashKey(points[e].X, points[e].Y)] = e;
            }

            hull = new int[hullSize];
            int s = hullStart;
            for (int i = 0; i < hullSize; i++)
            {
                hull[i] = s;
                s = hullNext[s];
            }

            hullPrev = hullNext = hullTri = null; // get rid of temporary arrays

            //// trim typed triangle mesh arrays
            Triangles = Triangles.Take(trianglesLen).ToArray();
            Halfedges = Halfedges.Take(trianglesLen).ToArray();
        }

        #region CreationLogic
        private int Legalize(int a)
        {
            int i = 0;
            int ar;

            // recursion eliminated with a fixed-size stack
            while (true)
            {
                int b = Halfedges[a];

                /* if the pair of triangles doesn't satisfy the Delaunay condition
                 * (p1 is inside the circumcircle of [p0, pl, pr]), flip them,
                 * then do the same check/flip recursively for the new pair of triangles
                 *
                 *           pl                    pl
                 *          /||\                  /  \
                 *       al/ || \bl            al/    \a
                 *        /  ||  \              /      \
                 *       /  a||b  \    flip    /___ar___\
                 *     p0\   ||   /p1   =>   p0\---bl---/p1
                 *        \  ||  /              \      /
                 *       ar\ || /br             b\    /br
                 *          \||/                  \  /
                 *           pr                    pr
                 */
                int a0 = a - a % 3;
                ar = a0 + (a + 2) % 3;

                if (b == -1)
                { // convex hull edge
                    if (i == 0) break;
                    a = EDGE_STACK[--i];
                    continue;
                }

                int b0 = b - b % 3;
                int al = a0 + (a + 1) % 3;
                int bl = b0 + (b + 2) % 3;

                int p0 = Triangles[ar];
                int pr = Triangles[a];
                int pl = Triangles[al];
                int p1 = Triangles[bl];

                bool illegal = InCircle(
                    Points[p0].X, Points[p0].Y,
                    Points[pr].X, Points[pr].Y,
                    Points[pl].X, Points[pl].Y,
                    Points[p1].X, Points[p1].Y);

                if (illegal)
                {
                    Triangles[a] = p1;
                    Triangles[b] = p0;

                    int hbl = Halfedges[bl];

                    // edge swapped on the other side of the hull (rare); fix the halfedge reference
                    if (hbl == -1)
                    {
                        int e = hullStart;
                        do
                        {
                            if (hullTri[e] == bl)
                            {
                                hullTri[e] = a;
                                break;
                            }
                            e = hullPrev[e];
                        } while (e != hullStart);
                    }
                    Link(a, hbl);
                    Link(b, Halfedges[ar]);
                    Link(ar, bl);

                    int br = b0 + (b + 1) % 3;

                    // don't worry about hitting the cap: it can only happen on extremely degenerate input
                    if (i < EDGE_STACK.Length)
                    {
                        EDGE_STACK[i++] = br;
                    }
                }
                else
                {
                    if (i == 0) break;
                    a = EDGE_STACK[--i];
                }
            }

            return ar;
        }
        private bool InCircle(float ax, float ay, float bx, float by, float cx, float cy, float px, float py)
        {
            float dx = ax - px;
            float dy = ay - py;
            float ex = bx - px;
            float ey = by - py;
            float fx = cx - px;
            float fy = cy - py;

            float ap = dx * dx + dy * dy;
            float bp = ex * ex + ey * ey;
            float cp = fx * fx + fy * fy;

            return dx * (ey * cp - bp * fy) -
                   dy * (ex * cp - bp * fx) +
                   ap * (ex * fy - ey * fx) < 0;
        }
        private int AddTriangle(int i0, int i1, int i2, int a, int b, int c)
        {
            int t = trianglesLen;

            Triangles[t] = i0;
            Triangles[t + 1] = i1;
            Triangles[t + 2] = i2;

            Link(t, a);
            Link(t + 1, b);
            Link(t + 2, c);

            trianglesLen += 3;
            return t;
        }
        private void Link(int a, int b)
        {
            Halfedges[a] = b;
            if (b != -1) Halfedges[b] = a;
        }
        private int HashKey(float x, float y) => (int)(Math.Floor(PseudoAngle(x - cx, y - cy) * hashSize) % hashSize);
        private float PseudoAngle(float dx, float dy)
        {
            float p = dx / (Math.Abs(dx) + Math.Abs(dy));
            return (dy > 0 ? 3 - p : 1 + p) / 4; // [0..1]
        }
        private void Quicksort(int[] ids, float[] dists, int left, int right)
        {
            if (right - left <= 20)
            {
                for (int i = left + 1; i <= right; i++)
                {
                    int temp = ids[i];
                    float tempDist = dists[temp];
                    int j = i - 1;
                    while (j >= left && dists[ids[j]] > tempDist) ids[j + 1] = ids[j--];
                    ids[j + 1] = temp;
                }
            }
            else
            {
                int median = (left + right) >> 1;
                int i = left + 1;
                int j = right;
                Swap(ids, median, i);
                if (dists[ids[left]] > dists[ids[right]]) Swap(ids, left, right);
                if (dists[ids[i]] > dists[ids[right]]) Swap(ids, i, right);
                if (dists[ids[left]] > dists[ids[i]]) Swap(ids, left, i);

                int temp = ids[i];
                float tempDist = dists[temp];
                while (true)
                {
                    do i++; while (dists[ids[i]] < tempDist);
                    do j--; while (dists[ids[j]] > tempDist);
                    if (j < i) break;
                    Swap(ids, i, j);
                }
                ids[left + 1] = ids[j];
                ids[j] = temp;

                if (right - i + 1 >= j - left)
                {
                    Quicksort(ids, dists, i, right);
                    Quicksort(ids, dists, left, j - 1);
                }
                else
                {
                    Quicksort(ids, dists, left, j - 1);
                    Quicksort(ids, dists, i, right);
                }
            }
        }
        private void Swap(int[] arr, int i, int j)
        {
            int tmp = arr[i];
            arr[i] = arr[j];
            arr[j] = tmp;
        }
        private bool Orient(float px, float py, float qx, float qy, float rx, float ry) => (qy - py) * (rx - qx) - (qx - px) * (ry - qy) < 0;
        private float Circumradius(float ax, float ay, float bx, float by, float cx, float cy)
        {
            float dx = bx - ax;
            float dy = by - ay;
            float ex = cx - ax;
            float ey = cy - ay;
            float bl = dx * dx + dy * dy;
            float cl = ex * ex + ey * ey;
            float d = 0.5f / (dx * ey - dy * ex);
            float x = (ey * bl - dy * cl) * d;
            float y = (dx * cl - ex * bl) * d;
            return x * x + y * y;
        }
        private Vector2 Circumcenter(float ax, float ay, float bx, float by, float cx, float cy)
        {
            float dx = bx - ax;
            float dy = by - ay;
            float ex = cx - ax;
            float ey = cy - ay;
            float bl = dx * dx + dy * dy;
            float cl = ex * ex + ey * ey;
            float d = 0.5f / (dx * ey - dy * ex);
            float x = ax + (ey * bl - dy * cl) * d;
            float y = ay + (dx * cl - ex * bl) * d;

            return new Vector2(x, y);
        }
        private float Dist(float ax, float ay, float bx, float by)
        {
            float dx = ax - bx;
            float dy = ay - by;
            return dx * dx + dy * dy;
        }
        #endregion CreationLogic

        #region GetMethods
        public IEnumerable<ITriangle> GetTriangles()
        {
            for (int t = 0; t < Triangles.Length / 3; t++)
            {
                yield return new Triangle(t, GetTrianglePoints(t));
            }
        }
        public IEnumerable<IEdge> GetEdges()
        {
            for (int e = 0; e < Triangles.Length; e++)
            {
                if (e > Halfedges[e])
                {
                    Vector2 p = Points[Triangles[e]];
                    Vector2 q = Points[Triangles[NextHalfedge(e)]];
                    yield return new Edge(e, p, q);
                }
            }
        }
        public IEnumerable<IEdge> GetVoronoiEdges()
        {
            for (int e = 0; e < Triangles.Length; e++)
            {
                if (e < Halfedges[e])
                {
                    Vector2 p = GetTriangleCenter(TriangleOfEdge(e));
                    Vector2 q = GetTriangleCenter(TriangleOfEdge(Halfedges[e]));
                    yield return new Edge(e, p, q);
                }
            }
        }
        public IEnumerable<IVoronoiCell> GetVoronoiCells()
        {
            HashSet<int> seen = new HashSet<int>();  // of point ids
            for (int triangleId = 0; triangleId < Triangles.Length; triangleId++)
            {
                int id = Triangles[NextHalfedge(triangleId)];
                if (!seen.Any(x => x == id))
                {
                    seen.Add(id);
                    IEnumerable<int> edges = EdgesAroundPoint(triangleId);
                    IEnumerable<int> triangles = edges.Select(x => TriangleOfEdge(x));
                    Vector2[] vertices = triangles.Select(x => GetTriangleCenter(x)).ToArray();
                    yield return new VoronoiCell(id, vertices);
                }
            }
        }
        public IEnumerable<IEdge> GetHullEdges() => CreateHull(GetHullPoints());
        public Vector2[] GetHullPoints() => hull.Select(x => Points[x]).ToArray();
        public Vector2[] GetTrianglePoints(int t) => PointsOfTriangle(t).Select(p => Points[p]).ToArray();
        public Vector2[] GetRellaxedPoints() => GetVoronoiCells().Select(x => GetCentroid(x.Points)).ToArray();
        public IEnumerable<IEdge> GetEdgesOfTriangle(int t) => CreateHull(EdgesOfTriangle(t).Select(p => Points[p]));
        public IEnumerable<IEdge> CreateHull(IEnumerable<Vector2> points) => points.Zip(points.Skip(1).Append(points.FirstOrDefault()), (a, b) => new Edge(0, a, b)).OfType<IEdge>();
        public Vector2 GetTriangleCenter(int t)
        {
            Vector2[] vertices = GetTrianglePoints(t);
            return GetCentroid(vertices);
        }
        public Vector2 GetCircumcenter(Vector2 a, Vector2 b, Vector2 c)
        {
            float ad = a.X * a.X + a.Y * a.Y;
            float bd = b.X * b.X + b.Y * b.Y;
            float cd = c.X * c.X + c.Y * c.Y;
            float D = 2 * (a.X * (b.Y - c.Y) + b.X * (c.Y - a.Y) + c.X * (a.Y - b.Y));
            return new Vector2(
                1 / D * (ad * (b.Y - c.Y) + bd * (c.Y - a.Y) + cd * (a.Y - b.Y)),
                1 / D * (ad * (c.X - b.X) + bd * (a.X - c.X) + cd * (b.X - a.X)));
        }
        public Vector2 GetCentroid(Vector2[] points)
        {
            float accumulatedArea = 0.0f;
            float centerX = 0.0f;
            float centerY = 0.0f;

            for (int i = 0, j = points.Count() - 1; i < points.Count(); j = i++)
            {
                float temp = points[i].X * points[j].Y - points[j].X * points[i].Y;
                accumulatedArea += temp;
                centerX += (points[i].X + points[j].X) * temp;
                centerY += (points[i].Y + points[j].Y) * temp;
            }

            if (Math.Abs(accumulatedArea) < 1E-7f)
                return new Vector2();

            accumulatedArea *= 3f;
            return new Vector2(centerX / accumulatedArea, centerY / accumulatedArea);
        }

        #endregion GetMethods

        #region ForEachMethods
        public void ForEachTriangle(Action<ITriangle> callback)
        {
            foreach (ITriangle triangle in GetTriangles())
            {
                callback?.Invoke(triangle);
            }
        }
        public void ForEachTriangleEdge(Action<IEdge> callback)
        {
            foreach (IEdge edge in GetEdges())
            {
                callback?.Invoke(edge);
            }
        }
        public void ForEachVoronoiEdge(Action<IEdge> callback)
        {
            foreach (IEdge edge in GetVoronoiEdges())
            {
                callback?.Invoke(edge);
            }
        }
        public void ForEachVoronoiCell(Action<IVoronoiCell> callback)
        {
            foreach (IVoronoiCell cell in GetVoronoiCells())
            {
                callback?.Invoke(cell);
            }
        }

        #endregion ForEachMethods

        #region Methods based on index
        public IEnumerable<int> EdgesAroundPoint(int start)
        {
            int incoming = start;
            do
            {
                yield return incoming;
                int outgoing = NextHalfedge(incoming);
                incoming = Halfedges[outgoing];
            } while (incoming != -1 && incoming != start);
        }
        public IEnumerable<int> PointsOfTriangle(int t) => EdgesOfTriangle(t).Select(e => Triangles[e]);
        public IEnumerable<int> TrianglesAdjacentToTriangle(int t)
        {
            List<int> adjacentTriangles = new List<int>();
            int[] triangleEdges = EdgesOfTriangle(t);
            foreach (int e in triangleEdges)
            {
                int opposite = Halfedges[e];
                if (opposite >= 0)
                {
                    adjacentTriangles.Add(TriangleOfEdge(opposite));
                }
            }
            return adjacentTriangles;
        }

        public int NextHalfedge(int e) => (e % 3 == 2) ? e - 2 : e + 1;
        public int PreviousHalfedge(int e) => (e % 3 == 0) ? e + 2 : e - 1;
        public int[] EdgesOfTriangle(int t) => new int[] { 3 * t, 3 * t + 1, 3 * t + 2 };
        public int TriangleOfEdge(int e) => (int)Math.Floor((float)(e / 3));
        #endregion Methods based on index
    }

    public interface ITriangle
    {
        IEnumerable<Vector2> Points { get; }
        int Index { get; }
    }

    public interface IEdge
    {
        Vector2 P { get; }
        Vector2 Q { get; }
        int Index { get; }
    }

    public interface IVoronoiCell
    {
        Vector2[] Points { get; }
        int Index { get; }
    }

    public struct Triangle : ITriangle
    {
        public int Index { get; set; }
        public IEnumerable<Vector2> Points { get; set; }

        public Triangle(int t, IEnumerable<Vector2> points)
        {
            Points = points;
            Index = t;
        }
    }

    public struct Edge : IEdge
    {
        public Vector2 P { get; set; }
        public Vector2 Q { get; set; }
        public int Index { get; set; }

        public Edge(int e, Vector2 p, Vector2 q)
        {
            Index = e;
            P = p;
            Q = q;
        }
    }

    public struct VoronoiCell : IVoronoiCell
    {
        public Vector2[] Points { get; set; }
        public int Index { get; set; }
        public VoronoiCell(int triangleIndex, Vector2[] points)
        {
            Points = points;
            Index = triangleIndex;
        }
    }
}