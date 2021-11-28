using System;
using System.Collections.Generic;
using System.Threading;

namespace VPS.ToiletSimulation
{
    public abstract class Queue : IQueue
    {
        #region Protected Constructors

        protected readonly IList<IJob> queue = new List<IJob>();
        private int completed;

        protected Queue()
        { }

        #endregion

        #region Public Properties

        public virtual bool IsCompleted
        {
            get
            {
                lock (queue)
                {
                    return completed == Parameters.Producers && queue.Count == 0;
                }
            }
        }

        #endregion

        #region Public Methods

        public virtual void CompleteAdding()
        {
            Interlocked.Increment(ref completed); // Own cpu instruction which cannot be interrupted
        }

        public abstract void Enqueue(IJob job);

        public abstract bool TryDequeue(out IJob job);

        #endregion
    }
}