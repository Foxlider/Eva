using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Eva.Services
{
    /// <summary>
    /// Logging service to handle Application-side logs
    /// </summary>
    public class Logger
    {
        public const int neutral = -1;
        public const int critical = 0;
        public const int error = 1;
        public const int warning = 2;
        public const int info = 3;
        public const int verbose = 4;
        public const int debug = 5;

        public static object Locked = new object();

        private static readonly List<(string, ConsoleColor)> logLevels = new List<(string, ConsoleColor)>
        {
            //  (Item1, Item2)
            ("Critical", ConsoleColor.DarkRed),
            ("Error", ConsoleColor.Red),
            ("Warning", ConsoleColor.DarkYellow),
            ("Info", ConsoleColor.Green),
            ("Verbose", ConsoleColor.Gray),
            ("Debug", ConsoleColor.Blue),
            ("Neutral", ConsoleColor.White)
        };


        private static string LogDirectory { get; set; }
        private static string LogFile { get; set; } 

        /// <summary>
        /// Constructor
        /// </summary>
        public Logger()
        {
            LogDirectory = Path.Combine(AppContext.BaseDirectory, "logs");
            if (!Directory.Exists(LogDirectory))
            { Directory.CreateDirectory(LogDirectory); }
            LogFile = Path.Combine(LogDirectory, $"EvaLogs-{DateTime.Now.ToString("yyyy-MM-dd")}.txt");
        }

        /// <summary>
        /// Log message function handling Discord Logging
        /// </summary>
        /// <param name="Severity">Message's severity</param>
        /// <param name="message">Message's text</param>
        /// <param name="source">Message's source</param>
        public static void Log(int Severity, string message, string source = "")
        {
            if (source == null)
            { source = ""; }
            LogToFile(Severity, message, source);
            LogToConsole(Severity, message, source);
        }

        private static void LogToFile(int severity, string message, string source)
        {
            lock (Locked)
            {
                if (Eva.logLvl >= severity)
                {
                    var currentlevel = logLevels[(severity % logLevels.Count + logLevels.Count) % logLevels.Count];
                    string Severity = currentlevel.Item1.PadRight(8);
                    string[] lines = message.Split("\n");
                    List<string> formatLines = new List<string>();
                    foreach (var line in FormatFullText(message, $"[{Severity} {source.PadLeft(20)}][{DateTime.Now.ToString()}] : "))
                    { formatLines.Add(line); }
                    File.AppendAllLines(LogFile, formatLines);
                }
            }
        }

        /// <summary>
        /// Logging message to console
        /// </summary>
        /// <param name="severity">Severity level of the message</param>
        /// <param name="message">Message sent</param>
        /// <param name="source">Source of the message</param>
        /// <param name="color">Console Color for the message</param>
        private static void LogToConsole(int severity, string message, string source)
        {
            if (Eva.logLvl >= severity)
            {
                var currentlevel = logLevels[(severity % logLevels.Count + logLevels.Count) % logLevels.Count];
                if (currentlevel.Item2 != ConsoleColor.White)
                { Console.ForegroundColor = currentlevel.Item2; } //Change default color if needed
                foreach (var line in FormatFullText(message, $"[{currentlevel.Item1.PadRight(8)} {source.PadLeft(15)}][{FormatDate()}] : "))
                { Console.WriteLine(line); }
                Console.ResetColor();
            }
        }

        /// <summary>
        /// Simple date formatter
        /// </summary>
        /// <returns></returns>
        private static string FormatDate()
        { return DateTime.Now.ToString("HH:mm:ss"); }
        
        private static string[] FormatFullText(string message, string prefix)
        {
            var bufferLen = Console.BufferWidth;
            var prefixLen = prefix.Length;
            var lines = message.Split("\n");
            List<string> result = new List<string>();
            foreach (var line in lines)
            {
                if (line.Length > bufferLen - prefixLen)
                {
                    var s = prefix;
                    var currLine = s;
                    foreach (var word in line.Split(' '))
                    {
                        if ($"{currLine} {word}".Length >= bufferLen)
                        {
                            s += $"\n{prefix}";
                            currLine = prefix;
                        }
                        currLine += " " + word;
                        s += " " + word;
                    }
                    result.Add(s.Split("\n")[0]);
                    result.Add(s.Split("\n")[1]);
                }
                else
                { result.Add($"{prefix} {line}"); }
            }
            return result.ToArray();
        }
    }
}
