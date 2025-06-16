using BBD.BodyMonitor.Configuration;
using BBD.BodyMonitor.Environment;
using BBD.BodyMonitor.Indicators;
using BBD.BodyMonitor.Sessions;

namespace BBD.BodyMonitor.Services
{
    /// <summary>
    /// Defines the contract for services that process data, manage data acquisition,
    /// and generate various outputs like CSV files, videos, FFT data, and EDF files.
    /// </summary>
    public interface IDataProcessorService
    {
        /// <summary>
        /// Retrieves the current application configuration.
        /// </summary>
        /// <returns>The current <see cref="BodyMonitorOptions"/>.</returns>
        BodyMonitorOptions GetConfig();

        /// <summary>
        /// Sets the application configuration.
        /// </summary>
        /// <param name="config">The <see cref="BodyMonitorOptions"/> to apply.</param>
        void SetConfig(BodyMonitorOptions config);

        /// <summary>
        /// Lists all connected and available data acquisition devices.
        /// </summary>
        /// <returns>An array of <see cref="ConnectedDevice"/> objects.</returns>
        ConnectedDevice[] ListDevices();

        /// <summary>
        /// Starts a new data acquisition session using the specified device.
        /// </summary>
        /// <param name="deviceSerialNumber">The serial number of the device to use for data acquisition.</param>
        /// <param name="session">The <see cref="Session"/> object that will store information about this acquisition session.</param>
        /// <returns>A string identifier for the acquisition task/thread, or null if starting failed.</returns>
        string? StartDataAcquisition(string deviceSerialNumber, Session session);

        /// <summary>
        /// Stops an ongoing data acquisition session for the specified device.
        /// </summary>
        /// <param name="deviceSerialNumber">The serial number of the device whose acquisition session should be stopped.</param>
        /// <returns>True if the acquisition was stopped successfully, false otherwise.</returns>
        bool StopDataAcquisition(string deviceSerialNumber);

        /// <summary>
        /// Generates a CSV file suitable for machine learning training from session data.
        /// </summary>
        /// <param name="foldername">The name of the folder containing the session data.</param>
        /// <param name="mlProfile">The <see cref="MLProfile"/> defining data processing and feature engineering steps.</param>
        /// <param name="includeHeaders">A boolean indicating whether to include headers in the CSV file.</param>
        /// <param name="tagFilterExpression">An optional tag filtering expression to select a subset of data.</param>
        /// <param name="validLabelExpression">An optional expression defining valid labels to include.</param>
        /// <param name="balanceOnTag">An optional tag used to balance the dataset.</param>
        /// <param name="maxRows">An optional maximum number of rows to include in the CSV file.</param>
        void GenerateMLCSV(string foldername, MLProfile mlProfile, bool includeHeaders, string tagFilterExpression, string validLabelExpression, string balanceOnTag, int? maxRows);

        /// <summary>
        /// Generates a video file from session data based on a machine learning profile.
        /// </summary>
        /// <param name="foldername">The name of the folder containing the session data.</param>
        /// <param name="mlProfile">The <see cref="MLProfile"/> to use for visualization parameters.</param>
        /// <param name="framerate">The frame rate of the generated video.</param>
        void GenerateVideo(string foldername, MLProfile mlProfile, double framerate);

        /// <summary>
        /// Performs a frequency response analysis for a specified device.
        /// </summary>
        /// <param name="deviceSerialNumber">The serial number of the device to analyze.</param>
        void FrequencyResponseAnalysis(string deviceSerialNumber);

        /// <summary>
        /// Generates Fast Fourier Transform (FFT) data from session files.
        /// </summary>
        /// <param name="foldername">The name of the folder containing the session data.</param>
        /// <param name="mlProfile">The <see cref="MLProfile"/> to be used for processing.</param>
        /// <param name="interval">The interval in seconds for FFT calculations.</param>
        void GenerateFFT(string foldername, MLProfile mlProfile, float interval);

        /// <summary>
        /// Generates an European Data Format (EDF) file from session data within a given time range.
        /// </summary>
        /// <param name="dataFoldername">The name of the folder containing the session data.</param>
        /// <param name="fromDateTime">The start date and time (inclusive) of the data to include.</param>
        /// <param name="toDateTime">The end date and time (inclusive) of the data to include.</param>
        void GenerateEDF(string dataFoldername, DateTime fromDateTime, DateTime toDateTime);

        /// <summary>
        /// Retrieves the latest evaluated bio-indicator results.
        /// </summary>
        /// <returns>An array of <see cref="IndicatorEvaluationResult"/> objects, or null if no results are available.</returns>
        IndicatorEvaluationResult[]? GetLatestIndicatorResults();
    }
}
