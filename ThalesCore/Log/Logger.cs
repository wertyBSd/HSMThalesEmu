using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThalesCore.Log
{
    public class Logger
    {
        public enum LogLevel
        {
            NoLogging = 0,
            Errror = 1,
            Warning = 2,
            Info = 3,
            Verbose = 4,
            Debug = 5
        }

        private static LogLevel curLogLevel = LogLevel.NoLogging;
        private static ILogProcs ILP = null;

        public static LogLevel CurrentLogLevel
        {
            get { return curLogLevel; }
            set { curLogLevel = value; }
        }

        public static ILogProcs LogInterface
        {
            set { ILP = value; }
        }

        public static void Major(string s, LogLevel level)
        {
            if (ILP != null)
                if (Convert.ToInt32(level) <= Convert.ToInt32(curLogLevel))
                    ILP.GetMajor(s);
        }

        public static void MajorError(string s)
        {
            Major(s, LogLevel.Errror);
        }

        public static void MajorWarning(string s)
        {
            Major(s, LogLevel.Warning);
        }

        public static void MajorVerbose(string s)
        {
            Major(s, LogLevel.Verbose);
        }

        public static void MajorDebug(string s)
        {
            Major(s, LogLevel.Debug);
        }

        public static void Minor(string s, LogLevel level)
        {
            if (ILP != null)
                if (Convert.ToInt32(level) <= Convert.ToInt32(curLogLevel))
                    ILP.GetMinor(s);
        }

        public static void MinorError(string s)
        {
            Minor(s, LogLevel.Errror);
        }

        public static void MinorWarning(string s)
        {
            Minor(s, LogLevel.Warning);
        }

        public static void MinorInfo(string s)
        {
            Minor(s, LogLevel.Info);
        }

        public static void MinorVerbose(string s)
        {
            Minor(s, LogLevel.Verbose);
        }

        public static void MinorDebug(string s)
        {
            Minor(s, LogLevel.Debug);
        }

        internal static void MajorInfo(string s)
        {
            Major(s, LogLevel.Info);
        }
    }
}
