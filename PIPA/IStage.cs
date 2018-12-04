using NLog;
using PIPA.Models;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace PIPA
{
    /// <summary>
    /// Interface which all pipeline stages are required to implement. 
    /// Read the readme of this project for full specifications of each element.
    /// </summary>
    public interface IStage
    {
        bool RequireCancellationToken { get; }
        bool AllowMultiThreading { get; }
        bool Initialize(dynamic parameters);
        void Run(IEnumerable<dynamic> input, List<BlockingCollection<dynamic>> output, CancellationManager token, Logger logger);
    }
}
