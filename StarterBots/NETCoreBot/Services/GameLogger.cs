using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NETCoreBot.Services
{
    using NETCoreBot.Models;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;

    public static class GameLogger
    {
        public static bool Enabled { get; set; } = true;
        public static bool BenchmarkEnabled { get; set; } = false;
        private static readonly List<(string Message, ConsoleColor Color)> _buffer = new();

        public static void LogInfo(string system, string message)
            => Log(system, message, ConsoleColor.White);

        public static void LogError(string system, string message)
            => Log(system, $"ERROR: {message}", ConsoleColor.Red);

        public static void LogWatch(string system, string message)
            => Log(system, message, ConsoleColor.Magenta);

        public static void LogBenchmark(string label, long ms)
        {
            if (!BenchmarkEnabled) return;
            Log("Benchmark", $"{label}: {ms} ms", ConsoleColor.Blue);
        }

        public static void LogCollection(string name,HashSet<(int,int)> cells)
        {
            var coords = cells.Select(n => $"({n.Item1},{n.Item2})");
            Log(name, $" " + string.Join(" -> ", coords), ConsoleColor.Green );
        }

        private static void Log(string system, string message, ConsoleColor color)
        {
            if (!Enabled) return;
            _buffer.Add(($"[{system}] {message}", color));
        }

        public static IDisposable Benchmark(string label)
        {
            return new BenchmarkBlock(label);
        }

        public static void Flush(int tick)
        {
            if (!Enabled || _buffer.Count == 0) return;

            Console.WriteLine($"\n------------------- Tick {tick} Log -------------------");
            foreach (var (line, color) in _buffer)
            {
                var previousColor = Console.ForegroundColor;
                Console.ForegroundColor = color;
                Console.WriteLine(line);
                Console.ForegroundColor = previousColor;
            }

            _buffer.Clear();
        }

        private class BenchmarkBlock : IDisposable
        {
            private readonly string _label;
            private readonly Stopwatch _sw;

            public BenchmarkBlock(string label)
            {
                _label = label;
                _sw = Stopwatch.StartNew();
            }

            public void Dispose()
            {
                _sw.Stop();
                LogBenchmark(_label, _sw.ElapsedMilliseconds);
            }
        }
    }

}
