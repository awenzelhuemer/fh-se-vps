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

        private static Bitmap GenerateImagePart(Area area, int startWidth, int endWidth, int startHeight,
            int endHeight, CancellationToken token)
        {
            if (token.IsCancellationRequested)
            {
                return null;
            }

            Bitmap bitmap = new Bitmap(endWidth - startWidth, endHeight - startHeight);
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
                for (int y = startHeight; y < endHeight; y++)
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

                    bitmap.SetPixel(x - startWidth, y - startHeight, ColorSchema.GetColor(iteration));
                }
            }

            return bitmap;
        }

        private void GenerateImagePart(object obj)
        {
            var tuple = (Tuple<Area, int, int, int, int, int, CancellationToken>)obj;
            var area = tuple.Item1;
            var index = tuple.Item6;
            var cancellationToken = tuple.Item7;

            var sw = Stopwatch.StartNew();
            var bitmap = GenerateImagePart(area, tuple.Item2, tuple.Item3, tuple.Item4, tuple.Item5, cancellationToken);
            sw.Stop();
            OnImageGenerated(area, bitmap, sw.Elapsed, index);
        }

        private Bitmap MergeBitmaps(Area area)
        {
            var result = new Bitmap(area.Width, area.Height);
            using (Graphics graphics = Graphics.FromImage(result))
            {
                var startWidth = 0;
                var startHeight = 0;
                for (var i = 0; i < bitmaps.Length; i++)
                {
                    graphics.DrawImage(bitmaps[i], startWidth, startHeight);
                    startHeight += bitmaps[i].Height;
                    if (startHeight >= area.Height)
                    {
                        startHeight = 0;
                        startWidth += bitmaps[i].Width;
                    }
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

            int rows;
            int cols;

            // Calculate how many image parts are needed
            if (Settings.DefaultSettings.Workers > 1)
            {
                rows = 2;
                cols = Settings.DefaultSettings.Workers / 2;
            }
            else
            {
                rows = 1;
                cols = 1;
            }

            var fractionWidth = (int)Math.Floor((double)area.Width / cols);
            var fractionHeight = (int)Math.Floor((double)area.Height / rows);
            var bitmapCount = (int)Math.Ceiling((double)area.Width / fractionWidth) *
                        (int)Math.Ceiling((double)area.Height / fractionHeight);

            bitmaps = new Bitmap[bitmapCount];

            var index = 0;
            for (var x = 0; x * fractionWidth < area.Width; x++)
            {
                var startWidth = x * fractionWidth;
                var endWidth = startWidth + fractionWidth > area.Width ? area.Width : startWidth + fractionWidth;
                for (var y = 0; y * fractionHeight < area.Height; y++)
                {
                    var startHeight = y * fractionHeight;
                    var endHeight = startHeight + fractionHeight > area.Height
                        ? area.Height
                        : startHeight + fractionHeight;
                    var thread = new Thread(GenerateImagePart);
                    thread.Start(new Tuple<Area, int, int, int, int, int, CancellationToken>(area, startWidth, endWidth, startHeight, endHeight, index, cancellationToken.Token));
                    index++;
                }
            }
        }

        #endregion
    }
}