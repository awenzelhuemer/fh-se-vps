using System;
using System.Threading;

namespace VPS.ToiletSimulation
{
    public class PeopleGenerator
    {
        #region Private Fields

        private ExponentialRandom exponentialRandom;
        private int personId;
        private IQueue queue;
        private Random random;

        #endregion

        #region Public Constructors

        public PeopleGenerator(string name, IQueue queue, int randomSeed)
        {
            Name = name;
            this.queue = queue;
            random = new Random(randomSeed);
            exponentialRandom = new ExponentialRandom(random, 1.0 / Parameters.MeanArrivalTime);
        }

        #endregion

        #region Public Properties

        public string Name { get; private set; }

        #endregion

        #region Private Methods

        private void Run()
        {
            personId = 0;
            for (int i = 0; i < Parameters.JobsPerProducer; i++)
            {
                Thread.Sleep((int)Math.Round(exponentialRandom.NextDouble()));
                personId++;
                queue.Enqueue(new Person(random, Name + " - Person " + personId.ToString("00")));
            }
            queue.CompleteAdding();
        }

        #endregion

        #region Public Methods

        public void Produce()
        {
            var thread = new Thread(Run);
            thread.Name = Name;
            thread.Start();
        }

        #endregion
    }
}