namespace BBD.BodyMonitor.Environment
{
    /// <summary>
    /// Represents a connected data acquisition device.
    /// </summary>
    public class ConnectedDevice
    {
        /// <summary>
        /// Gets or sets the brand of the device.
        /// </summary>
        public required string Brand { get; set; }
        /// <summary>
        /// Gets or sets the library or SDK used to interface with the device.
        /// </summary>
        public required string Library { get; set; }
        /// <summary>
        /// Gets or sets the index of the device if multiple similar devices are connected.
        /// </summary>
        public int Index { get; set; }
        /// <summary>
        /// Gets or sets the unique identifier of the device.
        /// </summary>
        public int Id { get; set; }
        /// <summary>
        /// Gets or sets the revision number or version of the device hardware/firmware.
        /// </summary>
        public int Revision { get; set; }
        /// <summary>
        /// Gets or sets the name of the device.
        /// </summary>
        public required string Name { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether the device is currently opened or in use.
        /// </summary>
        public bool IsOpened { get; set; }
        /// <summary>
        /// Gets or sets the user-defined name or alias for the device.
        /// </summary>
        public required string UserName { get; set; }
        /// <summary>
        /// Gets or sets the serial number of the device.
        /// </summary>
        public required string SerialNumber { get; set; }
        /// <summary>
        /// Gets or sets the GUID of the last session associated with this device.
        /// </summary>
        public Guid? LastSessionId { get; set; }
    }
}
