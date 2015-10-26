using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace PicMaster
{
    class Puzzle
    {
        static void Main(string[] args)
        {
            Settings settings  = new Settings();
            settings.pathBlocks = args[0];
            settings.pathMainImage = args[1];
            settings.pathTarget = args[2];

            new Processor().Process(settings);
        }
    }
}
