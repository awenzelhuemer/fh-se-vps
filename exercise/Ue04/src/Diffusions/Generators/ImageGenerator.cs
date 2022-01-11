using System;
using System.Diagnostics;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;

namespace Diffusions.Generators
{
    public abstract class ImageGenerator : IImageGenerator
    {
        public bool Finished { get; protected set; } = false;

        protected CancellationTokenSource cancellationTokenSource;

        protected ImageGenerator()
        {
            cancellationTokenSource = new CancellationTokenSource();
        }

        public void Start(Area area)
        {
            Task.Run(() =>
            {
                Finished = false;

                Stopwatch sw = new Stopwatch();
                sw.Start();
                for (int i = 0; i < Settings.Default.MaxIterations; i++)
                {
                    UpdateMatrix(area, cancellationTokenSource.Token);

                    if (i % Settings.Default.DisplayInterval == 0)
                    {
                        OnImageGenerated(area, ColorSchema.GenerateBitmap(area), sw.Elapsed);
                    }
                }
                sw.Stop();
                Finished = true;
                OnImageGenerated(area, ColorSchema.GenerateBitmap(area), sw.Elapsed);
            }, cancellationTokenSource.Token);
        }

        public void Stop()
        {
            cancellationTokenSource.Cancel();
        }

        protected abstract void UpdateMatrix(Area area, CancellationToken token);

        public event EventHandler<EventArgs<Tuple<Area, Bitmap, TimeSpan>>> ImageGenerated;
        protected void OnImageGenerated(Area area, Bitmap bitmap, TimeSpan timespan)
        {
            var handler = ImageGenerated;
            if (handler != null) handler(this, new EventArgs<Tuple<Area, Bitmap, TimeSpan>>(new Tuple<Area, Bitmap, TimeSpan>(area, bitmap, timespan)));
        }

    }
}
