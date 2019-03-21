﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Eva.Services
{
    class LoggingService
    {
        private string _logDirectory { get; set; }
        private string _logFile => Path.Combine(_logDirectory, $"{DateTime.UtcNow.ToString("yyyy-MM-dd")}.txt");

        /// <summary>
        /// Constructor
        /// </summary>
        public LoggingService()
        {
            _logDirectory = Path.Combine(AppContext.BaseDirectory, "logs");
        }
    }


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

        private static List<string> severities = new List<string>
        { "Critical", "Error", "Warning", "Info", "Verbose", "Debug", "Neutral" };

        private static string _logDirectory { get; set; }
        private static string _logFile { get; set; } 

        /// <summary>
        /// Constructor
        /// </summary>
        public Logger()
        {
            _logDirectory = Path.Combine(AppContext.BaseDirectory, "logs");
            if (!Directory.Exists(_logDirectory))
            { Directory.CreateDirectory(_logDirectory); }
            _logFile = Path.Combine(_logDirectory, $"EvaLogs-{DateTime.Now.ToString("yyyy-MM-dd")}.txt");
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
            LogFile(Severity, message, source);
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

        private static void LogFile(int severity, string message, string source)
        {
            if (Eva.logLvl >= severity)
            {
                string Severity =severities[(severity % severities.Count + severities.Count) % severities.Count ].PadRight(8);
                string[] lines = message.Split("\n");
                List<string> formatLines = new List<string>();
                foreach (var line in lines)
                { formatLines.Add($"[{Severity} {source.PadLeft(20)}][{DateTime.Now.ToString()}] : {line}"); }
                File.AppendAllLines(_logFile, formatLines);
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
                string[] lines = message.Split("\n");
                foreach (var line in lines)
                { Console.WriteLine($"[{Severity} {source.PadLeft(20)}][{DateTime.Now.ToString()}] : {line}"); }
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
                string[] lines = message.Split("\n");
                foreach (var line in lines)
                { Console.WriteLine($"[{Severity} {source.PadLeft(20)}][{DateTime.Now.ToString()}] : {line}"); }
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
                string[] lines = message.Split("\n");
                foreach (var line in lines)
                { Console.WriteLine($"[{Severity} {source.PadLeft(20)}][{DateTime.Now.ToString()}] : {line}"); }
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
                string[] lines = message.Split("\n");
                foreach (var line in lines)
                { Console.WriteLine($"[{Severity} {source.PadLeft(20)}][{DateTime.Now.ToString()}] : {line}"); }
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
                string[] lines = message.Split("\n");
                foreach (var line in lines)
                { Console.WriteLine($"[{Severity} {source.PadLeft(20)}][{DateTime.Now.ToString()}] : {line}"); }
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
                string[] lines = message.Split("\n");
                foreach (var line in lines)
                { Console.WriteLine($"[{Severity} {source.PadLeft(20)}][{DateTime.Now.ToString()}] : {line}"); }
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
            string[] lines = message.Split("\n");
            foreach (var line in lines)
            { Console.WriteLine($"[{Severity} {source.PadLeft(20)}][{DateTime.Now.ToString()}] : {line}"); }
            Console.ResetColor();
        }
    }
}
