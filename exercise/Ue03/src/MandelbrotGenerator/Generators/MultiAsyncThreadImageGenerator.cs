using System;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Threading;

namespace MandelbrotGenerator.Generators
{
    public class MultiAsyncThreadImageGenerator : IImageGenerator
    {
        #region Private Fields

        private Bitmap[] bitmaps;

        private CancellationTokenSource cancellationToken;

        #endregion

        #region Public Events

        public event EventHandler<ImageGeneratedEventArgs> ImageGenerated;

        #endregion

        #region Private Methods

        private static Bitmap GenerateImagePart(Area area, int startWidth, int endWidth, CancellationToken token)
        {
            if (token.IsCancellationRequested)
            {
                return null;
            }

            Bitmap bitmap = new Bitmap(endWidth - startWidth, area.Height);
            int maxIterations = Settings.DefaultSettings.MaxIterations;
            double zBorder = Settings.DefaultSettings.ZBorder * Settings.DefaultSettings.ZBorder;
            double cReal, cImg, zReal, zImg, zNewReal, zNewImg;

            for (int x = startWidth; x < endWidth; x++)
            {
                if (token.IsCancellationRequested)
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

                    bitmap.SetPixel(x - startWidth, y, ColorSchema.GetColor(iteration));
                }
            }

            return bitmap;
        }

        private void GenerateImagePart(object obj)
        {
            var tuple = (Tuple<Area, int, int, int, CancellationToken>)obj;
            var area = tuple.Item1;
            var index = tuple.Item4;
            var cancellationToken = tuple.Item5;

            var sw = Stopwatch.StartNew();
            var bitmap = GenerateImagePart(area, tuple.Item2, tuple.Item3, cancellationToken);
            sw.Stop();

            OnImageGenerated(area, bitmap, sw.Elapsed, index);
        }

        private Bitmap MergeBitmaps(Area area)
        {
            var result = new Bitmap(area.Width, area.Height);
            using (Graphics graphics = Graphics.FromImage(result))
            {
                var startWidth = 0;
                for (var index = 0; index < bitmaps.Length; index++)
                {
                    graphics.DrawImage(bitmaps[index], startWidth, 0);
                    startWidth += bitmaps[index].Width;
                }
            }

            return result;
        }

        private void OnImageGenerated(Area area, Bitmap bitmap, TimeSpan elapsed, int index)
        {
            bitmaps[index] = bitmap;
            if (bitmaps.Any(map => map == null))
            {
                return;
            }

            var resultingBitmap = MergeBitmaps(area);
            var handler = ImageGenerated;
            handler?.Invoke(this, new ImageGeneratedEventArgs(resultingBitmap, area, elapsed));
        }

        #endregion

        #region Public Methods

        public void GenerateImage(Area area)
        {
            // Cancel previous calculations
            cancellationToken?.Cancel(false);
            cancellationToken = new CancellationTokenSource();

            int cols = Settings.DefaultSettings.Workers;
            int fractionWidth = area.Width / cols;

            bitmaps = new Bitmap[cols];

            int startWidth = 0;
            for (int i = 0; i < cols; i++)
            {
                int endWidth = startWidth + fractionWidth;

                if (cols > 1 && i == cols - 1) // Fix problems with rounding for last column
                {
                    endWidth += area.Width % cols;
                }

                var thread = new Thread(GenerateImagePart);
                thread.Start(new Tuple<Area, int, int, int, CancellationToken>(area, startWidth, endWidth, i, cancellationToken.Token));
                startWidth += fractionWidth;
            }
        }

        #endregion
    }
}