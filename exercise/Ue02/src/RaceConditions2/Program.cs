using System;
using System.Threading;

namespace RaceConditions2
{
    internal class Program
    {
        class RaceConditionExample
        {
            private const int N = 1000;
            private const int BUFFER_SIZE = 10;

            private double[] buffer;
            private Semaphore empty, full;

            public void Run()
            {
                buffer = new double[BUFFER_SIZE];
                empty = new Semaphore(BUFFER_SIZE, BUFFER_SIZE);
                full = new Semaphore(0, BUFFER_SIZE);

                // start threads 
                var t1 = new Thread(Reader);
                var t2 = new Thread(Writer);
                t1.Start();
                t2.Start();

                // wait for threads 
                t1.Join();
                t2.Join();
            }

            private void Reader()
            {
                var readerIndex = 0;
                for (int i = 0; i < N; i++)
                {
                    full.WaitOne(); // Wait until item available
                    lock (buffer)
                    {
                        Console.WriteLine(buffer[readerIndex]);
                        readerIndex = (readerIndex + 1) % BUFFER_SIZE;
                    }
                    empty.Release(); // Signal place is free
                }
            }

            private void Writer()
            {
                var writerIndex = 0;
                for (int i = 0; i < N; i++)
                {
                    empty.WaitOne(); // Wait until place is free

                    lock (buffer)
                    {
                        buffer[writerIndex] = i;
                        writerIndex = (writerIndex + 1) % BUFFER_SIZE;
                    }
                    full.Release(); // Signal item available
                }
            }
        }

        static void Main(string[] args)
        {
            var condition = new RaceConditionExample();
            condition.Run();
        }
    }
}
