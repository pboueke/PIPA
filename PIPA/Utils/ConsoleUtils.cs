using System;
using System.Drawing;
using PIPA.Models;
using System.Collections.Generic;
using Colorful;
using System.Threading.Tasks;
using Console = Colorful.Console;
using NLog;
using System.Linq;

namespace PIPA.Utils
{
    /// <summary>
    /// Class used to hold static methods related to displaying data to the console.
    /// </summary>
    public static class ConsoleUtils
    {
        /// <summary>
        /// Method used to confirm with the user if the pipeline configuration is correct.
        /// In this particular case, checks if he really wants to go ahead with an unbounded, forever running pipeline.
        /// </summary>
        public static void ConfirmEndlessness(bool ignore = true)
        {
            if (ignore) return;
            Formatter[] info = new Formatter[4];
            info[0] = new Formatter("PIPA", Color.Red);
            info[1] = new Formatter("no endpoints", Color.Orange);
            info[2] = new Formatter("Y", Color.Green);
            info[3] = new Formatter("n", Color.Red);
            string question = "The {0} pipeline has been configured with {1}, meaning that the pipeline will run until manually aborted or timeouted. Are you sure?[{2}/{3}]";
            Console.WriteLineFormatted(question, Color.White, info);
            string response = Console.ReadLine();
            if (!(new string[] { "y", "yes", "" }).Contains(response.ToLower()))
                throw new Exception("Execution aborted by the user.");

        }
    }

    /// <summary>
    /// Utility class used for displaying run time data about the execution of the pipeline.
    /// </summary>
    public class Tabler
    {
        private int helloShows = 0;
        private int stageTableShows = 0;
        private int bufferTableShows = 0;
        private Formatter[] info;

        /// <summary>
        /// Stores the average of a single value, allowing easy sample increments
        /// </summary>
        private class AvgUsage
        {
            public float times { get; private set; }
            public float avg { get; private set; }

            public AvgUsage()
            {
                times = 0.0f;
                avg = 0.0f;
            }

            /// <summary>
            /// Returns the runtime average based on each time this method was called.
            /// </summary>
            /// <param name="currUsage"></param>
            /// <returns></returns>
            public float GetAvgUsage(float currUsage)
            {
                if (times == 0)
                {
                    times = 1;
                    avg = currUsage;
                } 
                else
                {
                    avg = ((avg * times) + currUsage) / ++times;
                }
                return avg;
            }
        }

        /// <summary>
        /// Saves the average usage of each buffer
        /// </summary>
        private Dictionary<string, AvgUsage> bufferUsageAvg = new Dictionary<string, AvgUsage>();

        public void LogBuffersUsage(Logger logger)
        {
            string data = "{ Buffers: [";
            string template = "{{ {0}: {1} }},";
            foreach (var kvp in bufferUsageAvg)
            {
                data += string.Format(template, kvp.Key, kvp.Value.avg);
            }
            logger.Info(data + "]}");
        }

        public Tabler(List<StageBuffer> Buffers)
        {
            info = new Formatter[6];
            info[0] = new Formatter("PIPA", Color.Red);
            info[1] = new Formatter("stages", Color.Orange);
            info[2] = new Formatter("buffers", Color.Orange);
            info[3] = new Formatter("README", Color.Green);
            info[4] = new Formatter("https://github.com/pboueke/PIPA", Color.Teal);
            info[5] = new Formatter("X", Color.Red);

            foreach (var b in Buffers)
                bufferUsageAvg.Add(b.BufferName, new AvgUsage());
        }

