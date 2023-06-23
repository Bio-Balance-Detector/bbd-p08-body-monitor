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
        public string CPUFullName => CPUName == "unknown" ? CPUName : $"{CPUName} @ {CPUFreq:0.0} GHz ({CPUCores} cores)";
        public float RAMSize { get; set; }
        public string[] IPAddresses { get; set; }
        public ConnectedDevice[] Devices { get; set; }
        public BodyMonitorOptions Configuration { get; set; }
        public Location[] Locations { get; set; }
        public Subject[] Subjects { get; set; }
        public Session[] Sessions { get; set; }

        public SystemInformation()
        {
            CurrentTimeUtc = DateTimeOffset.Now;

            try
            {
                // Get CPU name, frequency and core count
                using ManagementObjectSearcher cpuInfo = new("select Name, MaxClockSpeed, NumberOfCores from Win32_Processor");
                ManagementObjectCollection cpuInfoCollection = cpuInfo.Get();
                ManagementObjectCollection.ManagementObjectEnumerator? cpuInfoEnum = cpuInfoCollection?.GetEnumerator();

                if ((cpuInfoEnum != null) && cpuInfoEnum.MoveNext())
                {
                    CPUName = cpuInfoEnum.Current["Name"].ToString().Trim();
                    CPUFreq = (uint)cpuInfoEnum.Current["MaxClockSpeed"] / 1000.0;
                    CPUCores = (uint)cpuInfoEnum.Current["NumberOfCores"];
                }
            }
            catch (Exception)
            {
                CPUName = "unknown";
                CPUFreq = 0;
                CPUCores = 0;
            }

            try
            {
                // Get physical RAM size in bytes
                using ManagementObjectSearcher searcher = new("select Capacity from Win32_PhysicalMemory");
                foreach (ManagementBaseObject item in searcher.Get())
                {
                    ulong size = (ulong)item["Capacity"];
                    RAMSize += size / (1024f * 1024f * 1024f);
                }
            }
            catch (Exception)
            {
                RAMSize = 0;
            }
        }
    }
}
