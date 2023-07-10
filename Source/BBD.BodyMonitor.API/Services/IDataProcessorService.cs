using BBD.BodyMonitor.Configuration;
using BBD.BodyMonitor.Environment;
using BBD.BodyMonitor.Sessions;

namespace BBD.BodyMonitor.Services
{
    public interface IDataProcessorService
    {
        BodyMonitorOptions GetConfig();
        void SetConfig(BodyMonitorOptions config);
        ConnectedDevice[] ListDevices();
        string? StartDataAcquisition(string deviceSerialNumber, Session session);
        bool StopDataAcquisition(string deviceSerialNumber);
        void GenerateMLCSV(string foldername, MLProfile mlProfile, bool includeHeaders, string tagFilterExpression, string validLabelExpression, string balanceOnTag, int? maxRows);
        void GenerateVideo(string foldername, MLProfile mlProfile, double framerate);
        void FrequencyResponseAnalysis(string deviceSerialNumber);
        void GenerateFFT(string foldername, MLProfile mlProfile, float interval);
        void GenerateEDF(string dataFoldername, DateTime fromDateTime, DateTime toDateTime);
    }
}
