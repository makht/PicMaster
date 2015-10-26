using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Drawing;

namespace PicMaster
{
    class Resizer
    {
        static void Main(string[] args)
        {
            Process(args[0], args[1], new Size(int.Parse(args[2]), int.Parse(args[3])));
        }

        static void ResizeImage(string pathSrc, string pathDst, Size blockSize)
        {
            Stream BitmapStream = File.Open(pathSrc, System.IO.FileMode.Open);
            Image img = Image.FromStream(BitmapStream);
            Bitmap srcBmp = new Bitmap(img);
            Bitmap blockBmp = new Bitmap(blockSize.Width, blockSize.Height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);

            double ratio = Math.Min(((double)srcBmp.Size.Width) / blockSize.Width, ((double)srcBmp.Size.Height) / blockSize.Height);

            Graphics dc = Graphics.FromImage(blockBmp);
            dc.DrawImage(srcBmp, new Rectangle(0, 0, blockBmp.Size.Width, blockBmp.Size.Height),
                new Rectangle((srcBmp.Size.Width - (int)(blockSize.Width * ratio)) / 2, (srcBmp.Size.Height - (int)(blockSize.Height * ratio)) / 2,
                    (int)(blockSize.Width * ratio), (int)(blockSize.Height * ratio)),
                GraphicsUnit.Pixel);

            blockBmp.Save(pathDst, ImageFormat.Jpeg);

            dc.Dispose();
            blockBmp.Dispose();
            srcBmp.Dispose();
            BitmapStream.Dispose();
        }

        static public void Process(string pathSrc, string pathDst, Size blockSize)
        {
            CultureInfo oldCulture = System.Threading.Thread.CurrentThread.CurrentCulture =
                CultureInfo.InvariantCulture;
            string[] listFiles = Directory.GetFiles(pathSrc, "*.jp*g", SearchOption.TopDirectoryOnly);
            int nCnt = 0;
            foreach (string strFilePath in listFiles)
            {
                string name = nCnt.ToString("0000");
                nCnt++;

                string fullName = Path.Combine(pathDst, name + ".jpg");

                System.Console.WriteLine("{0} -> {1}", strFilePath, name);
                ResizeImage(strFilePath, fullName, blockSize);
            }
            System.Threading.Thread.CurrentThread.CurrentCulture = oldCulture;
        }
    }
}
