using Microsoft.ML;
using Microsoft.ML.AutoML;
using System.Management;

namespace BBD.BodyMonitor.Models
{
    internal class MLExperimentMonitor : IMonitor
    {
        private readonly ILogger _logger;
        private readonly MLContext _mlContext;
        private readonly List<TrialResult> _completedTrials;
        private readonly string _separatorString;
        private readonly SweepablePipeline _pipeline;
        private readonly DataViewSchema _dataSchema;
        private readonly string _modelFilename;

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

        public int ResourceUsageCheckInterval => 6000;

        public virtual void ReportBestTrial(TrialResult result)
        {
            string pipelineAlgorithm = _pipeline.ToString(result.TrialSettings.Parameter).Replace("ReplaceMissingValues=>Concatenate=>", "");
            _logger.LogInformation($"{"New Best".PadLeft(10)} Trial #{result.TrialSettings.TrialId.ToString().PadLeft(4)} - Estimator: {pipelineAlgorithm.PadRight(35)} -   Metric: {result.Metric.ToString("0.0000").PadLeft(13)}");
            _mlContext.Model.Save(result.Model, _dataSchema, $"{_modelFilename.Replace(".zip", "")}__#{result.TrialSettings.TrialId.ToString("000")}_{result.Metric.ToString("0.0000")}.zip");
            _logger.LogInformation($"{_separatorString}");
        }

        public virtual void ReportCompletedTrial(TrialResult result)
        {
            string pipelineAlgorithm = _pipeline.ToString(result.TrialSettings.Parameter).Replace("ReplaceMissingValues=>Concatenate=>", "");
            _logger.LogInformation($"{"Completed".PadLeft(10)} Trial #{result.TrialSettings.TrialId.ToString().PadLeft(4)} - Estimator: {pipelineAlgorithm.PadRight(35)} -   Metric: {result.Metric.ToString("0.0000").PadLeft(13)} - Duration: {result.DurationInMilliseconds / 1000 / 60:  0.0} minutes");
            _logger.LogInformation($"{_separatorString}");
            _completedTrials.Add(result);
        }

        public virtual void ReportFailTrial(TrialSettings trialSettings, Exception exception = null)
        {
            string pipelineAlgorithm = _pipeline.ToString(trialSettings.Parameter).Replace("ReplaceMissingValues=>Concatenate=>", "");
            string errorMessage = exception != null ? $" -   Error: {(exception.InnerException != null ? exception.InnerException.Message : exception.Message)}" : string.Empty;
            _logger.LogError($"{"Failed".PadLeft(10)} Trial #{trialSettings.TrialId.ToString().PadLeft(4)} - Estimator: {pipelineAlgorithm.PadRight(35)}{errorMessage}");
            _logger.LogInformation($"{_separatorString}");
        }

        public virtual void ReportRunningTrial(TrialSettings trialSettings)
        {
            _logger.LogInformation($"{"Running".PadLeft(10)} Trial #{trialSettings.TrialId.ToString().PadLeft(4)}");
        }
    }
}
