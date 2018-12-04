using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using PIPA.Models;
using PIPA.Utils;

namespace PIPA.Stage.Test
{
    class Processor : IStage
    {
        #region SPECIFIC STAGE PARAMETERS
        public int MsProcessingDelay;
        public bool Debug;
        #endregion

        bool IStage.AllowMultiThreading { get { return true; } }
        bool IStage.RequireCancellationToken { get { return false; } }

        bool IStage.Initialize(dynamic parameters)
        {
            MsProcessingDelay = (parameters.MsProductionDelay != null) ? parameters.MsProductionDelay : 100;
            Debug = parameters.Debug ?? true;
            return true;
        }

        void IStage.Run(IEnumerable<dynamic> input, List<BlockingCollection<dynamic>> output, CancellationManager token, NLog.Logger logger)
        {
            string id = Guid.NewGuid().ToString().Substring(0, 4);
            try
            {
                int delay = MsProcessingDelay;
                foreach (var rec in input)
                {
                    if (token.Stop()) break;
                    if (token.Continue(rec)) continue;
                    string result = rec;
                    if (Debug) Console.WriteLine("[" + id + "] Processing: " + rec);
                    Thread.Sleep(delay);
                    if (!PipelineUtils.SendResult(rec, output, token)) break;
                }
            }
            catch (OperationCanceledException) { }
            finally
            {
                Console.WriteLine("[" + id + "] Processor Stopped!");
            }
        }
    }
}
