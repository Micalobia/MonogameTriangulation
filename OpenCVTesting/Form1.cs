using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
//using System.Threading;

namespace OpenCVTesting
{
    public partial class TheForm : Form
    {
        private VideoCapture capture = new VideoCapture();
        private Timer timer = new Timer();
        public TheForm()
        {
            InitializeComponent();
            timer.Interval = 16;
            timer.Tick += Tick;
            timer.Start();
            capture = new VideoCapture
            {
                FlipHorizontal = true
            };
        }



        private void Tick(object sender, EventArgs e)
        {
            Mat bas = capture.QueryFrame();
            if (bas == null) return;
            Image<Bgr, byte> frame = bas.ToImage<Bgr, byte>();
            Image<Gray, byte>[] split = frame.Split();
            Image<Gray, byte>[] cannied = new Image<Gray, byte>[3]
            {
                Canny(split[0],trackBar1.Value,trackBar2.Value),
                Canny(split[1],trackBar1.Value,trackBar2.Value),
                Canny(split[2],trackBar1.Value,trackBar2.Value)
            };
            Image<Bgr, byte> n = new Image<Bgr, byte>(frame.Width, frame.Height);
            CvInvoke.Add(cannied[0], cannied[1], n);
            CvInvoke.Add(cannied[2], n, n);
            //CvInvoke.Max(cannied[0], cannied[1], n);
            //CvInvoke.Max(cannied[2], n, n);
            //CvInvoke.Merge(new VectorOfMat(cannied[0].Mat, cannied[1].Mat, cannied[2].Mat), n);
            Image<Bgr, byte> img = n;
            Bitmap bmp = new Bitmap(img.Width, img.Height, PixelFormat.Format32bppArgb);
            BitmapData data = bmp.LockBits(new Rectangle(0, 0, img.Width, img.Height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
            Image<Rgba, byte> image = new Image<Rgba, byte>(data.Width, data.Height, data.Stride, data.Scan0);
            image.ConvertFrom(img.Convert<Rgba, byte>());
            image.Dispose();
            bmp.UnlockBits(data);
            pictureBox1.Image?.Dispose();
            pictureBox1.Image = bmp;
            img.Dispose();
            frame.Dispose();
            split[0].Dispose();
            split[1].Dispose();
            split[2].Dispose();
            cannied[0].Dispose();
            cannied[1].Dispose();
            cannied[2].Dispose();
        }

        private Image<Gray, byte> Canny(Image<Gray, byte> gray, double thresh, double link)
        {
            Image<Gray, byte> small = gray.PyrDown();
            Image<Gray, byte> smooth = small.PyrUp();
            small.Dispose();
            Image<Gray, byte> canny = smooth.Canny(thresh, link);
            smooth.Dispose();
            return canny;
        }

        private Image<Gray, byte> YSobel(Image<Gray, byte> gray) => gray.Convolution(new ConvolutionKernelF(new float[,] { { 1, 2, 1 }, { 0, 0, 0 }, { -1, -2, -1 } })).Convert<Gray, byte>();
        private Image<Gray, byte> XSobel(Image<Gray, byte> gray) => gray.Convolution(new ConvolutionKernelF(new float[,] { { 1, 0, -1 }, { 2, 0, -2 }, { 1, 0, -1 } })).Convert<Gray, byte>();

        private unsafe Image<Gray, byte> LocalEntropy(Image<Gray, byte> gray)
        {
            for(int i = 0; i < gray.Width; i++)
            {
                for (int j = 0; j < gray.Height; j++)
                {
                    Rectangle roi = new Rectangle();
                    int threshhold = Math.Max(0, i - 4);
                    roi.X = threshhold;
                    threshhold = Math.Max(0, j - 4);
                    roi.Y = threshhold;
                    roi.Width = i - Math.Max(0, i - 4) + 1 + (Math.Min(gray.Width - 1, i + 4) - i);
                    roi.Height = j - Math.Max(0, j - 4) + 1 + (Math.Min(gray.Height - 1, j + 4) - j);
                    gray.ROI = roi;

                }
            }
        }
    }

    public static class Exten
    {
        public static Color Lerp(Color a, Color b, float t) => Color.FromArgb((int)(a.R + b.R * t), (int)(a.G + b.G * t), (int)(a.B + b.B * t));

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
    }
}
