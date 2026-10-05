using System;

namespace PrescriptionLens.Editor
{
    // SI units throughout this class; independent of Unity Mesh/Vector APIs.
    public static class LensOpticsCalculator
    {
        public static void RequireFinite(params double[] values)
        {
            foreach (double x in values)
                if (double.IsNaN(x) || double.IsInfinity(x))
                    throw new ArgumentException("Lens parameters must be finite.");
        }

        public static double NormalizeAxis(double degrees)
        {
            RequireFinite(degrees);
            return ((degrees % 180) + 180) % 180;
        }

        public static double Power(double sphere, double cylinder, double angle, double axis)
        {
            double s = Math.Sin(angle - axis);
            return sphere + cylinder * s * s;
        }

        public static double BackCurvature(double power, double frontPower, double index, double thickness)
        {
            RequireFinite(power, frontPower, index, thickness);
            if (index <= 1 || thickness <= 0) throw new ArgumentException("Index must exceed 1 and thickness must be positive.");
            double denominator = (index - 1) * (1 - thickness * frontPower / index);
            if (denominator <= 1e-10) throw new ArgumentException("Invalid thick-lens denominator.");
            return (frontPower - power) / denominator;
        }

        public static double Sag(double curvature, double radius)
        {
            RequireFinite(curvature, radius);
            double cr = curvature * radius;
            if (Math.Abs(cr) >= 1) throw new ArgumentException("Requested lens geometry is invalid for the specified aperture.");
            return curvature * radius * radius / (1 + Math.Sqrt(1 - cr * cr));
        }

        public static double BackSag(double x, double y, double sphere, double cylinder, double axis,
            double frontPower, double index, double thickness)
        {
            double angle = Math.Atan2(y, x);
            return Sag(BackCurvature(Power(sphere, cylinder, angle, axis), frontPower, index, thickness),
                Math.Sqrt(x * x + y * y));
        }

        // Inverse image magnification: negative power samples farther from the center.
        public static double SampleScale(double power, double apparentDepth)
        {
            double scale = 1 - power * apparentDepth;
            if (scale <= 0) throw new ArgumentException("Apparent depth and power give a non-positive image sampling scale.");
            return scale;
        }
    }
}
