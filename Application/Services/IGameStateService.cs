using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Zooscape.Domain.Interfaces;
using Zooscape.Domain.Models;
using Zooscape.Domain.Utilities;
using Zooscape.Domain.ValueObjects;


namespace Zooscape.Application.Services;

public interface IGameStateService
{
    public ConcurrentDictionary<string, (Guid BotId, string Nickname)> BotIds { get; set; }
    public List<string> Visualisers { get; set; }
    public IWorld World { get; }
    public Dictionary<Guid, IZookeeper> Zookeepers { get; }
    public Dictionary<Guid, IAnimal> Animals { get; }
    public Dictionary<int, List<GridCoords>> PelletsToRespawn { get; }

    public bool IsReady { get; }
    public int TickCounter { get; set; }

    /// <summary>
    /// Adds an animal to the game state
    /// </summary>
    /// <param name="botId">Unique identifier as received from the registering bot</param>
    /// <param name="nickname">Nickname as received from the registering bot</param>
    /// <returns>
    /// A <see cref="Result{T}"/> where <typeparamref name="T"/> is <see cref="IAnimal"/>.
    /// <list type="bullet">
    ///     <item><description>If successful, <see cref="Result{T}.IsSuccess"/> is <c>true</c>, and <see cref="Result{T}.Value"/> contains the newly created Animal.</description></item>
    ///     <item><description>If unsuccessful, <see cref="Result{T}.IsSuccess"/> is <c>false</c>, and <see cref="Result{T}.Error"/> contains an error describing the failure.</description></item>
    /// </list>
    /// </returns>
    public Result<IAnimal> AddAnimal(Guid botId, string nickname);

    /// <summary>
    /// Adds a zookeeper to the game state
    /// </summary>
    /// <returns>
    /// A <see cref="Result{T}"/> where <typeparamref name="T"/> is <see cref="IZookeeper"/>.
    /// <list type="bullet">
    ///     <item><description>If successful, <see cref="Result{T}.IsSuccess"/> is <c>true</c>, and <see cref="Result{T}.Value"/> contains the newly created Zookeeper.</description></item>
    ///     <item><description>If unsuccessful, <see cref="Result{T}.IsSuccess"/> is <c>false</c>, and <see cref="Result{T}.Error"/> contains an error describing the failure.</description></item>
    /// </list>
    /// </returns>
    public Result<IZookeeper> AddZookeeper();

    /// <summary>
    /// Adds a command to the specified animal's command queue
    /// </summary>
    /// <param name="botId">Unique identifier of the registered bot</param>
    /// <param name="command"></param>
    /// <returns>
    /// A <see cref="Result{T}"/> where <typeparamref name="T"/> is <see cref="int"/>.
    /// <list type="bullet">
    ///     <item><description>If successful, <see cref="Result{T}.IsSuccess"/> is <c>true</c>, and <see cref="Result{T}.Value"/> contains the new total number of commands on the queue.</description></item>
    ///     <item><description>If unsuccessful, <see cref="Result{T}.IsSuccess"/> is <c>false</c>, and <see cref="Result{T}.Error"/> contains an error describing the failure.</description></item>
    /// </list>
    /// </returns>
    public Result<int> EnqueueCommand(Guid botId, BotCommand command);

    /// <summary>
    /// Calculates and updates an animal's location based on its current location, direction and surrounding environment.
    /// </summary>
    /// <param name="animal">Animal to be moved</param>
    /// <returns>A <see cref="GridCoords"/> with the animal's new, or unchanged, location</returns>
    public GridCoords MoveAnimal(IAnimal animal);

    /// <summary>
    /// Calculates and updates a zookeeper's location based on its current location, direction and surrounding environment.
    /// </summary>
    /// <param name="zookeeper">Zookeeper to be moved</param>
    /// <returns>A <see cref="GridCoords"/> with the zookeeper's new, or unchanged, location</returns>

    public GridCoords MoveZookeeper(IZookeeper zookeeper);

    /// <summary>
    /// Processes the spawning of power ups/obstacles/Pellets.
    /// </summary>
    public void ProcessSpawning();

    /// <summary>
    /// Processes active power ups on the given animal.
    /// </summary>
    /// <param name="animal">The animal on which to process power ups.</param>
    public void ProcessPowerUps(IAnimal animal);

    /// <summary>
    /// Activates the animal's held power up.
    /// </summary>
    /// <param name="animal">The animal on which to activate the power up.</param>
    public void ActivatePowerUp(IAnimal animal);

    /// <summary>
    /// Gets the score for a power pellet.
    /// </summary>
    /// <returns>The score that a power pellet gives.</returns>
    public int GetPowerPelletScore();

    public int GetTicksUntilPelletRespawn(int tick);
}
