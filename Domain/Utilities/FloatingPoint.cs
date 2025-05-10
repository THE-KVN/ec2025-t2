using System;

namespace Zooscape.Domain.Utilities;

public class FloatingPoint
{
    private const double Epsilon = 1e-10;

    /// <summary>
    /// Calculates whether two double values are roughly equal within a given tolerance, epsilon.
    /// This is to help avoid floating point precision errors.
    /// </summary>
    /// <param name="a">First value for comparison</param>
    /// <param name="b">Second value for comparison</param>
    /// <param name="epsilon">Acceptable margin of error</param>
    /// <returns>True if a equals b within specified margin of error</returns>
    public static bool RoughlyEqual(double a, double b, double epsilon = Epsilon)
    {
        return Math.Abs(a - b) < epsilon;
    }
}
