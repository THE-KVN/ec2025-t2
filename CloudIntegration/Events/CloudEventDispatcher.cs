using System;
using System.Threading.Tasks;
using Zooscape.Application.Events;
using Zooscape.Infrastructure.CloudIntegration.Enums;
using Zooscape.Infrastructure.CloudIntegration.Services;

namespace Zooscape.Infrastructure.CloudIntegration.Events;

public class CloudEventDispatcher(ICloudIntegrationService cloudIntegrationService)
    : IEventDispatcher
{
    public async Task Dispatch<TEvent>(TEvent gameEvent)
        where TEvent : class
    {
        if (gameEvent is CloudCallbackEvent cloudCallbackEvent)
        {
            CloudCallbackType cloudCallbackType = cloudCallbackEvent.Type switch
            {
                CloudCallbackEventType.Ready => CloudCallbackType.Ready,
                CloudCallbackEventType.Started => CloudCallbackType.Started,
                CloudCallbackEventType.Failed => CloudCallbackType.Failed,
                CloudCallbackEventType.Finished => CloudCallbackType.Finished,
                CloudCallbackEventType.LoggingComplete => CloudCallbackType.LoggingComplete,
                _ => throw new ArgumentOutOfRangeException(paramName: nameof(gameEvent)),
            };
            await cloudIntegrationService.Announce(
                cloudCallbackType,
                cloudCallbackEvent.Exception,
                null,
                cloudCallbackEvent.Ticks
            );
        }
        else if (gameEvent is UpdatePlayerEvent updatePlayerEvent)
        {
            cloudIntegrationService.UpdatePlayer(
                updatePlayerEvent.PlayerId.ToString(),
                updatePlayerEvent.FinalScore,
                updatePlayerEvent.MatchScore,
                updatePlayerEvent.Placement
            );
        }
    }
}
