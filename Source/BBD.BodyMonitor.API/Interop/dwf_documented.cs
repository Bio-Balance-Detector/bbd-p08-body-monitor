using System.Runtime.InteropServices;

/// <summary>
/// Provides P/Invoke declarations for the DWF (Digilent WaveForms) library.
/// This class contains constants, enumerations, structures, and extern method declarations
/// for interacting with Digilent devices like Analog Discovery and Digital Discovery.
/// </summary>
public class dwf
{
    /// <summary>
    /// Represents a null or invalid device handle. This value is used to indicate that a device handle is not valid or has not been initialized.
    /// </summary>
    public const int hdwfNone = 0;

    // device enumeration filters
    /// <summary>Device enumeration filter: Enumerate all supported Digilent devices.</summary>
    public const int enumfilterAll = 0;
    /// <summary>Device enumeration filter: Enumerate Digilent Electronics Explorer devices.</summary>
    public const int enumfilterEExplorer = 1;
    /// <summary>Device enumeration filter: Enumerate Digilent Analog Discovery (original) devices.</summary>
    public const int enumfilterDiscovery = 2;
    /// <summary>Device enumeration filter: Enumerate Digilent Analog Discovery 2 devices.</summary>
    public const int enumfilterDiscovery2 = 3;
    /// <summary>Device enumeration filter: Enumerate Digilent Digital Discovery devices.</summary>
    public const int enumfilterDDiscovery = 4;
    /// <summary>Device enumeration filter: Enumerate Digilent Saluki devices (typically for internal use or specific applications).</summary>
    public const int enumfilterSaluki = 6;

    // device ID
    /// <summary>Device ID: Digilent Electronics Explorer.</summary>
    public const int devidEExplorer = 1;
    /// <summary>Device ID: Digilent Analog Discovery (original).</summary>
    public const int devidDiscovery = 2;
    /// <summary>Device ID: Digilent Analog Discovery 2.</summary>
    public const int devidDiscovery2 = 3;
    /// <summary>Device ID: Digilent Digital Discovery.</summary>
    public const int devidDDiscovery = 4;
    /// <summary>Device ID: Digilent Saluki (typically for internal use or specific applications).</summary>
    public const int devidSaluki = 6;

    // device version
    /// <summary>Device Version Identifier: Electronics Explorer Revision C.</summary>
    public const int devverEExplorerC = 2;
    /// <summary>Device Version Identifier: Electronics Explorer Revision E.</summary>
    public const int devverEExplorerE = 4;
    /// <summary>Device Version Identifier: Electronics Explorer Revision F.</summary>
    public const int devverEExplorerF = 5;
    /// <summary>Device Version Identifier: Analog Discovery Revision A.</summary>
    public const int devverDiscoveryA = 1;
    /// <summary>Device Version Identifier: Analog Discovery Revision B.</summary>
    public const int devverDiscoveryB = 2;
    /// <summary>Device Version Identifier: Analog Discovery Revision C.</summary>
    public const int devverDiscoveryC = 3;

    // trigger source
    /// <summary>Trigger Source: No trigger is configured or selected.</summary>
    public const byte trigsrcNone = 0;
    /// <summary>Trigger Source: PC (software) generated trigger. See <see cref="FDwfDeviceTriggerPC"/>.</summary>
    public const byte trigsrcPC = 1;
    /// <summary>Trigger Source: Internal analog input detector.</summary>
    public const byte trigsrcDetectorAnalogIn = 2;
    /// <summary>Trigger Source: Internal digital input detector.</summary>
    public const byte trigsrcDetectorDigitalIn = 3;
    /// <summary>Trigger Source: Signal from an analog input channel.</summary>
    public const byte trigsrcAnalogIn = 4;
    /// <summary>Trigger Source: Signal from a digital input channel or pin.</summary>
    public const byte trigsrcDigitalIn = 5;
    /// <summary>Trigger Source: Signal from a digital output channel or pin.</summary>
    public const byte trigsrcDigitalOut = 6;
    /// <summary>Trigger Source: Signal from analog output channel 1.</summary>
    public const byte trigsrcAnalogOut1 = 7;
    /// <summary>Trigger Source: Signal from analog output channel 2.</summary>
    public const byte trigsrcAnalogOut2 = 8;
    /// <summary>Trigger Source: Signal from analog output channel 3.</summary>
    public const byte trigsrcAnalogOut3 = 9;
    /// <summary>Trigger Source: Signal from analog output channel 4.</summary>
    public const byte trigsrcAnalogOut4 = 10;
    /// <summary>Trigger Source: External trigger input pin 1 (T1).</summary>
    public const byte trigsrcExternal1 = 11;
    /// <summary>Trigger Source: External trigger input pin 2 (T2).</summary>
    public const byte trigsrcExternal2 = 12;
    /// <summary>Trigger Source: External trigger input pin 3 (T3).</summary>
    public const byte trigsrcExternal3 = 13;
    /// <summary>Trigger Source: External trigger input pin 4 (T4).</summary>
    public const byte trigsrcExternal4 = 14;
    /// <summary>Trigger Source: A constant high signal level (always triggers if selected).</summary>
    public const byte trigsrcHigh = 15;
    /// <summary>Trigger Source: A constant low signal level (always triggers if selected).</summary>
    public const byte trigsrcLow = 16;

    // instrument states:
    /// <summary>Instrument State: The instrument is ready and idle.</summary>
    public const byte DwfStateReady = 0;
    /// <summary>Instrument State: The instrument is being configured by the host PC.</summary>
    public const byte DwfStateConfig = 4;
    /// <summary>Instrument State: The instrument is in a prefill state, acquiring pre-trigger samples.</summary>
    public const byte DwfStatePrefill = 5;
    /// <summary>Instrument State: The instrument is armed and waiting for a trigger event.</summary>
    public const byte DwfStateArmed = 1;
    /// <summary>Instrument State: The instrument is waiting, for instance, for a repeat trigger in scan screen or scan shift acquisition modes.</summary>
    public const byte DwfStateWait = 7;
    /// <summary>Instrument State: The instrument has been triggered or is currently running/acquiring data.</summary>
    public const byte DwfStateTriggered = 3;
    /// <summary>Instrument State: The instrument is running (equivalent to DwfStateTriggered for some instruments like Analog Out).</summary>
    public const byte DwfStateRunning = 3;
    /// <summary>Instrument State: The instrument operation (acquisition, generation) is done.</summary>
    public const byte DwfStateDone = 2;

    // Device configuration information items
    /// <summary>Device Configuration Information: Retrieves the number of analog input channels available on the device.</summary>
    public const byte DECIAnalogInChannelCount = 1;
    /// <summary>Device Configuration Information: Retrieves the number of analog output channels available on the device.</summary>
    public const byte DECIAnalogOutChannelCount = 2;
    /// <summary>Device Configuration Information: Retrieves the number of analog I/O channels (e.g., power supplies, references) available on the device.</summary>
    public const byte DECIAnalogIOChannelCount = 3;
    /// <summary>Device Configuration Information: Retrieves the number of digital input channels/pins available on the device.</summary>
    public const byte DECIDigitalInChannelCount = 4;
    /// <summary>Device Configuration Information: Retrieves the number of digital output channels/pins available on the device.</summary>
    public const byte DECIDigitalOutChannelCount = 5;
    /// <summary>Device Configuration Information: Retrieves the number of digital I/O channels/pins available on the device.</summary>
    public const byte DECIDigitalIOChannelCount = 6;
    /// <summary>Device Configuration Information: Retrieves the maximum buffer size (in samples) for the analog input instrument.</summary>
    public const byte DECIAnalogInBufferSize = 7;
    /// <summary>Device Configuration Information: Retrieves the maximum buffer size (in samples) for the analog output instrument.</summary>
    public const byte DECIAnalogOutBufferSize = 8;
    /// <summary>Device Configuration Information: Retrieves the maximum buffer size (in samples) for the digital input instrument.</summary>
    public const byte DECIDigitalInBufferSize = 9;
    /// <summary>Device Configuration Information: Retrieves the maximum buffer size (in samples) for the digital output instrument.</summary>
    public const byte DECIDigitalOutBufferSize = 10;

    // acquisition modes:
    /// <summary>Acquisition Mode: Acquire a single buffer of data.</summary>
    public const byte acqmodeSingle = 0;
    /// <summary>Acquisition Mode: Scan Shift. Acquire multiple buffers, shifting data for a rolling display effect. Useful for viewing slow signals.</summary>
    public const byte acqmodeScanShift = 1;
    /// <summary>Acquisition Mode: Scan Screen. Acquire multiple buffers, synchronizing with the screen refresh rate for a stable display.</summary>
    public const byte acqmodeScanScreen = 2;
    /// <summary>Acquisition Mode: Record. Continuously acquire data into a circular buffer, allowing for long acquisitions limited by PC memory.</summary>
    public const byte acqmodeRecord = 3;
    /// <summary>Acquisition Mode: Oversampling. Acquire data at a higher rate and then decimate/average to improve resolution or SNR.</summary>
    public const byte acqmodeOvers = 4;
    /// <summary>Acquisition Mode: Single (alternative, device-specific). Similar to acqmodeSingle, may have device-specific behavior.</summary>
    public const byte acqmodeSingle1 = 5;

    // analog acquisition filter:
    /// <summary>Analog Acquisition Filter: Decimation. Reduces sample rate by keeping one sample out of N.</summary>
    public const byte filterDecimate = 0;
    /// <summary>Analog Acquisition Filter: Averaging. Reduces sample rate by averaging N consecutive samples.</summary>
    public const byte filterAverage = 1;
    /// <summary>Analog Acquisition Filter: Min/Max. Reduces sample rate by keeping the minimum and maximum values from N consecutive samples.</summary>
    public const byte filterMinMax = 2;

    // analog in trigger mode:
    /// <summary>Analog In Trigger Mode: Trigger on a voltage edge (rising, falling, or either).</summary>
    public const byte trigtypeEdge = 0;
    /// <summary>Analog In Trigger Mode: Trigger on a pulse characteristic (e.g., pulse width).</summary>
    public const byte trigtypePulse = 1;
    /// <summary>Analog In Trigger Mode: Trigger on a voltage transition through a window or level.</summary>
    public const byte trigtypeTransition = 2;

    // trigger slope:
    /// <summary>Trigger Slope: Trigger on a rising edge of the signal.</summary>
    public const byte DwfTriggerSlopeRise = 0;
    /// <summary>Trigger Slope: Trigger on a falling edge of the signal.</summary>
    public const byte DwfTriggerSlopeFall = 1;
    /// <summary>Trigger Slope: Trigger on either a rising or a falling edge of the signal.</summary>
    public const byte DwfTriggerSlopeEither = 2;

    // trigger length condition
    /// <summary>Trigger Length Condition: Trigger if the pulse length is less than the specified time.</summary>
    public const byte triglenLess = 0;
    /// <summary>Trigger Length Condition: Trigger if the pulse length times out (e.g., stays in a state longer than expected).</summary>
    public const byte triglenTimeout = 1;
    /// <summary>Trigger Length Condition: Trigger if the pulse length is more than the specified time.</summary>
    public const byte triglenMore = 2;

    // error codes for the public static extern ints:
    /// <summary>DWF Error Code: No error occurred during the last operation.</summary>
    public const int dwfercNoErc = 0;
    /// <summary>DWF Error Code: An unknown or unspecified error occurred.</summary>
    public const int dwfercUnknownError = 1;
    /// <summary>DWF Error Code: API call timed out while waiting for a lock on a shared resource.</summary>
    public const int dwfercApiLockTimeout = 2;
    /// <summary>DWF Error Code: The specified device is already opened by this or another process.</summary>
    public const int dwfercAlreadyOpened = 3;
    /// <summary>DWF Error Code: The specified device or feature is not supported.</summary>
    public const int dwfercNotSupported = 4;
    /// <summary>DWF Error Code: An invalid parameter was supplied at argument position 0 of an API function.</summary>
    public const int dwfercInvalidParameter0 = 0x10;
    /// <summary>DWF Error Code: An invalid parameter was supplied at argument position 1 of an API function.</summary>
    public const int dwfercInvalidParameter1 = 0x11;
    /// <summary>DWF Error Code: An invalid parameter was supplied at argument position 2 of an API function.</summary>
    public const int dwfercInvalidParameter2 = 0x12;
    /// <summary>DWF Error Code: An invalid parameter was supplied at argument position 3 of an API function.</summary>
    public const int dwfercInvalidParameter3 = 0x13;
    /// <summary>DWF Error Code: An invalid parameter was supplied at argument position 4 of an API function.</summary>
    public const int dwfercInvalidParameter4 = 0x14;

    // analog out signal types
    /// <summary>Analog Out Signal Type: DC (constant voltage) level.</summary>
    public const byte funcDC = 0;
    /// <summary>Analog Out Signal Type: Sinusoidal waveform.</summary>
    public const byte funcSine = 1;
    /// <summary>Analog Out Signal Type: Square waveform.</summary>
    public const byte funcSquare = 2;
    /// <summary>Analog Out Signal Type: Triangle waveform.</summary>
    public const byte funcTriangle = 3;
    /// <summary>Analog Out Signal Type: Ramp-up (sawtooth) waveform.</summary>
    public const byte funcRampUp = 4;
    /// <summary>Analog Out Signal Type: Ramp-down (reverse sawtooth) waveform.</summary>
    public const byte funcRampDown = 5;
    /// <summary>Analog Out Signal Type: Noise waveform.</summary>
    public const byte funcNoise = 6;
    /// <summary>Analog Out Signal Type: Pulse waveform (with configurable duty cycle).</summary>
    public const byte funcPulse = 7;
    /// <summary>Analog Out Signal Type: Trapezium waveform.</summary>
    public const byte funcTrapezium = 8;
    /// <summary>Analog Out Signal Type: Sine power waveform (sine squared).</summary>
    public const byte funcSinePower = 9;
    /// <summary>Analog Out Signal Type: Custom waveform defined by user-supplied samples.</summary>
    public const byte funcCustom = 30;
    /// <summary>Analog Out Signal Type: Play (streaming arbitrary waveform generator) mode.</summary>
    public const byte funcPlay = 31;

    // analog io channel node types
    /// <summary>Analog I/O Channel Node Type: Master enable for the channel or a specific function.</summary>
    public const byte analogioEnable = 1;
    /// <summary>Analog I/O Channel Node Type: Voltage setting or measurement.</summary>
    public const byte analogioVoltage = 2;
    /// <summary>Analog I/O Channel Node Type: Current setting or measurement.</summary>
    public const byte analogioCurrent = 3;
    /// <summary>Analog I/O Channel Node Type: Power measurement.</summary>
    public const byte analogioPower = 4;
    /// <summary>Analog I/O Channel Node Type: Temperature measurement.</summary>
    public const byte analogioTemperature = 5;

    /// <summary>Analog Out Node Type: Represents the main carrier signal generator.</summary>
    public const byte AnalogOutNodeCarrier = 0;
    /// <summary>Analog Out Node Type: Represents the Frequency Modulation (FM) generator.</summary>
    public const byte AnalogOutNodeFM = 1;
    /// <summary>Analog Out Node Type: Represents the Amplitude Modulation (AM) generator.</summary>
    public const byte AnalogOutNodeAM = 2;

    /// <summary>Analog Out Operation Mode: The channel operates as a voltage source.</summary>
    public const byte DwfAnalogOutModeVoltage = 0;
    /// <summary>Analog Out Operation Mode: The channel operates as a current source.</summary>
    public const byte DwfAnalogOutModeCurrent = 1;

    /// <summary>Analog Out Idle Behavior: Disable the output when idle.</summary>
    public const byte DwfAnalogOutIdleDisable = 0;
    /// <summary>Analog Out Idle Behavior: Output the configured offset voltage when idle.</summary>
    public const byte DwfAnalogOutIdleOffset = 1;
    /// <summary>Analog Out Idle Behavior: Output the initial value of the configured waveform when idle.</summary>
    public const byte DwfAnalogOutIdleInitial = 2;

    /// <summary>Digital In Clock Source: Use the internal clock of the device.</summary>
    public const byte DwfDigitalInClockSourceInternal = 0;
    /// <summary>Digital In Clock Source: Use an external clock signal provided to a DIO pin.</summary>
    public const byte DwfDigitalInClockSourceExternal = 1;

    /// <summary>Digital In Sample Mode: Standard sampling mode.</summary>
    public const byte DwfDigitalInSampleModeSimple = 0;
    /// <summary>
    /// Digital In Sample Mode: Noise sampling mode.
    /// Captures alternate samples: noise|sample|noise|sample|...
    /// 'Noise' indicates that more than one transition occurred between two sample clock edges.
    /// </summary>
    public const byte DwfDigitalInSampleModeNoise = 1;

    /// <summary>Digital Out Output Type: Push-Pull drive (actively drives high and low).</summary>
    public const byte DwfDigitalOutOutputPushPull = 0;
    /// <summary>Digital Out Output Type: Open-Drain drive (actively drives low, high-impedance when high).</summary>
    public const byte DwfDigitalOutOutputOpenDrain = 1;
    /// <summary>Digital Out Output Type: Open-Source drive (actively drives high, high-impedance when low). Not commonly available.</summary>
    public const byte DwfDigitalOutOutputOpenSource = 2;
    /// <summary>Digital Out Output Type: Three-State (high-impedance). Used for custom and random data types where individual sample bits can control the output state.</summary>
    public const byte DwfDigitalOutOutputThreeState = 3;

