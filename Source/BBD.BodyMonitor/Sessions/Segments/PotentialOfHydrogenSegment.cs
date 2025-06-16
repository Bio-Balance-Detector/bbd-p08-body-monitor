namespace BBD.BodyMonitor.Sessions.Segments
{
    /// <summary>
    /// In chemistry, pH, historically denoting "potential of hydrogen" (or "power of hydrogen"), is a scale used to specify the acidity or basicity of an aqueous solution. Acidic solutions (solutions with higher concentrations of H+ ions) are measured to have lower pH values than basic or alkaline solutions.
    /// </summary>
    public class PotentialOfHydrogenSegment : Segment
    {
        /// <summary>
        /// pH value
        /// </summary>
        public float PotentialOfHydrogen { get; set; }
        /// <summary>
        /// Returns a string representation of the pH segment, including the pH value and start time.
        /// </summary>
        /// <returns>A string summarizing the pH data.</returns>
        public override string ToString()
        {
            return $"{base.ToString()}: pH is {PotentialOfHydrogen} at {Start}";
        }
    }
}