        /// <summary>
        /// Prints information about the PIPA application
        /// </summary>
        /// <param name="skip"></param>
        public void PrintInfo(int skip = 3)
        {
            helloShows += 1;
            if (helloShows % skip == 0) helloShows = 0;
            else return;

            string phrase = "{0} is currently running! See below the {1} and {2} monitoring tables for information on the status of the execution. For more information, checkout the {3} of the application at the repository here {4}. Press {5} to abort safely.";
            Console.WriteLineFormatted(phrase, Color.White, info);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="Buffers"></param>
        /// <param name="skip"></param>
        public void PrintBuffersTable(List<StageBuffer> Buffers, int skip = 3)
        {
            bufferTableShows += 1;
            if (bufferTableShows % skip == 0) bufferTableShows = 0;
            else return;

            int cols = 12;
            string row = "|{0}|{1}|{2}|{3}|||{4}|{5}|{6}|{7}|||{8}|{9}|{10}|{11}|";
            Console.WriteLine("");
            Console.WriteLine("BUFFER STATUS TABLE");
            Console.WriteLine(new string('-', 116));
            Formatter[] r = new Formatter[]
            {
                new Formatter(string.Format("{0,18}","NAME"), Color.Wheat),
                new Formatter(string.Format("{0,7}","SIZE"), Color.Wheat),
                new Formatter(string.Format("{0,4}","USG%"), Color.Wheat),
                new Formatter(string.Format("{0,4}","AVG%"), Color.Wheat),
                new Formatter(string.Format("{0,18}","NAME"), Color.Wheat),
                new Formatter(string.Format("{0,7}","SIZE"), Color.Wheat),
                new Formatter(string.Format("{0,4}","USG%"), Color.Wheat),
                new Formatter(string.Format("{0,4}","AVG%"), Color.Wheat),
                new Formatter(string.Format("{0,18}","NAME"), Color.Wheat),
                new Formatter(string.Format("{0,7}","SIZE"), Color.Wheat),
                new Formatter(string.Format("{0,4}","USG%"), Color.Wheat),
                new Formatter(string.Format("{0,4}","AVG%"), Color.Wheat),
            };
            Console.WriteLineFormatted(row, Color.White, r);

            r = new Formatter[cols];
            int i = 0;
            foreach (var b in Buffers)
            {
                float used = b.Buffer.Count;
                float usage = used / (float)b.BufferSize;
                float hist = bufferUsageAvg[b.BufferName].GetAvgUsage(usage);
                r[i++] = new Formatter(string.Format("{0,18}", b.BufferName.Substring(0, (b.BufferName.Length > 20) ? 20 : b.BufferName.Length)), Color.White);
                r[i++] = new Formatter(string.Format("{0,7}", b.BufferSize.ToString()), Color.Teal);
                r[i++] = new Formatter(string.Format("{0,4}", usage.ToString("0.00")), GetPercentColor(usage));
                r[i++] = new Formatter(string.Format("{0,4}", hist.ToString("0.00")), GetPercentColor(hist));

                if (i % cols == 0)
                {
                    i = 0;
                    Console.WriteLine(new string('-', 116));
                    Console.WriteLineFormatted(row, Color.White, r);
                    r = new Formatter[cols];
                }
            }
            if (i != 0)
            {
                Console.WriteLine(new string('-', 116));
                for (int j = i; j < cols; j += 4)
                {
                    r[j] = new Formatter(string.Format("{0,18}", ""), Color.White);
                    r[j + 1] = new Formatter(string.Format("{0,7}", ""), Color.White);
                    r[j + 2] = new Formatter(string.Format("{0,4}", ""), Color.White);
                    r[j + 3] = new Formatter(string.Format("{0,4}", ""), Color.White);

                }
                Console.WriteLineFormatted(row, Color.White, r);
            }
            Console.WriteLine(new string('-', 116));
            Console.WriteLine("Calculated at print time.");
            Console.WriteLine("");
        }

        /// <summary>
        /// Returns a Color object related to the usage status of a buffer
        /// </summary>
        /// <param name="prct"></param>
        /// <returns></returns>
        private Color GetPercentColor(float prct)
        {
            if (prct > 0.9) return Color.Red;
            if (prct > 0.8) return Color.Orange;
            if (prct > 0.7) return Color.Yellow;
            if (prct > 0.6) return Color.GreenYellow;
            if (prct > 0.5) return Color.Green;
            if (prct > 0.4) return Color.GreenYellow;
            if (prct > 0.3) return Color.YellowGreen;
            if (prct > 0.2) return Color.Yellow;
            if (prct > 0.1) return Color.Orange;
            else return Color.Red;
        }

        /// <summary>
        /// Prints a table with the status of the initialized threads.
        /// </summary>
        /// <param name="Stages"></param>
        /// <param name="skip"></param>
        public void PrintStagesTable(List<StageConfiguration> Stages, int skip = 3)
        {
            stageTableShows += 1;
            if (stageTableShows % skip == 0) stageTableShows = 0;
            else return;

            int cols = 9;
            string row = "|{0}|{1}|{2}|||{3}|{4}|{5}|||{6}|{7}|{8}|";
            Console.WriteLine("");
            Console.WriteLine("STAGE THREAD STATUS TABLE");
            Console.WriteLine(new string('-', 116));
            Formatter[] r = new Formatter[]
            {
                new Formatter(string.Format("{0,19}","NAME"), Color.Wheat),
                new Formatter(string.Format("{0,3}","ID"), Color.Wheat),
                new Formatter(string.Format("{0,12}","STATUS"), Color.Wheat),
                new Formatter(string.Format("{0,19}","NAME"), Color.Wheat),
                new Formatter(string.Format("{0,3}","ID"), Color.Wheat),
                new Formatter(string.Format("{0,12}","STATUS"), Color.Wheat),
                new Formatter(string.Format("{0,19}","NAME"), Color.Wheat),
                new Formatter(string.Format("{0,3}","ID"), Color.Wheat),
                new Formatter(string.Format("{0,12}","STATUS"), Color.Wheat),
            };
            Console.WriteLineFormatted(row, Color.White, r);
            r = new Formatter[cols];
            int i = 0;
            foreach (var s in Stages)
            {
                int counter = 0;
                foreach (var t in s.Threads)
                {
                    r[i++] = new Formatter(string.Format("{0,19}", s.StageName.Substring(0, (s.StageName.Length> 19) ? 19 : s.StageName.Length)), Color.White);
                    r[i++] = new Formatter(string.Format("{0,3}", (counter++).ToString()), Color.Teal);
                    Color c = Color.White;
                    switch (t.Status)
                    {
                        case TaskStatus.Created:
                            c = Color.LightYellow;
                            break;
                        case TaskStatus.WaitingForActivation:
                            c = Color.Yellow;
                            break;
                        case TaskStatus.WaitingForChildrenToComplete:
                            c = Color.YellowGreen;
                            break;
                        case TaskStatus.WaitingToRun:
                            c = Color.Orange;
                            break;
                        case TaskStatus.Running:
                            c = Color.GreenYellow;
                            break;
                        case TaskStatus.Canceled:
                            c = Color.OrangeRed;
                            break;
                        case TaskStatus.Faulted:
                            c = Color.Red;
                            break;
                        case TaskStatus.RanToCompletion:
                            c = Color.Green;
                            break;
                    }
                    r[i++] = new Formatter(string.Format("{0,12}", t.Status.ToString().Substring(0, (t.Status.ToString().Length > 12) ? 12 : t.Status.ToString().Length)), c);

                    if (i%cols == 0)
                    {
                        i = 0;
                        Console.WriteLine(new string('-', 116));
                        Console.WriteLineFormatted(row, Color.White, r);
                        r = new Formatter[cols];
                    }
                }
            }
            if (i != 0)
            {
                Console.WriteLine(new string('-', 116));
                for (int j = i; j < cols; j += 3)
                {
                    r[j] = new Formatter(string.Format("{0,19}", ""), Color.White);
                    r[j + 1] = new Formatter(string.Format("{0,3}", ""), Color.White);
                    r[j + 2] = new Formatter(string.Format("{0,12}", ""), Color.White);
                }
                Console.WriteLineFormatted(row, Color.White, r);
            }
            Console.WriteLine(new string('-', 116));
            Console.WriteLine("");
        }
    }
}
