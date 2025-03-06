using ColossalFramework.Plugins;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using UnityEngine;
using Debug = UnityEngine.Debug;


namespace BenCSCommons.Cities1
{
#if !DEBUG
    #if TRACE
        #error TRACE is defined outside of a DEBUG build, please remove
    #endif
#endif

    /// <summary>
    /// BLog.Trace, BLog.Debug, BLog.Info, BLog.Warning, BLog.Error, BLog.Assert -- these format the message in place,
    ///     ✔ Cheap if there is a const string or a very simple format call with a few args.
    ///     ✔ Cheap if wrapped in an if (booleanValue) { ... }
    ///     BLog.Debug, BLog.Trace and BLog.Assert are optimized away if not in Debug mode
    ///     ⚠ Expensive if multiple $"string {interpolations}" are used (like breaking into multiple lines)
    ///
    /// BLog.DebugFormat, BLog.InfoFormat, ... - these format message later, when logging. Good for
    /// very-very long format strings with multiple complex arguments.
    ///     ✔ As they use format string literal, it can be split multiple lines without perf penalty
    ///     💲 The cost incurred: bulding args array (with pointers)
    ///     Prevents multiple calls to string.Format as opposed to multiline $"string {interpolations}"
    ///     BLog.DebugFormat is optimized away, others are not, so is a good idea to wrap in if (boolValue)
    ///     ⚠ Expensive if not wrapped in a if () condition
    ///
    /// BLog.DebugIf -- first check a condition, and then call a lambda,
    /// which provides a formatted string.
    ///     ✔ Lambda building is just as cheap as format args building
    ///     💲 The cost incurred: each captured value (pointer) is copied into lambda
    ///     ✔ Actual string is formatted ONLY if the condition holds true
    ///     BLog.DebugIf is optimized away if not in Debug mode
    ///     ⚠ Cannot capture out and ref values
    ///
    /// BLog.NotImpl logs an error if something is not implemented and only in debug mode
    /// </summary>
    public static class BLog 
    {
        private static readonly object _logLock = new object();
        private static readonly string _logFileName = UserModBase.BaseName + ".log";
        private static readonly string _logFilePath;
        private static Stopwatch _sw = Stopwatch.StartNew();

        static BLog() {
            try {
                string dir = Application.dataPath;
                _logFilePath = Path.Combine(dir, _logFileName);
                if (File.Exists(_logFilePath)) {
                    File.Delete(_logFilePath); // delete old file to avoid confusion.
                }

                var args = Environment.GetCommandLineArgs();
                int index = Array.IndexOf(args, "-logFile");
                if (index >= 0) {
                    dir = args[index + 1];
                    dir = Path.GetDirectoryName(dir); // drop output_log.txt
                    _logFilePath = Path.Combine(dir, _logFileName);
                    if (File.Exists(_logFilePath)) {
                        File.Delete(_logFilePath);
                    }
                }
            }
            catch (Exception e) {
                UnityEngine.Debug.LogException(e);
            }
        }

        /// <summary>
        /// Print out all the properties of an object
        /// </summary>
        /// <param name="myObject"></param>
        [Conditional("DEBUG")]
        public static void DumpObject(object myObject)
        {
            string myObjectDetails = "";
            foreach (PropertyDescriptor descriptor in TypeDescriptor.GetProperties(myObject))
            {
                string name = descriptor.Name;
                object value = descriptor.GetValue(myObject);
                myObjectDetails += name + ": " + value + "\n";
            }
            //DebugOutputPanel.AddMessage(PluginManager.MessageType.Message, myObjectDetails);
            LogToFile(myObjectDetails, LogLevel.Debug);
        }

        /// <summary>
        /// Will log only if debug mode
        /// </summary>
        /// <param name="s">The text</param>
        [Conditional("DEBUG")]
        public static void Debug(string s) {
            LogToFile(s, LogLevel.Debug);
        }

        /// <summary>
        /// Will log only if debug mode, the string is prepared using string.Format
        /// </summary>
        /// <param name="format">The text</param>
        [Conditional("DEBUG")]
        public static void DebugFormat(string format, params object[] args) {
            LogToFile(string.Format(format, args), LogLevel.Debug);
        }

