using System;

namespace Zooscape.Application.Events;

public class UpdatePlayerEvent
{
    public readonly Guid PlayerId;
    public readonly int? FinalScore;
    public readonly int? MatchScore;
    public readonly int? Placement;

    public UpdatePlayerEvent(
        Guid playerId,
        int? matchScore = null,
        int? finalScore = null,
        int? placement = null
    )
    {
        PlayerId = playerId;
        FinalScore = finalScore;
        MatchScore = matchScore;
        Placement = placement;
    }
}
