using System;
using System.Collections.Generic;
using System.Linq;

namespace Zooscape.Domain.Utilities;

public static class Helpers
{
    private static readonly Random _random = new Random();
    private static List<string> _usedNames = [];
    private static Dictionary<string, List<TimeSpan>> _timespans = [];

    public static string GenerateRandomName()
    {
        // Define two lists of words
        string[] firstList = { "Big", "Lil'", "Stupid", "Stinky", "Fuzzy", "Squishy", "Phat" };
        string[] secondList = { "Ol'", "Red", "Rotten", "Wooden", "Russian", "Party" };
        string[] thirdList = { "Dog", "Bugger", "Robot", "Muppet", "Blob", "Crawler", "Snotter" };

        string newName = "";
        do
        {
            // Pick a random word from each list
            string firstWord = firstList[_random.Next(firstList.Length)];
            string secondWord = secondList[_random.Next(secondList.Length)];
            string thirdWord = thirdList[_random.Next(thirdList.Length)];

            // Combine the words to form a name
            newName = $"{firstWord} {secondWord} {thirdWord}";
        } while (_usedNames.Contains(newName));

        return newName;
    }

    public static T TrackExecutionTime<T>(string captureGroup, Func<T> func, out TimeSpan elapsed)
    {
        if (!_timespans.ContainsKey(captureGroup))
            _timespans.Add(captureGroup, []);

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        T result = func();
        stopwatch.Stop();
        elapsed = stopwatch.Elapsed;
        _timespans[captureGroup].Add(stopwatch.Elapsed);

        return result;
    }

    public static void TrackExecutionTime(string captureGroup, Action action, out TimeSpan elapsed)
    {
        if (!_timespans.ContainsKey(captureGroup))
            _timespans.Add(captureGroup, []);

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        action();
        stopwatch.Stop();
        elapsed = stopwatch.Elapsed;
        _timespans[captureGroup].Add(stopwatch.Elapsed);
    }

    public static Dictionary<string, (double Max, double Min, double Avg)> TrackedExecutionTimes()
    {
        Dictionary<string, (double, double, double)> retval = [];

        foreach (var (key, value) in _timespans)
            retval.Add(
                key,
                (
                    value.Max(x => x.TotalMilliseconds),
                    value.Min(x => x.TotalMilliseconds),
                    value.Average(x => x.TotalMilliseconds)
                )
            );

        return retval;
    }
}
