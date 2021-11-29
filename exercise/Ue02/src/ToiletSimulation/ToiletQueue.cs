using System;
using System.Threading;

namespace VPS.ToiletSimulation {
  public class ToiletQueue : Queue {

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
                int i = 0;
                while(i < queue.Count && job.DueDate.CompareTo(queue[i].DueDate) >= 0) { i++; }
                queue.Insert(i, job); // Insert sorted
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
                    for (int i = 0; i < queue.Count; i++)
                    {
                        if(queue[i].DueDate <= DateTime.Now)
                        {
                            continue;
                        }
                        job = queue[i];
                        queue.RemoveAt(i);
                        return true;
                    }
                    job = queue[0];
                    queue.RemoveAt(0);
                    return true;
                }
            }
            job = null;
            return false;
        }
    }
}
