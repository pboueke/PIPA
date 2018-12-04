using Microsoft.CSharp.RuntimeBinder;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PIPA.Models
{
    public class CancellationNotice
    {
        public bool IsCancellationRequested { get { return true; } }
    }

    /// <summary>
    /// Wraps around the CancellationToken used to stop the pipeline and the CancellationNotice, which floods
    /// the pipeline once the CancellationToken issues a cancel order. The flooding ensures that all stages will
    /// execute at least once more, enough to stop all stages and end the pipeline, avoiding starvations just before
    /// the execution end.
    /// </summary>
    public class CancellationManager : IDisposable
    {
        private CancellationTokenSource cts;
        public int RequiredRequestsForStopping { get; private set; }
        private int StopsRequested = 0;

        public CancellationManager()
        {
            cts = CancellationTokenSource.CreateLinkedTokenSource(new CancellationToken());
        }

        public void IncrementStopRequirement()
        {
            RequiredRequestsForStopping++;
        }

        /// <summary>
        /// Use this as a continue signal while reading data from a pipeline buffer.
        /// This handles the CancellationNotices once they enter the pipeline.
        /// </summary>
        /// <param name="value"></param>
        /// <returns>True if you should continue over your input buffer read iteration.</returns>
        public bool Continue(dynamic value)
        {
            try
            {
                return value != null && value.IsCancellationRequested != null && value.IsCancellationRequested;
            }
            catch (RuntimeBinderException)
            {
                return false;
            }
        }

        /// <summary>
        /// Access the Cancellation token status directly.
        /// </summary>
        /// <returns> True if the cancellation was requested.</returns>
        public bool Stop()
        {
            return cts.IsCancellationRequested;
        }

        /// <summary>
        /// Warns the manager that a cancellation was requested, changing its internal state.
        /// If all stages required to cancel the pipeline request the cancellation, the cancellation process starts.
        /// </summary>
        /// <param name="force">If true, the stop will be issued and stop the pipeline at all costs, ignoring the RequiredRequestsForStopping property.</param>
        public void RequestStop(bool force = false)
        {
            StopsRequested += 1;
            if (StopsRequested == RequiredRequestsForStopping || force)
                cts.Cancel();
        }

        public void Dispose()
        {
            cts.Dispose();
        }
    }
}