    /// <summary>Digital Out Data Type: Generate a pulse train with configurable parameters (counter, duty cycle).</summary>
    public const byte DwfDigitalOutTypePulse = 0;
    /// <summary>Digital Out Data Type: Generate custom data patterns from a buffer.</summary>
    public const byte DwfDigitalOutTypeCustom = 1;
    /// <summary>Digital Out Data Type: Generate random data patterns.</summary>
    public const byte DwfDigitalOutTypeRandom = 2;
    /// <summary>Digital Out Data Type: Output data from a Read-Only Memory (ROM) pattern. Also used for Finite State Machine (FSM) type.</summary>
    public const byte DwfDigitalOutTypeROM = 3;
    /// <summary>Digital Out Data Type: Finite State Machine (FSM). Functionally equivalent to ROM type for pattern generation.</summary>
    public const byte DwfDigitalOutTypeFSM = 3;

    /// <summary>Digital Out Idle Behavior: Output the initial value of the pattern when idle.</summary>
    public const byte DwfDigitalOutIdleInit = 0;
    /// <summary>Digital Out Idle Behavior: Output a low logic level when idle.</summary>
    public const byte DwfDigitalOutIdleLow = 1;
    /// <summary>Digital Out Idle Behavior: Output a high logic level when idle.</summary>
    public const byte DwfDigitalOutIdleHigh = 2;
    /// <summary>Digital Out Idle Behavior: Output high-impedance (Zet) state when idle.</summary>
    public const byte DwfDigitalOutIdleZet = 3;

    /// <summary>Analog Impedance Measurement Type: Impedance magnitude (Z) in Ohms.</summary>
    public const byte DwfAnalogImpedanceImpedance = 0;
    /// <summary>Analog Impedance Measurement Type: Impedance phase in Radians.</summary>
    public const byte DwfAnalogImpedanceImpedancePhase = 1;
    /// <summary>Analog Impedance Measurement Type: Resistance (real part of impedance) in Ohms.</summary>
    public const byte DwfAnalogImpedanceResistance = 2;
    /// <summary>Analog Impedance Measurement Type: Reactance (imaginary part of impedance) in Ohms.</summary>
    public const byte DwfAnalogImpedanceReactance = 3;
    /// <summary>Analog Impedance Measurement Type: Admittance magnitude (Y) in Siemens.</summary>
    public const byte DwfAnalogImpedanceAdmittance = 4;
    /// <summary>Analog Impedance Measurement Type: Admittance phase in Radians.</summary>
    public const byte DwfAnalogImpedanceAdmittancePhase = 5;
    /// <summary>Analog Impedance Measurement Type: Conductance (real part of admittance) in Siemens.</summary>
    public const byte DwfAnalogImpedanceConductance = 6;
    /// <summary>Analog Impedance Measurement Type: Susceptance (imaginary part of admittance) in Siemens.</summary>
    public const byte DwfAnalogImpedanceSusceptance = 7;
    /// <summary>Analog Impedance Measurement Type: Equivalent series capacitance in Farads.</summary>
    public const byte DwfAnalogImpedanceSeriesCapacitance = 8;
    /// <summary>Analog Impedance Measurement Type: Equivalent parallel capacitance in Farads.</summary>
    public const byte DwfAnalogImpedanceParallelCapacitance = 9;
    /// <summary>Analog Impedance Measurement Type: Equivalent series inductance in Henrys.</summary>
    public const byte DwfAnalogImpedanceSeriesInductance = 10;
    /// <summary>Analog Impedance Measurement Type: Equivalent parallel inductance in Henrys.</summary>
    public const byte DwfAnalogImpedanceParallelInductance = 11;
    /// <summary>Analog Impedance Measurement Type: Dissipation factor (D), unitless. Ratio of resistance to reactance.</summary>
    public const byte DwfAnalogImpedanceDissipation = 12;
    /// <summary>Analog Impedance Measurement Type: Quality factor (Q), unitless. Ratio of reactance to resistance.</summary>
    public const byte DwfAnalogImpedanceQuality = 13;

    /// <summary>Global DWF Parameter: USB power control. For Analog Discovery 2, set to 1 to keep USB power enabled even when an auxiliary power supply is connected, 0 to switch to auxiliary power.</summary>
    public const byte DwfParamUsbPower = 2;
    /// <summary>Global DWF Parameter: Controls the brightness of LEDs on devices like the Digital Discovery (0-100%).</summary>
    public const byte DwfParamLedBrightness = 3;
    /// <summary>Global DWF Parameter: Defines device behavior when <see cref="FDwfDeviceClose"/> is called.
    /// 0: Continue operation.
    /// 1: Stop operation.
    /// 2: Shutdown (power off where applicable).
    /// </summary>
    public const byte DwfParamOnClose = 4;
    /// <summary>Global DWF Parameter: Enables or disables audio output on devices like Analog Discovery 1 &amp; 2. 0: Disable, 1: Enable.</summary>
    public const byte DwfParamAudioOut = 5;
    /// <summary>Global DWF Parameter: Sets the USB power limit for devices like Analog Discovery 1 &amp; 2 (0-1000 mA). Use -1 for no limit (device default).</summary>
    public const byte DwfParamUsbLimit = 6;

    // Macro used to verify if bit is 1 or 0 in given bit field
    // This is a C preprocessor macro and not directly translatable to a C# constant.
    // Its usage in C is typically: #define IsBitSet(fs, bit) ((fs & (1<<bit)) != 0)
    // In C#, this would be achieved by a boolean expression: (fieldValue & (1 << bitNumber)) != 0;

