namespace TriAxis.RunSharp
{
    public static class RunSharpDebug
    {
        /// <summary>
        /// Set to minimal to improve generation performance
        /// </summary>
        public static LeakingDetectionMode LeakingDetection { get; set; } = LeakingDetectionMode.Minimal;

        public static bool CaptureStackOnUnreachable { get; set; }
    }

    public enum LeakingDetectionMode
    {
        Minimal = 0,
        DetectAndCaptureStack,
        DetectAndCaptureStackWithFiles,
    }
}