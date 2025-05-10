using System.IO.Compression;
using Serilog;
using Serilog.Sinks.File.GZip;

namespace Zooscape.Infrastructure.S3Logger;

public class StreamingFileDiffLogger : IStreamingFileDiffLogger
{
    private readonly ILogger _logger;
    private readonly bool _enabled;

    public StreamingFileDiffLogger(bool enabled, string logDir, string logFileName)
    {
        _enabled = enabled;

        if (!_enabled)
            return;

        string logFilePath = Path.Combine(logDir, $"{logFileName}.gz");
        Log.Information($"Logging output to: {logFilePath}");

        _logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.File(
                logFilePath,
                outputTemplate: "{Message:lj}{NewLine}",
                hooks: new GZipHooks(compressionLevel: CompressionLevel.Optimal)
            )
            .CreateLogger();
    }

    public void LogState(object state)
    {
        if (!_enabled)
            return;

        _logger.Information("{@state}", state);
    }

    public void CloseAndFlush()
    {
        if (!_enabled)
            return;

        ((IDisposable)_logger).Dispose();
    }
}
