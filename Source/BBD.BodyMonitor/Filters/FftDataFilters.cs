namespace BBD.BodyMonitor.Filters
{
    /// <summary>
    /// Collection of filters that can be applied to FFT data
    /// </summary>
    public static class FftDataFilters
    {
        /// <summary>
        /// Removes the 50Hz/60Hz components from the FFT data if present, in the range of 47-53Hz and 57-63Hz
        /// </summary>
        /// <param name="fftData"></param>
        /// <returns></returns>
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

            FftDataV3 result = new(fftData)
            {
                MagnitudeData = fftData.MagnitudeData.ToArray()
            };

            // get the index of 47-53 Hz
            int index47Hz = (int)((47 - result.FirstFrequency) / result.FrequencyStep);
            int index53Hz = (int)((53 - result.FirstFrequency) / result.FrequencyStep);

            // get the index of 54-63 Hz
            int index57Hz = (int)((57 - result.FirstFrequency) / result.FrequencyStep);
            int index63Hz = (int)((63 - result.FirstFrequency) / result.FrequencyStep);

            if (result.MagnitudeData.Length < index63Hz)
            {
                return result;
            }

            float avg50Hz = result.MagnitudeData[index47Hz..index53Hz].Average();
            float avg60Hz = result.MagnitudeData[index57Hz..index63Hz].Average();
            float avg = result.MagnitudeData[index47Hz..index63Hz].Average();

            if ((avg50Hz > avg * 0.9) || (avg60Hz > avg * 0.9))
            {
                // compare the averages of the two ranges
                if (avg50Hz > avg60Hz)
                {
                    // remove the 50 Hz peak by setting it to the minimum of the 47-53 Hz range
                    float min = result.MagnitudeData[index47Hz..index53Hz].Min();
                    for (int i = index47Hz; i < index53Hz; i++)
                    {
                        result.MagnitudeData[i] = min;
                    }
                }
                else
                {
                    // remove the 60 Hz peak by setting it to the minimum of the 57-63 Hz range
                    float min = result.MagnitudeData[index57Hz..index63Hz].Min();
                    for (int i = index57Hz; i < index63Hz; i++)
                    {
                        result.MagnitudeData[i] = min;
                    }
                }
            }

            _ = result.AppliedFilters.Append("RemoveNoiseFromTheMains");

            return result;
        }

        /// <summary>
        /// Transforms the FFT data to a relative scale (sum of all bins is 1)
        /// </summary>
        /// <param name="fftData"></param>
        /// <returns></returns>
        public static FftDataV3 MakeItRelative(this FftDataV3 fftData)
        {
            FftDataV3 result = new(fftData)
            {
                MagnitudeData = fftData.MagnitudeData.ToArray()
            };

            float fftTotal = result.MagnitudeData.Sum();
            for (int i = 0; i < result.MagnitudeData.Length; i++)
            {
                result.MagnitudeData[i] /= fftTotal;
            }

            _ = result.AppliedFilters.Append("MakeItRelative");

            return result;
        }
    }
}
