using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading.Tasks;

namespace IntegrateCSharp
{
    internal class Program
    {

        private static double F(double x)
        {
            return 4.0 / (1.0 + x * x);
        }

        private static double Integrate(double min, double max, int steps)
        {
            double stepSize = (max - min) / steps;
            double result = 0.0;
            for (int i = 0; i < steps; i++)
            {
                result += F(min + stepSize * i) * stepSize;
            }
            return result;
        }


        private static double IntegrateTpl3(int n, double a, double b)
        {
            var sum = 0.0;
            var w = (b - a) / n;
            object locker = new();

            var rangePartitioner = Partitioner.Create(0, n);
            Parallel.ForEach(rangePartitioner,
                () => 0.0,
                (range, state, partialSum) =>
                {
                    for (var i = range.Item1; i < range.Item2; i++)
                    {
                        partialSum += w * F(a + w * (i + 0.5));
                    }

                    return partialSum;
                },
                partialSum =>
                {
                    lock (locker)
                    {
                        sum += partialSum;
                    }
                }
            );
            return sum;
        }

        private static double IntegrateParallel(double min, double max, int steps)
        {
            double stepSize = (max - min) / steps;
            double totalResult = 0.0;

            object lockObj = new();
            var partitioner = Partitioner.Create(0, steps);
            Parallel.ForEach(partitioner,
                () => 0.0, // Initial state
                (range, _, startValue) =>
                {
                    double result = startValue;
                    for (int i = range.Item1; i < range.Item2; i++)
                    {
                        result += F(min + i * stepSize) * stepSize;
                    }
                    return result;
                },
                result =>
                {
                    lock (lockObj) // Lock sum obj
                    {
                        totalResult += result; // Add to total sum
                    }
                });

            return totalResult;
        }

        static void Main(string[] args)
        {
            // Threadpool has to be created thats why omp variant is initially slower
            var resParallel = IntegrateParallel(0.0, 1.0, 1);
            Console.WriteLine(resParallel);


            Console.WriteLine("Enter n: ");
            if (int.TryParse(Console.ReadLine(), out int n))
            {
                Console.WriteLine("StepSize ResultSeq TimeSeq ResultPar TimePar");
                while (n < 100_000_000)
                {
                    var stopWatch = Stopwatch.StartNew();
                    var res = Integrate(0.0, 1.0, n);
                    stopWatch.Stop();
                    double timeSeq = stopWatch.ElapsedTicks / (TimeSpan.TicksPerMillisecond / 1000);

                    stopWatch.Restart();
                    resParallel = IntegrateTpl3(n, 0.0, 1.0);
                    stopWatch.Stop();
                    double timeParallel = stopWatch.ElapsedTicks / (TimeSpan.TicksPerMillisecond / 1000);

                    Console.WriteLine($"{n} {res} {timeSeq} {resParallel} {timeParallel}");
                    n *= 10;
                }
            }
        }
    }
}
