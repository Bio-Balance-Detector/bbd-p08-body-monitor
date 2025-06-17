using Xunit;
using BBD.BodyMonitor;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;

namespace BBD.BodyMonitor.Tests
{
    public class FftDataV3Tests
    {
        private FftDataV3 CreateTestData(float[]? magnitudes = null, float freqStep = 1.0f, float firstFreq = 0.0f, string name = "test", DateTimeOffset? startTime = null, DateTimeOffset? endTime = null)
        {
            magnitudes ??= new float[] { 1.0f, 2.0f, 3.0f, 4.0f, 5.0f };

            var data = new FftDataV3
            {
                Name = name,
                BasedOnSamplesCount = magnitudes.Length * 2,
                FrequencyStep = freqStep,
                FirstFrequency = firstFreq,
                Start = startTime ?? DateTimeOffset.UtcNow.AddSeconds(-10),
                End = endTime ?? DateTimeOffset.UtcNow
            };
            data.MagnitudeData = (float[])magnitudes.Clone();
            return data;
        }

        [Fact]
        public void Constructor_Default_InitializesProperties()
        {
            var data = new FftDataV3();
            Assert.Equal(string.Empty, data.Name);
            Assert.NotNull(data.MagnitudeData);
            Assert.Empty(data.MagnitudeData);
            Assert.Equal(0, data.BasedOnSamplesCount);
            Assert.Equal(0, data.FrequencyStep); // Default float is 0
            Assert.Equal(0, data.FirstFrequency); // Default float is 0
            Assert.Equal(0, data.LastFrequency);
            Assert.Equal(0, data.FftSize);
            Assert.Empty(data.AppliedFilters);
            Assert.Equal(string.Empty, data.MLProfileName);
        }

        [Fact]
        public void Constructor_Copy_CopiesRelevantProperties()
        {
            var originalMagnitudes = new float[] { 1, 2, 3, 4, 5, 6, 7, 8 };
            var original = CreateTestData(originalMagnitudes, 1.0f, 0.0f, "OriginalData");
            original.Tags = new[] { "tag1", "tag2" };

            original.ApplyMedianFilter();

            var dummyProfile = new MLProfile {
                Name = "CopiedProfile",
                MinFrequency = original.FirstFrequency,
                MaxFrequency = original.LastFrequency,
                FrequencyStep = original.FrequencyStep
            };
            var profiledVersion = original.ApplyMLProfile(dummyProfile);

            var copy = new FftDataV3(profiledVersion);

            Assert.Equal(profiledVersion.Name, copy.Name);
            Assert.Equal(profiledVersion.FrequencyStep, copy.FrequencyStep, 5);
            Assert.Equal(profiledVersion.FirstFrequency, copy.FirstFrequency, 5);
            Assert.Equal(profiledVersion.LastFrequency, copy.LastFrequency, 5);
            Assert.Equal(profiledVersion.FftSize, copy.FftSize); // FftSize copied from profiledVersion
            Assert.Equal(profiledVersion.BasedOnSamplesCount, copy.BasedOnSamplesCount);
            Assert.Equal(profiledVersion.Start, copy.Start);
            Assert.Equal(profiledVersion.End, copy.End);
            Assert.Equal(dummyProfile.Name, copy.MLProfileName);
            Assert.Equal(profiledVersion.Tags, copy.Tags);
            Assert.Equal(profiledVersion.AppliedFilters, copy.AppliedFilters);

            Assert.NotNull(copy.MagnitudeData);
            Assert.Empty(copy.MagnitudeData); // MagnitudeData is not copied by FftDataV3(FftDataV3 other) constructor
                                              // FftSize is copied, but actual data array is not. Getter returns empty.
                                              // The FftSize of copy should reflect that its _magnitudeData is null.
                                              // This requires copy constructor to initialize _magnitudeData to null.
                                              // And FftSize should be 0 if _magnitudeData is null.
                                              // The current FftDataV3(FftDataV3 other) copies FftSize.
                                              // If _magnitudeData is not copied, then FftSize of copy should be 0.
                                              // This is a slight inconsistency. Let's assume FftSize reflects the source for now.
                                              // The test should be: Assert.Equal(profiledVersion.FftSize, copy.FftSize); (which is already there)
                                              // And if copy.MagnitudeData (the getter) returns empty, then copy.FftSize should ideally be 0
                                              // if the getter also reset FftSize, but it doesn't.
                                              // So, the most accurate test for current FftDataV3 copy ctor:
            Assert.Equal(profiledVersion.FftSize, copy.FftSize); // FftSize property is copied
            // Assert.Equal(0, copy.FftSize); // This would be true if FftSize was derived from _magnitudeData being null in copy
        }

