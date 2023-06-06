using System.Text;

namespace BBD.BodyMonitor.Sessions
{
    public class DeidentifiedData
    {
        public Guid Id { get; set; }
        public string Alias { get; set; }
        public string Name { get; set; }

        public DeidentifiedData()
        {
            var newGuid = Guid.NewGuid();

            var definition = new Nito.HashAlgorithms.CRC32.Definition
            {
                Initializer = 0xFFFFFFFF,
                TruncatedPolynomial = 0x04C11DB7,
                FinalXorValue = 0x00000000,
                ReverseResultBeforeFinalXor = true,
                ReverseDataBytes = true
            };

            var crc32 = new Nito.HashAlgorithms.CRC32(definition).ComputeHash(Encoding.UTF8.GetBytes(newGuid.ToString()));

            Id = newGuid;
            Alias = "0x" + Convert.ToHexString(crc32);
        }
    }
}
