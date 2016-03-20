using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;


namespace PicMaster
{
    class Processor
    {
        class Block
        {
            public bool IsActive;
            public FastBitmap fbmp;

            public Block(FastBitmap fb)
            {
                IsActive = true;
                fbmp = fb;
            }
        }
        List<Block> _blocks = new List<Block>();

        public class Position
        {
            public Rectangle rc;
            public List<double> errors;
        }
        List<Position> positions;

        Settings _settings;
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

        void MeasureAllErrors()
        {
            Parallel.For(0, positions.Count, (i, state) =>
            {
                for (int e = 0; e < _blocks.Count; e++)
                {
                    positions[i].errors[e] = MeasureError(positions[i].rc, _blocks[e].fbmp);
                }
                System.Console.Write("{0:0000} ", i);
            });
        }

        FastBitmap FindBestBlock(int pos)
        {
            double bestError = double.MaxValue;
            int best = -1;
            double mnError = double.MaxValue;
            int mn = -1;
            for (int i = 0; i < _blocks.Count; i++)
            {
                if (positions[pos].errors[i] == 0)
                    System.Console.WriteLine("ZeroError!");
                if (positions[pos].errors[i] < mnError)
                {
                    mnError = positions[pos].errors[i];
                    mn = i;
                }
                    if (positions[pos].errors[i] < bestError)
                {
                    bool isBetter = false;
                    for (int j = pos + 1; j < positions.Count; j++)
                    {
                        if (positions[j].errors[i] < positions[pos].errors[i])
                        {
                            bool isBest = true;
                            for (int k = 0; k < _blocks.Count; k++)
                            {
                                if (positions[j].errors[k] < positions[j].errors[i])
                                    isBest = false;
                            }
                            if (isBest)
                            {
                                isBetter = true;
                                break;
                            }
                        }
                    }
                    if (!isBetter)
                    {
                        best = i;
                        bestError = positions[pos].errors[i];
                    }
                }
            }

            if (best == -1)
                best = mn;
            FastBitmap res = _blocks[best].fbmp;
            res.UnlockBitmap();
            for (int j = pos + 1; j < positions.Count; j++)
                positions[j].errors[best] = double.MaxValue;

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
                _blocks.Add(new Block(new FastBitmap(bmp)));
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

            DateTime start = DateTime.Now;

            LoadBlocks();
            LoadMain();
            Spirale spirale = new Spirale(_main.GetBitmap().Size, settings.sizeBlock);
            positions = new List<Position>(spirale.Count);
            for (int i = 0; i < spirale.Count; i++)
            {
                positions.Add(new Position());
                positions[i].errors = new List<double>(_blocks.Count);
                spirale.GetNextBlockRect(out positions[i].rc);
                for (int j = 0; j < _blocks.Count; j++)
                    positions[i].errors.Add(0);
            }

            MeasureAllErrors();

            for (int i = 0; i < positions.Count; i++)
            {
                System.Console.Write("{0:0000} ", i);
                FastBitmap block = FindBestBlock(i);
                _targetDC.DrawImage(block.GetBitmap(), positions[i].rc,
                    new Rectangle(new Point(0, 0), block.GetBitmap().Size), GraphicsUnit.Pixel);
            }

            System.Console.WriteLine("\n Total Time {0} seconds", (DateTime.Now - start).TotalSeconds);

            _target.Save(_settings.pathTarget);
        }
    }
}
