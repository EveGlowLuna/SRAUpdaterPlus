using System;
using Spectre.Console;

namespace SRAUpdaterPlus.Tool
{
    internal static class LogHelper
    {
        public delegate void LogAddedHandler(string logLine);
        public static event LogAddedHandler? LogAdded;  // 新日志追加事件

        private static void WriteLog(string level, string levelColor, string message)
        {
            var time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            string formatted = $"{time} [{level,-5}] > {message}";

            // 输出到终端
            AnsiConsole.MarkupLine(
                $"[grey]{time}[/] [{levelColor}]{level,-5}[/] > [blue]{message}[/]");

            // 通知 UI
            LogAdded?.Invoke(formatted);
        }

        public static void Info(string message) => WriteLog("INFO", "green", message);
        public static void Warn(string message) => WriteLog("WARN", "yellow", message);
        public static void Error(string message) => WriteLog("ERROR", "red", message);
        public static void Debug(string message) => WriteLog("DEBUG", "aqua", message);
    }
}
