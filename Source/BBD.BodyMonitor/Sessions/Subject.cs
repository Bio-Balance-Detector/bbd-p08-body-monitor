namespace BBD.BodyMonitor.Sessions
{
    /// <summary>
    /// Represents a monitored subject with de-identification and various personal and medical details. Inherits from DeidentifiedData.
    /// </summary>
    public class Subject : DeidentifiedData
    {
        /// <summary>
        /// Gets or sets the gender of the subject.
        /// </summary>
        public string? Gender { get; set; }
        /// <summary>
        /// Gets or sets the birthdate of the subject.
        /// </summary>
        public DateTime? Birthdate { get; set; }
        /// <summary>
        /// Gets or sets the weight of the subject in kilograms.
        /// </summary>
        public float? Weight { get; set; }
        /// <summary>
        /// Gets or sets the height of the subject in centimeters.
        /// </summary>
        public float? Height { get; set; }
        /// <summary>
        /// Gets or sets the Fitbit user ID (encoded) associated with the subject.
        /// </summary>
        public string? FitbitEncodedID { get; set; }
        /// <summary>
        /// Gets or sets the ThingSpeak channel ID associated with the subject.
        /// </summary>
        public string? ThingSpeakChannel { get; set; }
        /// <summary>
        /// Gets or sets an array of medical conditions or relevant notes for the subject.
        /// </summary>
        public string[]? Conditions { get; set; }
        /// <summary>
        /// Gets or sets the personal identity information for the subject.
        /// </summary>
        public Identity? Identity { get; set; }

        /// <summary>
        /// Returns a string representation of the subject.
        /// </summary>
        /// <returns>A string in the format 'Subject Alias: 'Name''.</returns>
        public override string ToString()
        {
            return $"Subject {Alias}: '{Name}'";
        }
    }
}