        /// <summary>
        /// Will log only if debug mode is enabled and the condition is true
        /// NOTE: If a lambda contains values from `out` and `ref` scope args,
        /// then you can not use a lambda, instead use `if (cond) { BLog._Debug }`
        /// </summary>
        /// <param name="cond">The condition</param>
        /// <param name="formatFn">The function which returns text to log</param>
        // TODO: Add log thread and replace formatted strings with lists to perform late formatting in that thread
        [Conditional("DEBUG")]
        public static void DebugIf(bool cond, Func<string> formatFn) {
            if (cond) {
                LogToFile(formatFn(), LogLevel.Debug);
            }
        }

        [Conditional("TRACE")]
        public static void Trace(string s) {
            LogToFile(s, LogLevel.Trace);
        }

        public static void Info(string s) {
            LogToFile(s, LogLevel.Info);
        }

        public static void InfoFormat(string format, params object[] args) {
            LogToFile(string.Format(format, args), LogLevel.Info);
        }

        public static void Warning(string s)
        {
            LogToFile(s, LogLevel.Warning);
        }

        public static void WarningFormat(string format, params object[] args)
        {
            LogToFile(string.Format(format, args), LogLevel.Warning);
        }

        /// <summary>BLog error text only in debug mode and if the condition is false.</summary>
        /// <param name="cond">The condition must be false to log.</param>
        /// <param name="s">The error text.</param>
        [Conditional("DEBUG")]
        public static void Assert(bool cond, string s)
        {
            if (!cond)
            {
                LogToFile(s, LogLevel.Error);
            }
        }

        /// <summary>
        /// Writes an Error message about something not implemented. Debug only.
        /// </summary>
        /// <param name="what">The hint about what is not implemented</param>
        [Conditional("DEBUG")]
        public static void NotImpl(string what)
        {
            LogToFile("Not implemented: " + what, LogLevel.Error);
        }

        public static void Error(string s)
        {
            LogToFile(s, LogLevel.Error);
        }

        public static void ErrorFormat(string format, params object[] args)
        {
            LogToFile(string.Format(format, args), LogLevel.Error);
        }

        /// <summary>
        /// Will log a warning only if debug mode
        /// </summary>
        /// <param name="s">The text</param>
        [Conditional("DEBUG")]
        public static void DebugOnlyWarning(string s) {
            LogToFile(s, LogLevel.Warning);
        }

        /// <summary>
        /// BLog a warning only in debug mode if cond is true
        /// NOTE: If a lambda contains values from `out` and `ref` scope args,
        /// then you can not use a lambda, instead use `if (cond) { BLog._DebugOnlyWarning() }`
        /// </summary>
        /// <param name="cond">The condition</param>
        /// <param name="formatFn">The function which returns text to log</param>
        [Conditional("DEBUG")]
        public static void DebugOnlyWarningIf(bool cond, Func<string> formatFn) {
            if (cond) {
                LogToFile(formatFn(), LogLevel.Warning);
            }
        }

        /// <summary>BLog error only in debug mode.</summary>
        /// <param name="s">The text</param>
        [Conditional("DEBUG")]
        public static void DebugOnlyError(string s) {
            LogToFile(s, LogLevel.Error);
        }


        private static void LogToFile(string log, LogLevel level) {
            lock(_logLock)
            {
                using (StreamWriter w = File.AppendText(_logFilePath))
                {
                    long secs = _sw.ElapsedTicks / Stopwatch.Frequency;
                    long fraction = _sw.ElapsedTicks % Stopwatch.Frequency;
                    w.WriteLine(
                        $"{level.ToString()} " +
                        $"{secs:n0}.{fraction:D7}: " +
                        $"{log}");

                    if (level == LogLevel.Warning || level == LogLevel.Error) {
                        w.WriteLine((new System.Diagnostics.StackTrace(true)).ToString());
                        w.WriteLine();
                    }
                }
            }
        }

        private enum LogLevel
        {
            Trace,
            Debug,
            Info,
            Warning,
            Error,
        }
    }
}
