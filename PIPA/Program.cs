using Newtonsoft.Json;
using NLog;
using PIPA.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Configuration;
using System.Diagnostics;
using PIPA.Utils;

namespace PIPA
{
    class Program
    {
        private static Logger logger;
        private static PIPAConfiguration config;

        static void Main(string[] args)
        {
            try
            {

                // Config logger
                var nlogConfig = new NLog.Config.LoggingConfiguration();
                var logfile = new NLog.Targets.FileTarget("logfile") { FileName = "log.txt" };
                var logconsole = new NLog.Targets.ConsoleTarget("logconsole");
                nlogConfig.AddRule(LogLevel.Info, LogLevel.Fatal, logconsole);
                nlogConfig.AddRule(LogLevel.Debug, LogLevel.Fatal, logfile);
                LogManager.Configuration = nlogConfig;
                logger = NLog.LogManager.GetCurrentClassLogger();

                // Load Configuration
                logger.Info("Reading configuration file...");
                config = ConfigurationLoader.GetExporterOptions(
                    ConfigurationManager.AppSettings["configFile"]);

                Execute();
            }
            catch (Exception ex)
            {
                logger.Fatal(ex);
            }
        }

        private static void Execute()
        {
            logger.Info("Start");

            logger.Info("Initializing stages and buffers...");
            config.Initialize();

            using (CancellationManager cm = new CancellationManager())
            {
                List<Task> stages = new List<Task>();
                TaskFactory factory = new TaskFactory(TaskCreationOptions.LongRunning, TaskContinuationOptions.None);

                logger.Info("Initializing threads...");
                foreach (StageConfiguration s in config.StageList)
                {
                    // select buffers
                    StageBuffer input = config.BufferList.Select(x => x.BufferName).Contains(s.InputBufferName) ?
                        config.BufferList.Where(x => x.BufferName.Equals(s.InputBufferName)).FirstOrDefault() : null;
                    List<StageBuffer> output = (s.OutputBufferNames == null) ? null :
                        config.BufferList.Where(x => s.OutputBufferNames.Contains(x.BufferName)).ToList();
                    // initialize modules
                    s.Stage.Initialize(s.StageParameters);
                    // start threads
                    int threadsNumber = s.Stage.AllowMultiThreading ? s.StageThreadsNumber : 1;
                    for (int i = 0; i < threadsNumber; i++)
                    {
                        if (s.Stage.RequireCancellationToken) cm.IncrementStopRequirement();
                        if (output != null) foreach (StageBuffer sb in output) sb.Consumers += 1;
                        var thread = factory.StartNew(() => s.Stage.Run(
                            (input == null) ? null : input.Buffer.GetConsumingEnumerable(),
                            (output == null) ? null : output.Select(x => x.Buffer).ToList(),
                            cm, logger));
                        stages.Add(thread);
                        s.Threads.Add(thread);
                    }
                }
                logger.Info("Waiting for conclusion...");
                Wait(stages.ToArray(), config.BufferList, cm);
                logger.Info("Execution complete...");
            }

            logger.Info("End");
        }

        private static void Wait(Task[] stages, List<StageBuffer> buffers, CancellationManager cm)
        {
            ConsoleKeyInfo cki;
            Tabler t = new Tabler(buffers);
            Stopwatch timer = null;
            if (config.AutoCancellationTimeout > 0)
            {
                timer = new Stopwatch();
                timer.Start();
            }

            while (true)
            {
                // Check if the user requested a cancellation
                while (Console.KeyAvailable)
                {
                    cki = Console.ReadKey();
                    if (cki.Key == ConsoleKey.X)
                    {
                        logger.Info("Abortion requested by user. Stopping.");
                        cm.RequestStop(true);
                    }
                }

                // Check if autocancellation should be issued
                if (timer != null && timer.ElapsedMilliseconds > config.AutoCancellationTimeout)
                {
                    if (buffers.All(x => x.Buffer.Count == 0))
                    {
                        timer = null;
                        logger.Info("Reached autocancellation timeout. Stopping.");
                        cm.RequestStop(true);
                    }
                    else timer.Restart();
                }

                // If a cancellation was requested, we must flood the pipeline with empty objects to make sure
                // that any stage thread blocked or starved is able to check the cancellation status.
                if (cm.Stop())
                {
                    CancellationNotice cn = new CancellationNotice();
                    foreach (var b in buffers)
                    {
                        for (int i = 0; i < b.Consumers * 2; i++)
                            b.Buffer.TryAdd(cn);
                    }
                }

                // Print status
                if (config.EnableMonitoring)
                {
                    t.PrintInfo(config.MonitoringSkip);
                    t.PrintStagesTable(config.StageList, config.MonitoringSkip);
                    t.PrintBuffersTable(buffers, config.MonitoringSkip);
                }

                // If all is well, wait some time and check if the execution is completed
                if (Task.WaitAll(stages.ToArray(), config.MonitoringDelay))
                {
                    // END
                    if (config.EnableMonitoring)
                        t.LogBuffersUsage(logger);
                    break;
                }
            }
        }
    }
}
