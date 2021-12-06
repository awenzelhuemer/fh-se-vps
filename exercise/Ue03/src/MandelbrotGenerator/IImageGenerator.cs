using System;
using System.Drawing;

namespace MandelbrotGenerator
{
    public interface IImageGenerator
    {
        event EventHandler<ImageGeneratedEventArgs> ImageGenerated;
        void GenerateImage(Area area);
    }
}
