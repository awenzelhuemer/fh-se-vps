using System;
using System.Drawing;

namespace MandelbrotGenerator
{
    public class ImageGeneratedEventArgs : EventArgs
    {
        public ImageGeneratedEventArgs(Bitmap bitmap, Area area, TimeSpan timeSpan)
        {
            Bitmap = bitmap;
            Area = area;
            TimeSpan = timeSpan;
        }

        public Bitmap Bitmap { get; private set; }
        public Area Area { get; private set; }
        public TimeSpan TimeSpan { get; set; }
    }
}
