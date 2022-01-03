using System.ComponentModel;
using System.Drawing;
using System.Threading;

namespace MandelbrotGenerator
{
    public class SyncImageGenerator
    {
        public static Bitmap GenerateImage(Area area, CancellationToken cancellationToken)
        {
            int maxIterations;
            double zBorder;
            double cReal, cImg, zReal, zImg, zNewReal, zNewImg;

            maxIterations = Settings.DefaultSettings.MaxIterations;
            zBorder = Settings.DefaultSettings.ZBorder * Settings.DefaultSettings.ZBorder;

            Bitmap bitmap = new Bitmap(area.Width, area.Height);

            for (int x = 0; x < area.Width; x++)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return null;
                }
                cReal = area.MinReal + x * area.PixelWidth;
                for (int y = 0; y < area.Height; y++)
                {
                    cImg = area.MinImg + y * area.PixelHeight;
                    zReal = 0.0;
                    zImg = 0.0;
                    int iteration = 0;
                    while (iteration < maxIterations // Check if smaller max iterations
                        && zReal * zReal + zImg * zImg < zBorder) // Check if in border
                    {
                        zNewReal = zReal * zReal - zImg * zImg + cReal;
                        zNewImg = zReal * zImg * 2 + cImg;
                        zReal = zNewReal;
                        zImg = zNewImg;
                        iteration += 1;
                    }
                    bitmap.SetPixel(x, y, ColorSchema.GetColor(iteration));
                }
            }

            return bitmap;
        }
    }
}
