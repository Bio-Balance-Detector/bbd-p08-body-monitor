namespace BBD.BodyMonitor.Filters
{
    /// <summary>
    /// Provides extension methods for applying various filters to FftDataV3 objects.
    /// </summary>
    public static class FftDataFilters
    {
        /// <summary>
        /// Removes the 50Hz or 60Hz mains noise components from FFT data.
        /// It checks for noise in the 47-53Hz range (for 50Hz mains) and 57-63Hz range (for 60Hz mains).
        /// If significant noise is detected in one of these ranges (based on average magnitude compared to the combined range average),
        /// the magnitudes in that specific range are set to zero.
        /// </summary>
        /// <param name="fftData">The input FFT data.</param>
        /// <returns>A new <see cref="FftDataV3"/> object with the mains noise components removed and the "RemoveNoiseFromTheMains" filter logged in <see cref="FftDataV3.AppliedFilters"/>.</returns>
        public static FftDataV3 RemoveNoiseFromTheMains(this FftDataV3 fftData)
        {
            // If the FFT data doesn't cover the mains frequency ranges, return the original data.
            if (fftData.FirstFrequency > 64)
            {
                return fftData;
            }

            if (fftData.LastFrequency < 46) // Smallest frequency of interest is 47Hz
            {
                return fftData;
            }

            FftDataV3 result = new(fftData)
            {
                MagnitudeData = fftData.MagnitudeData.ToArray() // Create a copy to modify
            };

            // Calculate indices for the 50Hz range (47-53 Hz)
            // Ensure indices are within the bounds of the MagnitudeData array.
            int index47Hz = Math.Max(0, (int)((47 - result.FirstFrequency) / result.FrequencyStep));
            int index53Hz = Math.Min(result.MagnitudeData.Length - 1, (int)((53 - result.FirstFrequency) / result.FrequencyStep));

            // Calculate indices for the 60Hz range (57-63 Hz)
            int index57Hz = Math.Max(0, (int)((57 - result.FirstFrequency) / result.FrequencyStep));
            int index63Hz = Math.Min(result.MagnitudeData.Length - 1, (int)((63 - result.FirstFrequency) / result.FrequencyStep));

            // Ensure valid range for slicing and prevent issues if frequency step is too large or ranges are outside data
            if (index47Hz >= index53Hz && index57Hz >= index63Hz)
            {
                return result; // Not enough data points in the target ranges
            }

            // Calculate average magnitudes in the 50Hz and 60Hz bands, and the combined band
            // Ensure ranges are valid before attempting to slice/average
            float avg50Hz = (index47Hz < index53Hz) ? result.MagnitudeData[index47Hz..index53Hz].Average() : 0;
            float avg60Hz = (index57Hz < index63Hz) ? result.MagnitudeData[index57Hz..index63Hz].Average() : 0;

            // Use a combined range for overall average, ensuring it's valid
            int combinedStartIndex = Math.Min(index47Hz, index57Hz);
            int combinedEndIndex = Math.Max(index53Hz, index63Hz);
            float avgCombined = (combinedStartIndex < combinedEndIndex) ? result.MagnitudeData[combinedStartIndex..combinedEndIndex].Average() : 0;

            // If average of either 50Hz or 60Hz band is significantly higher (10% threshold) than the combined average,
            // it indicates dominant mains noise in that band.
            if ((avg50Hz > avgCombined * 1.1) || (avg60Hz > avgCombined * 1.1))
            {
                // Compare the averages of the two ranges to determine which mains frequency is dominant
                if (avg50Hz > avg60Hz)
                {
                    // Remove the 50 Hz peak by setting its range to 0.
                    // Original code commented out using Min() and used 0. Sticking to 0 for now.
                    // Consider using a local average or a value slightly below the noise floor if 0 is too aggressive.
                    if (index47Hz < index53Hz)
                    {
                        for (int i = index47Hz; i <= index53Hz; i++) // Inclusive of index53Hz
                        {
                            result.MagnitudeData[i] = 0;
                        }
                    }
                }
                else
                {
                    // Remove the 60 Hz peak by setting its range to 0.
                    if (index57Hz < index63Hz)
                    {
                        for (int i = index57Hz; i <= index63Hz; i++) // Inclusive of index63Hz
                        {
                            result.MagnitudeData[i] = 0;
                        }
                    }
                }
            }

            result.AddAppliedFilter("RemoveNoiseFromTheMains");

            return result;
        }

        /// <summary>
        /// Transforms the FFT data to a relative scale where the sum of all magnitude bins equals 1.
        /// This normalizes the FFT data, making it independent of the overall signal power.
        /// </summary>
        /// <param name="fftData">The input FFT data.</param>
        /// <returns>A new <see cref="FftDataV3"/> object with its magnitude data normalized, and the "MakeItRelative" filter logged in <see cref="FftDataV3.AppliedFilters"/>.</returns>
        public static FftDataV3 MakeItRelative(this FftDataV3 fftData)
        {
            FftDataV3 result = new(fftData)
            {
                MagnitudeData = fftData.MagnitudeData.ToArray() // Create a copy to modify
            };

            float fftTotal = result.MagnitudeData.Sum();
            if (fftTotal == 0) // Avoid division by zero if all magnitudes are zero
            {
                return result; // Or handle as an error/special case
            }

            for (int i = 0; i < result.MagnitudeData.Length; i++)
            {
                result.MagnitudeData[i] /= fftTotal;
            }

            result.AddAppliedFilter("MakeItRelative");

            return result;
        }
    }
}