    // Error and version APIs:
    /// <summary>
    /// Retrieves the last error code generated by a DWF API function.
    /// This function should be called after an API function returns failure (typically 0) to get more details about the error.
    /// </summary>
    /// <param name="pdwferc">When this method returns, contains the last error code. See dwferc* constants for specific error code meanings.</param>
    /// <returns>This function is documented to always return 1 (TRUE). However, it's good practice to check for non-zero as success if other DWF functions return 0 for failure.</returns>
    [DllImport("dwf", EntryPoint = "FDwfGetLastError", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern int FDwfGetLastError(out int pdwferc);

    /// <summary>
    /// Internal P/Invoke declaration for retrieving the last error message.
    /// This method is not intended for direct public use. Use the public wrapper <see cref="FDwfGetLastErrorMsg(out string)"/> instead.
    /// </summary>
    /// <param name="szError">A pre-allocated StringBuilder to receive the null-terminated error message. A recommended size is 512 characters.</param>
    /// <returns>Returns 1 (TRUE) if successful (message retrieved), 0 (FALSE) otherwise.</returns>
    [DllImport("dwf", EntryPoint = "FDwfGetLastErrorMsg", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    internal static extern int _FDwfGetLastErrorMsg([MarshalAs(UnmanagedType.LPStr)] System.Text.StringBuilder szError); // Buffer size 512

    /// <summary>
    /// Retrieves a human-readable message describing the last error that occurred in the DWF library.
    /// </summary>
    /// <param name="szError">When this method returns, contains the last error message string.</param>
    /// <returns>Returns 1 (TRUE) if the error message was successfully retrieved, 0 (FALSE) otherwise.</returns>
    public static int FDwfGetLastErrorMsg(out string szError)
    {
        System.Text.StringBuilder sb = new(512); // Pre-allocate buffer
        int ret = _FDwfGetLastErrorMsg(sb);
        szError = sb.ToString();
        return ret;
    }

    /// <summary>
    /// Internal P/Invoke declaration for retrieving the DWF library version.
    /// This method is not intended for direct public use. Use the public wrapper <see cref="FDwfGetVersion(out string)"/> instead.
    /// </summary>
    /// <param name="szVersion">A pre-allocated StringBuilder to receive the null-terminated version string. A recommended size is 32 characters.</param>
    /// <returns>Returns 1 (TRUE) if successful (version retrieved), 0 (FALSE) otherwise.</returns>
    [DllImport("dwf", EntryPoint = "FDwfGetVersion", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    internal static extern int _FDwfGetVersion([MarshalAs(UnmanagedType.LPStr)] System.Text.StringBuilder szVersion); // Buffer size 32

    /// <summary>
    /// Retrieves the version string of the DWF library (e.g., "3.8.5").
    /// </summary>
    /// <param name="szVersion">When this method returns, contains the DWF library version string.</param>
    /// <returns>Returns 1 (TRUE) if the version string was successfully retrieved, 0 (FALSE) otherwise.</returns>
    public static int FDwfGetVersion(out string szVersion)
    {
        System.Text.StringBuilder sb = new(32); // Pre-allocate buffer
        int ret = _FDwfGetVersion(sb);
        szVersion = sb.ToString();
        return ret;
    }

    /// <summary>
    /// Sets a global DWF library parameter.
    /// These parameters affect the general behavior of the DWF library or specific device types.
    /// </summary>
    /// <param name="param">The parameter identifier to set (e.g., <see cref="DwfParamOnClose"/>, <see cref="DwfParamUsbPower"/>, <see cref="DwfParamLedBrightness"/>, etc.).</param>
    /// <param name="value">The value to set for the specified parameter.</param>
    /// <returns>Returns 1 (TRUE) if the parameter was set successfully, 0 (FALSE) otherwise.</returns>
    [DllImport("dwf", EntryPoint = "FDwfParamSet", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern int FDwfParamSet(int param, int value);

    /// <summary>
    /// Gets the current value of a global DWF library parameter.
    /// </summary>
    /// <param name="param">The parameter identifier to get (e.g., <see cref="DwfParamOnClose"/>, <see cref="DwfParamUsbPower"/>, etc.).</param>
    /// <param name="pvalue">When this method returns, contains the current value of the specified parameter.</param>
    /// <returns>Returns 1 (TRUE) if the parameter value was retrieved successfully, 0 (FALSE) otherwise.</returns>
    [DllImport("dwf", EntryPoint = "FDwfParamGet", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern int FDwfParamGet(int param, out int pvalue);


    // DEVICE MANAGEMENT FUNCTIONS
    // Enumeration:
    /// <summary>
    /// Enumerates connected Digilent devices that match the specified filter.
    /// After calling this function, use other FDwfEnum* functions to get details about each enumerated device.
    /// </summary>
    /// <param name="enumfilter">A filter specifying the type of devices to enumerate (e.g., <see cref="enumfilterAll"/>, <see cref="enumfilterDiscovery2"/>, <see cref="enumfilterDDiscovery"/>, etc.).</param>
    /// <param name="pcDevice">When this method returns, contains the count of enumerated devices found that match the filter.</param>
    /// <returns>Returns 1 (TRUE) if the enumeration was successful, 0 (FALSE) otherwise.</returns>
    [DllImport("dwf", EntryPoint = "FDwfEnum", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern int FDwfEnum(int enumfilter, out int pcDevice);

    /// <summary>
    /// Retrieves the device ID (type) and hardware revision for an enumerated device.
    /// </summary>
    /// <param name="idxDevice">The zero-based index of the enumerated device (from 0 to pcDevice-1, where pcDevice is obtained from <see cref="FDwfEnum"/>).</param>
    /// <param name="pDeviceId">When this method returns, contains the device ID (e.g., <see cref="devidDiscovery2"/>, <see cref="devidDDiscovery"/>).</param>
    /// <param name="pDeviceRevision">When this method returns, contains the hardware revision number of the device.</param>
    /// <returns>Returns 1 (TRUE) if successful, 0 (FALSE) otherwise.</returns>
    [DllImport("dwf", EntryPoint = "FDwfEnumDeviceType", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern int FDwfEnumDeviceType(int idxDevice, out int pDeviceId, out int pDeviceRevision);

    /// <summary>
    /// Checks if an enumerated device is currently opened (in use) by any process.
    /// </summary>
    /// <param name="idxDevice">The zero-based index of the enumerated device.</param>
    /// <param name="pfIsUsed">When this method returns, contains 1 (TRUE) if the device is currently opened, or 0 (FALSE) if it is available.</param>
    /// <returns>Returns 1 (TRUE) if successful, 0 (FALSE) otherwise.</returns>
    [DllImport("dwf", EntryPoint = "FDwfEnumDeviceIsOpened", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern int FDwfEnumDeviceIsOpened(int idxDevice, out int pfIsUsed);

    /// <summary>
    /// Internal P/Invoke declaration for retrieving the user-configurable name of an enumerated device.
    /// This method is not intended for direct public use. Use the public wrapper <see cref="FDwfEnumUserName(int, out string)"/> instead.
    /// </summary>
    /// <param name="idxDevice">The zero-based index of the enumerated device.</param>
    /// <param name="szUserName">A pre-allocated StringBuilder to receive the user name. Recommended size is 32 characters.</param>
    /// <returns>Returns 1 (TRUE) if successful, 0 (FALSE) otherwise.</returns>
    [DllImport("dwf", EntryPoint = "FDwfEnumUserName", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    internal static extern int _FDwfEnumUserName(int idxDevice, [MarshalAs(UnmanagedType.LPStr)] System.Text.StringBuilder szUserName); // Buffer size 32

    /// <summary>
    /// Retrieves the user-configurable name (nickname) of an enumerated device.
    /// </summary>
    /// <param name="idxDevice">The zero-based index of the enumerated device.</param>
    /// <param name="szUserName">When this method returns, contains the user-configurable name of the device.</param>
    /// <returns>Returns 1 (TRUE) if the name was retrieved successfully, 0 (FALSE) otherwise.</returns>
    public static int FDwfEnumUserName(int idxDevice, out string szUserName)
    {
        System.Text.StringBuilder sb = new(32); // Pre-allocate buffer
        int ret = _FDwfEnumUserName(idxDevice, sb);
        szUserName = sb.ToString();
        return ret;
    }

    /// <summary>
    /// Internal P/Invoke declaration for retrieving the product name of an enumerated device.
    /// This method is not intended for direct public use. Use the public wrapper <see cref="FDwfEnumDeviceName(int, out string)"/> instead.
    /// </summary>
    /// <param name="idxDevice">The zero-based index of the enumerated device.</param>
    /// <param name="szDeviceName">A pre-allocated StringBuilder to receive the device name. Recommended size is 32 characters.</param>
    /// <returns>Returns 1 (TRUE) if successful, 0 (FALSE) otherwise.</returns>
    [DllImport("dwf", EntryPoint = "FDwfEnumDeviceName", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    internal static extern int _FDwfEnumDeviceName(int idxDevice, [MarshalAs(UnmanagedType.LPStr)] System.Text.StringBuilder szDeviceName); // Buffer size 32

    /// <summary>
    /// Retrieves the product name of an enumerated device (e.g., "Analog Discovery 2", "Digital Discovery").
    /// </summary>
    /// <param name="idxDevice">The zero-based index of the enumerated device.</param>
    /// <param name="szDeviceName">When this method returns, contains the product name of the device.</param>
    /// <returns>Returns 1 (TRUE) if the name was retrieved successfully, 0 (FALSE) otherwise.</returns>
    public static int FDwfEnumDeviceName(int idxDevice, out string szDeviceName)
    {
        System.Text.StringBuilder sb = new(32); // Pre-allocate buffer
        int ret = _FDwfEnumDeviceName(idxDevice, sb);
        szDeviceName = sb.ToString();
        return ret;
    }

    /// <summary>
    /// Internal P/Invoke declaration for retrieving the serial number string of an enumerated device.
    /// This method is not intended for direct public use. Use the public wrapper <see cref="FDwfEnumSN(int, out string)"/> instead.
    /// </summary>
    /// <param name="idxDevice">The zero-based index of the enumerated device.</param>
    /// <param name="szSN">A pre-allocated StringBuilder to receive the serial number. Recommended size is 32 characters.</param>
    /// <returns>Returns 1 (TRUE) if successful, 0 (FALSE) otherwise.</returns>
    [DllImport("dwf", EntryPoint = "FDwfEnumSN", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    internal static extern int _FDwfEnumSN(int idxDevice, [MarshalAs(UnmanagedType.LPStr)] System.Text.StringBuilder szSN); // Buffer size 32

    /// <summary>
    /// Retrieves the serial number string of an enumerated device.
    /// </summary>
    /// <param name="idxDevice">The zero-based index of the enumerated device.</param>
    /// <param name="szSN">When this method returns, contains the serial number of the device.</param>
    /// <returns>Returns 1 (TRUE) if the serial number was retrieved successfully, 0 (FALSE) otherwise.</returns>
    public static int FDwfEnumSN(int idxDevice, out string szSN)
    {
        System.Text.StringBuilder sb = new(32); // Pre-allocate buffer
        int ret = _FDwfEnumSN(idxDevice, sb);
        szSN = sb.ToString();
        return ret;
    }

    /// <summary>
    /// Retrieves the number of available configurations for an enumerated device.
    /// Most Digilent devices have a single configuration. Devices like the Eclypse Z7 may have multiple configurations.
    /// This function should be called before <see cref="FDwfEnumConfigInfo"/> or <see cref="FDwfDeviceConfigOpen"/>.
    /// </summary>
    /// <param name="idxDevice">The zero-based index of the enumerated device.</param>
    /// <param name="pcConfig">When this method returns, contains the count of available configurations for the device.</param>
    /// <returns>Returns 1 (TRUE) if successful, 0 (FALSE) otherwise.</returns>
    [DllImport("dwf", EntryPoint = "FDwfEnumConfig", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern int FDwfEnumConfig(int idxDevice, out int pcConfig);

    /// <summary>
    /// Retrieves specific information about a device configuration.
    /// This is used to query capabilities like channel counts and buffer sizes for a specific configuration of a device
    /// before opening it with <see cref="FDwfDeviceConfigOpen"/>.
    /// </summary>
    /// <param name="idxConfig">The zero-based index of the device configuration (from 0 to pcConfig-1, where pcConfig is obtained from <see cref="FDwfEnumConfig"/> for a specific device).</param>
    /// <param name="info">The type of configuration information to retrieve (e.g., <see cref="DECIAnalogInChannelCount"/>, <see cref="DECIAnalogInBufferSize"/>, etc.).</param>
    /// <param name="pv">When this method returns, contains the value of the requested configuration information item.</param>
    /// <returns>Returns 1 (TRUE) if successful, 0 (FALSE) otherwise.</returns>
    [DllImport("dwf", EntryPoint = "FDwfEnumConfigInfo", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern int FDwfEnumConfigInfo(int idxConfig, int info, out int pv);


    // Open/Close:
    /// <summary>
    /// Opens a Digilent device identified by its enumeration index, using the default (first available) configuration.
    /// If successful, it returns a device handle (<paramref name="phdwf"/>) that must be used in subsequent calls to device and instrument functions.
    /// </summary>
    /// <param name="idxDevice">The zero-based index of the enumerated device (from <see cref="FDwfEnum"/>). Use -1 to open the first available (free) device found by the library.</param>
    /// <param name="phdwf">When this method returns successfully, contains the device handle. On failure, it contains <see cref="hdwfNone"/>.</param>
    /// <returns>Returns 1 (TRUE) if the device was opened successfully, 0 (FALSE) otherwise. On failure, call <see cref="FDwfGetLastError"/> to get the error code.</returns>
    [DllImport("dwf", EntryPoint = "FDwfDeviceOpen", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern int FDwfDeviceOpen(int idxDevice, out int phdwf);

    /// <summary>
    /// Opens a Digilent device identified by its enumeration index and a specific configuration index.
    /// This is used for multi-configuration devices like the Eclypse Z7. For single-configuration devices, <see cref="FDwfDeviceOpen"/> is sufficient.
    /// </summary>
    /// <param name="idxDev">The zero-based index of the enumerated device (from <see cref="FDwfEnum"/>). Use -1 to open the first available (free) device with the specified configuration.</param>
    /// <param name="idxCfg">The zero-based index of the device configuration to use (from <see cref="FDwfEnumConfig"/>).</param>
    /// <param name="phdwf">When this method returns successfully, contains the device handle. On failure, it contains <see cref="hdwfNone"/>.</param>
    /// <returns>Returns 1 (TRUE) if the device was opened successfully with the specified configuration, 0 (FALSE) otherwise. On failure, call <see cref="FDwfGetLastError"/>.</returns>
    [DllImport("dwf", EntryPoint = "FDwfDeviceConfigOpen", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern int FDwfDeviceConfigOpen(int idxDev, int idxCfg, out int phdwf);

    /// <summary>
    /// Closes a previously opened Digilent device.
    /// After this function returns, the device handle <paramref name="hdwf"/> is no longer valid.
    /// It's important to close devices when they are no longer needed to free up resources.
    /// </summary>
    /// <param name="hdwf">The device handle of the device to be closed, obtained from <see cref="FDwfDeviceOpen"/> or <see cref="FDwfDeviceConfigOpen"/>.</param>
    /// <returns>Returns 1 (TRUE) if the device was closed successfully, 0 (FALSE) otherwise. On failure, call <see cref="FDwfGetLastError"/>.</returns>
    [DllImport("dwf", EntryPoint = "FDwfDeviceClose", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern int FDwfDeviceClose(int hdwf);

    /// <summary>
    /// Closes all Digilent devices currently opened by the calling process.
    /// </summary>
    /// <returns>Returns 1 (TRUE) if all devices were closed successfully, 0 (FALSE) otherwise. On failure, call <see cref="FDwfGetLastError"/>.</returns>
    [DllImport("dwf", EntryPoint = "FDwfDeviceCloseAll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern int FDwfDeviceCloseAll();

    /// <summary>
    /// Sets the auto-configure behavior for a device.
    /// When auto-configuration is enabled, instrument parameters are automatically set to default or sensible values when the device is opened or an instrument is reset.
    /// </summary>
    /// <param name="hdwf">Device handle.</param>
    /// <param name="fAutoConfigure">Set to 1 (TRUE) to enable auto-configuration, 0 (FALSE) to disable it.</param>
    /// <returns>Returns 1 (TRUE) if successful, 0 (FALSE) otherwise.</returns>
    [DllImport("dwf", EntryPoint = "FDwfDeviceAutoConfigureSet", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern int FDwfDeviceAutoConfigureSet(int hdwf, int fAutoConfigure);

    /// <summary>
    /// Gets the current auto-configure behavior setting for a device.
    /// </summary>
    /// <param name="hdwf">Device handle.</param>
    /// <param name="pfAutoConfigure">When this method returns, contains 1 (TRUE) if auto-configuration is enabled, or 0 (FALSE) if disabled.</param>
    /// <returns>Returns 1 (TRUE) if successful, 0 (FALSE) otherwise.</returns>
    [DllImport("dwf", EntryPoint = "FDwfDeviceAutoConfigureGet", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern int FDwfDeviceAutoConfigureGet(int hdwf, out int pfAutoConfigure);

    /// <summary>
    /// Resets a device and all its instruments to their default states.
    /// This is a comprehensive reset, affecting all instruments (Analog In, Analog Out, Digital I/O, etc.).
    /// </summary>
    /// <param name="hdwf">Device handle.</param>
    /// <returns>Returns 1 (TRUE) if the device was reset successfully, 0 (FALSE) otherwise.</returns>
    [DllImport("dwf", EntryPoint = "FDwfDeviceReset", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern int FDwfDeviceReset(int hdwf);

    /// <summary>
    /// Enables or disables a device. This function is not typically used by end-users and its behavior might be device-specific or reserved for internal use.
    /// </summary>
    /// <param name="hdwf">Device handle.</param>
    /// <param name="fEnable">Set to 1 (TRUE) to enable the device, 0 (FALSE) to disable it.</param>
    /// <returns>Returns 1 (TRUE) if successful, 0 (FALSE) otherwise.</returns>
    [DllImport("dwf", EntryPoint = "FDwfDeviceEnableSet", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern int FDwfDeviceEnableSet(int hdwf, int fEnable);

    /// <summary>
    /// Retrieves information about the available trigger sources that can be routed to the device's trigger I/O pins.
    /// The returned bitmask indicates which <c>trigsrc*</c> constants are valid for use with <see cref="FDwfDeviceTriggerSet"/>.
    /// </summary>
    /// <param name="hdwf">Device handle.</param>
    /// <param name="pfstrigsrc">When this method returns, contains a bitmask of available trigger sources. Each bit corresponds to a <c>trigsrc*</c> constant (e.g., (1 &lt;&lt; <see cref="trigsrcDetectorAnalogIn"/>)).</param>
    /// <returns>Returns 1 (TRUE) if successful, 0 (FALSE) otherwise.</returns>
    [DllImport("dwf", EntryPoint = "FDwfDeviceTriggerInfo", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern int FDwfDeviceTriggerInfo(int hdwf, out int pfstrigsrc);

    /// <summary>
    /// Sets the trigger source for a specific trigger I/O pin on the device (e.g., T1, T2).
    /// This allows routing internal instrument signals or other trigger sources to physical trigger pins for synchronization or external use.
    /// </summary>
    /// <param name="hdwf">Device handle.</param>
    /// <param name="idxPin">The zero-based index of the trigger pin (e.g., 0 for T1, 1 for T2, etc.).</param>
    /// <param name="trigsrc">The trigger source to associate with the specified pin (see <c>trigsrc*</c> constants).</param>
    /// <returns>Returns 1 (TRUE) if successful, 0 (FALSE) otherwise.</returns>
    [DllImport("dwf", EntryPoint = "FDwfDeviceTriggerSet", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern int FDwfDeviceTriggerSet(int hdwf, int idxPin, byte trigsrc);

    /// <summary>
    /// Gets the currently configured trigger source for a specific trigger I/O pin on the device.
    /// </summary>
    /// <param name="hdwf">Device handle.</param>
    /// <param name="idxPin">The zero-based index of the trigger pin.</param>
    /// <param name="ptrigsrc">When this method returns, contains the trigger source currently configured for the specified pin (see <c>trigsrc*</c> constants).</param>
    /// <returns>Returns 1 (TRUE) if successful, 0 (FALSE) otherwise.</returns>
    [DllImport("dwf", EntryPoint = "FDwfDeviceTriggerGet", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern int FDwfDeviceTriggerGet(int hdwf, int idxPin, out byte ptrigsrc);

    /// <summary>
    /// Sends a PC-generated (software) trigger pulse to the device.
    /// This can be used as a trigger source (<see cref="trigsrcPC"/>) for any instrument on the device.
    /// </summary>
    /// <param name="hdwf">Device handle.</param>
    /// <returns>Returns 1 (TRUE) if the PC trigger was sent successfully, 0 (FALSE) otherwise.</returns>
    [DllImport("dwf", EntryPoint = "FDwfDeviceTriggerPC", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern int FDwfDeviceTriggerPC(int hdwf);

    /// <summary>
    /// Retrieves information about the available trigger slopes (e.g., rising, falling) for the device's trigger I/O pins.
    /// </summary>
    /// <param name="hdwf">Device handle.</param>
    /// <param name="pfsslope">When this method returns, contains a bitmask indicating the supported trigger slopes. Each bit corresponds to a <see cref="DwfTriggerSlopeRise"/>, <see cref="DwfTriggerSlopeFall"/>, or <see cref="DwfTriggerSlopeEither"/> value (e.g., (1 &lt;&lt; <see cref="DwfTriggerSlopeRise"/>) | (1 &lt;&lt; <see cref="DwfTriggerSlopeFall"/>)).</param>
    /// <returns>Returns 1 (TRUE) if successful, 0 (FALSE) otherwise.</returns>
    [DllImport("dwf", EntryPoint = "FDwfDeviceTriggerSlopeInfo", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern int FDwfDeviceTriggerSlopeInfo(int hdwf, out int pfsslope);

    /// <summary>
    /// Sets a device-specific parameter. These are advanced parameters and their availability and meaning depend on the specific Digilent device.
    /// Refer to the device's documentation for details on supported parameters and values.
    /// </summary>
    /// <param name="hdwf">Device handle.</param>
    /// <param name="param">The identifier of the device-specific parameter to set.</param>
    /// <param name="value">The value to set for the parameter.</param>
    /// <returns>Returns 1 (TRUE) if successful, 0 (FALSE) otherwise.</returns>
    [DllImport("dwf", EntryPoint = "FDwfDeviceParamSet", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern int FDwfDeviceParamSet(int hdwf, int param, int value);

    /// <summary>
    /// Gets the current value of a device-specific parameter.
    /// Refer to the device's documentation for details on supported parameters.
    /// </summary>
    /// <param name="hdwf">Device handle.</param>
    /// <param name="param">The identifier of the device-specific parameter to get.</param>
    /// <param name="pvalue">When this method returns, contains the current value of the parameter.</param>
    /// <returns>Returns 1 (TRUE) if successful, 0 (FALSE) otherwise.</returns>
    [DllImport("dwf", EntryPoint = "FDwfDeviceParamGet", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern int FDwfDeviceParamGet(int hdwf, int param, out int pvalue);



    // ANALOG IN INSTRUMENT FUNCTIONS
    // Control and status:
    /// <summary>
    /// Resets all parameters of the AnalogIn instrument to their default values.
    /// It is highly recommended to call this function before configuring the AnalogIn instrument for a new acquisition task.
    /// </summary>
    /// <param name="hdwf">Device handle obtained from <see cref="FDwfDeviceOpen"/> or related open functions.</param>
    /// <returns>Returns 1 (TRUE) if the instrument was reset successfully, 0 (FALSE) otherwise.</returns>
    [DllImport("dwf", EntryPoint = "FDwfAnalogInReset", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern int FDwfAnalogInReset(int hdwf);

    /// <summary>
    /// Configures the AnalogIn instrument based on previously set parameters and optionally starts or stops the acquisition.
    /// This function must be called after setting parameters like frequency, buffer size, channel settings, and trigger settings.
    /// </summary>
    /// <param name="hdwf">Device handle.</param>
    /// <param name="fReconfigure">
    /// If set to 1 (TRUE), the instrument is reconfigured even if it is already running. This might abort an ongoing acquisition.
    /// If set to 0 (FALSE), the instrument is configured only if it is not currently running.
    /// </param>
    /// <param name="fStart">
    /// If set to 1 (TRUE), the acquisition is started immediately after configuration.
    /// If set to 0 (FALSE), the instrument is configured but remains in a stopped state (e.g., <see cref="DwfStateReady"/> or <see cref="DwfStateConfig"/>). The acquisition will start upon a subsequent call with <c>fStart = TRUE</c> or an appropriate trigger.
    /// </param>
    /// <returns>Returns 1 (TRUE) if the configuration (and optional start/stop) was successful, 0 (FALSE) otherwise.</returns>
    [DllImport("dwf", EntryPoint = "FDwfAnalogInConfigure", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern int FDwfAnalogInConfigure(int hdwf, int fReconfigure, int fStart);

    /// <summary>
    /// Forces a trigger event for the AnalogIn instrument if it is currently armed (<see cref="DwfStateArmed"/>) and waiting for a trigger.
    /// This is useful for testing trigger configurations or when an external trigger source might not occur as expected.
    /// </summary>
    /// <param name="hdwf">Device handle.</param>
    /// <returns>Returns 1 (TRUE) if the trigger was successfully forced, 0 (FALSE) otherwise (e.g., if the instrument was not armed).</returns>
    [DllImport("dwf", EntryPoint = "FDwfAnalogInTriggerForce", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern int FDwfAnalogInTriggerForce(int hdwf);

    /// <summary>
    /// Checks the current operational state of the AnalogIn acquisition and optionally reads data from the device into the PC's internal buffer.
    /// For single acquisition mode (<see cref="acqmodeSingle"/>), data should typically be retrieved only when the state is <see cref="DwfStateDone"/>.
    /// For record mode (<see cref="acqmodeRecord"/>), data can be retrieved while in <see cref="DwfStateRunning"/> or <see cref="DwfStateTriggered"/>.
    /// </summary>
    /// <param name="hdwf">Device handle.</param>
    /// <param name="fReadData">
    /// If set to 1 (TRUE), this function attempts to read available data from the device's hardware buffer into the PC's internal software buffer. This is necessary before calling data retrieval functions like <see cref="FDwfAnalogInStatusData"/>.
    /// If set to 0 (FALSE), only the status is checked without transferring data. This can be used for a quick poll of the instrument state.
    /// </param>
    /// <param name="psts">When this method returns, contains the current acquisition state of the AnalogIn instrument (see <c>DwfState*</c> constants like <see cref="DwfStateReady"/>, <see cref="DwfStateArmed"/>, <see cref="DwfStateTriggered"/>, <see cref="DwfStateDone"/>, etc.).</param>
    /// <returns>Returns 1 (TRUE) if the status check (and optional data read) was successful, 0 (FALSE) otherwise.</returns>
    [DllImport("dwf", EntryPoint = "FDwfAnalogInStatus", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern int FDwfAnalogInStatus(int hdwf, int fReadData, out byte psts);

    /// <summary>
    /// Retrieves the number of samples remaining in the device's hardware acquisition buffer before it becomes full.
    /// This is particularly useful in record mode to monitor buffer usage.
    /// </summary>
    /// <param name="hdwf">Device handle.</param>
    /// <param name="pcSamplesLeft">When this method returns, contains the count of samples remaining that can be acquired into the device buffer before it's full.</param>
    /// <returns>Returns 1 (TRUE) if successful, 0 (FALSE) otherwise.</returns>
    [DllImport("dwf", EntryPoint = "FDwfAnalogInStatusSamplesLeft", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern int FDwfAnalogInStatusSamplesLeft(int hdwf, out int pcSamplesLeft);

    /// <summary>
    /// Retrieves the number of valid (acquired and available for reading) samples currently stored in the PC's internal software buffer.
    /// It is essential to call <see cref="FDwfAnalogInStatus"/> with <c>fReadData = TRUE</c> before this function to transfer data from the device to the PC buffer.
    /// </summary>
    /// <param name="hdwf">Device handle.</param>
    /// <param name="pcSamplesValid">When this method returns, contains the count of valid samples currently available in the PC software buffer, ready to be read by functions like <see cref="FDwfAnalogInStatusData"/>.</param>
    /// <returns>Returns 1 (TRUE) if successful, 0 (FALSE) otherwise.</returns>
    [DllImport("dwf", EntryPoint = "FDwfAnalogInStatusSamplesValid", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern int FDwfAnalogInStatusSamplesValid(int hdwf, out int pcSamplesValid);

    /// <summary>
    /// Retrieves the current write index within the device's hardware acquisition buffer.
    /// This can be used to understand buffer utilization and data flow, especially in record mode.
    /// The index typically wraps around when the buffer is full.
    /// </summary>
    /// <param name="hdwf">Device handle.</param>
    /// <param name="pidxWrite">When this method returns, contains the current write index within the device's hardware acquisition buffer.</param>
    /// <returns>Returns 1 (TRUE) if successful, 0 (FALSE) otherwise.</returns>
    [DllImport("dwf", EntryPoint = "FDwfAnalogInStatusIndexWrite", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern int FDwfAnalogInStatusIndexWrite(int hdwf, out int pidxWrite);

    /// <summary>
    /// Checks if the last completed acquisition was auto-triggered due to a timeout, rather than by a configured trigger condition.
    /// </summary>
    /// <param name="hdwf">Device handle.</param>
    /// <param name="pfAuto">When this method returns, contains 1 (TRUE) if the last acquisition was auto-triggered, or 0 (FALSE) if it was triggered by a configured condition.</param>
    /// <returns>Returns 1 (TRUE) if successful, 0 (FALSE) otherwise.</returns>
    [DllImport("dwf", EntryPoint = "FDwfAnalogInStatusAutoTriggered", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern int FDwfAnalogInStatusAutoTriggered(int hdwf, out int pfAuto);

    /// <summary>
    /// Retrieves acquired analog input data samples (in Volts) from the specified channel (or all channels).
    /// This function copies <paramref name="cdData"/> samples, starting from the oldest available data in the PC's internal software buffer.
    /// Ensure <see cref="FDwfAnalogInStatus"/> was called with <c>fReadData = TRUE</c> to populate this buffer before calling this function.
    /// </summary>
    /// <param name="hdwf">Device handle.</param>
    /// <param name="idxChannel">The zero-based channel index from which to retrieve data. Use -1 to retrieve data from all enabled channels, interleaved in the <paramref name="rgdVoltData"/> buffer.</param>
    /// <param name="rgdVoltData">A pre-allocated array to receive the voltage samples. Its size should be at least <paramref name="cdData"/> samples (or <paramref name="cdData"/> multiplied by the number of enabled channels if <paramref name="idxChannel"/> is -1).</param>
    /// <param name="cdData">The number of samples to retrieve per channel. If <paramref name="idxChannel"/> is -1, this is the number of samples per channel, and the total samples copied will be <paramref name="cdData"/> times the number of enabled channels.</param>
    /// <returns>Returns 1 (TRUE) if data was successfully retrieved, 0 (FALSE) otherwise.</returns>
    [DllImport("dwf", EntryPoint = "FDwfAnalogInStatusData", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern int FDwfAnalogInStatusData(int hdwf, int idxChannel, [MarshalAs(UnmanagedType.LPArray)] double[] rgdVoltData, int cdData);

    /// <summary>
    /// Retrieves acquired analog input data samples (in Volts) from a specified channel (or all channels), starting at a given offset within the valid data in the PC's software buffer.
    /// Ensure <see cref="FDwfAnalogInStatus"/> was called with <c>fReadData = TRUE</c> to populate the buffer.
    /// </summary>
    /// <param name="hdwf">Device handle.</param>
    /// <param name="idxChannel">The zero-based channel index. Use -1 for all enabled channels (data will be interleaved).</param>
    /// <param name="rgdVoltData">A pre-allocated array to receive the voltage samples. Its size should be sufficient for <paramref name="cdData"/> samples (or <paramref name="cdData"/> * number of channels if <paramref name="idxChannel"/> is -1).</param>
    /// <param name="idxData">The zero-based offset (in samples per channel) from the beginning of the valid data in the PC software buffer from which to start copying samples.</param>
    /// <param name="cdData">The number of samples to retrieve per channel.</param>
    /// <returns>Returns 1 (TRUE) if data was successfully retrieved, 0 (FALSE) otherwise.</returns>
    [DllImport("dwf", EntryPoint = "FDwfAnalogInStatusData2", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern int FDwfAnalogInStatusData2(int hdwf, int idxChannel, [MarshalAs(UnmanagedType.LPArray)] double[] rgdVoltData, int idxData, int cdData);

    /// <summary>
    /// Retrieves acquired raw data samples (16-bit unsigned integer ADC values) from a specified channel (or all channels), starting at a given offset.
    /// This function provides direct access to the ADC values before conversion to Volts.
    /// Ensure <see cref="FDwfAnalogInStatus"/> was called with <c>fReadData = TRUE</c>.
    /// </summary>
    /// <param name="hdwf">Device handle.</param>
    /// <param name="idxChannel">The zero-based channel index. Use -1 for all enabled channels (data interleaved).</param>
    /// <param name="rgu16Data">A pre-allocated array to receive the raw 16-bit samples. Size accordingly.</param>
    /// <param name="idxData">The zero-based offset (in samples per channel) from the beginning of valid data in the PC buffer.</param>
    /// <param name="cdData">The number of 16-bit samples to retrieve per channel.</param>
    /// <returns>Returns 1 (TRUE) if data was successfully retrieved, 0 (FALSE) otherwise.</returns>
    [DllImport("dwf", EntryPoint = "FDwfAnalogInStatusData16", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern int FDwfAnalogInStatusData16(int hdwf, int idxChannel, [MarshalAs(UnmanagedType.LPArray)] ushort[] rgu16Data, int idxData, int cdData);

    /// <summary>
    /// Retrieves noise data (minimum and maximum voltage pairs) from the specified channel.
    /// This function is used when the acquisition filter is set to <see cref="filterMinMax"/>. It copies <paramref name="cdData"/> min/max pairs from the beginning of the valid data in the PC buffer.
    /// Ensure <see cref="FDwfAnalogInStatus"/> was called with <c>fReadData = TRUE</c>.
    /// </summary>
    /// <param name="hdwf">Device handle.</param>
    /// <param name="idxChannel">The zero-based channel index.</param>
    /// <param name="rgdMin">A pre-allocated array to receive the minimum voltage values. Size should be at least <paramref name="cdData"/>.</param>
    /// <param name="rgdMax">A pre-allocated array to receive the maximum voltage values. Size should be at least <paramref name="cdData"/>.</param>
    /// <param name="cdData">The number of minimum/maximum pairs to retrieve.</param>
    /// <returns>Returns 1 (TRUE) if noise data was successfully retrieved, 0 (FALSE) otherwise.</returns>
    [DllImport("dwf", EntryPoint = "FDwfAnalogInStatusNoise", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern int FDwfAnalogInStatusNoise(int hdwf, int idxChannel, [MarshalAs(UnmanagedType.LPArray)] double[] rgdMin, [MarshalAs(UnmanagedType.LPArray)] double[] rgdMax, int cdData);

    /// <summary>
    /// Retrieves noise data (minimum and maximum voltage pairs) from a specified channel, starting at a given offset within the valid data.
    /// This function is used when the acquisition filter is set to <see cref="filterMinMax"/>.
    /// Ensure <see cref="FDwfAnalogInStatus"/> was called with <c>fReadData = TRUE</c>.
    /// </summary>
    /// <param name="hdwf">Device handle.</param>
    /// <param name="idxChannel">The zero-based channel index.</param>
    /// <param name="rgdMin">A pre-allocated array to receive the minimum voltage values. Size should be at least <paramref name="cdData"/>.</param>
    /// <param name="rgdMax">A pre-allocated array to receive the maximum voltage values. Size should be at least <paramref name="cdData"/>.</param>
    /// <param name="idxData">The zero-based offset (in min/max pairs) from the beginning of valid min/max data in the PC buffer from which to start copying.</param>
    /// <param name="cdData">The number of minimum/maximum pairs to retrieve.</param>
    /// <returns>Returns 1 (TRUE) if noise data was successfully retrieved, 0 (FALSE) otherwise.</returns>
    [DllImport("dwf", EntryPoint = "FDwfAnalogInStatusNoise2", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern int FDwfAnalogInStatusNoise2(int hdwf, int idxChannel, [MarshalAs(UnmanagedType.LPArray)] double[] rgdMin, [MarshalAs(UnmanagedType.LPArray)] double[] rgdMax, int idxData, int cdData);

    /// <summary>
    /// Retrieves the last acquired voltage sample (instantaneous reading) from the specified channel.
    /// This is useful for performing quick ADC readings without configuring and running a full acquisition.
    /// For continuous or triggered acquisitions, ensure <see cref="FDwfAnalogInStatus"/> was called with <c>fReadData = TRUE</c> recently to update sample values if relying on buffered data.
    /// </summary>
    /// <param name="hdwf">Device handle.</param>
    /// <param name="idxChannel">The zero-based channel index.</param>
    /// <param name="pdVoltSample">When this method returns, contains the voltage value of the last acquired sample from the specified channel.</param>
    /// <returns>Returns 1 (TRUE) if the sample was successfully retrieved, 0 (FALSE) otherwise.</returns>
    [DllImport("dwf", EntryPoint = "FDwfAnalogInStatusSample", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern int FDwfAnalogInStatusSample(int hdwf, int idxChannel, out double pdVoltSample);

    /// <summary>
    /// Retrieves information about the record acquisition process, including the number of samples available, lost, and potentially corrupted.
    /// This function is crucial when using record mode (<see cref="acqmodeRecord"/>) to manage data flow and detect buffer overflows.
    /// Data loss occurs if the device's hardware buffer is overwritten because the PC does not read data fast enough.
    /// Corrupt samples may indicate that the device buffer was overwritten during the previous read operation by the PC.
    /// To avoid data loss/corruption: optimize the data reading loop in the PC application, reduce the sampling frequency, or ensure the record length is appropriately managed relative to buffer capacity and sampling frequency.
    /// </summary>
    /// <param name="hdwf">Device handle.</param>
    /// <param name="pcdDataAvailable">When this method returns, contains the number of new samples available in the PC's software buffer since the last call to <see cref="FDwfAnalogInStatusRecord"/> or <see cref="FDwfAnalogInStatus"/> (with <c>fReadData = TRUE</c>).</param>
    /// <param name="pcdDataLost">When this method returns, contains the number of samples lost due to device hardware buffer overwrite since the last call.</param>
    /// <param name="pcdDataCorrupt">When this method returns, contains the number of samples that might be corrupt due to an overwrite that occurred during the last PC read operation.</param>
    /// <returns>Returns 1 (TRUE) if the record status was successfully retrieved, 0 (FALSE) otherwise.</returns>
    [DllImport("dwf", EntryPoint = "FDwfAnalogInStatusRecord", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern int FDwfAnalogInStatusRecord(int hdwf, out int pcdDataAvailable, out int pcdDataLost, out int pcdDataCorrupt);

    /// <summary>
    /// Sets the record length (duration) for the AnalogIn instrument when operating in record mode (<see cref="acqmodeRecord"/>).
    /// </summary>
    /// <param name="hdwf">Device handle.</param>
    /// <param name="sLegth">The desired record length in seconds. A value of 0.0 typically means unlimited length, where the acquisition runs until explicitly stopped or the device buffer overflows if data is not read quickly enough.</param>
    /// <returns>Returns 1 (TRUE) if the record length was set successfully, 0 (FALSE) otherwise.</returns>
    [DllImport("dwf", EntryPoint = "FDwfAnalogInRecordLengthSet", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern int FDwfAnalogInRecordLengthSet(int hdwf, double sLegth);

    /// <summary>
    /// Gets the currently configured record length (duration) for the AnalogIn instrument.
    /// </summary>
    /// <param name="hdwf">Device handle.</param>
    /// <param name="psLegth">When this method returns, contains the configured record length in seconds. A value of 0.0 typically indicates unlimited length.</param>
    /// <returns>Returns 1 (TRUE) if the record length was retrieved successfully, 0 (FALSE) otherwise.</returns>
    [DllImport("dwf", EntryPoint = "FDwfAnalogInRecordLengthGet", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern int FDwfAnalogInRecordLengthGet(int hdwf, out double psLegth);


    // Acquisition configuration:
    /// <summary>
    /// Retrieves information about the valid range (minimum and maximum) for the AnalogIn instrument's sampling frequency.
    /// </summary>
    /// <param name="hdwf">Device handle.</param>
    /// <param name="phzMin">When this method returns, contains the minimum supported sampling frequency in Hertz (Hz).</param>
    /// <param name="phzMax">When this method returns, contains the maximum supported sampling frequency in Hertz (Hz).</param>
    /// <returns>Returns 1 (TRUE) if the frequency information was retrieved successfully, 0 (FALSE) otherwise.</returns>
    [DllImport("dwf", EntryPoint = "FDwfAnalogInFrequencyInfo", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern int FDwfAnalogInFrequencyInfo(int hdwf, out double phzMin, out double phzMax);

    /// <summary>
    /// Sets the sampling frequency for the AnalogIn instrument.
    /// The DWF library will attempt to set the closest possible frequency that the hardware supports.
    /// It is recommended to call <see cref="FDwfAnalogInFrequencyGet"/> after this function to verify the actual frequency that was set.
    /// </summary>
    /// <param name="hdwf">Device handle.</param>
    /// <param name="hzFrequency">The desired sampling frequency in Hertz (Hz).</param>
    /// <returns>Returns 1 (TRUE) if the frequency was set successfully (or a close valid frequency was selected), 0 (FALSE) otherwise.</returns>
    [DllImport("dwf", EntryPoint = "FDwfAnalogInFrequencySet", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern int FDwfAnalogInFrequencySet(int hdwf, double hzFrequency);

    /// <summary>
    /// Gets the currently configured (actual) sampling frequency for the AnalogIn instrument.
    /// This value may differ slightly from the value set by <see cref="FDwfAnalogInFrequencySet"/> due to hardware limitations.
    /// </summary>
    /// <param name="hdwf">Device handle.</param>
    /// <param name="phzFrequency">When this method returns, contains the current actual sampling frequency in Hertz (Hz).</param>
    /// <returns>Returns 1 (TRUE) if the frequency was retrieved successfully, 0 (FALSE) otherwise.</returns>
    [DllImport("dwf", EntryPoint = "FDwfAnalogInFrequencyGet", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern int FDwfAnalogInFrequencyGet(int hdwf, out double phzFrequency);

    /// <summary>
    /// Retrieves the number of ADC (Analog-to-Digital Converter) bits, which determines the resolution of the AnalogIn instrument.
    /// </summary>
    /// <param name="hdwf">Device handle.</param>
    /// <param name="pnBits">When this method returns, contains the number of ADC bits (e.g., 12, 14, or 16 for typical devices).</param>
    /// <returns>Returns 1 (TRUE) if the ADC bits information was retrieved successfully, 0 (FALSE) otherwise.</returns>
    [DllImport("dwf", EntryPoint = "FDwfAnalogInBitsInfo", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern int FDwfAnalogInBitsInfo(int hdwf, out int pnBits);

    /// <summary>
    /// Retrieves information about the valid range (minimum and maximum) for the AnalogIn instrument's acquisition buffer size, in samples.
    /// </summary>
    /// <param name="hdwf">Device handle.</param>
    /// <param name="pnSizeMin">When this method returns, contains the minimum supported acquisition buffer size in samples.</param>
    /// <param name="pnSizeMax">When this method returns, contains the maximum supported acquisition buffer size in samples.</param>
    /// <returns>Returns 1 (TRUE) if the buffer size information was retrieved successfully, 0 (FALSE) otherwise.</returns>
    [DllImport("dwf", EntryPoint = "FDwfAnalogInBufferSizeInfo", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern int FDwfAnalogInBufferSizeInfo(int hdwf, out int pnSizeMin, out int pnSizeMax);

    /// <summary>
    /// Sets the acquisition buffer size for the AnalogIn instrument.
    /// The DWF library may adjust the requested size to the closest valid size supported by the hardware.
    /// It is recommended to call <see cref="FDwfAnalogInBufferSizeGet"/> after this function to verify the actual buffer size that was set.
    /// </summary>
    /// <param name="hdwf">Device handle.</param>
    /// <param name="nSize">The desired acquisition buffer size in samples.</param>
    /// <returns>Returns 1 (TRUE) if the buffer size was set successfully, 0 (FALSE) otherwise.</returns>
    [DllImport("dwf", EntryPoint = "FDwfAnalogInBufferSizeSet", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern int FDwfAnalogInBufferSizeSet(int hdwf, int nSize);

    /// <summary>
    /// Gets the currently configured (actual) acquisition buffer size for the AnalogIn instrument, in samples.
    /// This value may differ from the value set by <see cref="FDwfAnalogInBufferSizeSet"/> due to hardware limitations.
    /// </summary>
    /// <param name="hdwf">Device handle.</param>
    /// <param name="pnSize">When this method returns, contains the current actual acquisition buffer size in samples.</param>
    /// <returns>Returns 1 (TRUE) if the buffer size was retrieved successfully, 0 (FALSE) otherwise.</returns>
    [DllImport("dwf", EntryPoint = "FDwfAnalogInBufferSizeGet", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern int FDwfAnalogInBufferSizeGet(int hdwf, out int pnSize);

    /// <summary>
    /// Retrieves the maximum size for the noise buffer in samples (min/max pairs). This buffer is used when the acquisition filter is set to <see cref="filterMinMax"/>.
    /// </summary>
    /// <param name="hdwf">Device handle.</param>
    /// <param name="pnSizeMax">When this method returns, contains the maximum noise buffer size in samples (number of min/max pairs).</param>
    /// <returns>Returns 1 (TRUE) if the noise buffer size information was retrieved successfully, 0 (FALSE) otherwise.</returns>
    [DllImport("dwf", EntryPoint = "FDwfAnalogInNoiseSizeInfo", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern int FDwfAnalogInNoiseSizeInfo(int hdwf, out int pnSizeMax);

    /// <summary>
    /// Sets the noise buffer size for the AnalogIn instrument. This is used when the acquisition filter is set to <see cref="filterMinMax"/>.
    /// The DWF library may adjust the requested size. Use <see cref="FDwfAnalogInNoiseSizeGet"/> to verify the actual size.
    /// </summary>
    /// <param name="hdwf">Device handle.</param>
    /// <param name="nSize">The desired noise buffer size in samples (number of min/max pairs).</param>
    /// <returns>Returns 1 (TRUE) if the noise buffer size was set successfully, 0 (FALSE) otherwise.</returns>
    [DllImport("dwf", EntryPoint = "FDwfAnalogInNoiseSizeSet", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern int FDwfAnalogInNoiseSizeSet(int hdwf, int nSize);

    /// <summary>
    /// Gets the currently configured noise buffer size for the AnalogIn instrument, in samples (min/max pairs).
    /// </summary>
    /// <param name="hdwf">Device handle.</param>
    /// <param name="pnSize">When this method returns, contains the current actual noise buffer size in samples (number of min/max pairs).</param>
    /// <returns>Returns 1 (TRUE) if the noise buffer size was retrieved successfully, 0 (FALSE) otherwise.</returns>
    [DllImport("dwf", EntryPoint = "FDwfAnalogInNoiseSizeGet", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern int FDwfAnalogInNoiseSizeGet(int hdwf, out int pnSize);

    /// <summary>
    /// Retrieves information about the supported acquisition modes for the AnalogIn instrument.
    /// </summary>
    /// <param name="hdwf">Device handle.</param>
    /// <param name="pfsacqmode">When this method returns, contains a bitmask of supported acquisition modes. Each bit corresponds to an <c>acqmode*</c> constant (e.g., (1 &lt;&lt; <see cref="acqmodeSingle"/>) | (1 &lt;&lt; <see cref="acqmodeRecord"/>)).</param>
    /// <returns>Returns 1 (TRUE) if the acquisition mode information was retrieved successfully, 0 (FALSE) otherwise.</returns>
    [DllImport("dwf", EntryPoint = "FDwfAnalogInAcquisitionModeInfo", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern int FDwfAnalogInAcquisitionModeInfo(int hdwf, out int pfsacqmode);

    /// <summary>
    /// Sets the acquisition mode for the AnalogIn instrument.
    /// </summary>
    /// <param name="hdwf">Device handle.</param>
    /// <param name="acqmode">The desired acquisition mode (see <c>acqmode*</c> constants like <see cref="acqmodeSingle"/>, <see cref="acqmodeRecord"/>, <see cref="acqmodeScanShift"/>, etc.).</param>
    /// <returns>Returns 1 (TRUE) if the acquisition mode was set successfully, 0 (FALSE) otherwise.</returns>
    [DllImport("dwf", EntryPoint = "FDwfAnalogInAcquisitionModeSet", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern int FDwfAnalogInAcquisitionModeSet(int hdwf, int acqmode);

    /// <summary>
    /// Gets the currently configured acquisition mode for the AnalogIn instrument.
    /// </summary>
    /// <param name="hdwf">Device handle.</param>
    /// <param name="pacqmode">When this method returns, contains the current acquisition mode (see <c>acqmode*</c> constants).</param>
    /// <returns>Returns 1 (TRUE) if the acquisition mode was retrieved successfully, 0 (FALSE) otherwise.</returns>
    [DllImport("dwf", EntryPoint = "FDwfAnalogInAcquisitionModeGet", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern int FDwfAnalogInAcquisitionModeGet(int hdwf, out int pacqmode);


    // Channel configuration:
    /// <summary>
    /// Retrieves the number of physical analog input channels available on the device.
    /// </summary>
    /// <param name="hdwf">Device handle.</param>
    /// <param name="pcChannel">When this method returns, contains the count of available analog input channels.</param>
    /// <returns>Returns 1 (TRUE) if successful, 0 (FALSE) otherwise.</returns>
    [DllImport("dwf", EntryPoint = "FDwfAnalogInChannelCount", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern int FDwfAnalogInChannelCount(int hdwf, out int pcChannel);

    /// <summary>
    /// Enables or disables a specific analog input channel for acquisition.
    /// Only enabled channels will acquire data.
    /// </summary>
    /// <param name="hdwf">Device handle.</param>
    /// <param name="idxChannel">The zero-based index of the channel to enable or disable.</param>
    /// <param name="fEnable">Set to 1 (TRUE) to enable the specified channel, or 0 (FALSE) to disable it.</param>
    /// <returns>Returns 1 (TRUE) if the channel enable state was set successfully, 0 (FALSE) otherwise.</returns>
    [DllImport("dwf", EntryPoint = "FDwfAnalogInChannelEnableSet", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern int FDwfAnalogInChannelEnableSet(int hdwf, int idxChannel, int fEnable);

    /// <summary>
    /// Gets the current enable status of a specific analog input channel.
    /// </summary>
    /// <param name="hdwf">Device handle.</param>
    /// <param name="idxChannel">The zero-based index of the channel to query.</param>
    /// <param name="pfEnable">When this method returns, contains 1 (TRUE) if the specified channel is enabled, or 0 (FALSE) if it is disabled.</param>
    /// <returns>Returns 1 (TRUE) if the channel enable status was retrieved successfully, 0 (FALSE) otherwise.</returns>
    [DllImport("dwf", EntryPoint = "FDwfAnalogInChannelEnableGet", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern int FDwfAnalogInChannelEnableGet(int hdwf, int idxChannel, out int pfEnable);

    /// <summary>
    /// Retrieves information about the supported filter types for the analog input channels (e.g., decimation, averaging).
    /// </summary>
    /// <param name="hdwf">Device handle.</param>
    /// <param name="pfsfilter">When this method returns, contains a bitmask of supported filter types. Each bit corresponds to a <c>filter*</c> constant (e.g., (1 &lt;&lt; <see cref="filterDecimate"/>) | (1 &lt;&lt; <see cref="filterAverage"/>)).</param>
    /// <returns>Returns 1 (TRUE) if the filter information was retrieved successfully, 0 (FALSE) otherwise.</returns>
    [DllImport("dwf", EntryPoint = "FDwfAnalogInChannelFilterInfo", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern int FDwfAnalogInChannelFilterInfo(int hdwf, out int pfsfilter);

    /// <summary>
    /// Sets the data acquisition filter type for a specific analog input channel.
    /// </summary>
    /// <param name="hdwf">Device handle.</param>
    /// <param name="idxChannel">The zero-based index of the channel for which to set the filter.</param>
    /// <param name="filter">The desired filter type (see <c>filter*</c> constants like <see cref="filterDecimate"/>, <see cref="filterAverage"/>, <see cref="filterMinMax"/>).</param>
    /// <returns>Returns 1 (TRUE) if the filter type was set successfully, 0 (FALSE) otherwise.</returns>
    [DllImport("dwf", EntryPoint = "FDwfAnalogInChannelFilterSet", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern int FDwfAnalogInChannelFilterSet(int hdwf, int idxChannel, int filter);

    /// <summary>
    /// Gets the currently configured data acquisition filter type for a specific analog input channel.
    /// </summary>
    /// <param name="hdwf">Device handle.</param>
    /// <param name="idxChannel">The zero-based index of the channel to query.</param>
    /// <param name="pfilter">When this method returns, contains the current filter type for the specified channel (see <c>filter*</c> constants).</param>
    /// <returns>Returns 1 (TRUE) if the filter type was retrieved successfully, 0 (FALSE) otherwise.</returns>
    [DllImport("dwf", EntryPoint = "FDwfAnalogInChannelFilterGet", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern int FDwfAnalogInChannelFilterGet(int hdwf, int idxChannel, out int pfilter);

    /// <summary>
    /// Retrieves information about the valid voltage range settings for the analog input channels, including minimum, maximum, and the number of discrete steps (selectable ranges).
    /// </summary>
    /// <param name="hdwf">Device handle.</param>
    /// <param name="pvoltsMin">When this method returns, contains the minimum voltage of the overall selectable input range (typically a negative value for symmetric ranges, e.g., -25V).</param>
    /// <param name="pvoltsMax">When this method returns, contains the maximum voltage of the overall selectable input range (typically a positive value for symmetric ranges, e.g., +25V).</param>
    /// <param name="pnSteps">When this method returns, contains the number of discrete voltage range steps (e.g., different attenuation settings) available for selection.</param>
    /// <returns>Returns 1 (TRUE) if the range information was retrieved successfully, 0 (FALSE) otherwise.</returns>
    [DllImport("dwf", EntryPoint = "FDwfAnalogInChannelRangeInfo", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern int FDwfAnalogInChannelRangeInfo(int hdwf, out double pvoltsMin, out double pvoltsMax, out double pnSteps);

    /// <summary>
    /// Retrieves the list of discrete voltage range steps (full scale or peak-to-peak values) supported by the analog input channels.
    /// To determine the required buffer size for <paramref name="rgVoltsStep"/>, first call this function with <paramref name="pnSteps"/> set to 0 and <paramref name="rgVoltsStep"/> as null. The function will return the number of steps in <paramref name="pnSteps"/>. Then, allocate the buffer and call again.
    /// </summary>
    /// <param name="hdwf">Device handle.</param>
    /// <param name="rgVoltsStep">A pre-allocated array to receive the supported voltage range steps (e.g., 0.1V, 1V, 10V, 50V). The size of this array should be at least the value returned in <paramref name="pnSteps"/> from a preliminary call (or a sufficiently large buffer if the number of steps is known).</param>
    /// <param name="pnSteps">As input, this parameter should specify the size of the <paramref name="rgVoltsStep"/> array. Upon successful return, it contains the actual number of supported voltage range steps written to the array.</param>
    /// <returns>Returns 1 (TRUE) if the range steps were retrieved successfully, 0 (FALSE) otherwise.</returns>
    [DllImport("dwf", EntryPoint = "FDwfAnalogInChannelRangeSteps", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern int FDwfAnalogInChannelRangeSteps(int hdwf, [MarshalAs(UnmanagedType.LPArray)] double[] rgVoltsStep, out int pnSteps);

    /// <summary>
    /// Sets the voltage range (full scale or peak-to-peak value) for a specific analog input channel.
    /// The DWF library may adjust the requested range to the closest valid range supported by the hardware.
    /// It is recommended to call <see cref="FDwfAnalogInChannelRangeGet"/> after this function to verify the actual range that was set.
    /// </summary>
    /// <param name="hdwf">Device handle.</param>
    /// <param name="idxChannel">The zero-based index of the channel for which to set the voltage range.</param>
    /// <param name="voltsRange">The desired voltage range (full scale, peak-to-peak value, e.g., 50.0 for a +/-25V range, or 5.0 for a +/-2.5V range).</param>
    /// <returns>Returns 1 (TRUE) if the voltage range was set successfully, 0 (FALSE) otherwise.</returns>
    [DllImport("dwf", EntryPoint = "FDwfAnalogInChannelRangeSet", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern int FDwfAnalogInChannelRangeSet(int hdwf, int idxChannel, double voltsRange);

    /// <summary>
    /// Gets the currently configured voltage range (full scale or peak-to-peak value) for a specific analog input channel.
    /// This value may differ from the value set by <see cref="FDwfAnalogInChannelRangeSet"/> due to hardware adjustments.
    /// </summary>
    /// <param name="hdwf">Device handle.</param>
    /// <param name="idxChannel">The zero-based index of the channel to query.</param>
    /// <param name="pvoltsRange">When this method returns, contains the current actual voltage range (full scale, peak-to-peak value) of the specified channel.</param>
    /// <returns>Returns 1 (TRUE) if the voltage range was retrieved successfully, 0 (FALSE) otherwise.</returns>
    [DllImport("dwf", EntryPoint = "FDwfAnalogInChannelRangeGet", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern int FDwfAnalogInChannelRangeGet(int hdwf, int idxChannel, out double pvoltsRange);

    /// <summary>
    /// Retrieves information about the valid voltage offset (DC offset) range for the analog input channels, including minimum, maximum, and the number of discrete steps.
    /// </summary>
    /// <param name="hdwf">Device handle.</param>
    /// <param name="pvoltsMin">When this method returns, contains the minimum settable offset voltage in Volts.</param>
    /// <param name="pvoltsMax">When this method returns, contains the maximum settable offset voltage in Volts.</param>
    /// <param name="pnSteps">When this method returns, contains the number of discrete steps available for offset adjustment (0 if continuous or not applicable).</param>
    /// <returns>Returns 1 (TRUE) if the offset information was retrieved successfully, 0 (FALSE) otherwise.</returns>
    [DllImport("dwf", EntryPoint = "FDwfAnalogInChannelOffsetInfo", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern int FDwfAnalogInChannelOffsetInfo(int hdwf, out double pvoltsMin, out double pvoltsMax, out double pnSteps);

    /// <summary>
    /// Sets the voltage offset (DC offset) for a specific analog input channel.
    /// The DWF library may adjust the requested offset to the closest valid value supported by the hardware.
    /// It is recommended to call <see cref="FDwfAnalogInChannelOffsetGet"/> after this function to verify the actual offset that was set.
    /// </summary>
    /// <param name="hdwf">Device handle.</param>
    /// <param name="idxChannel">The zero-based index of the channel for which to set the offset.</param>
    /// <param name="voltOffset">The desired voltage offset in Volts.</param>
    /// <returns>Returns 1 (TRUE) if the offset was set successfully, 0 (FALSE) otherwise.</returns>
    [DllImport("dwf", EntryPoint = "FDwfAnalogInChannelOffsetSet", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern int FDwfAnalogInChannelOffsetSet(int hdwf, int idxChannel, double voltOffset);

    /// <summary>
    /// Gets the currently configured voltage offset (DC offset) for a specific analog input channel.
    /// This value may differ from the value set by <see cref="FDwfAnalogInChannelOffsetSet"/> due to hardware adjustments.
    /// </summary>
    /// <param name="hdwf">Device handle.</param>
    /// <param name="idxChannel">The zero-based index of the channel to query.</param>
    /// <param name="pvoltOffset">When this method returns, contains the current actual voltage offset in Volts for the specified channel.</param>
    /// <returns>Returns 1 (TRUE) if the offset was retrieved successfully, 0 (FALSE) otherwise.</returns>
    [DllImport("dwf", EntryPoint = "FDwfAnalogInChannelOffsetGet", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern int FDwfAnalogInChannelOffsetGet(int hdwf, int idxChannel, out double pvoltOffset);

    /// <summary>
    /// Sets the attenuation factor for a specific analog input channel. This is typically used to account for external probes (e.g., 1x, 10x probes).
    /// The configured attenuation factor is applied to the acquired data, so voltage readings returned by functions like <see cref="FDwfAnalogInStatusData"/> will be scaled accordingly.
    /// For example, with a 10x probe, set <paramref name="xAttenuation"/> to 10.0.
    /// </summary>
    /// <param name="hdwf">Device handle.</param>
    /// <param name="idxChannel">The zero-based index of the channel for which to set the attenuation.</param>
    /// <param name="xAttenuation">The desired attenuation factor (e.g., 1.0 for a 1x probe, 10.0 for a 10x probe). A value of 0.0 might be invalid or reset to default (1.0).</param>
    /// <returns>Returns 1 (TRUE) if the attenuation factor was set successfully, 0 (FALSE) otherwise.</returns>
    [DllImport("dwf", EntryPoint = "FDwfAnalogInChannelAttenuationSet", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern int FDwfAnalogInChannelAttenuationSet(int hdwf, int idxChannel, double xAttenuation);

    /// <summary>
    /// Gets the currently configured attenuation factor for a specific analog input channel.
    /// </summary>
    /// <param name="hdwf">Device handle.</param>
    /// <param name="idxChannel">The zero-based index of the channel to query.</param>
    /// <param name="pxAttenuation">When this method returns, contains the current attenuation factor for the specified channel.</param>
    /// <returns>Returns 1 (TRUE) if the attenuation factor was retrieved successfully, 0 (FALSE) otherwise.</returns>
    [DllImport("dwf", EntryPoint = "FDwfAnalogInChannelAttenuationGet", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern int FDwfAnalogInChannelAttenuationGet(int hdwf, int idxChannel, out double pxAttenuation);


    // Trigger configuration:
    /// <summary>
    /// Sets the trigger source for the AnalogIn instrument.
    /// The trigger source determines what event will initiate the data acquisition process.
    /// </summary>
    /// <param name="hdwf">Device handle.</param>
    /// <param name="trigsrc">The desired trigger source. See <c>trigsrc*</c> constants like <see cref="trigsrcDetectorAnalogIn"/> (internal analog trigger), <see cref="trigsrcExternal1"/> (external trigger pin T1), <see cref="trigsrcPC"/> (software trigger), <see cref="trigsrcNone"/> (no trigger, for free-running acquisition), etc.</param>
    /// <returns>Returns 1 (TRUE) if the trigger source was set successfully, 0 (FALSE) otherwise.</returns>
    [DllImport("dwf", EntryPoint = "FDwfAnalogInTriggerSourceSet", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern int FDwfAnalogInTriggerSourceSet(int hdwf, byte trigsrc);

    /// <summary>
    /// Gets the currently configured trigger source for the AnalogIn instrument.
    /// </summary>
    /// <param name="hdwf">Device handle.</param>
    /// <param name="ptrigsrc">When this method returns, contains the current trigger source (see <c>trigsrc*</c> constants).</param>
    /// <returns>Returns 1 (TRUE) if the trigger source was retrieved successfully, 0 (FALSE) otherwise.</returns>
    [DllImport("dwf", EntryPoint = "FDwfAnalogInTriggerSourceGet", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern int FDwfAnalogInTriggerSourceGet(int hdwf, out byte ptrigsrc);

    /// <summary>
    /// Retrieves information about the valid range (minimum, maximum) and number of discrete steps for the trigger position (also known as trigger delay).
    /// The trigger position defines the location of the trigger event within the acquisition buffer, allowing for pre-trigger and post-trigger data capture.
    /// It is expressed in seconds relative to the center of the buffer. A negative value means more pre-trigger samples, a positive value means more post-trigger samples.
    /// </summary>
    /// <param name="hdwf">Device handle.</param>
    /// <param name="psecMin">When this method returns, contains the minimum configurable trigger position in seconds (can be negative, indicating maximum pre-trigger data).</param>
    /// <param name="psecMax">When this method returns, contains the maximum configurable trigger position in seconds (can be positive, indicating maximum post-trigger data).</param>
    /// <param name="pnSteps">When this method returns, contains the number of discrete steps or the resolution for trigger position adjustment. This is often related to the sample period.</param>
    /// <returns>Returns 1 (TRUE) if the trigger position information was retrieved successfully, 0 (FALSE) otherwise.</returns>
    [DllImport("dwf", EntryPoint = "FDwfAnalogInTriggerPositionInfo", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern int FDwfAnalogInTriggerPositionInfo(int hdwf, out double psecMin, out double psecMax, out double pnSteps);

    /// <summary>
    /// Sets the trigger position (delay) relative to the start of the acquisition buffer.
    /// The trigger position determines how many samples are recorded before and after the trigger event.
    /// The value is specified in seconds. A positive value means the trigger event occurs after the start of data capture (more post-trigger samples).
    /// A negative value means the trigger event occurs before the end of data capture (more pre-trigger samples).
    /// The relationship is approximately: Position_seconds = (SamplesAfterTrigger - TotalSamplesInDeviceBuffer/2) / AcquisitionFrequency.
    /// </summary>
    /// <param name="hdwf">Device handle.</param>
    /// <param name="secPosition">The desired trigger position in seconds. 0.0 typically centers the trigger in the buffer.</param>
    /// <returns>Returns 1 (TRUE) if the trigger position was set successfully, 0 (FALSE) otherwise.</returns>
    [DllImport("dwf", EntryPoint = "FDwfAnalogInTriggerPositionSet", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern int FDwfAnalogInTriggerPositionSet(int hdwf, double secPosition);

    /// <summary>
    /// Gets the currently configured trigger position (delay) in seconds, relative to the start of the acquisition buffer.
    /// </summary>
    /// <param name="hdwf">Device handle.</param>
    /// <param name="psecPosition">When this method returns, contains the current trigger position in seconds.</param>
    /// <returns>Returns 1 (TRUE) if the trigger position was retrieved successfully, 0 (FALSE) otherwise.</returns>
    [DllImport("dwf", EntryPoint = "FDwfAnalogInTriggerPositionGet", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern int FDwfAnalogInTriggerPositionGet(int hdwf, out double psecPosition);

    /// <summary>
    /// Retrieves the actual trigger position status after an acquisition. This can be useful if the requested position was adjusted by the device due to hardware constraints or if the acquisition was auto-triggered.
    /// </summary>
    /// <param name="hdwf">Device handle.</param>
    /// <param name="psecPosition">When this method returns, contains the actual trigger position in seconds, relative to the start of the acquired buffer data.</param>
    /// <returns>Returns 1 (TRUE) if the trigger position status was retrieved successfully, 0 (FALSE) otherwise.</returns>
    [DllImport("dwf", EntryPoint = "FDwfAnalogInTriggerPositionStatus", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern int FDwfAnalogInTriggerPositionStatus(int hdwf, out double psecPosition);

    /// <summary>
    /// Retrieves information about the valid range (minimum, maximum) and number of discrete steps for the auto-trigger timeout setting.
    /// The auto-trigger timeout causes the acquisition to trigger automatically if no valid configured trigger condition occurs within the specified time after arming.
    /// </summary>
    /// <param name="hdwf">Device handle.</param>
    /// <param name="psecMin">When this method returns, contains the minimum configurable auto-trigger timeout in seconds.</param>
    /// <param name="psecMax">When this method returns, contains the maximum configurable auto-trigger timeout in seconds.</param>
    /// <param name="pnSteps">When this method returns, contains the number of discrete steps or the resolution for timeout adjustment (0 if continuous or not applicable).</param>
    /// <returns>Returns 1 (TRUE) if the auto-trigger timeout information was retrieved successfully, 0 (FALSE) otherwise.</returns>
    [DllImport("dwf", EntryPoint = "FDwfAnalogInTriggerAutoTimeoutInfo", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern int FDwfAnalogInTriggerAutoTimeoutInfo(int hdwf, out double psecMin, out double psecMax, out double pnSteps);

    /// <summary>
    /// Sets the auto-trigger timeout. If the AnalogIn instrument is armed and no valid trigger condition occurs within this specified time, the acquisition will be automatically triggered.
    /// </summary>
    /// <param name="hdwf">Device handle.</param>
    /// <param name="secTimeout">The desired auto-trigger timeout in seconds. Set to 0.0 to disable auto-triggering (the instrument will wait indefinitely for a configured trigger condition).</param>
    /// <returns>Returns 1 (TRUE) if the auto-trigger timeout was set successfully, 0 (FALSE) otherwise.</returns>
    [DllImport("dwf", EntryPoint = "FDwfAnalogInTriggerAutoTimeoutSet", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern int FDwfAnalogInTriggerAutoTimeoutSet(int hdwf, double secTimeout);

    /// <summary>
    /// Gets the currently configured auto-trigger timeout value.
    /// </summary>
    /// <param name="hdwf">Device handle.</param>
    /// <param name="psecTimeout">When this method returns, contains the current auto-trigger timeout in seconds. A value of 0.0 indicates that auto-triggering is disabled.</param>
    /// <returns>Returns 1 (TRUE) if the auto-trigger timeout was retrieved successfully, 0 (FALSE) otherwise.</returns>
    [DllImport("dwf", EntryPoint = "FDwfAnalogInTriggerAutoTimeoutGet", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern int FDwfAnalogInTriggerAutoTimeoutGet(int hdwf, out double psecTimeout);

    /// <summary>
    /// Retrieves information about the valid range (minimum, maximum) and step size for the trigger hold-off time.
    /// Trigger hold-off prevents re-triggering for a specified duration after an initial trigger event.
    /// </summary>
    /// <param name="hdwf">Device handle.</param>
    /// <param name="psecMin">When this method returns, contains the minimum configurable trigger hold-off time in seconds.</param>
    /// <param name="psecMax">When this method returns, contains the maximum configurable trigger hold-off time in seconds.</param>
    /// <param name="pnStep">When this method returns, contains the step size or resolution for hold-off time adjustment.</param>
    /// <returns>Returns 1 (TRUE) if the hold-off information was retrieved successfully, 0 (FALSE) otherwise.</returns>
    [DllImport("dwf", EntryPoint = "FDwfAnalogInTriggerHoldOffInfo", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern int FDwfAnalogInTriggerHoldOffInfo(int hdwf, out double psecMin, out double psecMax, out double pnStep);

    /// <summary>
    /// Sets the trigger hold-off time. This is the duration after a trigger event during which the trigger circuit ignores further potential trigger conditions.
    /// This is useful for stabilizing triggers on noisy signals or preventing multiple triggers on events like ringing.
    /// </summary>
    /// <param name="hdwf">Device handle.</param>
    /// <param name="secHoldOff">The desired trigger hold-off time in seconds. Set to 0.0 to disable hold-off.</param>
    /// <returns>Returns 1 (TRUE) if the hold-off time was set successfully, 0 (FALSE) otherwise.</returns>
    [DllImport("dwf", EntryPoint = "FDwfAnalogInTriggerHoldOffSet", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern int FDwfAnalogInTriggerHoldOffSet(int hdwf, double secHoldOff);

    /// <summary>
    /// Gets the currently configured trigger hold-off time.
    /// </summary>
    /// <param name="hdwf">Device handle.</param>
    /// <param name="psecHoldOff">When this method returns, contains the current trigger hold-off time in seconds. A value of 0.0 indicates that hold-off is disabled.</param>
    /// <returns>Returns 1 (TRUE) if the hold-off time was retrieved successfully, 0 (FALSE) otherwise.</returns>
    [DllImport("dwf", EntryPoint = "FDwfAnalogInTriggerHoldOffGet", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern int FDwfAnalogInTriggerHoldOffGet(int hdwf, out double psecHoldOff);

    /// <summary>
    /// Retrieves information about the supported trigger types for the AnalogIn instrument (e.g., edge, pulse, transition).
    /// </summary>
    /// <param name="hdwf">Device handle.</param>
    /// <param name="pfstrigtype">When this method returns, contains a bitmask of supported trigger types. Each bit corresponds to a <c>trigtype*</c> constant (e.g., (1 &lt;&lt; <see cref="trigtypeEdge"/>) | (1 &lt;&lt; <see cref="trigtypePulse"/>)).</param>
    /// <returns>Returns 1 (TRUE) if the trigger type information was retrieved successfully, 0 (FALSE) otherwise.</returns>
    [DllImport("dwf", EntryPoint = "FDwfAnalogInTriggerTypeInfo", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern int FDwfAnalogInTriggerTypeInfo(int hdwf, out int pfstrigtype);

    /// <summary>
    /// Sets the trigger type for the AnalogIn instrument.
    /// </summary>
    /// <param name="hdwf">Device handle.</param>
    /// <param name="trigtype">The desired trigger type (see <c>trigtype*</c> constants like <see cref="trigtypeEdge"/>, <see cref="trigtypePulse"/>, <see cref="trigtypeTransition"/>).</param>
    /// <returns>Returns 1 (TRUE) if the trigger type was set successfully, 0 (FALSE) otherwise.</returns>
    [DllImport("dwf", EntryPoint = "FDwfAnalogInTriggerTypeSet", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern int FDwfAnalogInTriggerTypeSet(int hdwf, int trigtype);

    /// <summary>
    /// Gets the currently configured trigger type for the AnalogIn instrument.
    /// </summary>
    /// <param name="hdwf">Device handle.</param>
    /// <param name="ptrigtype">When this method returns, contains the current trigger type (see <c>trigtype*</c> constants).</param>
    /// <returns>Returns 1 (TRUE) if the trigger type was retrieved successfully, 0 (FALSE) otherwise.</returns>
    [DllImport("dwf", EntryPoint = "FDwfAnalogInTriggerTypeGet", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern int FDwfAnalogInTriggerTypeGet(int hdwf, out int ptrigtype);


    // Channel configuration (Trigger Channel Specific):
    /// <summary>
    /// Retrieves information about the valid range (minimum and maximum index) for selecting an analog input channel as the trigger source.
    /// This is applicable when the main trigger source (<see cref="FDwfAnalogInTriggerSourceSet"/>) is set to <see cref="trigsrcAnalogIn"/>.
    /// </summary>
    /// <param name="hdwf">Device handle.</param>
    /// <param name="pidxMin">When this method returns, contains the minimum valid channel index that can be used as a trigger source.</param>
    /// <param name="pidxMax">When this method returns, contains the maximum valid channel index that can be used as a trigger source.</param>
    /// <returns>Returns 1 (TRUE) if the trigger channel information was retrieved successfully, 0 (FALSE) otherwise.</returns>
    [DllImport("dwf", EntryPoint = "FDwfAnalogInTriggerChannelInfo", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern int FDwfAnalogInTriggerChannelInfo(int hdwf, out int pidxMin, out int pidxMax);

    /// <summary>
    /// Sets the specific analog input channel to be used as the source for the trigger detector.
    /// This function is used when the main trigger source (<see cref="FDwfAnalogInTriggerSourceSet"/>) is configured to <see cref="trigsrcAnalogIn"/>.
    /// </summary>
    /// <param name="hdwf">Device handle.</param>
    /// <param name="idxChannel">The zero-based index of the analog input channel to use for triggering.</param>
    /// <returns>Returns 1 (TRUE) if the trigger channel was set successfully, 0 (FALSE) otherwise.</returns>
    [DllImport("dwf", EntryPoint = "FDwfAnalogInTriggerChannelSet", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern int FDwfAnalogInTriggerChannelSet(int hdwf, int idxChannel);

    /// <summary>
    /// Gets the currently configured analog input channel that is used as the trigger source.
    /// This is relevant when the main trigger source is <see cref="trigsrcAnalogIn"/>.
    /// </summary>
    /// <param name="hdwf">Device handle.</param>
    /// <param name="pidxChannel">When this method returns, contains the zero-based index of the analog input channel currently used for triggering.</param>
    /// <returns>Returns 1 (TRUE) if the trigger channel was retrieved successfully, 0 (FALSE) otherwise.</returns>
    [DllImport("dwf", EntryPoint = "FDwfAnalogInTriggerChannelGet", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern int FDwfAnalogInTriggerChannelGet(int hdwf, out int pidxChannel);

    /// <summary>
    /// Retrieves information about the supported filter types that can be applied specifically to the selected trigger channel.
    /// </summary>
    /// <param name="hdwf">Device handle.</param>
    /// <param name="pfsfilter">When this method returns, contains a bitmask of supported filter types for the trigger channel (see <c>filter*</c> constants).</param>
    /// <returns>Returns 1 (TRUE) if the trigger filter information was retrieved successfully, 0 (FALSE) otherwise.</returns>
    [DllImport("dwf", EntryPoint = "FDwfAnalogInTriggerFilterInfo", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern int FDwfAnalogInTriggerFilterInfo(int hdwf, out int pfsfilter);

    /// <summary>
    /// Sets the filter type to be applied to the signal from the selected trigger channel before it is evaluated by the trigger detector.
    /// </summary>
    /// <param name="hdwf">Device handle.</param>
    /// <param name="filter">The desired filter type for the trigger channel (see <c>filter*</c> constants like <see cref="filterDecimate"/>, <see cref="filterAverage"/>).</param>
    /// <returns>Returns 1 (TRUE) if the trigger filter was set successfully, 0 (FALSE) otherwise.</returns>
    [DllImport("dwf", EntryPoint = "FDwfAnalogInTriggerFilterSet", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern int FDwfAnalogInTriggerFilterSet(int hdwf, int filter);

    /// <summary>
    /// Gets the currently configured filter type for the trigger channel.
    /// </summary>
    /// <param name="hdwf">Device handle.</param>
    /// <param name="pfilter">When this method returns, contains the current filter type applied to the trigger channel signal (see <c>filter*</c> constants).</param>
    /// <returns>Returns 1 (TRUE) if the trigger filter was retrieved successfully, 0 (FALSE) otherwise.</returns>
    [DllImport("dwf", EntryPoint = "FDwfAnalogInTriggerFilterGet", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern int FDwfAnalogInTriggerFilterGet(int hdwf, out int pfilter);

    /// <summary>
    /// Retrieves information about the valid range (minimum, maximum) and number of discrete steps for setting the trigger level.
    /// The trigger level is the voltage threshold used for edge, pulse, or transition triggers.
    /// </summary>
    /// <param name="hdwf">Device handle.</param>
    /// <param name="pvoltsMin">When this method returns, contains the minimum configurable trigger level in Volts.</param>
    /// <param name="pvoltsMax">When this method returns, contains the maximum configurable trigger level in Volts.</param>
    /// <param name="pnSteps">When this method returns, contains the number of discrete steps or the resolution for trigger level adjustment.</param>
    /// <returns>Returns 1 (TRUE) if the trigger level information was retrieved successfully, 0 (FALSE) otherwise.</returns>
    [DllImport("dwf", EntryPoint = "FDwfAnalogInTriggerLevelInfo", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern int FDwfAnalogInTriggerLevelInfo(int hdwf, out double pvoltsMin, out double pvoltsMax, out double pnSteps);

    /// <summary>
    /// Sets the trigger level in Volts for the AnalogIn instrument.
    /// This level is used by the trigger detector to determine when a trigger condition (e.g., edge crossing) occurs.
    /// </summary>
    /// <param name="hdwf">Device handle.</param>
    /// <param name="voltsLevel">The desired trigger level in Volts.</param>
    /// <returns>Returns 1 (TRUE) if the trigger level was set successfully, 0 (FALSE) otherwise.</returns>
    [DllImport("dwf", EntryPoint = "FDwfAnalogInTriggerLevelSet", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern int FDwfAnalogInTriggerLevelSet(int hdwf, double voltsLevel);

    /// <summary>
    /// Gets the currently configured trigger level in Volts.
    /// </summary>
    /// <param name="hdwf">Device handle.</param>
    /// <param name="pvoltsLevel">When this method returns, contains the current trigger level in Volts.</param>
    /// <returns>Returns 1 (TRUE) if the trigger level was retrieved successfully, 0 (FALSE) otherwise.</returns>
    [DllImport("dwf", EntryPoint = "FDwfAnalogInTriggerLevelGet", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern int FDwfAnalogInTriggerLevelGet(int hdwf, out double pvoltsLevel);

    /// <summary>
    /// Retrieves information about the valid range (minimum, maximum) and number of discrete steps for the trigger hysteresis.
    /// Hysteresis defines a voltage window around the trigger level to prevent multiple triggers on noisy signals or signals with slow transitions.
    /// </summary>
    /// <param name="hdwf">Device handle.</param>
    /// <param name="pvoltsMin">When this method returns, contains the minimum configurable hysteresis value in Volts.</param>
    /// <param name="pvoltsMax">When this method returns, contains the maximum configurable hysteresis value in Volts.</param>
    /// <param name="pnSteps">When this method returns, contains the number of discrete steps or the resolution for hysteresis adjustment.</param>
    /// <returns>Returns 1 (TRUE) if the hysteresis information was retrieved successfully, 0 (FALSE) otherwise.</returns>
    [DllImport("dwf", EntryPoint = "FDwfAnalogInTriggerHysteresisInfo", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern int FDwfAnalogInTriggerHysteresisInfo(int hdwf, out double pvoltsMin, out double pvoltsMax, out double pnSteps);

    /// <summary>
    /// Sets the trigger hysteresis in Volts.
    /// For a rising edge trigger, the signal must first fall below (TriggerLevel - Hysteresis) before it can trigger when rising above TriggerLevel.
    /// For a falling edge trigger, the signal must first rise above (TriggerLevel + Hysteresis) before it can trigger when falling below TriggerLevel.
    /// </summary>
    /// <param name="hdwf">Device handle.</param>
    /// <param name="voltsLevel">The desired hysteresis value in Volts. This is typically a small positive value. Using 0.0 disables hysteresis.</param>
    /// <returns>Returns 1 (TRUE) if the hysteresis was set successfully, 0 (FALSE) otherwise.</returns>
    [DllImport("dwf", EntryPoint = "FDwfAnalogInTriggerHysteresisSet", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern int FDwfAnalogInTriggerHysteresisSet(int hdwf, double voltsLevel);

    /// <summary>
    /// Gets the currently configured trigger hysteresis in Volts.
    /// </summary>
    /// <param name="hdwf">Device handle.</param>
    /// <param name="pvoltsHysteresis">When this method returns, contains the current hysteresis value in Volts.</param>
    /// <returns>Returns 1 (TRUE) if the hysteresis was retrieved successfully, 0 (FALSE) otherwise.</returns>
    [DllImport("dwf", EntryPoint = "FDwfAnalogInTriggerHysteresisGet", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern int FDwfAnalogInTriggerHysteresisGet(int hdwf, out double pvoltsHysteresis);

    /// <summary>
    /// Retrieves information about the supported trigger conditions (e.g., rising slope, falling slope, either slope).
    /// </summary>
    /// <param name="hdwf">Device handle.</param>
    /// <param name="pfstrigcond">When this method returns, contains a bitmask of supported trigger conditions. Each bit corresponds to a <c>DwfTriggerSlope*</c> constant (e.g., (1 &lt;&lt; <see cref="DwfTriggerSlopeRise"/>)).</param>
    /// <returns>Returns 1 (TRUE) if the trigger condition information was retrieved successfully, 0 (FALSE) otherwise.</returns>
    [DllImport("dwf", EntryPoint = "FDwfAnalogInTriggerConditionInfo", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern int FDwfAnalogInTriggerConditionInfo(int hdwf, out int pfstrigcond);

    /// <summary>
    /// Sets the trigger condition, which typically defines the slope for edge triggers (e.g., rising edge, falling edge, or either).
    /// </summary>
    /// <param name="hdwf">Device handle.</param>
    /// <param name="trigcond">The desired trigger condition, usually one of the <c>DwfTriggerSlope*</c> constants like <see cref="DwfTriggerSlopeRise"/>, <see cref="DwfTriggerSlopeFall"/>, or <see cref="DwfTriggerSlopeEither"/>.</param>
    /// <returns>Returns 1 (TRUE) if the trigger condition was set successfully, 0 (FALSE) otherwise.</returns>
    [DllImport("dwf", EntryPoint = "FDwfAnalogInTriggerConditionSet", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern int FDwfAnalogInTriggerConditionSet(int hdwf, int trigcond);

    /// <summary>
    /// Gets the currently configured trigger condition (slope).
    /// </summary>
    /// <param name="hdwf">Device handle.</param>
    /// <param name="ptrigcond">When this method returns, contains the current trigger condition (see <c>DwfTriggerSlope*</c> constants).</param>
    /// <returns>Returns 1 (TRUE) if the trigger condition was retrieved successfully, 0 (FALSE) otherwise.</returns>
    [DllImport("dwf", EntryPoint = "FDwfAnalogInTriggerConditionGet", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern int FDwfAnalogInTriggerConditionGet(int hdwf, out int ptrigcond);

    /// <summary>
    /// Retrieves information about the valid range (minimum, maximum) and number of discrete steps for setting the trigger pulse length.
    /// This is applicable when the trigger type (<see cref="FDwfAnalogInTriggerTypeSet"/>) is set to <see cref="trigtypePulse"/>.
    /// </summary>
    /// <param name="hdwf">Device handle.</param>
    /// <param name="psecMin">When this method returns, contains the minimum configurable pulse length in seconds.</param>
    /// <param name="psecMax">When this method returns, contains the maximum configurable pulse length in seconds.</param>
    /// <param name="pnSteps">When this method returns, contains the number of discrete steps or the resolution for pulse length adjustment.</param>
    /// <returns>Returns 1 (TRUE) if the pulse length information was retrieved successfully, 0 (FALSE) otherwise.</returns>
    [DllImport("dwf", EntryPoint = "FDwfAnalogInTriggerLengthInfo", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern int FDwfAnalogInTriggerLengthInfo(int hdwf, out double psecMin, out double psecMax, out double pnSteps);

    /// <summary>
    /// Sets the trigger pulse length. This setting is used in conjunction with <see cref="trigtypePulse"/> trigger type and a trigger length condition (<see cref="FDwfAnalogInTriggerLengthConditionSet"/>).
    /// </summary>
    /// <param name="hdwf">Device handle.</param>
    /// <param name="secLength">The desired pulse length in seconds to compare against.</param>
    /// <returns>Returns 1 (TRUE) if the pulse length was set successfully, 0 (FALSE) otherwise.</returns>
    [DllImport("dwf", EntryPoint = "FDwfAnalogInTriggerLengthSet", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern int FDwfAnalogInTriggerLengthSet(int hdwf, double secLength);

    /// <summary>
    /// Gets the currently configured trigger pulse length.
    /// </summary>
    /// <param name="hdwf">Device handle.</param>
    /// <param name="psecLength">When this method returns, contains the current pulse length setting in seconds.</param>
    /// <returns>Returns 1 (TRUE) if the pulse length was retrieved successfully, 0 (FALSE) otherwise.</returns>
    [DllImport("dwf", EntryPoint = "FDwfAnalogInTriggerLengthGet", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern int FDwfAnalogInTriggerLengthGet(int hdwf, out double psecLength);

    /// <summary>
    /// Retrieves information about the supported trigger length conditions (e.g., trigger if pulse is less than, greater than, or times out relative to the set length).
    /// This is used with the <see cref="trigtypePulse"/> trigger type.
    /// </summary>
    /// <param name="hdwf">Device handle.</param>
    /// <param name="pfstriglen">When this method returns, contains a bitmask of supported trigger length conditions. Each bit corresponds to a <c>triglen*</c> constant (e.g., (1 &lt;&lt; <see cref="triglenLess"/>)).</param>
    /// <returns>Returns 1 (TRUE) if the trigger length condition information was retrieved successfully, 0 (FALSE) otherwise.</returns>
    [DllImport("dwf", EntryPoint = "FDwfAnalogInTriggerLengthConditionInfo", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern int FDwfAnalogInTriggerLengthConditionInfo(int hdwf, out int pfstriglen);

    /// <summary>
    /// Sets the trigger length condition for pulse triggers. This defines how the measured pulse length is compared to the value set by <see cref="FDwfAnalogInTriggerLengthSet"/>.
    /// </summary>
    /// <param name="hdwf">Device handle.</param>
    /// <param name="triglen">The desired trigger length condition (see <c>triglen*</c> constants like <see cref="triglenLess"/>, <see cref="triglenTimeout"/>, <see cref="triglenMore"/>).</param>
    /// <returns>Returns 1 (TRUE) if the trigger length condition was set successfully, 0 (FALSE) otherwise.</returns>
    [DllImport("dwf", EntryPoint = "FDwfAnalogInTriggerLengthConditionSet", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern int FDwfAnalogInTriggerLengthConditionSet(int hdwf, int triglen);

    /// <summary>
    /// Gets the currently configured trigger length condition.
    /// </summary>
    /// <param name="hdwf">Device handle.</param>
    /// <param name="ptriglen">When this method returns, contains the current trigger length condition (see <c>triglen*</c> constants).</param>
    /// <returns>Returns 1 (TRUE) if the trigger length condition was retrieved successfully, 0 (FALSE) otherwise.</returns>
    [DllImport("dwf", EntryPoint = "FDwfAnalogInTriggerLengthConditionGet", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern int FDwfAnalogInTriggerLengthConditionGet(int hdwf, out int ptriglen);

    /// <summary>
    /// Sets the sampling source for the AnalogIn instrument. This allows using an external clock signal (e.g., from a trigger pin) instead of the internal clock.
    /// </summary>
    /// <param name="hdwf">Device handle.</param>
    /// <param name="trigsrc">The desired sampling source. To use an external clock, this would typically be an external trigger source like <see cref="trigsrcExternal1"/>, <see cref="trigsrcExternal2"/>, etc., corresponding to the pin where the external clock is provided.</param>
    /// <returns>Returns 1 (TRUE) if the sampling source was set successfully, 0 (FALSE) otherwise.</returns>
    [DllImport("dwf", EntryPoint = "FDwfAnalogInSamplingSourceSet", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern int FDwfAnalogInSamplingSourceSet(int hdwf, byte trigsrc);

    /// <summary>
    /// Gets the currently configured sampling source for the AnalogIn instrument.
    /// </summary>
    /// <param name="hdwf">Device handle.</param>
    /// <param name="ptrigsrc">When this method returns, contains the current sampling source (see <c>trigsrc*</c> constants). If using internal clock, this would typically be <see cref="trigsrcNone"/> or a device-specific default.</param>
    /// <returns>Returns 1 (TRUE) if the sampling source was retrieved successfully, 0 (FALSE) otherwise.</returns>
    [DllImport("dwf", EntryPoint = "FDwfAnalogInSamplingSourceGet", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern int FDwfAnalogInSamplingSourceGet(int hdwf, out byte ptrigsrc);

    /// <summary>
    /// Sets the sampling slope (e.g., rising or falling edge) to be used when an external clock source is configured for the AnalogIn instrument.
    /// </summary>
    /// <param name="hdwf">Device handle.</param>
    /// <param name="slope">The desired slope on which to sample the external clock (see <c>DwfTriggerSlope*</c> constants: <see cref="DwfTriggerSlopeRise"/> or <see cref="DwfTriggerSlopeFall"/>).</param>
    /// <returns>Returns 1 (TRUE) if the sampling slope was set successfully, 0 (FALSE) otherwise.</returns>
    [DllImport("dwf", EntryPoint = "FDwfAnalogInSamplingSlopeSet", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern int FDwfAnalogInSamplingSlopeSet(int hdwf, int slope);

    /// <summary>
    /// Gets the currently configured sampling slope used with an external clock source.
    /// </summary>
    /// <param name="hdwf">Device handle.</param>
    /// <param name="pslope">When this method returns, contains the current sampling slope (see <c>DwfTriggerSlope*</c> constants).</param>
    /// <returns>Returns 1 (TRUE) if the sampling slope was retrieved successfully, 0 (FALSE) otherwise.</returns>
    [DllImport("dwf", EntryPoint = "FDwfAnalogInSamplingSlopeGet", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern int FDwfAnalogInSamplingSlopeGet(int hdwf, out int pslope);

    /// <summary>
    /// Sets the sampling delay when using an external clock source for the AnalogIn instrument.
    /// This allows fine-tuning of the sampling instant relative to the active edge of the external clock.
    /// The valid range for the delay is device-dependent.
    /// </summary>
    /// <param name="hdwf">Device handle.</param>
    /// <param name="sec">The desired delay in seconds. This value can be positive or negative, within the device's supported range.</param>
    /// <returns>Returns 1 (TRUE) if the sampling delay was set successfully, 0 (FALSE) otherwise.</returns>
    [DllImport("dwf", EntryPoint = "FDwfAnalogInSamplingDelaySet", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern int FDwfAnalogInSamplingDelaySet(int hdwf, double sec);

    /// <summary>
    /// Gets the currently configured sampling delay when using an external clock source.
    /// </summary>
    /// <param name="hdwf">Device handle.</param>
    /// <param name="psec">When this method returns, contains the current sampling delay in seconds.</param>
    /// <returns>Returns 1 (TRUE) if the sampling delay was retrieved successfully, 0 (FALSE) otherwise.</returns>
    [DllImport("dwf", EntryPoint = "FDwfAnalogInSamplingDelayGet", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern int FDwfAnalogInSamplingDelayGet(int hdwf, out double psec);



    // ANALOG OUT INSTRUMENT FUNCTIONS
    // Configuration:
    /// <summary>
    /// Retrieves the number of available analog output channels on the device.
    /// </summary>
    /// <param name="hdwf">Device handle.</param>
    /// <param name="pcChannel">When this method returns, contains the count of available analog output channels.</param>
    /// <returns>Returns 1 (TRUE) if successful, 0 (FALSE) otherwise.</returns>
    [DllImport("dwf", EntryPoint = "FDwfAnalogOutCount", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern int FDwfAnalogOutCount(int hdwf, out int pcChannel);

    /// <summary>
    /// Sets the master channel for a specific analog output channel.
    /// This is used for synchronized operations where multiple channels can be configured to start or be controlled based on the state or trigger of a designated master channel.
    /// </summary>
    /// <param name="hdwf">Device handle.</param>
    /// <param name="idxChannel">The zero-based index of the analog output channel to configure. This channel will become a slave to the <paramref name="idxMaster"/> channel.</param>
    /// <param name="idxMaster">The zero-based index of the analog output channel to be designated as the master. To make <paramref name="idxChannel"/> operate independently or as a master itself, set <paramref name="idxMaster"/> to the same value as <paramref name="idxChannel"/>.</param>
    /// <returns>Returns 1 (TRUE) if the master channel was set successfully, 0 (FALSE) otherwise.</returns>
    [DllImport("dwf", EntryPoint = "FDwfAnalogOutMasterSet", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern int FDwfAnalogOutMasterSet(int hdwf, int idxChannel, int idxMaster);

    /// <summary>
    /// Gets the currently configured master channel for a specific analog output channel.
    /// </summary>
    /// <param name="hdwf">Device handle.</param>
    /// <param name="idxChannel">The zero-based index of the analog output channel to query.</param>
    /// <param name="pidxMaster">When this method returns, contains the zero-based index of the master channel for the specified <paramref name="idxChannel"/>. If they are the same, the channel is its own master or operates independently.</param>
    /// <returns>Returns 1 (TRUE) if the master channel information was retrieved successfully, 0 (FALSE) otherwise.</returns>
    [DllImport("dwf", EntryPoint = "FDwfAnalogOutMasterGet", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern int FDwfAnalogOutMasterGet(int hdwf, int idxChannel, out int pidxMaster);

    /// <summary>
    /// Sets the trigger source for a specific analog output channel.
    /// The trigger source determines what event will initiate the waveform generation on this channel.
    /// </summary>
    /// <param name="hdwf">Device handle.</param>
    /// <param name="idxChannel">The zero-based index of the analog output channel.</param>
    /// <param name="trigsrc">The desired trigger source (see <c>trigsrc*</c> constants like <see cref="trigsrcPC"/>, <see cref="trigsrcExternal1"/>, <see cref="trigsrcAnalogIn"/>, etc.).</param>
    /// <returns>Returns 1 (TRUE) if the trigger source was set successfully, 0 (FALSE) otherwise.</returns>
    [DllImport("dwf", EntryPoint = "FDwfAnalogOutTriggerSourceSet", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern int FDwfAnalogOutTriggerSourceSet(int hdwf, int idxChannel, byte trigsrc);

    /// <summary>
    /// Gets the currently configured trigger source for a specific analog output channel.
    /// </summary>
    /// <param name="hdwf">Device handle.</param>
    /// <param name="idxChannel">The zero-based index of the analog output channel.</param>
    /// <param name="ptrigsrc">When this method returns, contains the current trigger source for the specified channel (see <c>trigsrc*</c> constants).</param>
    /// <returns>Returns 1 (TRUE) if the trigger source was retrieved successfully, 0 (FALSE) otherwise.</returns>
    [DllImport("dwf", EntryPoint = "FDwfAnalogOutTriggerSourceGet", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern int FDwfAnalogOutTriggerSourceGet(int hdwf, int idxChannel, out byte ptrigsrc);

    /// <summary>
    /// Sets the trigger slope for a specific analog output channel when an edge-based trigger source (e.g., external trigger, analog input trigger) is used.
    /// </summary>
    /// <param name="hdwf">Device handle.</param>
    /// <param name="idxChannel">The zero-based index of the analog output channel.</param>
    /// <param name="slope">The desired trigger slope (see <c>DwfTriggerSlope*</c> constants: <see cref="DwfTriggerSlopeRise"/>, <see cref="DwfTriggerSlopeFall"/>, <see cref="DwfTriggerSlopeEither"/>).</param>
    /// <returns>Returns 1 (TRUE) if the trigger slope was set successfully, 0 (FALSE) otherwise.</returns>
    [DllImport("dwf", EntryPoint = "FDwfAnalogOutTriggerSlopeSet", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern int FDwfAnalogOutTriggerSlopeSet(int hdwf, int idxChannel, int slope);

    /// <summary>
    /// Gets the currently configured trigger slope for a specific analog output channel.
    /// </summary>
    /// <param name="hdwf">Device handle.</param>
    /// <param name="idxChannel">The zero-based index of the analog output channel.</param>
    /// <param name="pslope">When this method returns, contains the current trigger slope for the specified channel (see <c>DwfTriggerSlope*</c> constants).</param>
    /// <returns>Returns 1 (TRUE) if the trigger slope was retrieved successfully, 0 (FALSE) otherwise.</returns>
    [DllImport("dwf", EntryPoint = "FDwfAnalogOutTriggerSlopeGet", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern int FDwfAnalogOutTriggerSlopeGet(int hdwf, int idxChannel, out int pslope);

    /// <summary>
    /// Retrieves information about the valid run length (duration) range for waveform generation on a specific analog output channel.
    /// </summary>
    /// <param name="hdwf">Device handle.</param>
    /// <param name="idxChannel">The zero-based index of the analog output channel.</param>
    /// <param name="psecMin">When this method returns, contains the minimum configurable run length in seconds.</param>
    /// <param name="psecMax">When this method returns, contains the maximum configurable run length in seconds. A value of 0.0 often indicates indefinite run time capability.</param>
    /// <returns>Returns 1 (TRUE) if the run length information was retrieved successfully, 0 (FALSE) otherwise.</returns>
    [DllImport("dwf", EntryPoint = "FDwfAnalogOutRunInfo", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern int FDwfAnalogOutRunInfo(int hdwf, int idxChannel, out double psecMin, out double psecMax);

    /// <summary>
    /// Sets the run length (duration) for waveform generation on a specific analog output channel.
    /// </summary>
    /// <param name="hdwf">Device handle.</param>
    /// <param name="idxChannel">The zero-based index of the analog output channel.</param>
    /// <param name="secRun">The desired run length in seconds. Use 0.0 for indefinite run time (waveform generation continues until explicitly stopped or the configured repeat count is met).</param>
    /// <returns>Returns 1 (TRUE) if the run length was set successfully, 0 (FALSE) otherwise.</returns>
    [DllImport("dwf", EntryPoint = "FDwfAnalogOutRunSet", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern int FDwfAnalogOutRunSet(int hdwf, int idxChannel, double secRun);

    /// <summary>
    /// Gets the currently configured run length (duration) for a specific analog output channel.
    /// </summary>
    /// <param name="hdwf">Device handle.</param>
    /// <param name="idxChannel">The zero-based index of the analog output channel.</param>
    /// <param name="psecRun">When this method returns, contains the current run length in seconds. A value of 0.0 typically indicates indefinite run time.</param>
    /// <returns>Returns 1 (TRUE) if the run length was retrieved successfully, 0 (FALSE) otherwise.</returns>
    [DllImport("dwf", EntryPoint = "FDwfAnalogOutRunGet", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern int FDwfAnalogOutRunGet(int hdwf, int idxChannel, out double psecRun);

    /// <summary>
    /// Retrieves the current run status, indicating the remaining run time for waveform generation on a specific analog output channel.
    /// </summary>
    /// <param name="hdwf">Device handle.</param>
    /// <param name="idxChannel">The zero-based index of the analog output channel.</param>
    /// <param name="psecRun">When this method returns, contains the remaining run time in seconds. If the run time is indefinite, this might return a specific large value or 0, depending on the device and library version.</param>
    /// <returns>Returns 1 (TRUE) if the run status was retrieved successfully, 0 (FALSE) otherwise.</returns>
    [DllImport("dwf", EntryPoint = "FDwfAnalogOutRunStatus", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern int FDwfAnalogOutRunStatus(int hdwf, int idxChannel, out double psecRun);

    /// <summary>
    /// Retrieves information about the valid wait time range for a specific analog output channel.
    /// The wait time is the delay from the start command or trigger event before the waveform generation actually begins.
    /// </summary>
    /// <param name="hdwf">Device handle.</param>
    /// <param name="idxChannel">The zero-based index of the analog output channel.</param>
    /// <param name="psecMin">When this method returns, contains the minimum configurable wait time in seconds.</param>
    /// <param name="psecMax">When this method returns, contains the maximum configurable wait time in seconds.</param>
    /// <returns>Returns 1 (TRUE) if the wait time information was retrieved successfully, 0 (FALSE) otherwise.</returns>
    [DllImport("dwf", EntryPoint = "FDwfAnalogOutWaitInfo", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern int FDwfAnalogOutWaitInfo(int hdwf, int idxChannel, out double psecMin, out double psecMax);

    /// <summary>
    /// Sets the wait time for a specific analog output channel. This is the delay before waveform generation starts after a trigger or start command.
    /// </summary>
    /// <param name="hdwf">Device handle.</param>
    /// <param name="idxChannel">The zero-based index of the analog output channel.</param>
    /// <param name="secWait">The desired wait time in seconds.</param>
    /// <returns>Returns 1 (TRUE) if the wait time was set successfully, 0 (FALSE) otherwise.</returns>
    [DllImport("dwf", EntryPoint = "FDwfAnalogOutWaitSet", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern int FDwfAnalogOutWaitSet(int hdwf, int idxChannel, double secWait);

    /// <summary>
    /// Gets the currently configured wait time for a specific analog output channel.
    /// </summary>
    /// <param name="hdwf">Device handle.</param>
    /// <param name="idxChannel">The zero-based index of the analog output channel.</param>
    /// <param name="psecWait">When this method returns, contains the current wait time in seconds.</param>
    /// <returns>Returns 1 (TRUE) if the wait time was retrieved successfully, 0 (FALSE) otherwise.</returns>
    [DllImport("dwf", EntryPoint = "FDwfAnalogOutWaitGet", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern int FDwfAnalogOutWaitGet(int hdwf, int idxChannel, out double psecWait);

    /// <summary>
    /// Retrieves information about the valid repeat count range for waveform generation on a specific analog output channel.
    /// The repeat count determines how many times the waveform cycle is generated.
    /// </summary>
    /// <param name="hdwf">Device handle.</param>
    /// <param name="idxChannel">The zero-based index of the analog output channel.</param>
    /// <param name="pnMin">When this method returns, contains the minimum configurable repeat count (usually 1 for a single cycle).</param>
    /// <param name="pnMax">When this method returns, contains the maximum configurable repeat count. A value of 0 often indicates indefinite repetition capability.</param>
    /// <returns>Returns 1 (TRUE) if the repeat count information was retrieved successfully, 0 (FALSE) otherwise.</returns>
    [DllImport("dwf", EntryPoint = "FDwfAnalogOutRepeatInfo", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern int FDwfAnalogOutRepeatInfo(int hdwf, int idxChannel, out int pnMin, out int pnMax);

    /// <summary>
    /// Sets the repeat count for waveform generation on a specific analog output channel.
    /// </summary>
    /// <param name="hdwf">Device handle.</param>
    /// <param name="idxChannel">The zero-based index of the analog output channel.</param>
    /// <param name="cRepeat">The desired number of times the waveform cycle should be repeated. Use 0 for indefinite repetition (continues until explicitly stopped, run duration is met, or power is lost).</param>
    /// <returns>Returns 1 (TRUE) if the repeat count was set successfully, 0 (FALSE) otherwise.</returns>
    [DllImport("dwf", EntryPoint = "FDwfAnalogOutRepeatSet", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern int FDwfAnalogOutRepeatSet(int hdwf, int idxChannel, int cRepeat);

    /// <summary>
    /// Gets the currently configured repeat count for a specific analog output channel.
    /// </summary>
    /// <param name="hdwf">Device handle.</param>
    /// <param name="idxChannel">The zero-based index of the analog output channel.</param>
    /// <param name="pcRepeat">When this method returns, contains the current repeat count. A value of 0 typically indicates indefinite repetition.</param>
    /// <returns>Returns 1 (TRUE) if the repeat count was retrieved successfully, 0 (FALSE) otherwise.</returns>
    [DllImport("dwf", EntryPoint = "FDwfAnalogOutRepeatGet", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern int FDwfAnalogOutRepeatGet(int hdwf, int idxChannel, out int pcRepeat);

    /// <summary>
    /// Retrieves the current repeat status, indicating the number of remaining waveform repetitions for a specific analog output channel.
    /// </summary>
    /// <param name="hdwf">Device handle.</param>
    /// <param name="idxChannel">The zero-based index of the analog output channel.</param>
    /// <param name="pcRepeat">When this method returns, contains the number of remaining repetitions. If configured for indefinite repeat, this might return a specific large value or 0, depending on the device and library.</param>
    /// <returns>Returns 1 (TRUE) if the repeat status was retrieved successfully, 0 (FALSE) otherwise.</returns>
    [DllImport("dwf", EntryPoint = "FDwfAnalogOutRepeatStatus", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern int FDwfAnalogOutRepeatStatus(int hdwf, int idxChannel, out int pcRepeat);

    /// <summary>
    /// Enables or disables the repeat trigger functionality for a specific analog output channel.
    /// If enabled, after each waveform repetition (as defined by <see cref="FDwfAnalogOutRepeatSet"/>), the channel will wait for its configured trigger condition (<see cref="FDwfAnalogOutTriggerSourceSet"/>) before starting the next repetition.
    /// </summary>
    /// <param name="hdwf">Device handle.</param>
    /// <param name="idxChannel">The zero-based index of the analog output channel.</param>
    /// <param name="fRepeatTrigger">Set to 1 (TRUE) to enable the repeat trigger functionality, or 0 (FALSE) to disable it (waveform repeats immediately without waiting for a trigger).</param>
    /// <returns>Returns 1 (TRUE) if the repeat trigger setting was successfully applied, 0 (FALSE) otherwise.</returns>
    [DllImport("dwf", EntryPoint = "FDwfAnalogOutRepeatTriggerSet", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern int FDwfAnalogOutRepeatTriggerSet(int hdwf, int idxChannel, int fRepeatTrigger);

    /// <summary>
    /// Gets the current state of the repeat trigger setting for a specific analog output channel.
    /// </summary>
    /// <param name="hdwf">Device handle.</param>
    /// <param name="idxChannel">The zero-based index of the analog output channel.</param>
    /// <param name="pfRepeatTrigger">When this method returns, contains 1 (TRUE) if repeat trigger is enabled, or 0 (FALSE) if it is disabled.</param>
    /// <returns>Returns 1 (TRUE) if the repeat trigger setting was retrieved successfully, 0 (FALSE) otherwise.</returns>
    [DllImport("dwf", EntryPoint = "FDwfAnalogOutRepeatTriggerGet", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern int FDwfAnalogOutRepeatTriggerGet(int hdwf, int idxChannel, out int pfRepeatTrigger);


    // EExplorer channel 3&4 current/voltage limitation
    /// <summary>
    /// Retrieves information about the valid limitation range (e.g., current or voltage limit) for a specific analog output channel.
    /// This function is primarily relevant for power supply channels on devices like the Electronics Explorer, where output current or voltage can be limited.
    /// </summary>
    /// <param name="hdwf">Device handle.</param>
    /// <param name="idxChannel">The zero-based index of the analog output channel (typically channel 2 or 3 for Electronics Explorer power supplies).</param>
    /// <param name="pMin">When this method returns, contains the minimum configurable limit value (e.g., minimum current in Amps or minimum voltage in Volts).</param>
    /// <param name="pMax">When this method returns, contains the maximum configurable limit value (e.g., maximum current in Amps or maximum voltage in Volts).</param>
    /// <returns>Returns 1 (TRUE) if the limitation information was retrieved successfully, 0 (FALSE) otherwise.</returns>
    [DllImport("dwf", EntryPoint = "FDwfAnalogOutLimitationInfo", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern int FDwfAnalogOutLimitationInfo(int hdwf, int idxChannel, out double pMin, out double pMax);

    /// <summary>
    /// Sets the limitation (e.g., current or voltage limit) for a specific analog output channel.
    /// This is primarily for power supply channels on devices like the Electronics Explorer.
    /// </summary>
    /// <param name="hdwf">Device handle.</param>
    /// <param name="idxChannel">The zero-based index of the analog output channel.</param>
    /// <param name="limit">The desired limit value. Depending on the channel's mode (<see cref="FDwfAnalogOutModeSet"/>), this could be a current limit in Amps or a voltage limit in Volts.</param>
    /// <returns>Returns 1 (TRUE) if the limitation was set successfully, 0 (FALSE) otherwise.</returns>
    [DllImport("dwf", EntryPoint = "FDwfAnalogOutLimitationSet", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern int FDwfAnalogOutLimitationSet(int hdwf, int idxChannel, double limit);

    /// <summary>
    /// Gets the currently configured limitation for a specific analog output channel.
    /// This is primarily for power supply channels on devices like the Electronics Explorer.
    /// </summary>
    /// <param name="hdwf">Device handle.</param>
    /// <param name="idxChannel">The zero-based index of the analog output channel.</param>
    /// <param name="plimit">When this method returns, contains the current limit value (e.g., current in Amps or voltage in Volts).</param>
    /// <returns>Returns 1 (TRUE) if the limitation was retrieved successfully, 0 (FALSE) otherwise.</returns>
    [DllImport("dwf", EntryPoint = "FDwfAnalogOutLimitationGet", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern int FDwfAnalogOutLimitationGet(int hdwf, int idxChannel, out double plimit);

    /// <summary>
    /// Sets the operation mode for a specific analog output channel, for example, configuring a power supply channel to operate as a voltage source or a current source.
    /// This function is primarily relevant for devices like the Electronics Explorer which have versatile power supply channels.
    /// </summary>
    /// <param name="hdwf">Device handle.</param>
    /// <param name="idxChannel">The zero-based index of the analog output channel.</param>
    /// <param name="mode">The desired operation mode (see <c>DwfAnalogOutMode*</c> constants like <see cref="DwfAnalogOutModeVoltage"/> or <see cref="DwfAnalogOutModeCurrent"/>).</param>
    /// <returns>Returns 1 (TRUE) if the operation mode was set successfully, 0 (FALSE) otherwise.</returns>
    [DllImport("dwf", EntryPoint = "FDwfAnalogOutModeSet", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern int FDwfAnalogOutModeSet(int hdwf, int idxChannel, int mode);

    /// <summary>
    /// Gets the currently configured operation mode for a specific analog output channel.
    /// </summary>
    /// <param name="hdwf">Device handle.</param>
    /// <param name="idxChannel">The zero-based index of the analog output channel.</param>
    /// <param name="pmode">When this method returns, contains the current operation mode of the channel (see <c>DwfAnalogOutMode*</c> constants).</param>
    /// <returns>Returns 1 (TRUE) if the operation mode was retrieved successfully, 0 (FALSE) otherwise.</returns>
    [DllImport("dwf", EntryPoint = "FDwfAnalogOutModeGet", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern int FDwfAnalogOutModeGet(int hdwf, int idxChannel, out int pmode);

    /// <summary>
    /// Retrieves information about the supported idle behaviors for a specific analog output channel.
    /// The idle behavior defines what the channel outputs when it is enabled but not actively generating a waveform (e.g., before starting, after finishing, or when stopped).
    /// </summary>
    /// <param name="hdwf">Device handle.</param>
    /// <param name="idxChannel">The zero-based index of the analog output channel.</param>
    /// <param name="pfsidle">When this method returns, contains a bitmask of supported idle behaviors. Each bit corresponds to a <c>DwfAnalogOutIdle*</c> constant (e.g., (1 &lt;&lt; <see cref="DwfAnalogOutIdleDisable"/>) | (1 &lt;&lt; <see cref="DwfAnalogOutIdleOffset"/>)).</param>
    /// <returns>Returns 1 (TRUE) if the idle behavior information was retrieved successfully, 0 (FALSE) otherwise.</returns>
    [DllImport("dwf", EntryPoint = "FDwfAnalogOutIdleInfo", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern int FDwfAnalogOutIdleInfo(int hdwf, int idxChannel, out int pfsidle);

    /// <summary>
    /// Sets the idle behavior for a specific analog output channel.
    /// This determines the output state of the channel when it is not actively generating a waveform.
    /// </summary>
    /// <param name="hdwf">Device handle.</param>
    /// <param name="idxChannel">The zero-based index of the analog output channel.</param>
    /// <param name="idle">The desired idle behavior (see <c>DwfAnalogOutIdle*</c> constants like <see cref="DwfAnalogOutIdleDisable"/>, <see cref="DwfAnalogOutIdleOffset"/>, or <see cref="DwfAnalogOutIdleInitial"/>).</param>
    /// <returns>Returns 1 (TRUE) if the idle behavior was set successfully, 0 (FALSE) otherwise.</returns>
    [DllImport("dwf", EntryPoint = "FDwfAnalogOutIdleSet", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern int FDwfAnalogOutIdleSet(int hdwf, int idxChannel, int idle);

    /// <summary>
    /// Gets the currently configured idle behavior for a specific analog output channel.
    /// </summary>
    /// <param name="hdwf">Device handle.</param>
    /// <param name="idxChannel">The zero-based index of the analog output channel.</param>
    /// <param name="pidle">When this method returns, contains the current idle behavior of the channel (see <c>DwfAnalogOutIdle*</c> constants).</param>
    /// <returns>Returns 1 (TRUE) if the idle behavior was retrieved successfully, 0 (FALSE) otherwise.</returns>
    [DllImport("dwf", EntryPoint = "FDwfAnalogOutIdleGet", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern int FDwfAnalogOutIdleGet(int hdwf, int idxChannel, out int pidle);
