using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Zooscape.Domain.Utilities;

/// <summary>
/// A wrapper around the System.Random class allowing injection as a singleton and containing additional distributed
/// random number generating functions.
/// </summary>
public class GlobalSeededRandomizer
{
    private readonly Random _random;

    public int Seed;

    /// <summary>Initializes a new instance of the GlobalSeededRandomizer class, using the specified seed value.</summary>
    /// <param name="Seed">
    /// A number used to calculate a starting value for the pseudo-random number sequence. If a negative number
    /// is specified, the absolute value of the number is used.
    /// </param>
    public GlobalSeededRandomizer(int seed)
    {
        Seed = seed;
        _random = new Random(seed);
    }

    /// <summary>Returns a non-negative random integer.</summary>
    /// <returns>A 32-bit signed integer that is greater than or equal to 0 and less than <see cref="int.MaxValue"/>.</returns>
    public int Next()
    {
        return _random.Next();
    }

    /// <summary>Returns a random integer that is within a specified range.</summary>
    /// <param name="minValue">The inclusive lower bound of the random number returned.</param>
    /// <param name="maxValue">The exclusive upper bound of the random number returned. <paramref name="maxValue"/> must be greater than or equal to <paramref name="minValue"/>.</param>
    /// <returns>
    /// A 32-bit signed integer greater than or equal to <paramref name="minValue"/> and less than <paramref name="maxValue"/>; that is, the range of return values includes <paramref name="minValue"/>
    /// but not <paramref name="maxValue"/>. If minValue equals <paramref name="maxValue"/>, <paramref name="minValue"/> is returned.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="minValue"/> is greater than <paramref name="maxValue"/>.</exception>
    public int Next(int minValue, int maxValue)
    {
        return _random.Next(minValue, maxValue);
    }

    /// <summary>Returns a random floating-point number that is greater than or equal to 0.0, and less than 1.0.</summary>
    /// <returns>A double-precision floating point number that is greater than or equal to 0.0, and less than 1.0.</returns>
    public double NextDouble()
    {
        return _random.NextDouble();
    }

    /// <summary>
    /// Returns pseudo random number normally distributed around a specified mean, with a specified standard deviation,
    /// and clipped between a specified minimum and maximum values.
    /// </summary>
    /// <param name="mean">Mean of normal distribution</param>
    /// <param name="stdDev">Standard deviation of normal distribution</param>
    /// <param name="min">Minimum returned value</param>
    /// <param name="max">Maximum returned value</param>
    /// <returns>A double-precision floating point number that is greater than or equal to 0.0, and less than 1.0.</returns>
    public double NormalNextDouble(double mean, double stdDev, double min, double max)
    {
        double u1 = 1.0 - _random.NextDouble();
        double u2 = 1.0 - _random.NextDouble();
        double randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
        double randNormal = mean + stdDev * randStdNormal;

        // Clip the result between min and max
        return Math.Max(Math.Min(randNormal, max), min);
    }

    /// <summary>
    /// Randomly selects an option from a set of items, where each option has a weight associated with it.
    /// </summary>
    /// <param name="weightedValues">A dictionary where the keys are the options and the values are the weights.</param>
    /// <typeparam name="T">The type of value to select.</typeparam>
    /// <returns>The key of the randomly chosen item in the given dictionary.</returns>
    public T NextWeightedValue<T>(Dictionary<T, int> weightedValues)
        where T : notnull
    {
        List<KeyValuePair<T, int>> orderedWeightedValues = weightedValues
            .Where(kv => kv.Value > 0)
            .OrderBy(v => v.Value)
            .ToList();

        if (orderedWeightedValues.Count == 0)
            throw new ArgumentException("No options with weights > 0 have been provided");

        var total = orderedWeightedValues.Select(v => v.Value).Sum();

        var choice = Next(0, total);

        var cursor = 0;
        foreach (var kv in orderedWeightedValues)
        {
            cursor += kv.Value;
            if (cursor >= choice)
                return kv.Key;
        }
        return orderedWeightedValues[0].Key;
    }

    /// <summary>
    /// Gets a random element from the given enumerable.
    /// </summary>
    /// <param name="enumerable">The enumerable to select an element from.</param>
    /// <typeparam name="T">The type that the enumerable contains.</typeparam>
    /// <returns>A randomly selected value from the given enumerable.</returns>
    public T GetRandomElement<T>(IEnumerable<T> source)
    {
        // If source is a list, we can pick a random element very quickly
        if (source is IList<T> list)
        {
            return list[Next(0, list.Count)];
        }

        // If source has count but no indexing, we can pick a random element slightly slower
        if (source is ICollection<T> col)
        {
            int index = Next(0, col.Count);
            return source.Skip(index).First();
        }

        // If source is a true IEnumerable, use Reservoir Sampling
        using var enumerator = source.GetEnumerator();
        if (!enumerator.MoveNext())
            throw new InvalidOperationException("Sequence was empty");

        T result = enumerator.Current;
        int i = 1;
        while (enumerator.MoveNext())
        {
            i++;
            if (Next(0, i) == 0)
            {
                result = enumerator.Current;
            }
        }

        return result;
    }

    /// <summary>
    /// Gets a random element from the given enumerable, matching the conditions defined by the predicate function.
    /// </summary>
    /// <param name="enumerable">The enumerable to select an element from.</param>
    /// <param name="predicate">Predicate function to test validity of selected element.</param>
    /// <param name="enumerable">Timeout in milliseconds within which element has to be found.</param>
    /// <typeparam name="T">The type that the enumerable contains.</typeparam>
    /// <returns>A randomly selected value from the given enumerable matching the conditions defined by predicate.
    /// Null if no valid element could be found within the allocated time.</returns>
    public T? GetRandomElement<T>(IEnumerable<T> source, Func<T, bool> predicate, int timeout)
    {
        var stopwatch = new Stopwatch();
        stopwatch.Start();
        do
        {
            var retval = GetRandomElement(source);
            if (predicate(retval))
                return retval;
        } while (stopwatch.ElapsedMilliseconds < timeout);

        return default;
    }
}
