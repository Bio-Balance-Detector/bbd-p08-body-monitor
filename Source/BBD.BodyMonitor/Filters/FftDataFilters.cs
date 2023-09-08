namespace BBD.BodyMonitor.Filters
{
    public static class FftDataFilters
    {
        public static FftDataV3 RemoveNoiseFromTheMains(this FftDataV3 fftData)
        {
            if (fftData.FirstFrequency > 64)
            {
                return fftData;
            }

            if (fftData.LastFrequency < 46)
            {
                return fftData;
            }

            // get the index of 47-53 Hz
            int index47Hz = (int)((47 - fftData.FirstFrequency) / fftData.FrequencyStep);
            int index53Hz = (int)((53 - fftData.FirstFrequency) / fftData.FrequencyStep);

            // get the index of 54-63 Hz
            int index57Hz = (int)((57 - fftData.FirstFrequency) / fftData.FrequencyStep);
            int index63Hz = (int)((63 - fftData.FirstFrequency) / fftData.FrequencyStep);

            float avg50Hz = fftData.MagnitudeData[index47Hz..index53Hz].Average();
            float avg60Hz = fftData.MagnitudeData[index57Hz..index63Hz].Average();
            float avg = fftData.MagnitudeData[index47Hz..index63Hz].Average();

            if ((avg50Hz > avg * 0.9) || (avg60Hz > avg * 0.9))
            {
                // compare the averages of the two ranges
                if (avg50Hz > avg60Hz)
                {
                    // remove the 50 Hz peak by setting it to the minimum of the 47-53 Hz range
                    float min = fftData.MagnitudeData[index47Hz..index53Hz].Min();
                    for (int i = index47Hz; i < index53Hz; i++)
                    {
                        fftData.MagnitudeData[i] = min;
                    }
                }
                else
                {
                    // remove the 60 Hz peak by setting it to the minimum of the 57-63 Hz range
                    float min = fftData.MagnitudeData[index57Hz..index63Hz].Min();
                    for (int i = index57Hz; i < index63Hz; i++)
                    {
                        fftData.MagnitudeData[i] = min;
                    }
                }
            }

            return fftData;
        }

        public static FftDataV3 MakeItRelative(this FftDataV3 fftData)
        {
            float fftTotal = fftData.MagnitudeData.Sum();
            for (int i = 0; i < fftData.MagnitudeData.Length; i++)
            {
                fftData.MagnitudeData[i] /= fftTotal;
            }

            return fftData;
        }
    }
}
