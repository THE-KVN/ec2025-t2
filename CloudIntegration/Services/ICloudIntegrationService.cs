using System;
using System.Threading.Tasks;
using Zooscape.Infrastructure.CloudIntegration.Enums;

namespace Zooscape.Infrastructure.CloudIntegration.Services;

public interface ICloudIntegrationService
{
    Task Announce(
        CloudCallbackType callbackType,
        Exception? e = null,
        int? seed = null,
        int? ticks = null
    );

    void AddPlayer(string playerId);
    void UpdatePlayer(
        string playerId,
        int? finalScore = null,
        int? matchPoints = null,
        int? placement = null
    );
}
