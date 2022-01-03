using System;
using System.Diagnostics;
using System.Threading;

namespace MandelbrotGenerator
{
    public class AsyncThreadImageGenerator : IImageGenerator
    {
        private Thread thread;
        private CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

        public event EventHandler<ImageGeneratedEventArgs> ImageGenerated;

        public void GenerateImage(Area area)
        {
            cancellationTokenSource.Cancel(); // Cancel previous calculation
            cancellationTokenSource = new CancellationTokenSource(); // Create new cancellation source
            var token = cancellationTokenSource.Token;
            thread = new Thread(() =>
            {
                var watch = Stopwatch.StartNew();
                var bitmap = SyncImageGenerator.GenerateImage(area, token);
                var args = new ImageGeneratedEventArgs(bitmap, area, watch.Elapsed);
                if (!token.IsCancellationRequested)
                {
                    ImageGenerated?.Invoke(this, args);
                }
            });
            thread.Start();
        }
    }
}
