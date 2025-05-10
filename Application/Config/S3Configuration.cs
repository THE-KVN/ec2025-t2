using Microsoft.Extensions.Configuration;

namespace Zooscape.Application.Config;

public class S3Configuration
{
    public string PushLogsToS3 { get; set; }
}
