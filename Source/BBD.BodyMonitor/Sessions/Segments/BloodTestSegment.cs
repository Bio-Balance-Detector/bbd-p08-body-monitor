namespace BBD.BodyMonitor.Sessions.Segments
{
    public class BloodTestSegment : Segment
    {
        /// <summary>
        /// C-reactive protein (CRP) is a protein produced in the liver in response to inflammation.
        /// </summary>
        public float CRP { get; set; }
        public float Cholesterol { get; set; }
        /// <summary>
        /// High-density lipoprotein (HDL)
        /// </summary>
        public float HDL { get; set; }
        /// <summary>
        /// Low-density lipoprotein (LDL)
        /// </summary>
        public float LDL { get; set; }
        public override string ToString()
        {
            return $"{base.ToString()}: CRP={CRP:0.00}, Cholesterol={Cholesterol:0.00}, HDL={HDL:0.00}, LDL={LDL:0.00}";
        }
    }
}
