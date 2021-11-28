using System;
using System.Threading;

namespace RaceConditions
{
    class Program
    {
        static readonly DateTime startTime = DateTime.Now.AddSeconds(1);

        static void Main(string[] args)
        {
            RaceCondition();
            FixedRaceCondition();
        }

        private static void RaceCondition()
        {
            Console.WriteLine("With race condition");

            int x = 0;
            void incrementX()
            {
                while (DateTime.Now < startTime)
                {
                    //do nothing
                }

                x++;
            }

            Thread worker1 = new(incrementX);
            Thread worker2 = new(incrementX);

            worker1.Start();
            worker2.Start();

            worker1.Join();
            worker2.Join();

            Console.WriteLine($"x => {x}");

        }

        private static void FixedRaceCondition()
        {
            Console.WriteLine("With fixed race condition");

            int x = 0;
            object locker = new();

            void incrementX()
            {
                while (DateTime.Now < startTime)
                {
                    //do nothing
                }

                // Locking for x to prevent race condition
                lock (locker)
                {
                    x++;
                }
            }

            Thread worker1 = new(incrementX);
            Thread worker2 = new(incrementX);

            worker1.Start();
            worker2.Start();

            worker1.Join();
            worker2.Join();

            Console.WriteLine($"x => {x}");
        }
    }
}
