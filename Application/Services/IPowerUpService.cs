using Zooscape.Domain.Enums;
using Zooscape.Domain.Models;

namespace Zooscape.Application.Services;

public interface IPowerUpService
{
    /// <summary>
    /// The minimum distance away from players that power ups can spawn.
    /// </summary>
    public int DistanceFromPlayers { get; }

    /// <summary>
    /// The minimum distance away from other power ups that power ups can spawn.
    /// </summary>
    public int DistanceFromOtherPowerUps { get; }

    /// <summary>
    /// Gets the number of ticks until the next power up should spawn.
    /// </summary>
    /// <returns>The number of ticks to wait for the next power up.</returns>
    public int GetTimeToNextPowerUp();

    /// <summary>
    /// Randomly generates a power up type to spawn.
    /// </summary>
    /// <returns>The power up type that should be spawned.</returns>
    public PowerUpType SpawnPowerUp();

    /// <summary>
    /// Generates an active power up from the given power up type.
    /// </summary>
    /// <param name="type">The type to generate the active power up for.</param>
    /// <returns>The active power up constructed from the power up type.</returns>
    public ActivePowerUp GetActivePowerUp(PowerUpType type);

    /// <summary>
    /// Gets the value assigned to a given power up type.
    /// </summary>
    /// <param name="type">The type to get the value for.</param>
    /// <returns>The value associated with the given type.</returns>
    public int GetPowerUpValue(PowerUpType type);
}