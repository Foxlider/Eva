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

        private static List<string> severities = new List<string>
        { "Critical", "Error", "Warning", "Info", "Verbose", "Debug", "Neutral" };

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
            switch (Severity)
            {
                case critical:
                    Critical(message, source);
                    break;
                case error:
                    Error(message, source);
                    break;
                case warning:
                    Warning(message, source);
                    break;
                case info:
                    Information(message, source);
                    break;
                case verbose:
                    Verbose(message, source);
                    break;
                case debug:
                    Debug(message, source);
                    break;
                case neutral:
                    Neutral(message, source);
                    break;
                default:
                    break;
            }
        }

        private static void LogToFile(int severity, string message, string source)
        {
            lock (Locked)
            {
                if (Eva.logLvl >= severity)
                {
                    string Severity = severities[(severity % severities.Count + severities.Count) % severities.Count].PadRight(8);
                    string[] lines = message.Split("\n");
                    List<string> formatLines = new List<string>();
                    foreach (var line in FormatFullText(message, $"[{Severity} {source.PadLeft(20)}][{DateTime.Now.ToString()}] : "))
                    { formatLines.Add(line); }
                    File.AppendAllLines(LogFile, formatLines);
                }
            }
        }

        /// <summary>
        /// Logging debug (lvl 5)
        /// </summary>
        /// <param name="message"></param>
        /// <param name="source"></param>
        private static void Debug(string message, string source = "")
        {
            if (Eva.logLvl >= 5)
            {
                Console.ForegroundColor = ConsoleColor.Blue;
                string Severity = "Debug".PadRight(8);
                foreach (var line in FormatFullText(message, $"[{Severity} {source.PadLeft(15)}][{FormatDate()}] : "))
                { Console.WriteLine(line); }
                Console.ResetColor();
            }
        }

        /// <summary>
        /// Logging Verbose (lvl 4)
        /// </summary>
        /// <param name="message"></param>
        /// <param name="source"></param>
        private static void Verbose(string message, string source = "")
        {
            if (Eva.logLvl >= 4)
            {
                Console.ForegroundColor = ConsoleColor.Gray;
                string Severity = "Verbose".PadRight(8);
                foreach (var line in FormatFullText(message, $"[{Severity} {source.PadLeft(15)}][{FormatDate()}] : "))
                { Console.WriteLine(line); }
                Console.ResetColor();
            }
        }

        /// <summary>
        /// Logging Error (lvl 1)
        /// </summary>
        /// <param name="message"></param>
        /// <param name="source"></param>
        private static void Error(string message, string source = "")
        {
            if (Eva.logLvl >= 1)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                string Severity = "Error".PadRight(8);
                foreach (var line in FormatFullText(message, $"[{Severity} {source.PadLeft(15)}][{FormatDate()}] : "))
                { Console.WriteLine(line); }
                Console.ResetColor();
            }
        }

        /// <summary>
        /// Logging Critical (lvl 0)
        /// </summary>
        /// <param name="message"></param>
        /// <param name="source"></param>
        private static void Critical(string message, string source = "")
        {
            if (Eva.logLvl >= 0)
            {
                Console.ForegroundColor = ConsoleColor.DarkRed;
                string Severity = "Critical".PadRight(8);
                foreach (var line in FormatFullText(message, $"[{Severity} {source.PadLeft(15)}][{FormatDate()}] : "))
                { Console.WriteLine(line); }
                Console.ResetColor();
            }
        }

        /// <summary>
        /// Logging Information (lvl 3) DEFAULT VALUE
        /// </summary>
        /// <param name="message"></param>
        /// <param name="source"></param>
        private static void Information(string message, string source = "")
        {
            if (Eva.logLvl >= 3)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                string Severity = "Info".PadRight(8);
                foreach (var line in FormatFullText(message, $"[{Severity} {source.PadLeft(15)}][{FormatDate()}] : "))
                { Console.WriteLine(line); }
                Console.ResetColor();
            }
        }

        /// <summary>
        /// Logging Warning (lvl 2)
        /// </summary>
        /// <param name="message"></param>
        /// <param name="source"></param>
        private static void Warning(string message, string source = "")
        {
            if (Eva.logLvl >= 2)
            {
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                string Severity = "Warning".PadRight(8);
                foreach (var line in FormatFullText(message, $"[{Severity} {source.PadLeft(15)}][{FormatDate()}] : "))
                { Console.WriteLine(line); }
                Console.ResetColor();
            }
        }

        /// <summary>
        /// Logging Message
        /// </summary>
        /// <param name="message"></param>
        /// <param name="source"></param>
        private static void Neutral(string message, string source = "")
        {
            string Severity = "Normal".PadRight(8);
            foreach (var line in FormatFullText(message, $"[{Severity} {source.PadLeft(15)}][{FormatDate()}] : "))
            { Console.WriteLine(line); }
            //foreach (var line in FormatText(message))
            //{ Console.WriteLine($"[{Severity} {source.PadLeft(15)}][{FormatDate()}] : {line}"); }
            Console.ResetColor();
        }

        private static string FormatDate()
        { return DateTime.Now.ToString("hh:mm:ss"); }
        
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
