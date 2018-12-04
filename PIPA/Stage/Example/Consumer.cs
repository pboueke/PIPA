using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using PIPA.Models;

namespace PIPA.Stage.Test
{
    class Consumer : IStage
    {
        #region SPECIFIC STAGE PARAMETERS
        public int MsConsumingDelay;
        public int Limit;
        public bool Debug;
        #endregion

        private int counter = 0;

        bool IStage.AllowMultiThreading { get { return false; } }
        bool IStage.RequireCancellationToken { get { return true; } }

        bool IStage.Initialize(dynamic parameters)
        {
            MsConsumingDelay = (parameters.MsProductionDelay != null) ? parameters.MsProductionDelay : 100;
            Limit = (parameters.Limit != null) ? parameters.Limit : 100;
            Debug = parameters.Debug ?? true;
            return true;
        }

        void IStage.Run(IEnumerable<dynamic> input, List<BlockingCollection<dynamic>> output, CancellationManager token, NLog.Logger logger)
        {
            try
            {
                foreach (var rec in input)
                {
                    if (token.Stop()) break;
                    if (token.Continue(rec)) continue;
                    if (Debug) Console.WriteLine(string.Format("[{0}/{1}] Consuming {2}", ++counter, Limit, rec));
                    Thread.Sleep(MsConsumingDelay);
                    if (counter >= Limit)
                    {
                        Console.WriteLine("Consumer satisfied. Finalizing pipeline.");
                        token.RequestStop();
                        break;
                    }
                }
            }
            catch (OperationCanceledException) { }
            finally
            {
                Console.WriteLine("Consumer stopped!");
            }
        }
    }
}
