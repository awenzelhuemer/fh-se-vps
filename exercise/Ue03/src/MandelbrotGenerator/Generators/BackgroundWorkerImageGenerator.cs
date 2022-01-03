using MandelbrotGenerator.Generators;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;

namespace MandelbrotGenerator
{
    public class BackgroundWorkerImageGenerator : IImageGenerator
    {
        #region Private Fields

        private BackgroundWorker backgroundWorker;

        #endregion

        #region Public Constructors

        public BackgroundWorkerImageGenerator()
        {
            InitializeBackgroundWorker();
        }

        #endregion

        #region Public Events

        public event EventHandler<ImageGeneratedEventArgs> ImageGenerated;

        #endregion

        #region Private Methods

        private static Bitmap GenerateImage(Area area, BackgroundWorker worker, DoWorkEventArgs e)
        {
            if (worker.CancellationPending)
            {
                e.Cancel = true;
                return null;
            }

            int maxIterations;
            double zBorder;
            double cReal, cImg, zReal, zImg, zNewReal, zNewImg;

            maxIterations = Settings.DefaultSettings.MaxIterations;
            zBorder = Settings.DefaultSettings.ZBorder * Settings.DefaultSettings.ZBorder;

            Bitmap bitmap = new Bitmap(area.Width, area.Height);

            for (int x = 0; x < area.Width; x++)
            {
                if (worker.CancellationPending)
                {
                    e.Cancel = true;
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

        private void Completed(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Cancelled)
            {
                return;
            }

            ImageGenerated?.Invoke(this, (ImageGeneratedEventArgs)e.Result);
        }

        private void InitializeBackgroundWorker()
        {
            backgroundWorker = new BackgroundWorker();

            backgroundWorker.DoWork += Run;
            backgroundWorker.WorkerSupportsCancellation = true;
            backgroundWorker.RunWorkerCompleted += Completed;
        }

        private void Run(object sender, DoWorkEventArgs e)
        {
            var area = (Area)e.Argument;

            var watch = Stopwatch.StartNew();
            var bitmap = GenerateImage(area, sender as BackgroundWorker, e);
            var args = new ImageGeneratedEventArgs(bitmap, area, watch.Elapsed);

            e.Result = args;
        }

        #endregion

        #region Public Methods

        public void GenerateImage(Area area)
        {
            if (backgroundWorker.IsBusy)
            {
                backgroundWorker.CancelAsync();
                InitializeBackgroundWorker();
            }

            backgroundWorker.RunWorkerAsync(area);
        }

        #endregion
    }
}