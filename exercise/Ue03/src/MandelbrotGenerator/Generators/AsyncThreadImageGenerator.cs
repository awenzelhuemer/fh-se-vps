using System;
using System.Diagnostics;
using System.Drawing;
using System.Threading;

namespace MandelbrotGenerator.Generators
{
    public class AsyncThreadImageGenerator : IImageGenerator
    {
        #region Private Fields

        private CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private Thread thread;

        #endregion

        #region Public Events

        public event EventHandler<ImageGeneratedEventArgs> ImageGenerated;

        #endregion

        #region Public Methods

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

        public void GenerateImage(Area area)
        {
            cancellationTokenSource.Cancel(); // Cancel previous calculation
            cancellationTokenSource = new CancellationTokenSource(); // Create new cancellation source
            var token = cancellationTokenSource.Token;
            thread = new Thread(() =>
            {
                var watch = Stopwatch.StartNew();
                var bitmap = GenerateImage(area, token);
                var args = new ImageGeneratedEventArgs(bitmap, area, watch.Elapsed);
                if (!token.IsCancellationRequested)
                {
                    ImageGenerated?.Invoke(this, args);
                }
            });
            thread.Start();
        }

        #endregion
    }
}