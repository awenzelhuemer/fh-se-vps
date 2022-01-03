using System;
using System.Drawing;

namespace MandelbrotGenerator.Generators
{
    public interface IImageGenerator
    {
        event EventHandler<ImageGeneratedEventArgs> ImageGenerated;
        void GenerateImage(Area area);
    }
}
