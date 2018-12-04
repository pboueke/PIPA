using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using PIPA.Models;
using PIPA.Utils;

namespace PIPA.Stage.Example
{
    class Producer : IStage
    {
        #region SPECIFIC STAGE PARAMETERS
        public int MsProductionDelay;
        public bool Debug;
        #endregion

        bool IStage.AllowMultiThreading { get { return true; } }
        bool IStage.RequireCancellationToken { get { return false; } }

        bool IStage.Initialize(dynamic parameters)
        {
            Debug = parameters.Debug ?? true;
            MsProductionDelay = (parameters.MsProductionDelay != null) ? parameters.MsProductionDelay : 100;
            return true;
        }

        void IStage.Run(IEnumerable<dynamic> input, List<BlockingCollection<dynamic>> output, CancellationManager token, NLog.Logger logger)
        {
            try
            {
                while (!token.Stop())
                {
                    string result = Guid.NewGuid().ToString();
                    if (Debug) Console.WriteLine("Producing " + result);
                    if (!PipelineUtils.SendResult(result, output, token)) break;
                    Thread.Sleep(MsProductionDelay);
                }
                Console.WriteLine("Producer Stopping!");
            }
            catch (OperationCanceledException) { }
            finally
            {
                Console.WriteLine("Producer Stopped!");
            }
        }
    }
}
