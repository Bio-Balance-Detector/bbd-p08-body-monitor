using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BBD.BodyMonitor.Environment
{
    public class ConnectedDevice
    {
        public string Brand { get; set; }
        public string Library { get; set; }
        public int Id { get; set; }
        public int Revision { get; set; }
        public string Name { get; set; }
        public bool IsOpened { get; set; }
        public string UserName { get; set; }
        public string SerialNumber { get; set; }
    }
}
