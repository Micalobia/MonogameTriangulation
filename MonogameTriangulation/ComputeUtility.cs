using Cudafy;
using Cudafy.Host;
using Cudafy.Maths;
using Cudafy.Translator;
using Cudafy.Types;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonogameTriangulation
{
    public static class ComputeUtility
    {
        public const int N = 65536;
        public static int tricount;
        public static int width;

        public static uint[] Average(Triangle[] triangles, Texture2D tex)
        {
            CudafyModes.Target = eGPUType.OpenCL;
            CudafyModes.DeviceId = 0;
            CudafyTranslator.Language = CudafyModes.Target == eGPUType.OpenCL ? eLanguage.OpenCL : eLanguage.Cuda;

            if (CudafyHost.GetDeviceCount(CudafyModes.Target) == 0)
                throw new ArgumentException("No suitable devices found.", "original");

            GPGPU gpu = CudafyHost.GetDevice(CudafyModes.Target, CudafyModes.DeviceId);
            Console.WriteLine("Running example using {0}", gpu.GetDeviceProperties(false).TotalMemory);

            CudafyModule km = CudafyTranslator.Cudafy();
            gpu.LoadModule(km);

            List<Vector2> vecs = new List<Vector2>();
            triangles.ForEach(x => vecs.AddRange(x.Points));
            float[] points = new float[vecs.Count << 1];
            for (int i = 0; i < vecs.Count; i++)
            {
                points[i << 1] = vecs[i].X;
                points[(i << 1) + 1] = vecs[i].Y;
            }
            uint[] cols = new uint[tex.Width * tex.Height];
            uint[] output = new uint[triangles.Length];
            tex.GetData(cols);

            float[] dev_p = gpu.CopyToDevice(points);
            uint[] dev_c = gpu.CopyToDevice(cols);
            uint[] dev_o = gpu.Allocate(output);
            tricount = triangles.Length;
            width = tex.Width;

            List<Vector2> list1 = new List<Vector2>();
            List<Vector2> list2 = new List<Vector2>();
            List<Vector2> newlist = new List<Vector2>();

            list1.ForEach(item1 =>
            {
                list2.ForEach(item2 =>
                {
                    if (item1.X == item2.X) item1.Y = item2.Y;
                });
                newlist.Add(item1);
            });

            gpu.Launch((triangles.Length - 1) / 1024 + 1, 1024).cuda_average(dev_p, dev_c, dev_o);
            gpu.CopyFromDevice(dev_o, output);
            gpu.FreeAll();
            return output;
        }

        [Cudafy]
        public static void cuda_average(GThread t, float[] points, uint[] tex, uint[] output)
        {
            int tid = t.blockIdx.x * t.blockDim.x + t.threadIdx.x;
            if (tid < tricount)
            {
                int pind = tid * 6;
                float x0 = points[pind];
                float y0 = points[pind + 1];
                float x1 = points[pind + 2];
                float y1 = points[pind + 3];
                float x2 = points[pind + 4];
                float y2 = points[pind + 5];
                int left = (int)x0;
                if (left > x1) left = (int)x1;
                if (left > x2) left = (int)x2;
                int right = (int)x0;
                if (right > x1) right = (int)x1;
                if (right > x2) right = (int)x2;
                int top = (int)y0;
                if (top < y1) top = (int)y1;
                if (top < y2) top = (int)y2;
                int bottom = (int)y0;
                if (bottom < y1) bottom = (int)y1;
                if (bottom < y2) bottom = (int)y2;
                uint r = 0, g = 0, b = 0, colcount = 0;
                for (int i = top; i < bottom; ++i)
                {
                    for (int j = left; j < right; ++j)
                    {
                        float d1, d2, d3;
                        bool has_neg, has_pos;

                        // (a.X - c.X) * (b.Y - c.Y) - (b.X - c.X) * (a.Y - c.Y);
                        // k,0,1
                        // k,1,2
                        // k,2,0

                        d1 = (j - x1) * (y0 - y1) - (x0 - x1) * (i - y1);
                        d2 = (j - x2) * (y1 - y2) - (x1 - x2) * (i - y2);
                        d3 = (j - x0) * (y2 - y0) - (x2 - x0) * (i - y0);

                        has_neg = (d1 < 0) || (d2 < 0) || (d3 < 0);
                        has_pos = (d1 > 0) || (d2 > 0) || (d3 > 0);

                        bool intri = !(has_neg && has_pos);

                        if(intri)
                        {
                            uint c = tex[i * width + j];
                            r += c & 0xff;
                            g += (c & 0xff00) >> 0x8;
                            b += (c & 0xff0000) >> 0x10;
                            ++colcount;
                        }
                    }
                }
                if(colcount == 0) output[tid] = 0xff000000;
                else
                {
                    r /= colcount;
                    g /= colcount;
                    b /= colcount;
                    g <<= 0x8;
                    b <<= 0x10;
                    output[tid] = r | g | b | 0xff000000;
                }
            }
        }

        public static void Run()
        {
            CudafyModes.Target = eGPUType.OpenCL;
            CudafyModes.DeviceId = 0;
            CudafyTranslator.Language = CudafyModes.Target == eGPUType.OpenCL ? eLanguage.OpenCL : eLanguage.Cuda;

            if (CudafyHost.GetDeviceCount(CudafyModes.Target) == 0)
                throw new ArgumentException("No suitable devices found.", "original");

            GPGPU gpu = CudafyHost.GetDevice(CudafyModes.Target, CudafyModes.DeviceId);
            Console.WriteLine("Running example using {0}", gpu.GetDeviceProperties(false).TotalMemory);

            CudafyModule km = CudafyTranslator.Cudafy();
            gpu.LoadModule(km);

            float[] a = new float[N];
            float[] b = new float[N];
            float[] c = new float[N];

            float[] dev_c = gpu.Allocate(c);

            for (int i = 0; i < N; i++)
            {
                a[i] = i;
                b[i] = (float)i * i;
            }

            float[] dev_a = gpu.CopyToDevice(a);
            float[] dev_b = gpu.CopyToDevice(b);

            gpu.Launch((N - 1) / 1024 + 1, 1024).add(dev_a, dev_b, dev_c);
            //gpu.Launch(new dim3)

            gpu.CopyFromDevice(dev_c, c);

            for (int i = 0; i < N; i++)
                Console.WriteLine("{0}+{1}={2}", a[i], b[i], c[i]);

            gpu.FreeAll();
        }

        [Cudafy]
        public static void add(GThread t, float[] a, float[] b, float[] c)
        {
            int tid = t.blockIdx.x * t.blockDim.x + t.threadIdx.x;
            if (tid < N)
                c[tid] = a[tid] + b[tid];
        }
    }
}
