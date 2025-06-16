using BBD.BodyMonitor.Configuration;
using BBD.BodyMonitor.Sessions;
using System.Management;
using System.Runtime.InteropServices;

namespace BBD.BodyMonitor.Environment
{
    /// <summary>
    /// Provides information about the system environment, including OS, CPU, RAM, and application-specific details.
    /// </summary>
    public class SystemInformation
    {
        /// <summary>
        /// Gets the current UTC date and time when the SystemInformation object was created.
        /// </summary>
        public DateTimeOffset CurrentTimeUtc { get; }
        /// <summary>
        /// Gets the description of the operating system.
        /// </summary>
        public string OperatingSystem { get; private set; }
        /// <summary>
        /// Gets the description of the .NET runtime.
        /// </summary>
        public string Runtime { get; private set; }
        /// <summary>
        /// Gets the name of the CPU. Retrieved using WMI, may be "unknown" if WMI query fails.
        /// </summary>
        public string CPUName { get; }
        /// <summary>
        /// Gets the maximum clock speed of the CPU in GHz. Retrieved using WMI, may be 0 if WMI query fails.
        /// </summary>
        public double CPUFreq { get; }
        /// <summary>
        /// Gets the number of cores in the CPU. Retrieved using WMI, may be 0 if WMI query fails.
        /// </summary>
        public uint CPUCores { get; }
        /// <summary>
        /// Gets a full descriptive string for the CPU, including name, frequency, and core count.
        /// Returns "unknown" if CPUName is "unknown".
        /// </summary>
        public string CPUFullName => CPUName == "unknown" ? CPUName : $"{CPUName} @ {CPUFreq:0.0} GHz ({CPUCores} cores)";
        /// <summary>
        /// Gets or sets the total physical RAM size in Gigabytes (GB). Retrieved using WMI, may be 0 if WMI query fails.
        /// </summary>
        public float RAMSize { get; set; }
        /// <summary>
        /// Gets or sets an array of IP addresses for the system. This property is not populated by the constructor.
        /// </summary>
        public string[]? IPAddresses { get; set; }
        /// <summary>
        /// Gets or sets an array of connected data acquisition devices. This property is not populated by the constructor.
        /// </summary>
        public ConnectedDevice[]? Devices { get; set; }
        /// <summary>
        /// Gets or sets the application's configuration options. This property is not populated by the constructor.
        /// </summary>
        public BodyMonitorOptions? Configuration { get; set; }
        /// <summary>
        /// Gets or sets an array of defined locations. This property is not populated by the constructor.
        /// </summary>
        public Location[]? Locations { get; set; }
        /// <summary>
        /// Gets or sets an array of defined subjects. This property is not populated by the constructor.
        /// </summary>
        public Subject[]? Subjects { get; set; }
        /// <summary>
        /// Gets or sets an array of recorded sessions. This property is not populated by the constructor.
        /// </summary>
        public Session[]? Sessions { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SystemInformation"/> class.
        /// Populates OS, Runtime, CPU, and RAM information.
        /// </summary>
        /// <remarks>
        /// CPU and RAM information are retrieved using WMI (Windows Management Instrumentation) and may only be available on Windows.
        /// If WMI queries fail, CPU properties will be set to default values ("unknown", 0) and RAMSize to 0.
        /// Other properties like IPAddresses, Devices, Configuration, Locations, Subjects, and Sessions are not populated by this constructor
        /// and should be set separately if needed.
        /// </remarks>
        public SystemInformation()
        {
            CurrentTimeUtc = DateTimeOffset.Now;

            OperatingSystem = $"{RuntimeInformation.OSDescription} ({RuntimeInformation.OSArchitecture.ToString().ToLowerInvariant()})";
            Runtime = $"{RuntimeInformation.FrameworkDescription} ({RuntimeInformation.RuntimeIdentifier.ToString().ToLowerInvariant()})";

            try
            {
                // Get CPU name, frequency and core count using WMI
                // This part will likely only work on Windows.
                using ManagementObjectSearcher cpuInfo = new("select Name, MaxClockSpeed, NumberOfCores from Win32_Processor");
                ManagementObjectCollection cpuInfoCollection = cpuInfo.Get();
                ManagementObjectCollection.ManagementObjectEnumerator? cpuInfoEnum = cpuInfoCollection?.GetEnumerator();

                if ((cpuInfoEnum != null) && cpuInfoEnum.MoveNext() && cpuInfoEnum.Current["Name"] != null && cpuInfoEnum.Current["MaxClockSpeed"] != null && cpuInfoEnum.Current["NumberOfCores"] != null)
                {
                    CPUName = cpuInfoEnum.Current["Name"].ToString()?.Trim() ?? "unknown";
                    CPUFreq = Convert.ToUInt32(cpuInfoEnum.Current["MaxClockSpeed"]) / 1000.0; // MaxClockSpeed is in MHz
                    CPUCores = Convert.ToUInt32(cpuInfoEnum.Current["NumberOfCores"]);
                }
                else
                {
                    CPUName = "unknown";
                    CPUFreq = 0;
                    CPUCores = 0;
                }
            }
            catch (Exception ex) // Catch specific exceptions like PlatformNotSupportedException or ManagementException if possible
            {
                // Log exception details if a logger is available
                System.Diagnostics.Debug.WriteLine($"WMI CPU query failed: {ex.Message}");
                CPUName = "unknown";
                CPUFreq = 0;
                CPUCores = 0;
            }

            try
            {
                // Get physical RAM size in bytes using WMI
                // This part will likely only work on Windows.
                using ManagementObjectSearcher searcher = new("select Capacity from Win32_PhysicalMemory");
                ulong totalRamBytes = 0;
                foreach (ManagementBaseObject item in searcher.Get())
                {
                    if (item["Capacity"] != null)
                    {
                        totalRamBytes += Convert.ToUInt64(item["Capacity"]);
                    }
                }
                RAMSize = totalRamBytes / (1024f * 1024f * 1024f); // Convert bytes to GB
            }
            catch (Exception ex) // Catch specific exceptions
            {
                // Log exception details
                System.Diagnostics.Debug.WriteLine($"WMI RAM query failed: {ex.Message}");
                RAMSize = 0;
            }
        }
    }
}
