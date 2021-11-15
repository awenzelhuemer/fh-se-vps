using System;
using System.Threading;

namespace RaceConditionExample
{
    class Program
    {
        static void Main(string[] args)
        {
            Test test = new Test();
            Thread worker1 = new Thread(test.Work1);
            Thread worker2 = new Thread(test.Work2);
            Thread worker3 = new Thread(test.Work3);
            worker1.Start();
            worker2.Start();
            worker3.Start();
            Console.WriteLine(test.Result);
            Console.Read();
        }
    }
}
