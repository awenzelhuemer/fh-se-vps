using System;
using System.Threading;

namespace VPS.ToiletSimulation
{
    public class Toilet
    {
        #region Private Fields

        private readonly IQueue queue;
        private Thread thread;

        #endregion

        #region Public Constructors

        public Toilet(string name, IQueue queue)
        {
            Name = name;
            this.queue = queue;
        }

        #endregion

        #region Public Properties

        public string Name { get; private set; }

        #endregion

        #region Private Methods

        private void Run()
        {
            while (!queue.IsCompleted)
            {
                if (queue.TryDequeue(out var job))
                {
                    job.Process();
                }
                else
                {
                    Console.WriteLine("No job -.-");
                }
            }
        }

        #endregion

        #region Public Methods

        public void Consume()
        {
            thread = new Thread(Run);
            thread.Start();
        }

        public void Join()
        {
            thread.Join();
        }

        #endregion
    }
}