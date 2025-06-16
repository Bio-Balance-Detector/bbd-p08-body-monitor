using Microsoft.ML;
using Microsoft.ML.AutoML;
using System.Management;

namespace BBD.BodyMonitor.Models
{
    /// <summary>
    /// Monitors the progress of an AutoML experiment, logging trial results and saving the best model.
    /// This class is intended for internal use within the application.
    /// </summary>
    internal class MLExperimentMonitor : IMonitor
    {
        private readonly ILogger _logger;
        private readonly MLContext _mlContext;
        private readonly List<TrialResult> _completedTrials;
        private readonly string _separatorString;
        private readonly SweepablePipeline _pipeline;
        private readonly DataViewSchema _dataSchema;
        private readonly string _modelFilename;

        /// <summary>
        /// Initializes a new instance of the <see cref="MLExperimentMonitor"/> class.
        /// </summary>
        /// <param name="logger">The logger instance for recording experiment progress and events.</param>
        /// <param name="mlContext">The ML.NET context.</param>
        /// <param name="pipeline">The sweepable pipeline being used in the AutoML experiment, used for logging estimator details.</param>
        /// <param name="dataSchema">The schema of the data used for training, required for saving the model.</param>
        /// <param name="modelFilename">The base filename for saving trained models. Best models will be saved with trial details appended.</param>
        public MLExperimentMonitor(ILogger logger, MLContext mlContext, SweepablePipeline pipeline, DataViewSchema dataSchema, string modelFilename)
        {
            _logger = logger;
            _mlContext = mlContext;
            _pipeline = pipeline;
            _dataSchema = dataSchema;
            _modelFilename = modelFilename;
            _completedTrials = new List<TrialResult>();
            _separatorString = new String(Enumerable.Repeat('-', 150).ToArray());
        }

        /// <summary>
        /// Gets the interval, in milliseconds, at which resource usage should be checked.
        /// Default is 6000 milliseconds (6 seconds).
        /// </summary>
        public int ResourceUsageCheckInterval => 6000;

        /// <summary>
        /// Called when a new best trial is found during the AutoML experiment.
        /// Logs information about the best trial and saves the corresponding model to a file.
        /// The model filename includes the trial ID and metric value.
        /// </summary>
        /// <param name="result">The <see cref="TrialResult"/> of the best trial.</param>
        public virtual void ReportBestTrial(TrialResult result)
        {
            string pipelineAlgorithm = _pipeline.ToString(result.TrialSettings.Parameter).Replace("ReplaceMissingValues=>Concatenate=>", "");
            _logger.LogInformation($"{"New Best".PadLeft(10)} Trial #{result.TrialSettings.TrialId.ToString().PadLeft(4)} - Estimator: {pipelineAlgorithm.PadRight(35)} -   Metric: {result.Metric.ToString("0.0000").PadLeft(13)}");
            // Sanitize filename components that might be problematic
            string sanitizedPipelineAlgorithm = string.Join("_", pipelineAlgorithm.Split(Path.GetInvalidFileNameChars()));
            string modelPath = $"{_modelFilename.Replace(".zip", "")}__#{result.TrialSettings.TrialId.ToString("000")}_{sanitizedPipelineAlgorithm}_{result.Metric.ToString("0.0000")}.zip";
            _mlContext.Model.Save(result.Model, _dataSchema, modelPath);
            _logger.LogInformation($"Saved new best model: {modelPath}");
            _logger.LogInformation($"{_separatorString}");
        }

        /// <summary>
        /// Called when an AutoML trial completes.
        /// Logs information about the completed trial, including its ID, estimator, metric, and duration.
        /// </summary>
        /// <param name="result">The <see cref="TrialResult"/> of the completed trial.</param>
        public virtual void ReportCompletedTrial(TrialResult result)
        {
            string pipelineAlgorithm = _pipeline.ToString(result.TrialSettings.Parameter).Replace("ReplaceMissingValues=>Concatenate=>", "");
            _logger.LogInformation($"{"Completed".PadLeft(10)} Trial #{result.TrialSettings.TrialId.ToString().PadLeft(4)} - Estimator: {pipelineAlgorithm.PadRight(35)} -   Metric: {result.Metric.ToString("0.0000").PadLeft(13)} - Duration: {result.DurationInMilliseconds / 1000 / 60:  0.0} minutes");
            _logger.LogInformation($"{_separatorString}");
            _completedTrials.Add(result);
        }

        /// <summary>
        /// Called when an AutoML trial fails.
        /// Logs information about the failed trial, including its ID, estimator, and exception details if available.
        /// </summary>
        /// <param name="trialSettings">The <see cref="TrialSettings"/> of the failed trial.</param>
        /// <param name="exception">The exception that caused the trial to fail, if any. Defaults to null.</param>
        public virtual void ReportFailTrial(TrialSettings trialSettings, Exception? exception = null) // Added nullable annotation for exception
        {
            string pipelineAlgorithm = _pipeline.ToString(trialSettings.Parameter).Replace("ReplaceMissingValues=>Concatenate=>", "");
            string errorMessage = exception != null ? $" -   Error: {(exception.InnerException != null ? exception.InnerException.Message : exception.Message)}" : string.Empty;
            _logger.LogError($"{"Failed".PadLeft(10)} Trial #{trialSettings.TrialId.ToString().PadLeft(4)} - Estimator: {pipelineAlgorithm.PadRight(35)}{errorMessage}");
            if (exception != null)
            {
                _logger.LogDebug(exception, $"Full exception for failed Trial #{trialSettings.TrialId}:");
            }
            _logger.LogInformation($"{_separatorString}");
        }

        /// <summary>
        /// Called when an AutoML trial starts running.
        /// Logs information that a trial has started.
        /// </summary>
        /// <param name="trialSettings">The <see cref="TrialSettings"/> of the trial that is now running.</param>
        public virtual void ReportRunningTrial(TrialSettings trialSettings)
        {
            _logger.LogInformation($"{"Running".PadLeft(10)} Trial #{trialSettings.TrialId.ToString().PadLeft(4)} - Estimator: {_pipeline.ToString(trialSettings.Parameter).Replace("ReplaceMissingValues=>Concatenate=>", "")}");
        }
    }
}
