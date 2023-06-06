using System.ComponentModel;

namespace BBD.BodyMonitor.GUI.Models
{
    public enum OrderType
    {
        [Description("Dine In")]
        DineIn,
        [Description("Carry Out")]
        CarryOut,
        [Description("Delivery")]
        Delivery
    }
}

