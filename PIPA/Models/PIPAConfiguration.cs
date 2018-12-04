using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace PIPA.Models
{
    public static class ConfigurationLoader
    {
        /// <summary>
        /// Helper method used to deserialize the exporter configuration object. 
        /// Also applies the aliases found at the file, preparing it for JSON parsing
        /// </summary>
        /// <param name="filepath"></param>
        /// <returns></returns>
        public static PIPAConfiguration GetExporterOptions(string filepath)
        {
            Dictionary<string, string> aliases = new Dictionary<string, string>();
            string[] lines = File.ReadAllLines(filepath);
            foreach (string line in lines)
            {
                if (line.Trim().StartsWith("#alias"))
                {
                    string[] data = line.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                    if (data.Length < 3) continue;
                    aliases[data[1]] = data.Where((x, i) => i > 1).Aggregate((x, y) => x + " " + y);
                }
            }
            string config = lines.Where(x => !x.Trim().StartsWith("#alias")).Aggregate((x, y) => x + "\n" + y);
            foreach (var alias in aliases) config = config.Replace(alias.Key, alias.Value);
            return JsonConvert.DeserializeObject<PIPAConfiguration>(config);
        }
    }

    public class PIPAConfiguration
    {
        #region User Defined Parameters
        public List<StageBuffer> BufferList { get; set; }
        public List<StageConfiguration> StageList { get; set; }
        public bool EnableMonitoring { get; set; }
        public bool ConfirmEndlesness { get; set; }
        public int MonitoringDelay { get; set; }
        public int MonitoringSkip { get; set; }
        public int AutoCancellationTimeout { get; set; }
        #endregion

        public PIPAConfiguration()
        {
            ConfirmEndlesness = false;
            EnableMonitoring = true;
            MonitoringDelay = 10000;
            MonitoringSkip = 1;
            AutoCancellationTimeout = -1;
        }

        public void Initialize()
        {
            string err = "";
            if (!ValidateParameters(out err))
            {
                throw new ArgumentException(err);
            }
            InitializeBuffers();
            InitializeStages();
        }

        public bool ValidateParameters(out string error)
        {
            error = "";
            int counter = 0;
            HashSet<string> bufferNames = new HashSet<string>();
            HashSet<string> stageNames = new HashSet<string>();

            #region buffers parameter validation
            foreach (StageBuffer buffer in BufferList)
            {
                if (string.IsNullOrEmpty(buffer.BufferName))
                {
                    error = string.Format("All buffers must have an unique name, buffer[{0}] does not have a name.", counter);
                    return false;
                }
                if (bufferNames.Contains(buffer.BufferName))
                {
                    error = string.Format("Duplicated BufferName: {0}", buffer.BufferName);
                    return false;
                }
                bufferNames.Add(buffer.BufferName);
                counter += 1;
            }
            #endregion

            #region stages parameter validation
            counter = 0;
            foreach (StageConfiguration stage in StageList)
            {
                if (string.IsNullOrWhiteSpace(stage.StageName))
                {
                    stage.StageName = Guid.NewGuid().ToString();
                }
                if (stageNames.Contains(stage.StageName))
                {
                    error = string.Format("Duplicated StageName: {0}", stage.StageName);
                    return false;
                }
                stageNames.Add(stage.StageName);
                if (stage.OutputBufferNames != null && stage.OutputBufferNames.Any(x => string.IsNullOrWhiteSpace(x)))
                {
                    error = string.Format("All OutputBufferNames of a stage must be valid names: {0}", stage.StageName);
                    return false;
                }
                if ((!string.IsNullOrWhiteSpace(stage.InputBufferName) && !bufferNames.Contains(stage.InputBufferName))
                    || (stage.OutputBufferNames != null && stage.OutputBufferNames.Any(x => !bufferNames.Contains(x))))
                {
                    error = string.Format("All stages must have an InputBufferName and an OutputBufferName that are defined at the BufferList: {0}", stage.StageName);
                    return false;
                }
            }
            #endregion

            return true;
        }

        public void InitializeBuffers()
        {
            foreach (StageBuffer buffer in BufferList)
            {
                buffer.Buffer = new BlockingCollection<dynamic>(buffer.BufferSize);
            }
        }

        public void InitializeStages()
        {
            var assembly = Assembly.GetExecutingAssembly();
            foreach (StageConfiguration stage in StageList)
            {
                var type = assembly.GetTypes().First(t => t.FullName == string.Format("PIPA.Stage.{0}", stage.StageType));
                stage.Stage = (IStage)Activator.CreateInstance(type);
            }
        }
    }

    public class StageBuffer
    {
        public string BufferName { get; set; }
        public int BufferSize { get; set; }

        public int Consumers { get; set; }
        public BlockingCollection<dynamic> Buffer = null;
        public StageBuffer()
        {
            Consumers = 0;
            BufferSize = 100;
        }
    }

    public class StageConfiguration
    {
        public string StageName { get; set; }
        public string StageType { get; set; }
        public string InputBufferName { get; set; }
        public List<string> OutputBufferNames { get; set; }
        public dynamic StageParameters { get; set; }
        public int StageThreadsNumber { get; set; }

        public List<Task> Threads { get; set; }
        public IStage Stage { get; set; }
        public StageConfiguration()
        {
            StageThreadsNumber = 1;
            Threads = new List<Task>();
        }
    }
}