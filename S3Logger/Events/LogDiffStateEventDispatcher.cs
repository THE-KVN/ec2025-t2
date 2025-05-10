using JsonDiffPatchDotNet;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Zooscape.Application.Events;
using Zooscape.Domain.Utilities;

namespace Zooscape.Infrastructure.S3Logger.Events;

public class LogDiffStateEventDispatcher(IStreamingFileDiffLogger logger) : IEventDispatcher
{
    private readonly JsonDiffPatch _jdp = new();
    private JToken? _previousState;

    public async Task Dispatch<TEvent>(TEvent gameEvent)
        where TEvent : class
    {
        if (gameEvent is GameStateEvent gameStateEvent)
        {
            Helpers.TrackExecutionTime(
                "LogDiffStateEventDispatcher.Dispatch",
                () =>
                {
                    var state = new Models.GameState(gameStateEvent.GameState);
                    var currentState = JToken.FromObject(state);

                    string jsonToLog;
                    if (_previousState == null)
                    {
                        // Log full snapshot
                        jsonToLog = JsonConvert.SerializeObject(
                            new { type = "full", data = currentState }
                        );
                    }
                    else
                    {
                        // Log diff
                        var diff = _jdp.Diff(_previousState, currentState);
                        jsonToLog = JsonConvert.SerializeObject(new { type = "diff", data = diff });
                    }

                    logger.LogState(jsonToLog);

                    _previousState = currentState;
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
