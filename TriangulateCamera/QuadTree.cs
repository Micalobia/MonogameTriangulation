using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace TriangulateCamera
{
    internal class PoissonGrid
    {
        private readonly Vector2[] _points;
        private int _pointCount;
        private readonly int[] _grid;
        private const int N = 2;
        private const float SQRTN = 1.4142135623731f;
        private readonly float _width;
        private readonly float _height;
        private readonly int _cols;
        private readonly int _rows;
        private readonly float _cellsize;
        private readonly float _r;
        private readonly float _rq;

        public int Capacity => _grid.Length;
        public int Length => _pointCount;


        public PoissonGrid(float width, float height, float r)
        {
            _cellsize = r / SQRTN;
            _width = width;
            _height = height;
            _cols = (int)(width / _cellsize);
            _rows = (int)(height / _cellsize);
            _r = r;
            _rq = r * r;
            _grid = new int[_cols * _rows];
            for (int i = 0; i < Capacity; i++) _grid[i] = -1;
            _points = new Vector2[Capacity];
            _pointCount = 0;
        }

        public Vector2 this[int i] => _points[i];

        public bool AddPoint(Vector2 point)
        {
            int x = (int)(point.X / _cellsize);
            int y = (int)(point.Y / _cellsize);
            if (x < 0) return false;
            if (x >= _cols) return false;
            if (y < 0) return false;
            if (y >= _rows) return false;
            int i = y * _cols + x;
            if (_grid[i] >= 0) return false;
            if (TooClose(point, x, y)) return false;
            _grid[i] = _pointCount;
            _points[_pointCount] = point;
            ++_pointCount;
            return true;
        }

        public Vector2[] GetPoints()
        {
            Vector2[] ret = new Vector2[_pointCount];
            for (int i = 0; i < _pointCount; i++)
                ret[i] = _points[i];
            return ret;
        }

        private bool TooClose(Vector2 point, int x, int y)
        {
            bool gx = x > 0;
            bool lx = x < _cols - 1;
            bool gy = y > 0;
            bool ly = y < _rows - 1;
            if (gx)
            {
                if (_comp(x - 1, y)) return true;
                if (gy && _comp(x - 1, y - 1)) return true;
                if (ly && _comp(x - 1, y + 1)) return true;
            }
            if (gy && _comp(x, y - 1)) return true;
            if (ly && _comp(x, y + 1)) return true;
            if (lx)
            {
                if (_comp(x + 1, y)) return true;
                if (gy && _comp(x + 1, y - 1)) return true;
                if (ly && _comp(x + 1, y + 1)) return true;
            }
            return false;
            bool _comp(int i, int j)
            {
                int ind = j * _cols + i;
                if (_grid[ind] < 0) return false;
                return point.SquareDistanceFrom(_points[_grid[ind]]) < _rq;
            }
        }
    }
    //internal class QuadTree
    //{
    //    public readonly int CountPerNode;
    //    public AABB Boundary => _boundary;
    //    private AABB _boundary;
    //    private List<Vector2> _points;
    //    private QuadTree _topleft;
    //    private QuadTree _topright;
    //    private QuadTree _bottomleft;
    //    private QuadTree _bottomright;
    //    private float _size;
    //    private Vector2 _center;
    //    public QuadTree(int countPerNode, Vector2 center, float size)
    //    {
    //        _boundary = new AABB(center, size / 2f);
    //        CountPerNode = countPerNode;
    //        _points = new List<Vector2>(CountPerNode);
    //        _topleft = _topright = _bottomleft = _bottomright = null;
    //        _size = size;
    //        _center = center;
    //    }
    //    public bool Insert(Vector2 point)
    //    {
    //        if (!_boundary.Contains(point)) return false;
    //        if (_topright is null)
    //        {
    //            if (_points.Count < CountPerNode)
    //            {
    //                _points.Add(point);
    //                return true;
    //            }
    //            Subdivide();
    //        }
    //        //for (int i = _points.Count - 1; i > 0; --i)
    //        //{
    //        //    SubInsert(_points[i]);
    //        //    _points.RemoveAt(i);
    //        //}
    //        return SubInsert(point);
    //    }
    //    private bool SubInsert(Vector2 point)
    //    {
    //        if (_topleft.Insert(point)) return true;
    //        if (_topright.Insert(point)) return true;
    //        if (_bottomleft.Insert(point)) return true;
    //        if (_bottomright.Insert(point)) return true;
    //        return false; //Shouldn't happen
    //    }
    //    private void Subdivide()
    //    {
    //        float half = _size / 2f;
    //        Vector2 tl = new Vector2(-half, half);
    //        Vector2 tr = new Vector2(half, half);
    //        _topleft = new QuadTree(CountPerNode, _center + tl, half);
    //        _topright = new QuadTree(CountPerNode, _center + tr, half);
    //        _bottomleft = new QuadTree(CountPerNode, _center - tr, half);
    //        _bottomright = new QuadTree(CountPerNode, _center - tl, half);
    //    }
    //    public List<Vector2> QueryRange(AABB range)
    //    {
    //        List<Vector2> ret = new List<Vector2>();
    //        if (!_boundary.Intersects(range))
    //            return ret;
    //        for (int i = 0; i < _points.Count; ++i)
    //            if (range.Contains(_points[i]))
    //                ret.Add(_points[i]);
    //        if (_topleft is null) return ret;
    //        ret.AddRange(_topleft.QueryRange(range));
    //        ret.AddRange(_topright.QueryRange(range));
    //        ret.AddRange(_bottomleft.QueryRange(range));
    //        ret.AddRange(_bottomright.QueryRange(range));

    //        return ret;
    //    }
    //    /// <summary>
    //    /// Radius is radius of a square, not a circle
    //    /// </summary>
    //    public List<Vector2> QueryRange(float x, float y, float radius) => QueryRange(new AABB(x, y, radius));
    //    /// <summary>
    //    /// Radius is radius of a square, not a circle
    //    /// </summary>
    //    public List<Vector2> QueryRange(Vector2 v, float radius) => QueryRange(new AABB(v, radius));
    //}

    //internal struct AABB
    //{
    //    private float left;
    //    private float right;
    //    private float top;
    //    private float bottom;

    //    public AABB(float x, float y, float half)
    //    {
    //        left = x - half;
    //        right = x + half;
    //        top = y - half;
    //        bottom = y + half;
    //    }
    //    public AABB(Vector2 v, float half) : this(v.X, v.Y, half) { }
    //    public bool Contains(Vector2 v) => v.X >= left && v.X < right && v.Y >= top && v.Y < bottom;
    //    public bool Intersects(AABB v) => left < v.right && right > v.left && top < v.bottom && bottom > v.top;
    //}
}
