using System;
using System.Threading.Tasks;
using Zooscape.Application.Events;
using Zooscape.Infrastructure.CloudIntegration.Enums;
using Zooscape.Infrastructure.CloudIntegration.Services;
using Microsoft.Extensions.Options;
using Zooscape.Application.Config;

namespace Zooscape.Infrastructure.CloudIntegration.Events;

public class CloudEventDispatcher(
    ICloudIntegrationService cloudIntegrationService,
    IOptions<GameSettings> gameSettings
) : IEventDispatcher
{

    private readonly GameSettings _gameSettings = gameSettings.Value;
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
                callbackType: cloudCallbackType,
                e: cloudCallbackEvent.Exception,
                seed: _gameSettings.Seed,
                ticks: cloudCallbackEvent.Ticks
            );
        }
        else if (gameEvent is UpdatePlayerEvent updatePlayerEvent)
        {
            cloudIntegrationService.UpdatePlayer(
                playerId: updatePlayerEvent.PlayerId.ToString(),
                finalScore: updatePlayerEvent.FinalScore,
                matchPoints: updatePlayerEvent.MatchScore,
                placement: updatePlayerEvent.Placement
            );
        }
    }
}
