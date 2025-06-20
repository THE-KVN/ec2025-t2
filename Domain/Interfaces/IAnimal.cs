using System;
using Zooscape.Domain.Enums;
using Zooscape.Domain.Models;
using Zooscape.Domain.Utilities;
using Zooscape.Domain.ValueObjects;

namespace Zooscape.Domain.Interfaces;

public interface IAnimal
{
    public Guid Id { get; init; }
    public string Nickname { get; }
    public GridCoords Location { get; }
    public GridCoords SpawnPoint { get; init; }
    public Direction CurrentDirection { get; }
    public int Score { get; }
    public int TicksOnSpawn { get; }
    public DateTime? FirstCommandTimeStamp { get; }
    public int CapturedCounter { get; }
    public int DistanceCovered { get; }
    public bool IsViable { get; set; }
    public ScoreStreak ScoreStreak { get; }
    public PowerUpType? HeldPowerUp { get; set; }
    public ActivePowerUp? ActivePowerUp { get; set; }
    public int PowerUpsUsed { get; set; }

    public int TimeInCage { get; } // Add this property
    public AnimalQueue<AnimalCommand> _commandQueue { get; } // Add this property




    /// <summary>
    /// Enqueues a command onto the animal's command queue
    /// </summary>
    /// <param name="command">Command to be enqueued</param>
    /// <returns>
    /// A <see cref="Result{T}"/> where <typeparamref name="T"/> is <see cref="int"/>.
    /// <list type="bullet">
    ///     <item><description>If successful, <see cref="Result{T}.IsSuccess"/> is <c>true</c>, and <see cref="Result{T}.Value"/> contains the new total number of commands on the animal's command queue.</description></item>
    ///     <item><description>If unsuccessful, <see cref="Result{T}.IsSuccess"/> is <c>false</c>, and <see cref="Result{T}.Error"/> contains an error describing the failure.</description></item>
    /// </list>
    /// </returns>
    public Result<int> AddCommand(AnimalCommand command);

    /// <summary>
    /// Pops the first command from the animal's command queue.
    /// </summary>
    /// <returns>A <see cref="AnimalCommand"/> containing the first command on the command queue. If there are no commands on the command queue, returns the last command returned previously. If no commands had ever been queued, returns <c>null</c></returns>
    public AnimalCommand? GetNextCommand();

    /// <summary>
    /// Sets the animal's <see cref="CurrentDirection"/>
    /// </summary>
    /// <param name="newDirection">New direction</param>
    public void SetDirection(Direction newDirection);

    /// <summary>
    /// Sets the animal's <see cref="Location"/> and increments <see cref="DistanceCovered"/> by 1 if new location is different from existing location.
    /// </summary>
    /// <param name="newLocation">New location</param>
    public void SetLocation(GridCoords newLocation);

    /// <summary>
    /// Sets the animal's <see cref="Score"/>
    /// </summary>
    /// <param name="newScore">New score</param>
    public void SetScore(int newScore);

    /// <summary>
    /// Adds points to animal's score after multiplying by animal's score streak
    /// </summary>
    /// <param name="points"></param>
    /// /// <param name="multiplier"></param>
    public void AddToScore(int points, double multiplier);


    /// <summary>
    /// Increment's the animal's <see cref="TicksOnSpawn"/> value by 1
    /// </summary>
    public void IncrementTimeOnSpawn();

    /// <summary>
    /// Returns an animal to its spawn point, set it to idle and clears its command queue
    /// </summary>
    public void Capture();
}
