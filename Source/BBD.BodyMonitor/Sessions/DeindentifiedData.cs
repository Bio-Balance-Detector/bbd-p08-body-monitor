using System.Text;

namespace BBD.BodyMonitor.Sessions
{
    /// <summary>
    /// Base class for data entities that require a de-identified alias. Generates a unique ID and a CRC32-based alias.
    /// </summary>
    public class DeidentifiedData
    {
        /// <summary>
        /// Gets or sets the unique identifier (GUID) for the data entity.
        /// </summary>
        public Guid Id { get; set; }
        /// <summary>
        /// Gets or sets the de-identified alias, generated as a CRC32 hash of the Id.
        /// </summary>
        public string Alias { get; set; }
        /// <summary>
        /// Gets or sets the human-readable name for the data entity.
        /// </summary>
        public string Name { get; set; }

        public DeidentifiedData()
        {
            Guid newGuid = Guid.NewGuid();

            // Generate a unique alias using CRC32 hash of the GUID string for de-identification purposes.
            Nito.HashAlgorithms.CRC32.Definition definition = new()
            {
                Initializer = 0xFFFFFFFF,
                TruncatedPolynomial = 0x04C11DB7,
                FinalXorValue = 0x00000000,
                ReverseResultBeforeFinalXor = true,
                ReverseDataBytes = true
            };

            byte[] crc32 = new Nito.HashAlgorithms.CRC32(definition).ComputeHash(Encoding.UTF8.GetBytes(newGuid.ToString()));

            Id = newGuid;
            Alias = "0x" + Convert.ToHexString(crc32);
        }
    }
}
