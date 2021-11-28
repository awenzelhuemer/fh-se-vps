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
                List<Thread> threads = new();

                foreach (var url in urls)
                {
                    Thread t = new(DownloadFile);
                    t.Start(url);
                    threads.Add(t);

                    if (threads.Count == 10 || urls.Last().Equals(url))
                    {
                        threads.ForEach(thread => thread.Join());
                        threads.Clear();
                    }
                }
            }

            public void DownloadFiles(IEnumerable<string> urls)
            {
                List<Thread> threads = new();

                foreach (var url in urls)
                {
                    var thread = new Thread(DownloadFile);
                    thread.Start(url);
                    threads.Add(thread);
                }

                threads.ForEach(thread => thread.Join());
            }

            private void DownloadFile(object url)
            {
                Random r = new();
                Thread.Sleep(r.Next(200, 5000));
                Console.WriteLine($"Download file from {url}");
            }
        }

        static void Main(string[] args)
        {
            new LimitedConnectionsExample().DownloadFilesAsync(new List<string>{
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
