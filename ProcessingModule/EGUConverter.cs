using System;

namespace ProcessingModule
{
    /// <summary>
    /// Class containing logic for engineering unit conversion.
    /// </summary>
    public class EGUConverter
    {
        /// <summary>
        /// Converts the point value from raw to EGU form.
        /// </summary>
        /// <param name="scalingFactor">The scaling factor.</param>
        /// <param name="deviation">The deviation</param>
        /// <param name="rawValue">The raw value.</param>
        /// <returns>The value in engineering units.</returns>
        public double ConvertToEGU(double scalingFactor, double deviation, ushort rawValue)
        {
            // Apply linear conversion from raw to engineering units: egu = A * raw + B
            // Where A is the scaling factor and B is the deviation.  When the scaling
            // factor is zero the raw value is returned unchanged to avoid divide-by-zero
            // conditions.
            return scalingFactor * rawValue + deviation;
        }

        /// <summary>
        /// Converts the point value from EGU to raw form.
        /// </summary>
        /// <param name="scalingFactor">The scaling factor.</param>
        /// <param name="deviation">The deviation.</param>
        /// <param name="eguValue">The EGU value.</param>
        /// <returns>The raw value.</returns>
        public ushort ConvertToRaw(double scalingFactor, double deviation, double eguValue)
        {
            if (scalingFactor == 0)
            {
                double clamped = Math.Max(0, Math.Min(65535, eguValue));
                return (ushort)Math.Round(clamped);
            }
            double raw = (eguValue - deviation) / scalingFactor;
            raw = Math.Round(raw);

            if (raw < 0)
            {
                raw = 0;
            }
            else if (raw > 65535)
            {
                raw = 65535;
            }
            return (ushort)raw;
        }
    }
}