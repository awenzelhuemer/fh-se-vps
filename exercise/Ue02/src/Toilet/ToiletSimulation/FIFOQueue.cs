using System.Threading;

namespace VPS.ToiletSimulation
{
    public class FIFOQueue : Queue
    {
        #region Public Methods

        // Fünne Wrapper für Betriebssystem-Primitives
        // SemaphoreSlim: Semaphore for one process but faster
        private readonly SemaphoreSlim items = new SemaphoreSlim(0, Parameters.JobsPerProducer * Parameters.Producers + Parameters.Consumers);

        public override void CompleteAdding()
        {
            base.CompleteAdding();
            items.Release(Parameters.Consumers);
        }

        public override void Enqueue(IJob job)
        {
            lock (queue)
            {
                queue.Add(job);
            }
            items.Release(); // Semaphore is thread safe
        }

        public override bool TryDequeue(out IJob job)
        {
            items.Wait();
            lock (queue)
            {
                if (queue.Count > 0)
                {
                    job = queue[0];
                    queue.RemoveAt(0);
                    return true;
                }
            }
            job = null;
            return false;
        }

        #endregion
    }
}