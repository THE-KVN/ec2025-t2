namespace Zooscape.Application.Services;

public interface IObstacleService
{
    /// <summary>
    /// The minimum distance from players that obstacles can spawn.
    /// </summary>
    public int DistanceFromPlayers { get; }

    /// <summary>
    /// The minimum distance from player spawn points that obstacles can spawn.
    /// </summary>
    public int DistanceFromPlayerSpawnPoints { get; }

    /// <summary>
    /// The minimum distance from other obstacles that obstacles can spawn.
    /// </summary>
    public int DistanceFromOtherObstacles { get; }

    /// <summary>
    /// Gets the number of ticks until the next obstacle should spawn.
    /// </summary>
    /// <returns>The number of ticks to wait for the next obstacle.</returns>
    public int GetTimeToNextObstacle();
}