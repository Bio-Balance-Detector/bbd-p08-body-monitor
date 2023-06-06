using BBD.BodyMonitor.Configuration;
using BBD.BodyMonitor.Sessions;
using System.Management;

namespace BBD.BodyMonitor.Environment
{
    public class SystemInformation
    {
        public DateTimeOffset CurrentTimeUtc { get; }
        public string CPUName { get; }
        public double CPUFreq { get; }
        public uint CPUCores { get; }
        public string CPUFullName => this.CPUName == "unknown" ? this.CPUName : $"{this.CPUName} @ {this.CPUFreq:0.0} GHz ({this.CPUCores} cores)";
        public float RAMSize { get; set; }
        public ConnectedDevice[] Devices { get; set; }
        public BodyMonitorOptions Configuration { get; set; }
        public Session[] Sessions { get; set; }

        public SystemInformation()
        {
            this.CurrentTimeUtc = DateTimeOffset.Now;

            try
            {
                // Get CPU name, frequency and core count
                using (var cpuInfo = new ManagementObjectSearcher("select Name, MaxClockSpeed, NumberOfCores from Win32_Processor"))
                {
                    var cpuInfoCollection = cpuInfo.Get();
                    var cpuInfoEnum = cpuInfoCollection?.GetEnumerator();

                    if ((cpuInfoEnum != null) && (cpuInfoEnum.MoveNext()))
                    {
                        this.CPUName = cpuInfoEnum.Current["Name"].ToString().Trim();
                        this.CPUFreq = (uint)cpuInfoEnum.Current["MaxClockSpeed"] / 1000.0;
                        this.CPUCores = (uint)cpuInfoEnum.Current["NumberOfCores"];
                    }
                }
            }
            catch (Exception)
            {
                this.CPUName = "unknown";
                this.CPUFreq = 0;
                this.CPUCores = 0;
            }

            try
            {
                // Get physical RAM size in bytes
                using (var searcher = new ManagementObjectSearcher("select Capacity from Win32_PhysicalMemory"))
                {
                    foreach (var item in searcher.Get())
                    {
                        var size = (UInt64)item["Capacity"];
                        this.RAMSize += size / (1024f * 1024f * 1024f);
                    }
                }
            }
            catch (Exception)
            {
                this.RAMSize = 0;
            }
        }
    }
}
