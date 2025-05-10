using System;
using Zooscape.Domain.Algorithms.DataStructures;
using Zooscape.Domain.Enums;
using Zooscape.Domain.ValueObjects;

namespace Zooscape.Domain.Interfaces;

public interface IZookeeper
{
    public Guid Id { get; }
    public string Nickname { get; }
    public GridCoords Location { get; }
    public GridCoords SpawnPoint { get; }
    public Direction CurrentDirection { get; }
    public IAnimal? CurrentTarget { get; set; }
    public int TicksSinceTargetCalculated { get; set; }
    public Path? CurrentPath { get; set; }

    /// <summary>
    /// Sets the zookeeper's <see cref="CurrentDirection"/>
    /// </summary>
    /// <param name="newDirection">New direction</param>
    public void SetDirection(Direction newDirection);

    /// <summary>
    /// Sets the zookeeper's <see cref="Location"/>
    /// </summary>
    /// <param name="newLocation">Mew location</param>
    public void SetLocation(GridCoords newLocation);
}
