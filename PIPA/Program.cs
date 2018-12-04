using Newtonsoft.Json;
using NLog;
using PIPA.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;


namespace PIPA
{
    class Program
    {
        private static Logger logger;
        private static PIPAConfiguration config;

        static void Main(string[] args)
        {
            // Config logger
            var nlogConfig = new NLog.Config.LoggingConfiguration();
            var logfile = new NLog.Targets.FileTarget("logfile") { FileName = "log.txt" };
            var logconsole = new NLog.Targets.ConsoleTarget("logconsole");
            nlogConfig.AddRule(LogLevel.Info, LogLevel.Fatal, logconsole);
            nlogConfig.AddRule(LogLevel.Debug, LogLevel.Fatal, logfile);
            LogManager.Configuration = nlogConfig;

            // Load Configuration
            config = JsonConvert.DeserializeObject<PIPAConfiguration>(
                ConfigurationManager.AppSettings["configFile"]);

            Execute();
        }

        private static void Execute()
        {
            logger.Info("Start");

            logger.Info("End");
        }
    }
}