        [Fact]
        public void Downsample_ValidFrequencyStep_DownsamplesCorrectly()
        {
            var data = CreateTestData(new float[] { 1, 2, 3, 4, 5, 6, 7, 8 }, 1.0f, 0.0f);
            float newFreqStep = 2.0f;
            var downsampledData = data.Downsample(newFreqStep);

            var expectedMagnitudes = new float[] { 1.5f, 3.5f, 5.5f, 7.5f };
            Assert.Equal(expectedMagnitudes.Length, downsampledData.FftSize);
             for(int i=0; i < expectedMagnitudes.Length; i++)
            {
                Assert.InRange(downsampledData.MagnitudeData[i], expectedMagnitudes[i] - 0.0001f, expectedMagnitudes[i] + 0.0001f);
            }
            Assert.Equal(newFreqStep, downsampledData.FrequencyStep, 5);
            Assert.Equal(0.0f, downsampledData.FirstFrequency, 5);
            Assert.Equal(downsampledData.FirstFrequency + (expectedMagnitudes.Length - 1) * newFreqStep, downsampledData.LastFrequency, 5);
            Assert.Contains($"BBD.Downsample({newFreqStep.ToString(CultureInfo.InvariantCulture)})", downsampledData.AppliedFilters);
        }

        [Fact]
        public void Downsample_FirstFrequencyNotZero_ThrowsException()
        {
            var data = CreateTestData(new float[] { 1, 2, 3, 4 }, 1.0f, 10.0f);
            var ex = Assert.Throws<Exception>(() => data.Downsample(2.0f));
            Assert.Contains("FirstFrequency must be 0", ex.Message);
        }

        [Theory]
        [InlineData(1.0f)]
        [InlineData(0.5f)]
        public void Downsample_NewStepNotGreaterThanCurrent_ThrowsException(float newFreqStep)
        {
            var data = CreateTestData(new float[] { 1, 2, 3, 4 }, 1.0f, 0.0f);
            var ex = Assert.Throws<Exception>(() => data.Downsample(newFreqStep));
            Assert.Contains("Target frequencyStep must be greater than current FrequencyStep", ex.Message);
        }

        [Fact]
        public void ApplyMLProfile_DownsampleAndWindow_AppliesCorrectly()
        {
            var dataMagnitudes = Enumerable.Range(0,8).Select(i=>(float)i).ToArray();
            var data = CreateTestData(dataMagnitudes, 1.0f, 0.0f);
            var profile = new MLProfile
            {
                Name = "TestProfile", MinFrequency = 2.0f, MaxFrequency = 5.0f, FrequencyStep = 2.0f,
            };

            var result = data.ApplyMLProfile(profile);
            var expectedMagnitudes = new float[] { 2.5f, 4.5f };

            Assert.Equal(expectedMagnitudes.Length, result.FftSize);
            for(int i=0; i < expectedMagnitudes.Length; i++)
            {
                Assert.InRange(result.MagnitudeData[i], expectedMagnitudes[i] - 0.0001f, expectedMagnitudes[i] + 0.0001f);
            }
            Assert.Equal(profile.FrequencyStep, result.FrequencyStep, 5);
            Assert.Equal(2.0f, result.FirstFrequency, 5);
            Assert.Equal(2.0f + (2-1)*2.0f, result.LastFrequency, 5); // Expected LastFreq for [2.5, 4.5] with FirstF=2, Step=2 is 2+(2-1)*2 = 4
            Assert.Equal(profile.Name, result.MLProfileName);
            Assert.Contains($"BBD.Downsample({profile.FrequencyStep.ToString(CultureInfo.InvariantCulture)})", result.AppliedFilters);
        }

        [Fact]
        public void ApplyMLProfile_WindowOnly_AppliesCorrectly()
        {
            var data = CreateTestData(Enumerable.Range(0,8).Select(i=>(float)i).ToArray(), 1.0f, 0.0f);
            var profile = new MLProfile
            {
                Name = "WindowProfile", MinFrequency = 2.0f, MaxFrequency = 5.0f, FrequencyStep = 1.0f,
            };

            var result = data.ApplyMLProfile(profile);
            var expectedMagnitudes = new float[] { 2f,3f,4f,5f }; // Indices 2,3,4,5
            Assert.Equal(expectedMagnitudes.Length, result.FftSize);
             for(int i=0; i < expectedMagnitudes.Length; i++)
            {
                Assert.InRange(result.MagnitudeData[i], expectedMagnitudes[i] - 0.0001f, expectedMagnitudes[i] + 0.0001f);
            }
            Assert.Equal(1.0f, result.FrequencyStep, 5);
            Assert.Equal(2.0f, result.FirstFrequency, 5); // First element is 2 (at index 2), so FirstFreq is 2.0
            Assert.Equal(5.0f, result.LastFrequency, 5); // Last element is 5 (at index 5), LastFreq is 5.0
            Assert.Equal(profile.Name, result.MLProfileName);
            Assert.DoesNotContain("BBD.Downsample", string.Join(";", result.AppliedFilters));
        }

