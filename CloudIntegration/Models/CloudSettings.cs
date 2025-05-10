using System;

namespace Zooscape.Infrastructure.CloudIntegration.Models;

public class CloudSettings
{
    private static string appEnvironment =>
        Environment.GetEnvironmentVariable("ENVIRONMENT") ?? "Development";

    public string? ApiUrl => IsCloud ? Environment.GetEnvironmentVariable("API_URL") : null;

    public string? ApiKey => IsCloud ? Environment.GetEnvironmentVariable("API_KEY") : null;

    public string? MatchId => IsCloud ? Environment.GetEnvironmentVariable("MATCH_ID") : null;

    public bool IsLocal =>
        appEnvironment.Equals("Development", StringComparison.InvariantCultureIgnoreCase);
    public bool IsProduction =>
        appEnvironment.Equals("Production", StringComparison.InvariantCultureIgnoreCase);
    public bool IsCloud => !IsLocal;
}
