namespace EDFlib
{
    /// <summary>
    /// Annotation data structure for EDF+ files.
    /// </summary>
    public struct EDFAnnotation
    {
        /// <summary>
        /// The onset time of the annotation expressed in units of 100 nanoSeconds. Relative to the start of the recording.
        /// </summary>
        public long Onset;
        /// <summary>
        /// the duration of the annotation expressed in units of 100 nanoSeconds. If unknown or not applicable, set to -1.
        /// </summary>
        public long Duration;
        /// <summary>
        /// Free text that describes the annotation/event.
        /// </summary>
        public string? Description;
    }
}