        [Fact]
        public void ApplyMLProfile_DataFirstFreqGreaterThanProfileMinFreq_ThrowsException()
        {
            var data = CreateTestData(new float[10], 1.0f, 10.0f);
            var profile = new MLProfile { Name="P1", MinFrequency = 5.0f, MaxFrequency = 15.0f, FrequencyStep = 1.0f };
            var ex = Assert.Throws<Exception>(() => data.ApplyMLProfile(profile));
            Assert.Contains($"The dataset's effective FirstFrequency ({data.FirstFrequency}Hz) is higher than the ML profile's MinFrequency ({profile.MinFrequency}Hz)", ex.Message);
        }

        [Fact]
        public void ApplyMLProfile_DataLastFreqLessThanProfileMaxFreq_ThrowsException()
        {
            var data = CreateTestData(new float[10], 1.0f, 0.0f);
            var profile = new MLProfile { Name="P2", MinFrequency = 0.0f, MaxFrequency = 15.0f, FrequencyStep = 1.0f };
            var ex = Assert.Throws<Exception>(() => data.ApplyMLProfile(profile));
            Assert.Contains($"The dataset's effective LastFrequency ({data.LastFrequency}Hz) is lower than the MaxFrequency ({profile.MaxFrequency}Hz)", ex.Message);
        }

        [Fact]
        public void GetMagnitudeStats_CalculatesCorrectly()
        {
            var data = CreateTestData(new float[] { 5, 1, 3, 2, 4 });
            var stats = data.GetMagnitudeStats();

            Assert.Equal(1f, stats.Min); Assert.Equal(1, stats.MinIndex);
            Assert.Equal(5f, stats.Max); Assert.Equal(0, stats.MaxIndex);
            Assert.Equal(3f, stats.Average);
            Assert.Equal(3f, stats.Median);
        }

        [Fact]
        public void ApplyMedianFilter_AppliesCorrectly()
        {
            var data = CreateTestData(new float[] { 10, 20, 30, 40, 50 });
            float median = 30f;
            data.ApplyMedianFilter();

            var expectedMagnitudes = new float[] { 10/median, 20/median, 30/median, 40/median, 50/median };
            for(int i=0; i < expectedMagnitudes.Length; i++)
            {
                Assert.InRange(data.MagnitudeData[i], expectedMagnitudes[i] - 0.0001f, expectedMagnitudes[i] + 0.0001f);
            }
            Assert.Contains("BBD.Median()", data.AppliedFilters);
        }

        [Theory]
        [InlineData(0.5)]
        [InlineData(2.0)]
        public void ApplyCompressorFilter_AppliesCorrectly(double power)
        {
            var data = CreateTestData(new float[] { 1, 2, 3, 4 });
            data.ApplyCompressorFilter(power);

            var expected = new float[4];
            for(int i=0; i<4; i++) expected[i] = (float)Math.Pow(i+1, power);

            for(int i=0; i<4; i++) Assert.InRange(data.MagnitudeData[i], expected[i] - 0.001f, expected[i] + 0.001f);
            Assert.Contains($"BBD.Compressor({power.ToString(CultureInfo.InvariantCulture)})", data.AppliedFilters);
        }

        [Theory]
        [InlineData(0.0)]
        [InlineData(-1.0)]
        public void ApplyCompressorFilter_InvalidPower_ThrowsException(double power)
        {
            var data = CreateTestData(new float[] { 1, 2, 3 });
            Assert.Throws<Exception>(() => data.ApplyCompressorFilter(power));
        }

        [Fact]
        public void ClearData_ClearsMagnitudeDataAndResetsProperties()
        {
            var data = CreateTestData(new float[] { 1, 2, 3 }, firstFreq: 5.0f);
            data.ClearData();
            Assert.NotNull(data.MagnitudeData);
            Assert.Empty(data.MagnitudeData);
            Assert.Equal(0, data.FftSize);
            Assert.Equal(data.FirstFrequency, data.LastFrequency); // After clear, Last = First
        }

        [Fact]
        public void GetFFTRange_ReturnsCorrectString()
        {
            var data = CreateTestData(new float[10], freqStep: 0.5f, firstFreq: 10.0f);
            string expected = "10p00-14p50Hz (0p50Hz/10)"; // .Replace('.', 'p') applied
            Assert.Equal(expected, data.GetFFTRange());
        }
    }
}