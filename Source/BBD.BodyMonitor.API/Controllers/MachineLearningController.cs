using BBD.BodyMonitor.Configuration;
using BBD.BodyMonitor.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.ML;
using Microsoft.ML.AutoML;
using Microsoft.ML.AutoML.CodeGen;
using Microsoft.ML.SearchSpace.Option;
using Microsoft.ML.SearchSpace;
using System.Reflection.Emit;
using BBD.BodyMonitor.Models;
using Microsoft.ML.Runtime;

namespace BBD.BodyMonitor.API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class MachineLearningController : ControllerBase
    {
        private readonly ILogger<MachineLearningController> _logger;
        private readonly IDataProcessorService _dataProcessor;

        public MachineLearningController(ILogger<MachineLearningController> logger, IDataProcessorService dataProcessor)
        {
            _logger = logger;
            _dataProcessor = dataProcessor;
        }

        /// <summary>
        /// Prepare the data for the machine learning model training and save it as a .csv file.
        /// </summary>
        /// <param name="trainingDataFoldername">The folder where the training data is located</param>
        /// <param name="mlProfileName">Begining of the ML profile name (eg. "ML14")</param>
        /// <param name="tagFilterExpression">Tag filtering expression to select a subset of the data (eg. "Subject_" or "AttachedTo_LeftForearm || AttachedTo_RightForearm")</param>
        /// <param name="validLabelExpression">List of labels to add to the CSV file (eg. "Session.SegmentedData.Sleep.Level" or "Subject_0xBAC08836" or "Session.SegmentedData.Sleep.Level+Session.SegmentedData.HeartRate.BeatsPerMinute")</param>
        /// <param name="balanceOnTag">Make sure that the number of items in the CSV file that has this value and the ones that don't have are balanced (eg. "Subject_None")</param>
        /// <param name="maxRows">The maximum number of row in the CSV file</param>
        /// <returns>The identifier of the task.</returns>
        [HttpGet]
        [Route("pretraining/{trainingDataFoldername}/{mlProfileName}/{tagFilterExpression?}/{validLabelExpression?}/{balanceOnTag?}/{maxRows?}")]
        public int PrepareTraining(string trainingDataFoldername, string mlProfileName, string tagFilterExpression, string validLabelExpression, string balanceOnTag, int? maxRows)
        {
            int taskId = Task.Run(() =>
            {
                BodyMonitorOptions config = _dataProcessor.GetConfig();

                var mlProfile = config.MachineLearning.Profiles.FirstOrDefault(p => p.Name.StartsWith(mlProfileName));

                if (mlProfile == null)
                {
                    _logger.LogError($"The profile '{mlProfileName}' was not found in the machine learning profile list. Make sure that you have it defined in the appsettings.json file.");
                    return;
                }
                else
                {
                    _logger.LogInformation($"Generating ML CSV and mbconfig files based on the machine learning profile '{mlProfile.Name}'.");
                    _dataProcessor.GenerateMLCSV(trainingDataFoldername, mlProfile, config.MachineLearning.CSVHeaders, tagFilterExpression, validLabelExpression, balanceOnTag, maxRows);
                }
            }).Id;

            return taskId;
        }

        [HttpGet]
        [Route("train/{preparedTrainingDataFilename}/{mlProfileName}/{trainingTimeInSeconds}")]
        public int StartTraining(string preparedTrainingDataFilename, string mlProfileName, uint trainingTimeInSeconds)

        {
            if (!System.IO.File.Exists(preparedTrainingDataFilename))
            {
                preparedTrainingDataFilename = Path.Combine(_dataProcessor.GetConfig().DataDirectory, Path.GetFileName(preparedTrainingDataFilename));
            }

            if (!System.IO.File.Exists(preparedTrainingDataFilename))
            {
                return 0;
            }

            string modelFilename = Path.Combine(_dataProcessor.GetConfig().DataDirectory, Path.GetFileNameWithoutExtension(preparedTrainingDataFilename) + ".zip");

            int taskId = Task.Run(async () =>
            {
                _logger.LogInformation("Starting model training for {length} seconds, using '{filename}' as data source with the '{profile}' profile.", trainingTimeInSeconds, Path.GetFileName(preparedTrainingDataFilename), mlProfileName);

                var mlContext = new MLContext();

                IDataView? data = null;

                if (mlProfileName.StartsWith("MLP05"))
                {
                    data = mlContext.Data.LoadFromTextFile<MLProfiles.MLP05>(preparedTrainingDataFilename, hasHeader: true, separatorChar: ',');
                }
                else if (mlProfileName.StartsWith("MLP06"))
                {
                    data = mlContext.Data.LoadFromTextFile<MLProfiles.MLP06>(preparedTrainingDataFilename, hasHeader: true, separatorChar: ',');
                }
                else if (mlProfileName.StartsWith("MLP09"))
                {
                    data = mlContext.Data.LoadFromTextFile<MLProfiles.MLP09>(preparedTrainingDataFilename, hasHeader: true, separatorChar: ',');
                }
                else if (mlProfileName.StartsWith("MLP10"))
                {
                    data = mlContext.Data.LoadFromTextFile<MLProfiles.MLP10>(preparedTrainingDataFilename, hasHeader: true, separatorChar: ',');
                }
                else if (mlProfileName.StartsWith("MLP12"))
                {
                    data = mlContext.Data.LoadFromTextFile<MLProfiles.MLP12>(preparedTrainingDataFilename, hasHeader: true, separatorChar: ',');
                }
                else if (mlProfileName.StartsWith("MLP14"))
                {
                    data = mlContext.Data.LoadFromTextFile<MLProfiles.MLP14>(preparedTrainingDataFilename, hasHeader: true, separatorChar: ',');
                }
                else if (mlProfileName.StartsWith("MLP15"))
                {
                    data = mlContext.Data.LoadFromTextFile<MLProfiles.MLP15>(preparedTrainingDataFilename, hasHeader: true, separatorChar: ',');
                }
                else if (mlProfileName.StartsWith("MLP16"))
                {
                    data = mlContext.Data.LoadFromTextFile<MLProfiles.MLP16>(preparedTrainingDataFilename, hasHeader: true, separatorChar: ',');
                }
                else
                {
                    throw new Exception($"ML Profile '{mlProfileName}' is not supported yet.");
                }

                var trainTestData = mlContext.Data.TrainTestSplit(data, testFraction: 0.1);


                // We have to limit the search space for LightGBM to prevent out of memory expecion. See: https://github.com/dotnet/machinelearning/issues/6465#issuecomment-1335148576
                var lgbmDefaultOption = new LgbmOption
                {
                    LabelColumnName = "Label",
                    FeatureColumnName = "Features",
                };
                var lgbmSearchSpace = new SearchSpace<LgbmOption>(lgbmDefaultOption);

                // use a smaller NumberOfLeaves and NumberOfTrees to avoid OOM
                lgbmSearchSpace["NumberOfLeaves"] = new UniformIntOption(4, 1024 * 4, true, 4);
                lgbmSearchSpace["NumberOfTrees"] = new UniformIntOption(4, 1024 * 4, true, 4);

                var pipeline =
                    mlContext.Auto().Featurizer(trainTestData.TrainSet, numericColumns: new[] { "Features" })
                        //.Append(mlContext.Auto().Regression(useFastTree: true, useLbfgs: false, useSdca: false, useFastForest: true, useLgbm: false));
                        .Append(mlContext.Auto().Regression(useFastTree: true, useLbfgs: true, useSdca: true, useFastForest: true, useLgbm: false));
                        //.Append(mlContext.Auto().Regression(useFastTree: false, useLbfgs: false, useSdca: false, useFastForest: false, useLgbm: true, lgbmSearchSpace: lgbmSearchSpace));

                var monitor = new MLExperimentMonitor(_logger, mlContext, pipeline, data.Schema, modelFilename);

                var experiment = mlContext.Auto().CreateExperiment();

                experiment
                    .SetPipeline(pipeline)
                    .SetTrainingTimeInSeconds(trainingTimeInSeconds)
                    .SetRegressionMetric(RegressionMetric.RSquared, labelColumn: "Label")
                    .SetDataset(trainTestData.TrainSet, trainTestData.TestSet)
                    .SetMonitor(monitor);
                    //.SetPerformanceMonitor((service) =>
                    //{
                    //    var channel = service.GetService<IChannel>();
                    //    var settings = service.GetRequiredService<AutoMLExperiment.AutoMLExperimentSettings>();
                    //    return new MLExperimentPerformanceMonitor(_logger, pipeline, settings, channel, 100);
                    //});

                var result = await experiment.RunAsync();

                _logger.LogInformation($"AutoML result: {result.Metric}. Saving model as '{modelFilename}'");

                mlContext.Model.Save(result.Model, data.Schema, modelFilename);
            }).Id;

            return taskId;
        }
    }
}