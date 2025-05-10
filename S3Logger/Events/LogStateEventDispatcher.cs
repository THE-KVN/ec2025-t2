using System.Text.Json;
using Zooscape.Application.Events;
using Zooscape.Domain.Utilities;
using Zooscape.Infrastructure.SignalRHub.Models;

namespace Zooscape.Infrastructure.S3Logger.Events;

public class LogStateEventDispatcher(IStreamingFileLogger logger) : IEventDispatcher
{
    public async Task Dispatch<TEvent>(TEvent gameEvent)
        where TEvent : class
    {
        if (gameEvent is GameStateEvent gameStateEvent)
        {
            Helpers.TrackExecutionTime(
                "LogStateEventDispatcher.Dispatch",
                () =>
                {
                    var state = new GameState(gameStateEvent.GameState);
                    logger.LogState(JsonSerializer.Serialize(state));
                },
                out var _
            );
        }
        else if (gameEvent is CloseAndFlushLogsEvent)
        {
            logger.CloseAndFlush();
        }
    }
}
