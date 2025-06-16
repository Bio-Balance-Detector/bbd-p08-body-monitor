using System.Drawing;

namespace BBD.BodyMonitor
{
    /// <summary>
    /// Provides functionality to interpolate between two colors.
    /// </summary>
    /// <remarks>
    /// Based on an answer by user Jason:
    /// http://stackoverflow.com/questions/1236683/color-interpolation-between-3-colors-in-net
    /// </remarks>
    public class ColorInterpolator
    {
        private delegate byte ComponentSelector(Color color);
        private static readonly ComponentSelector _redSelector = color => color.R;
        private static readonly ComponentSelector _greenSelector = color => color.G;
        private static readonly ComponentSelector _blueSelector = color => color.B;

        /// <summary>
        /// Interpolates between two specified colors based on a lambda value.
        /// </summary>
        /// <param name="endPoint1">The starting color of the interpolation.</param>
        /// <param name="endPoint2">The ending color of the interpolation.</param>
        /// <param name="lambda">A value between 0.0 and 1.0 that represents the weight of the interpolation.
        /// A value of 0.0 will return <paramref name="endPoint1"/>,
        /// a value of 1.0 will return <paramref name="endPoint2"/>,
        /// and values in between will return a color on the gradient between them.</param>
        /// <returns>The interpolated <see cref="Color"/>.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if lambda is less than 0 or greater than 1.</exception>
        public static Color InterpolateBetween(
            Color endPoint1,
            Color endPoint2,
            double lambda)
        {
            if (lambda < 0 || lambda > 1)
            {
                throw new ArgumentOutOfRangeException(nameof(lambda), "Lambda must be between 0 and 1.");
            }
            Color color = Color.FromArgb(
                InterpolateComponent(endPoint1, endPoint2, lambda, _redSelector),
                InterpolateComponent(endPoint1, endPoint2, lambda, _greenSelector),
                InterpolateComponent(endPoint1, endPoint2, lambda, _blueSelector)
            );

            return color;
        }

        private static byte InterpolateComponent(
            Color endPoint1,
            Color endPoint2,
            double lambda,
            ComponentSelector selector)
        {
            return (byte)(selector(endPoint1)
                + (selector(endPoint2) - selector(endPoint1)) * lambda);
        }
    }
}
