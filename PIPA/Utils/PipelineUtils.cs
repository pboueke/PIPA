using PIPA.Models;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace PIPA.Utils
{
    public static class PipelineUtils
    {
        /// <summary>
        /// This helper method makes sure that no threads will be blocked at the end of the pipeline trying to save records to a blocked collection.
        /// Use it whenever sending objects to the next stage or if you dont know the availability of your output buffer(s).
        /// </summary>
        /// <param name="value">Value to be sent to the collection(s)</param>
        /// <param name="output">List of collections</param>
        /// <param name="cm">Cancellation manager helper object</param>
        /// <param name="delay">Delay, in ms, to be waited between each insertion try.</param>
        /// <returns>'false' if the cancellation order was issued.</returns>
        public static bool SendResult(dynamic value, List<BlockingCollection<dynamic>> output, CancellationManager cm, int delay = 1000)
        {
            if (output != null)
            {
                foreach (var o in output)
                {
                    while (!o.TryAdd(value))
                    {
                        Thread.Sleep(delay);
                        if (cm.Stop()) return false;
                    }
                }
            }
            return true;
        }
    }
}
