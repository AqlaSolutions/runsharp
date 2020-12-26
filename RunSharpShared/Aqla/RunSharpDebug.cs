using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace TriAxis.RunSharp
{
    public static class RunSharpDebug
    {
        /// <summary>
        /// Set to minimal to improve generation performance
        /// </summary>
        public static LeakingDetectionMode LeakingDetection { get; set; } = LeakingDetectionMode.Minimal;

        public static bool CaptureStackOnUnreachable { get; set; }
        
        static List<string> _leaks = new List<string>();

        public static List<string> RetrieveLeaks()
        {
            List<string> r;
            lock (_leaks)
            {
                r = _leaks;
                _leaks = new List<string>();
            }

            return r;
        }
        
        internal static void StoreLeak(string message)
        {
            lock (_leaks)
            {
                if (LeakingDetection == LeakingDetectionMode.StoreAndContinue)
                    message += $"\r\nLeak detected at {new StackTrace(false)}";
                if (_leaks.Count > 100)
                    _leaks.RemoveRange(0, 50);
                _leaks.Add(message);
            }
        }
    }

    public enum LeakingDetectionMode
    {
        Minimal = 0,
        DetectAndCaptureStack,
        DetectAndCaptureStackWithFiles,
        StoreAndContinue
    }
}