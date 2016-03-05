using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Drawing;
using System.Threading;

namespace PicMaster
{
    class Processor
    {
        Settings _settings;
        List<FastBitmap> _blocks = new List<FastBitmap>();
        FastBitmap _main;
        Bitmap _target;
        Graphics _targetDC;

        double MeasureError(Rectangle blockRect, FastBitmap block)
        {
            double error = 0;

            for (int y = 0; y < blockRect.Height; y++)
            {
                for (int x = 0; x < blockRect.Width; x++)
                {
                    Color m = _main.GetColor(blockRect.X + x, blockRect.Y + y);
                    Color b = block.GetColor(x, y);
                    double dist = Math.Sqrt(
                        (m.R - b.R) * (m.R - b.R) +
                        (m.G - b.G) * (m.G - b.G) +
                        (m.B - b.B) * (m.B - b.B));
                    error += dist;
                }
            }

            return error;
        }

        int _ep;

        public int GetNextBlockIndex()
        {
            return Interlocked.Increment(ref _ep);
        }

        class Work
        {
            public Processor parent;
            public List<double> errors;
            public List<FastBitmap> blocks;
            public Rectangle blockRect;
            public void DoMoreWork()
            {
                for (; ; )
                {
                    int i = parent.GetNextBlockIndex();
                    if (i >= blocks.Count)
                        break;
                        errors[i] = parent.MeasureError(blockRect, blocks[i]);
                }
            }
        }

        List<double> MeasureErrors(Rectangle blockRect)
        {
            List<double> errors = new List<double>(_blocks.Count);
            for (int i = 0; i < _blocks.Count; i++)
                errors.Add(0);
            _ep = -1;

            List<Thread> threads = new List<Thread>();
            for (int i = 0; i < 8; i++)
            {
                Work w = new Work();
                w.parent = this;
                w.blocks = _blocks;
                w.errors = errors;
                w.blockRect = blockRect;
                ThreadStart threadDelegate = new ThreadStart(w.DoMoreWork);
                threads.Add(new Thread(threadDelegate));
                threads[i].Start();
            }

            foreach (Thread thread in threads)
                thread.Join();

            return errors;
        }

        FastBitmap FindBestBlock(Rectangle blockRect)
        {
            double minError = -1;
            int best = 0;
            List<double> errors = MeasureErrors(blockRect);
            for (int i = 0; i < _blocks.Count; i++)
            {
                if (errors[i] == 0)
                    System.Console.WriteLine("ZeroError!");
                if (minError == -1 || errors[i] < minError)
                {
                    best = i;
                    minError = errors[i];
                }
            }

            FastBitmap res = _blocks[best];
            _blocks.RemoveAt(best);
            res.UnlockBitmap();

            return res;
        }

        void LoadBlocks()
        {
            string[] listFiles = Directory.GetFiles(_settings.pathBlocks, "*.jp*g", SearchOption.TopDirectoryOnly);
            foreach (string strFilePath in listFiles)
            {
                Stream BitmapStream = File.Open(strFilePath, System.IO.FileMode.Open);
                Image img = Image.FromStream(BitmapStream);
                Bitmap bmp = new Bitmap(img);
                _blocks.Add(new FastBitmap(bmp));
                if (_settings.sizeBlock == new Size(0, 0))
                    _settings.sizeBlock = bmp.Size;

                BitmapStream.Dispose();
            }
        }

        void LoadMain()
        {

            Stream BitmapStream = File.Open(_settings.pathMainImage, System.IO.FileMode.Open);
            Image img = Image.FromStream(BitmapStream, true, false);
            Bitmap bmp = new Bitmap(img);
            _main = new FastBitmap(bmp);
            _target = new Bitmap(bmp.Size.Width, bmp.Size.Height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            _targetDC = Graphics.FromImage(_target);
        }


        public void Process(Settings settings)
        {
            _settings = settings;

            LoadBlocks();
            LoadMain();
            Spirale spirale = new Spirale(_main.GetBitmap().Size, settings.sizeBlock);

            DateTime start = DateTime.Now;

            Rectangle blockRect;
            while (spirale.GetNextBlockRect(out blockRect))
            {
                System.Console.Write("{0:0000} ", spirale.BlockNumber);
                FastBitmap block = FindBestBlock(blockRect);
                _targetDC.DrawImage(block.GetBitmap(), blockRect,
                    new Rectangle(new Point(0, 0), block.GetBitmap().Size), GraphicsUnit.Pixel);
            }

            System.Console.WriteLine("\n Total Time {0} seconds", (DateTime.Now - start).TotalSeconds);

            _target.Save(_settings.pathTarget);
        }
    }
}
