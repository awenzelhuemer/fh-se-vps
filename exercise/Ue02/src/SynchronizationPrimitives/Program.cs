using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace SynchronizationPrimitives
{
    internal class Program
    {
        class LimitedConnectionsExample
        {
            public void DownloadFilesAsync(IEnumerable<string> urls)
            {
                ThreadPool.SetMaxThreads(10, 10);
                foreach (var url in urls)
                {
                    ThreadPool.QueueUserWorkItem(DownloadFile, url);
                }
            }

            public void DownloadFiles(IEnumerable<string> urls)
            {
                var downloadCount = urls.Count();
                using ManualResetEvent resetEvent = new(false);
                foreach (var url in urls)
                {
                    ThreadPool.QueueUserWorkItem(x =>
                    {
                        DownloadFile(x);
                        if (Interlocked.Decrement(ref downloadCount) == 0)
                            resetEvent.Set();
                    }, url);
                }
                resetEvent.WaitOne();
            }

            private void DownloadFile(object url)
            {
                Console.WriteLine($"Download file from {url}");
            }
        }

        static void Main(string[] args)
        {
            new LimitedConnectionsExample().DownloadFiles(new List<string>{
            "www.google1.com",
            "www.google2.com",
            "www.google3.com",
            "www.google4.com",
            "www.google5.com",
            "www.google6.com",
            "www.google7.com",
            "www.google8.com",
            "www.google9.com",
            "www.google10.com",
            "www.google12.com",
            "www.google13.com",
            "www.google14.com",
            "www.google15.com",
            "www.google16.com",
            "www.google17.com",
            "www.google18.com",
            "www.google19.com",
            "www.google20.com",
             "www.google21.com",
            });
        }
    }
}
