using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BBD.BodyMonitor.EDF
{
    /// <summary>
    /// European Data Format 'plus' (EDF+), an EDF alike standard format for the exchange of physiological data.
    /// </summary>
    public class EDFFile
    {
        public string Version { get; set; } = "0";
        public string PatientID { get; set; }
        public string RecordID { get; set; }
        public string StartDate { get; set; }
        public string StartTime { get; set; }
        public int HeaderLength { get; set; }
        public int NumberOfRecords { get; set; }
        public int DurationOfRecords { get; set; }
        public int NumberOfSignals { get; set; }
        public List<Signal> Signals { get; set; }
        public List<Annotation> Annotations { get; set; }

        public EDFFile()
        {
            Signals = new List<Signal>();
            Annotations = new List<Annotation>();
        }

        public static EDFFile FromStream(Stream edfStream)
        {
            EDFFile result = new EDFFile();
            try
            {
                using (var reader = new BinaryReader(edfStream))
                {
                    // Read the header of the EDF file
                    result.Version = new string(reader.ReadChars(8));
                    result.PatientID = new string(reader.ReadChars(80)).Trim();
                    result.RecordID = new string(reader.ReadChars(80)).Trim();
                    result.StartDate = new string(reader.ReadChars(8)).Trim();
                    result.StartTime = new string(reader.ReadChars(8)).Trim();
                    result.HeaderLength = reader.ReadInt32();
                    result.NumberOfRecords = reader.ReadInt32();
                    result.DurationOfRecords = reader.ReadInt32();
                    result.NumberOfSignals = reader.ReadInt32();

                    // Read the signal information and data
                    for (int i = 0; i < result.NumberOfSignals; i++)
                    {
                        Signal signal = new Signal();

                        signal.Label = new string(reader.ReadChars(16)).Trim();
                        signal.TransducerType = new string(reader.ReadChars(80)).Trim();
                        signal.PhysicalDimension = new string(reader.ReadChars(8)).Trim();
                        signal.PhysicalMinimum = reader.ReadSingle();
                        signal.PhysicalMaximum = reader.ReadSingle();
                        signal.DigitalMinimum = reader.ReadSingle();
                        signal.DigitalMaximum = reader.ReadSingle();
                        signal.NumberOfSamples = reader.ReadInt32();
                        signal.Reserved = new string(reader.ReadChars(32)).Trim();

                        // Read the signal data
                        float[] signalData = new float[result.NumberOfRecords * signal.NumberOfSamples];
                        for (int j = 0; j < signalData.Length; j++)
                        {
                            signalData[j] = reader.ReadSingle();
                        }

                        result.Signals.Add(signal);
                    }

                    // Read the annotations
                    int annotationLength = result.HeaderLength - (256 + (result.NumberOfSignals * 256));
                    if (annotationLength > 0)
                    {
                        string annotationData = new string(reader.ReadChars(annotationLength));
                        string[] annotations = annotationData.Split('\0');
                        foreach (string annotation in annotations)
                        {
                            if (annotation.Length > 0)
                            {
                                string[] annotationParts = annotation.Split(' ');
                                if (annotationParts.Length > 1)
                                {
                                    Annotation newAnnotation = new Annotation();
                                    newAnnotation.TimeStamp = double.Parse(annotationParts[0]);
                                    newAnnotation.Description = annotationParts[1];
                                    result.Annotations.Add(newAnnotation);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred: " + ex.Message);
            }
            return result;
        }

        public void ToStream(Stream edfStream)
        {
            using (var writer = new BinaryWriter(edfStream))
            {
                // Write the header of the EDF file
                writer.Write(Version.ToCharArray());
                writer.Write(PatientID.ToCharArray());
                writer.Write(RecordID.ToCharArray());
                writer.Write(StartDate.ToCharArray());
                writer.Write(StartTime.ToCharArray());
                writer.Write(HeaderLength);
                writer.Write(NumberOfRecords);
                writer.Write(DurationOfRecords);
                writer.Write(NumberOfSignals);

                // Write the signal information and data
                foreach (Signal signal in Signals)
                {
                    writer.Write(signal.Label.ToCharArray());
                    writer.Write(signal.TransducerType.ToCharArray());
                    writer.Write(signal.PhysicalDimension.ToCharArray());
                    writer.Write(signal.PhysicalMinimum);
                    writer.Write(signal.PhysicalMaximum);
                    writer.Write(signal.DigitalMinimum);
                    writer.Write(signal.DigitalMaximum);
                    writer.Write(signal.NumberOfSamples);
                    writer.Write(signal.Reserved.ToCharArray());

                    // Write the signal data
                    foreach (float data in signal.Data)
                    {
                        writer.Write(data);
                    }
                }

                // Write the annotations
                foreach (Annotation annotation in Annotations)
                {
                    string annotationString = annotation.TimeStamp.ToString() + " " + annotation.Description;
                    writer.Write(annotationString.ToCharArray());
                }

                // Write the padding
                int paddingLength = HeaderLength - (256 + (NumberOfSignals * 256)) - (Annotations.Count * 2);
                if (paddingLength > 0)
                {
                    writer.Write(new char[paddingLength]);
                }
            }
        }
    }

    public class Signal
    {
        public string Label { get; set; }
        public string TransducerType { get; set; }
        public string PhysicalDimension { get; set; }
        public float PhysicalMinimum { get; set; }
        public float PhysicalMaximum { get; set; }
        public float DigitalMinimum { get; set; }
        public float DigitalMaximum { get; set; }
        public int NumberOfSamples { get; set; }
        public string Reserved { get; set; }
        public float[] Data { get; set; }
    }

    public class Annotation
    {
        public double TimeStamp { get; set; }
        public string Description { get; set; }
    }
}
