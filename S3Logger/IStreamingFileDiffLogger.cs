namespace Zooscape.Infrastructure.S3Logger;

public interface IStreamingFileDiffLogger
{
    void LogState(object state);

    void CloseAndFlush();
}
