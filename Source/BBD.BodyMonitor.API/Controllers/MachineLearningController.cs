using BBD.BodyMonitor.Configuration;
using BBD.BodyMonitor.Models;
using BBD.BodyMonitor.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.ML;
using Microsoft.ML.AutoML;
using Microsoft.ML.AutoML.CodeGen;
using Microsoft.ML.SearchSpace;
using Microsoft.ML.SearchSpace.Option;

namespace BBD.BodyMonitor.Controllers
{
    /// <summary>
    /// Controller for managing machine learning processes, including data preparation and model training.
    /// </summary>
    [ApiController]
    [Route("[controller]")]
    public class MachineLearningController : ControllerBase
    {
        private readonly ILogger<MachineLearningController> _logger;
        private readonly IDataProcessorService _dataProcessor;

        /// <summary>
        /// Initializes a new instance of the <see cref="MachineLearningController"/> class.
        /// </summary>
        /// <param name="logger">The logger instance for logging messages.</param>
        /// <param name="dataProcessor">The data processor service for handling data operations.</param>
        public MachineLearningController(ILogger<MachineLearningController> logger, IDataProcessorService dataProcessor)
        {
            _logger = logger;
            _dataProcessor = dataProcessor;
        }

        /// <summary>
        /// Prepares training data for machine learning models and saves it as a CSV file.
        /// This process involves selecting data based on specified filters and balancing the dataset if required.
        /// </summary>
        /// <param name="trainingDataFoldername">The name of the folder containing the training data. This folder is typically located within the configured data directory.</param>
        /// <param name="mlProfileName">The beginning of the machine learning profile name (e.g., "ML14"). This profile defines data processing and feature engineering steps.</param>
        /// <param name="tagFilterExpression">Optional. A tag filtering expression to select a subset of the data (e.g., "Subject_" or "AttachedTo_LeftForearm || AttachedTo_RightForearm").</param>
        /// <param name="validLabelExpression">Optional. An expression defining the labels to be included in the CSV file (e.g., "Session.SegmentedData.Sleep.Level" or "Subject_0xBAC08836" or "Session.SegmentedData.Sleep.Level+Session.SegmentedData.HeartRate.BeatsPerMinute").</param>
        /// <param name="balanceOnTag">Optional. A tag used to balance the dataset, ensuring an equal number of items with and without this tag (e.g., "Subject_None").</param>
        /// <param name="maxRows">Optional. The maximum number of rows to include in the generated CSV file.</param>
        /// <returns>The ID of the background task performing the data preparation.</returns>
        /// <response code="200">Returns the ID of the background task successfully initiated for data preparation.</response>
        /// <response code="404">If the specified ML profile is not found (though the current implementation logs an error and returns a task ID).</response>
        [HttpGet]
        [Route("pretraining/{trainingDataFoldername}/{mlProfileName}/{tagFilterExpression?}/{validLabelExpression?}/{balanceOnTag?}/{maxRows?}")]
        public int PrepareTraining(string trainingDataFoldername, string mlProfileName, string tagFilterExpression, string validLabelExpression, string balanceOnTag, int? maxRows)
        {
            int taskId = Task.Run(() =>
            {
                BodyMonitorOptions config = _dataProcessor.GetConfig();

                MLProfile? mlProfile = config.MachineLearning.Profiles.FirstOrDefault(p => p.Name.StartsWith(mlProfileName));

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

        /// <summary>
        /// Starts the training process for a machine learning model using prepared data.
        /// </summary>
        /// <remarks>
        /// This method initiates an AutoML experiment to train a regression model.
        /// It uses the specified CSV file as input and trains for a defined duration.
        /// The resulting model is saved as a .zip file.
        /// </remarks>
        /// <param name="preparedTrainingDataFilename">The filename of the CSV file containing the prepared training data. If not an absolute path, it's assumed to be in the application's data directory.</param>
        /// <param name="mlProfileName">The name of the machine learning profile to use for training (e.g., "MLP05", "MLP14"). This determines the data schema and features.</param>
        /// <param name="trainingTimeInSeconds">The duration, in seconds, for which the training process should run.</param>
        /// <returns>The ID of the background task performing the model training. Returns 0 if the prepared training data file is not found (this should ideally be a 404 response).</returns>
        /// <response code="200">Returns the ID of the background task successfully initiated for model training.</response>
        /// <response code="404">If the prepared training data file is not found (current implementation returns 0 instead of a direct HTTP error).</response>
        /// <exception cref="Exception">Thrown if the specified ML Profile is not supported.</exception>
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

                MLContext mlContext = new();

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
                else
                {
                    data = mlProfileName.StartsWith("MLP14")
                        ? mlContext.Data.LoadFromTextFile<MLProfiles.MLP14>(preparedTrainingDataFilename, hasHeader: true, separatorChar: ',')
                        : mlProfileName.StartsWith("MLP15")
                                            ? mlContext.Data.LoadFromTextFile<MLProfiles.MLP15>(preparedTrainingDataFilename, hasHeader: true, separatorChar: ',')
                                            : mlProfileName.StartsWith("MLP16")
                                                                ? mlContext.Data.LoadFromTextFile<MLProfiles.MLP16>(preparedTrainingDataFilename, hasHeader: true, separatorChar: ',')
                                                                : throw new Exception($"ML Profile '{mlProfileName}' is not supported yet.");
                }

                DataOperationsCatalog.TrainTestData trainTestData = mlContext.Data.TrainTestSplit(data, testFraction: 0.1);


                // We have to limit the search space for LightGBM to prevent out of memory expecion. See: https://github.com/dotnet/machinelearning/issues/6465#issuecomment-1335148576
                LgbmOption lgbmDefaultOption = new()
                {
                    LabelColumnName = "Label",
                    FeatureColumnName = "Features",
                };
                SearchSpace<LgbmOption> lgbmSearchSpace = new(lgbmDefaultOption)
                {
                    // use a smaller NumberOfLeaves and NumberOfTrees to avoid OOM
                    ["NumberOfLeaves"] = new UniformIntOption(4, 1024 * 4, true, 4),
                    ["NumberOfTrees"] = new UniformIntOption(4, 1024 * 4, true, 4)
                };

                SweepablePipeline pipeline =
                    mlContext.Auto().Featurizer(trainTestData.TrainSet, numericColumns: new[] { "Features" })
                        .Append(mlContext.Auto().Regression(useFastTree: false, useFastForest: true, useLbfgsPoissonRegression: false, useSdca: false, useLgbm: false));
                //.Append(mlContext.Auto().Regression(useFastTree: true, useFastForest: true, useLbfgs: true, useSdca: true, useLgbm: false));
                //.Append(mlContext.Auto().Regression(useFastTree: false, useFastForest: false, useLbfgs: false, useSdca: false, useLgbm: true, lgbmSearchSpace: lgbmSearchSpace));

                MLExperimentMonitor monitor = new(_logger, mlContext, pipeline, data.Schema, modelFilename);

                AutoMLExperiment experiment = mlContext.Auto().CreateExperiment();

                _ = experiment
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

                TrialResult result = await experiment.RunAsync();

                _logger.LogInformation($"AutoML result: {result.Metric}. Saving model as '{modelFilename}'");

                mlContext.Model.Save(result.Model, data.Schema, modelFilename);
            }).Id;

            return taskId;
        }
    }
}