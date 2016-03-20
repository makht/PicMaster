using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace PicMaster
{
    class Spirale
    {
        Size szArea;
        Size szBlock;

        public Spirale(Size szArea, Size szBlock)
        {
            this.szArea = szArea;
            this.szBlock = szBlock;
        }

        public int BlockNumber
        {
            get { return blockNumber; }
        }

        public int Count
        {
            get { return (szArea.Width / (szBlock.Width * 2) * 2) *
                    (szArea.Height / (szBlock.Width * 2) * 2); }
        }

        // Block Position Coordinates
        int p = 0;
        int d = 0;
        int r = 0;
        int pc = 1;
        int blockNumber = -1;

        void IncrementBlockPosition()
        {
            p++;
            if (p == pc)
            {
                p = 0;
                d++;
                if (d == 4)
                {
                    d = 0;
                    r++;
                    pc += 2;
                }
            }
        }

        public bool GetNextBlockRect(out Rectangle rect)
        {
            blockNumber++;
            rect = new Rectangle();

            rect.Width = szBlock.Width;
            rect.Height = szBlock.Height;

            Point center = new Point(szArea.Width / 2, szArea.Height / 2);

            bool res = false;
            int attempts = 0;
            do
            {
                attempts++;
                switch (d)
                {
                    case 0:
                        rect.X = center.X - szBlock.Width * (r + 1 - p);
                        rect.Y = center.Y - szBlock.Height * (r + 1);
                        break;
                    case 1:
                        rect.X = center.X + szBlock.Width * (r);
                        rect.Y = center.Y - szBlock.Height * (r + 1 - p);
                        break;
                    case 2:
                        rect.X = center.X + szBlock.Width * (r - p);
                        rect.Y = center.Y + szBlock.Height * (r);
                        break;
                    case 3:
                        rect.X = center.X - szBlock.Width * (r + 1);
                        rect.Y = center.Y + szBlock.Height * (r - p);
                        break;
                }

                if (Rectangle.Intersect(rect, new Rectangle(new Point(0, 0), szArea)) == rect)
                    res = true;

                IncrementBlockPosition();
            } while (!(res || attempts > 4 + 8 * (r + 1)));

            return res;
        }

    }
}
