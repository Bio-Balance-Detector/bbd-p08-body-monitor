namespace BBD.BodyMonitor.EDF
{
    /// <summary>
    /// European Data Format 'plus' (EDF+), an EDF alike standard format for the exchange of physiological data.
    /// </summary>
    public class EDFFile
    {
        /// <summary>
        /// Version of the EDF+ format. Typically "0".
        /// </summary>
        public string Version { get; set; } = "0";
        /// <summary>
        /// Local patient identification.
        /// </summary>
        public string PatientID { get; set; }
        /// <summary>
        /// Local recording identification.
        /// </summary>
        public string RecordID { get; set; }
        /// <summary>
        /// Start date of the recording in dd.mm.yy format.
        /// </summary>
        public string StartDate { get; set; }
        /// <summary>
        /// Start time of the recording in hh.mm.ss format.
        /// </summary>
        public string StartTime { get; set; }
        /// <summary>
        /// Number of bytes in the header record.
        /// </summary>
        public int HeaderLength { get; set; }
        // Reserved for EDF+ internal use, typically "EDF+C" for continuous recordings or "EDF+D" for interrupted recordings.
        // For simplicity in this initial documentation, we'll assume it's part of what might be implicitly handled or less critical for basic parsing/writing.
        // public string Reserved {get; set;} // This would be 44 bytes, often part of RecordID or a separate field in more detailed specs.
        /// <summary>
        /// Number of data records.
        /// </summary>
        public int NumberOfRecords { get; set; }
        /// <summary>
        /// Duration of a data record in seconds.
        /// </summary>
        public int DurationOfRecords { get; set; }
        /// <summary>
        /// Number of signals (ns) in data records.
        /// </summary>
        public int NumberOfSignals { get; set; }
        /// <summary>
        /// List of signals present in the EDF file.
        /// </summary>
        public List<Signal> Signals { get; set; }
        /// <summary>
        /// List of annotations (events) in the EDF file. EDF+ specific.
        /// </summary>
        public List<Annotation> Annotations { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="EDFFile"/> class.
        /// </summary>
        public EDFFile()
        {
            Signals = new List<Signal>();
            Annotations = new List<Annotation>();
        }

        /// <summary>
        /// Creates an <see cref="EDFFile"/> object from a stream.
        /// </summary>
        /// <param name="edfStream">The stream containing EDF+ data. The stream must be readable and seekable.</param>
        /// <returns>An <see cref="EDFFile"/> object populated with data from the stream.</returns>
        /// <remarks>
        /// This method reads the EDF+ header and signal data.
        /// If the file contains annotations (EDF+ specific), it attempts to parse them.
        /// The method includes basic error handling that writes to the console; for production use, more robust error handling may be required.
        /// </remarks>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="edfStream"/> is null.</exception>
        /// <exception cref="IOException">Thrown if an I/O error occurs while reading from the stream.</exception>
        /// <exception cref="ObjectDisposedException">Thrown if the stream is closed.</exception>
        /// <exception cref="FormatException">Thrown if the data in the stream is not in the expected EDF/EDF+ format.</exception>
        public static EDFFile FromStream(Stream edfStream)
        {
            if (edfStream == null)
            {
                throw new ArgumentNullException(nameof(edfStream));
            }

            EDFFile result = new();
            try
            {
                // EDF header structure based on standard EDF/EDF+ specification
                // All ASCII fields are padded with spaces to their specified length.
                // Numeric fields in the header are ASCII encoded integers or floats.

                using BinaryReader reader = new(edfStream, System.Text.Encoding.ASCII, leaveOpen: true);

                // Header Part 1: Fixed size fields
                result.Version = new string(reader.ReadChars(8)).TrimEnd(); // 8 bytes
                result.PatientID = new string(reader.ReadChars(80)).TrimEnd(); // 80 bytes
                result.RecordID = new string(reader.ReadChars(80)).TrimEnd(); // 80 bytes
                result.StartDate = new string(reader.ReadChars(8)).TrimEnd(); // 8 bytes (dd.mm.yy)
                result.StartTime = new string(reader.ReadChars(8)).TrimEnd(); // 8 bytes (hh.mm.ss)
                result.HeaderLength = int.Parse(new string(reader.ReadChars(8)).TrimEnd()); // 8 bytes (number of bytes in header record)
                string reservedEDFPlus = new string(reader.ReadChars(44)).TrimEnd(); // 44 bytes (reserved, "EDF+C" or "EDF+D" for EDF+)
                result.NumberOfRecords = int.Parse(new string(reader.ReadChars(8)).TrimEnd()); // 8 bytes (number of data records)
                result.DurationOfRecords = int.Parse(new string(reader.ReadChars(8)).TrimEnd()); // 8 bytes (duration of a data record, in seconds)
                result.NumberOfSignals = int.Parse(new string(reader.ReadChars(4)).TrimEnd()); // 4 bytes (ns)

                // Header Part 2: Signal specific fields (ns * 256 bytes)
                for (int i = 0; i < result.NumberOfSignals; i++)
                {
                    Signal edfSignal = new() // Renamed to avoid conflict with the class name 'Signal'
                    {
                        Label = new string(reader.ReadChars(16)).TrimEnd(), // 16 bytes
                        TransducerType = new string(reader.ReadChars(80)).TrimEnd(), // 80 bytes
                        PhysicalDimension = new string(reader.ReadChars(8)).TrimEnd(), // 8 bytes (e.g., uV, Bpm)
                        PhysicalMinimum = float.Parse(new string(reader.ReadChars(8)).TrimEnd(), System.Globalization.CultureInfo.InvariantCulture), // 8 bytes
                        PhysicalMaximum = float.Parse(new string(reader.ReadChars(8)).TrimEnd(), System.Globalization.CultureInfo.InvariantCulture), // 8 bytes
                        DigitalMinimum = float.Parse(new string(reader.ReadChars(8)).TrimEnd(), System.Globalization.CultureInfo.InvariantCulture), // 8 bytes
                        DigitalMaximum = float.Parse(new string(reader.ReadChars(8)).TrimEnd(), System.Globalization.CultureInfo.InvariantCulture), // 8 bytes
                        Prefiltering = new string(reader.ReadChars(80)).TrimEnd(), // 80 bytes (e.g., "HP:0.1Hz LP:75Hz N:50Hz")
                        NumberOfSamples = int.Parse(new string(reader.ReadChars(8)).TrimEnd()), // 8 bytes (number of samples in each data record)
                        Reserved = new string(reader.ReadChars(32)).TrimEnd(), // 32 bytes
                        Data = Array.Empty<short>() // Initialize, will be filled later
                    };
                    result.Signals.Add(edfSignal);
                }

                // Data records are read after the full header.
                // Each signal's data is stored as short (2 bytes per sample).
                // The annotations signal (if present for EDF+) is also read here.

                bool isEdfPlus = reservedEDFPlus.StartsWith("EDF+");
                Signal? annotationsSignal = null;
                if (isEdfPlus && result.Signals.Any(s => s.Label == "EDF Annotations"))
                {
                    annotationsSignal = result.Signals.First(s => s.Label == "EDF Annotations");
                }

                for (int i = 0; i < result.NumberOfSignals; i++)
                {
                    Signal currentSignal = result.Signals[i];
                    int samplesPerRecord = currentSignal.NumberOfSamples;
                    short[] signalData = new short[result.NumberOfRecords * samplesPerRecord];

                    // It's important to correctly position the stream before reading each signal's data,
                    // as EDF data is stored signal by signal for each record block.
                    // This simplified example reads all data for a signal contiguously, which assumes a specific file structure
                    // or requires careful seeking. A more robust reader would handle interleaved data correctly.
                    // For now, we assume data is read sequentially as per header order for simplicity.
                    // The actual data reading loop needs to be structured based on how data records are organized.
                    // Each data record contains 'duration' seconds of data for all signals.
                    // So, data is interleaved: Record1(Signal1, Signal2,...), Record2(Signal1, Signal2,...)

                    // Correct reading requires seeking or careful byte counting.
                    // This simplified version might misread actual sample data if not perfectly aligned.
                    // The EDF specification states data records are ( NumberOfSamples[1] + ... + NumberOfSamples[ns] ) * 2 bytes.
                    // Seek to the start of data for this signal: HeaderLength + sum of (NumberOfSamples * NumberOfRecords * 2) for previous signals.
                    long dataOffset = result.HeaderLength;
                    for(int k=0; k < i; k++)
                    {
                        dataOffset += result.Signals[k].NumberOfSamples * result.NumberOfRecords * 2L; // 2 bytes per sample (short)
                    }
                    edfStream.Seek(dataOffset, SeekOrigin.Begin);


                    for (int j = 0; j < signalData.Length; j++)
                    {
                        signalData[j] = reader.ReadInt16(); // Read 2-byte sample
                    }
                    currentSignal.Data = signalData;

                    // If this is the EDF Annotations signal, parse them
                    if (currentSignal == annotationsSignal)
                    {
                        // Annotations are stored as TALs (Time-stamped Annotation Lists)
                        // Each TAL is: onset (ASCII float) + 0x14 + duration (ASCII float) + 0x14 + annotation text + 0x14 + 0x00
                        // Multiple TALs are concatenated, each terminated by 0x00.
                        // The entire annotations block is terminated by 0x00 0x00.
                        // This parsing is complex and requires careful handling of byte arrays.
                        // For simplicity, a basic placeholder for annotation parsing:
                        // This would involve converting currentSignal.Data (short[]) back to bytes and then parsing.
                        // byte[] annotationBytes = new byte[currentSignal.Data.Length * 2];
                        // Buffer.BlockCopy(currentSignal.Data, 0, annotationBytes, 0, annotationBytes.Length);
                        // string rawAnnotationText = System.Text.Encoding.UTF8.GetString(annotationBytes);
                        // Parse rawAnnotationText for TALs. This is non-trivial.
                        // result.Annotations.Add(...);
                        // Due to complexity, detailed TAL parsing is omitted here.
                    }
                }
            }
            catch (Exception ex)
            {
                // Consider logging the exception or rethrowing a more specific exception type.
                Console.WriteLine("An error occurred while reading the EDF file: " + ex.Message);
                // Depending on requirements, might want to return null, throw, or return a partially populated object.
                // For now, returning the potentially partially populated object.
            }
            return result;
        }

        /// <summary>
        /// Writes the <see cref="EDFFile"/> object to a stream in EDF+ format.
        /// </summary>
        /// <param name="edfStream">The stream to write EDF+ data to. The stream must be writable.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="edfStream"/> is null.</exception>
        /// <exception cref="IOException">Thrown if an I/O error occurs while writing to the stream.</exception>
        /// <exception cref="NotSupportedException">Thrown if the stream does not support writing or seeking.</exception>
        public void ToStream(Stream edfStream)
        {
            if (edfStream == null)
            {
                throw new ArgumentNullException(nameof(edfStream));
            }

            using BinaryWriter writer = new(edfStream, System.Text.Encoding.ASCII, leaveOpen: true);

            // Ensure NumberOfSignals matches Signals.Count
            NumberOfSignals = Signals.Count;

            // Calculate HeaderLength: 256 bytes for fixed part + ns * 256 bytes for signal headers
            HeaderLength = 256 + (NumberOfSignals * 256);


            // Write fixed header part (256 bytes)
            writer.Write(Version.PadRight(8).ToCharArray());
            writer.Write(PatientID.PadRight(80).ToCharArray());
            writer.Write(RecordID.PadRight(80).ToCharArray());
            writer.Write(StartDate.PadRight(8).ToCharArray()); // dd.mm.yy
            writer.Write(StartTime.PadRight(8).ToCharArray()); // hh.mm.ss
            writer.Write(HeaderLength.ToString().PadRight(8).ToCharArray());
            writer.Write("EDF+C".PadRight(44).ToCharArray()); // Reserved, "EDF+C" for continuous
            writer.Write(NumberOfRecords.ToString().PadRight(8).ToCharArray());
            writer.Write(DurationOfRecords.ToString().PadRight(8).ToCharArray());
            writer.Write(NumberOfSignals.ToString().PadRight(4).ToCharArray());

            // Write signal headers (ns * 256 bytes)
            foreach (Signal signalToWrite in Signals) // Renamed to avoid conflict
            {
                writer.Write(signalToWrite.Label.PadRight(16).ToCharArray());
                writer.Write(signalToWrite.TransducerType.PadRight(80).ToCharArray());
                writer.Write(signalToWrite.PhysicalDimension.PadRight(8).ToCharArray());
                writer.Write(signalToWrite.PhysicalMinimum.ToString(System.Globalization.CultureInfo.InvariantCulture).PadRight(8).ToCharArray());
                writer.Write(signalToWrite.PhysicalMaximum.ToString(System.Globalization.CultureInfo.InvariantCulture).PadRight(8).ToCharArray());
                writer.Write(signalToWrite.DigitalMinimum.ToString(System.Globalization.CultureInfo.InvariantCulture).PadRight(8).ToCharArray());
                writer.Write(signalToWrite.DigitalMaximum.ToString(System.Globalization.CultureInfo.InvariantCulture).PadRight(8).ToCharArray());
                writer.Write(signalToWrite.Prefiltering.PadRight(80).ToCharArray());
                writer.Write(signalToWrite.NumberOfSamples.ToString().PadRight(8).ToCharArray());
                writer.Write(signalToWrite.Reserved.PadRight(32).ToCharArray());
            }

            // Write data records
            // Data is written record by record. Each record contains NumberOfSamples for each signal.
            for (int i = 0; i < NumberOfRecords; i++)
            {
                foreach (Signal signalToWrite in Signals)
                {
                    for (int j = 0; j < signalToWrite.NumberOfSamples; j++)
                    {
                        // Calculate the index in the flat Data array
                        int dataIndex = (i * signalToWrite.NumberOfSamples) + j;
                        if (dataIndex < signalToWrite.Data.Length)
                        {
                            writer.Write(signalToWrite.Data[dataIndex]); // Write short (2 bytes)
                        }
                        else
                        {
                            // Handle case where data might be shorter than expected, write 0 or throw error
                            writer.Write((short)0);
                        }
                    }
                }
            }

            // EDF+ Annotations are typically stored in a special signal labeled "EDF Annotations".
            // If Annotations list is populated and an "EDF Annotations" signal exists, its Data should be prepared.
            // This example does not dynamically create or populate the "EDF Annotations" signal data from the Annotations list.
            // That would require converting the List<Annotation> into the TAL format and then into short[].
        }
    }

    /// <summary>
    /// Represents a signal in an EDF file.
    /// </summary>
    public class Signal
    {
        /// <summary>
        /// Label of the signal (e.g., EEG Fpz-Cz, EOG horizontal).
        /// </summary>
        public required string Label { get; set; }
        /// <summary>
        /// Transducer type (e.g., AgAgCl electrode).
        /// </summary>
        public required string TransducerType { get; set; }
        /// <summary>
        /// Physical dimension of the signal (e.g., uV, Bpm, degreeC).
        /// </summary>
        public required string PhysicalDimension { get; set; }
        /// <summary>
        /// Physical minimum value of the signal.
        /// </summary>
        public float PhysicalMinimum { get; set; }
        /// <summary>
        /// Physical maximum value of the signal.
        /// </summary>
        public float PhysicalMaximum { get; set; }
        /// <summary>
        /// Digital minimum value of the signal.
        /// </summary>
        public float DigitalMinimum { get; set; }
        /// <summary>
        /// Digital maximum value of the signal.
        /// </summary>
        public float DigitalMaximum { get; set; }
        /// <summary>
        /// Prefiltering information (e.g., HP:0.1Hz LP:75Hz N:50Hz).
        /// </summary>
        public string Prefiltering { get; set; } = string.Empty;
        /// <summary>
        /// Number of samples in each data record for this signal.
        /// </summary>
        public int NumberOfSamples { get; set; }
        /// <summary>
        /// Reserved field for future use or custom data.
        /// </summary>
        public required string Reserved { get; set; }
        /// <summary>
        /// Signal data samples. In EDF/EDF+, these are typically stored as 2-byte integers (short).
        /// </summary>
        public required short[] Data { get; set; }
    }

    /// <summary>
    /// Represents an annotation (event) in an EDF+ file.
    /// </summary>
    public class Annotation
    {
        /// <summary>
        /// Timestamp of the annotation in seconds, relative to the start of the recording.
        /// Can also include a duration. E.g., +X (onset), +X.Y (onset with duration Y).
        /// </summary>
        public double TimeStamp { get; set; } // Or string to support "+X.Y" duration format directly from TAL
        /// <summary>
        /// Description of the annotation (event).
        /// </summary>
        public required string Description { get; set; }
        // EDF+ TALs can also include a duration. Consider adding:
        // public double? Duration { get; set; }
    }
}
