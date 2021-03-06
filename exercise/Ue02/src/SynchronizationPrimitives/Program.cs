using System;
using System.Collections.Generic;
using System.Threading;

namespace SynchronizationPrimitives
{
    internal class Program
    {
        class LimitedConnectionsExample
        {
            readonly Semaphore semaphore = new(10, 10);

            public void DownloadFilesAsync(IEnumerable<string> urls)
            {
                new Thread(_ =>
                {
                    foreach (var url in urls)
                    {
                        semaphore.WaitOne();
                        new Thread(DownloadFile).Start(url);
                    }
                }).Start();
            }

            public void DownloadFiles(IEnumerable<string> urls)
            {
                ICollection<Thread> threads = new List<Thread>();

                foreach (var url in urls)
                {
                    semaphore.WaitOne();
                    Thread thread = new(DownloadFile);
                    threads.Add(thread);
                    thread.Start(url);
                }

                foreach (var thread in threads)
                {
                    thread.Join();
                }
            }

            private void DownloadFile(object url)
            {
                Thread.Sleep(1000);
                Console.WriteLine($"Download file from {url}");
                semaphore.Release();
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
