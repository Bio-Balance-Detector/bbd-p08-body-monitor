namespace BBD.BodyMonitor.Sessions.Segments
{
    /// <summary>
    /// Represents a segment of data containing blood test results.
    /// </summary>
    public class BloodTestSegment : Segment
    {
        /// <summary>
        /// Gets or sets the C-reactive protein (CRP) level in mg/L. CRP is a protein produced in the liver in response to inflammation.
        /// </summary>
        public float CRP { get; set; }
        /// <summary>
        /// Gets or sets the total cholesterol level in mg/dL.
        /// </summary>
        public float Cholesterol { get; set; }
        /// <summary>
        /// Gets or sets the High-density lipoprotein (HDL) cholesterol level in mg/dL. Often referred to as "good" cholesterol.
        /// </summary>
        public float HDL { get; set; }
        /// <summary>
        /// Gets or sets the Low-density lipoprotein (LDL) cholesterol level in mg/dL. Often referred to as "bad" cholesterol.
        /// </summary>
        public float LDL { get; set; }
        /// <summary>
        /// Returns a string representation of the blood test segment, including CRP, Cholesterol, HDL, and LDL values.
        /// </summary>
        /// <returns>A string summarizing the blood test results.</returns>
        public override string ToString()
        {
            return $"{base.ToString()}: CRP={CRP:0.00}, Cholesterol={Cholesterol:0.00}, HDL={HDL:0.00}, LDL={LDL:0.00}";
        }
    }
}
