using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TriangulateCamera
{
    internal class FramerateTracker
    {
        private Queue<double> _samples;
        private int _sampleCount;
        public double Framerate => 1f / _samples.Average();
        public FramerateTracker(int samples)
        {
            _sampleCount = samples;
            _samples = new Queue<double>();
        }
        public void Update(GameTime gameTime)
        {
            _samples.Enqueue(gameTime.ElapsedGameTime.TotalSeconds);
            if (_samples.Count > _sampleCount)
                _samples.Dequeue();
        }
    }
}
