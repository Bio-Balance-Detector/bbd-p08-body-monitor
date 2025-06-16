/*
The following code for `MLExperimentPerformanceMonitor` is commented out.
It appears to be an implementation of `Microsoft.ML.Runtime.IMonitor` (or a similar performance monitoring interface from ML.NET,
possibly `Microsoft.ML.AutoML.DefaultPerformanceMonitor` as hinted by the commented-out base class)
intended to monitor resource usage (CPU, memory) during AutoML experiments.
It includes logic to log performance metrics and potentially cancel trials based on resource limits or timeouts.
*/

//using Microsoft.ML;
//using Microsoft.ML.AutoML;
//using Microsoft.ML.Runtime;
//using System.Management;

//namespace BBD.BodyMonitor.Models
//{
//    internal class MLExperimentPerformanceMonitor : DefaultPerformanceMonitor
//    {
//        private readonly ILogger _logger;
//        private readonly SweepablePipeline _pipeline;

//        public MLExperimentPerformanceMonitor(ILogger logger, SweepablePipeline pipeline, AutoMLExperiment.AutoMLExperimentSettings settings, IChannel channel, int checkIntervalInMilliseconds) : base(settings, channel, checkIntervalInMilliseconds)
//        {
//            _logger = logger;
//            _pipeline = pipeline;
//        }

//        public override void OnPerformanceMetricsUpdatedHandler(TrialSettings trialSettings, TrialPerformanceMetrics metrics, CancellationTokenSource trialCancellationTokenSource)
//        {
//            var timeElapsed = DateTime.UtcNow - trialSettings.StartedAtUtc;
//            string lastEstimator = _pipeline.ToString(trialSettings.Parameter).Replace("ReplaceMissingValues=>Concatenate=>", "");

//            _logger.LogInformation($"{"Resources".PadLeft(10)} Trial #{trialSettings.TrialId.ToString().PadLeft(4)} - Estimator: {lastEstimator.PadRight(35)} -   Memory: {(metrics.MemoryUsage / 1024).ToString("0.0 GB").PadLeft(13)} -      CPU: {metrics.CpuUsage.ToString("0.0%").PadLeft(13)} - Duration: {timeElapsed.TotalMinutes:  0.0} minutes");

//            if (timeElapsed.TotalSeconds > 15 * 60)
//            {
//                _logger.LogWarning($"{"Resources".PadLeft(10)} Trial #{trialSettings.TrialId.ToString().PadLeft(4)} - Cancelling trial due to timeout");
//                trialCancellationTokenSource.Cancel();
//            }

//            if (metrics.PeakMemoryUsage > 36 * 1024)
//            {
//                _logger.LogWarning($"{"Resources".PadLeft(10)} Trial #{trialSettings.TrialId.ToString().PadLeft(4)} - Cancelling trial due to memory usage");
//                trialCancellationTokenSource.Cancel();
//            }

//            if (OperatingSystem.IsWindows())
//            {
//                ObjectQuery wql = new ObjectQuery("SELECT * FROM Win32_OperatingSystem");
//                ManagementObjectSearcher searcher = new ManagementObjectSearcher(wql);
//                ManagementObjectCollection results = searcher.Get();

//                foreach (ManagementObject result in results)
//                {
//                    ulong totalVisibleMemorySize = (ulong)result["TotalVisibleMemorySize"];
//                    ulong freePhysicalMemory = (ulong)result["FreePhysicalMemory"];
//                    ulong totalVirtualMemorySize = (ulong)result["TotalVirtualMemorySize"];
//                    ulong freeVirtualMemory = (ulong)result["FreeVirtualMemory"];

//                    if (freeVirtualMemory < 2 * 1024 * 1024)
//                    {
//                        _logger.LogWarning($"{"Resources".PadLeft(10)} Trial #{trialSettings.TrialId.ToString().PadLeft(4)} - Cancelling trial due to low virtual memory");
//                        trialCancellationTokenSource.Cancel();
//                    }
//                }
//            }
//        }
//    }
//}
namespace BBD.BodyMonitor.Models
{
    // The class MLExperimentPerformanceMonitor is currently commented out in the original source file.
    // If uncommented, it would likely implement a performance monitor for ML.NET AutoML experiments,
    // logging resource usage and potentially cancelling trials based on certain criteria.
}
