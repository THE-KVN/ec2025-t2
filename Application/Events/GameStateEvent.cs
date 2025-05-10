using Zooscape.Application.Services;

namespace Zooscape.Application.Events;

public class GameStateEvent
{
    public IGameStateService GameState { get; }

    public GameStateEvent(IGameStateService gameState)
    {
        GameState = gameState;
    }
}
