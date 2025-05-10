using System;
using Zooscape.Domain.Algorithms.DataStructures;
using Zooscape.Domain.Enums;
using Zooscape.Domain.Interfaces;
using Zooscape.Domain.ValueObjects;

namespace Zooscape.Domain.Models;

public class Zookeeper : IZookeeper
{
    public Guid Id { get; init; }
    public string Nickname { get; init; }
    public GridCoords Location { get; private set; }
    public GridCoords SpawnPoint { get; init; }
    public Direction CurrentDirection { get; private set; }
    public IAnimal? CurrentTarget { get; set; }
    public int TicksSinceTargetCalculated { get; set; }
    public Path? CurrentPath { get; set; }

    public Zookeeper(Guid id, string nickname, GridCoords spawnPoint)
    {
        Id = id;
        Nickname = nickname;
        SpawnPoint = spawnPoint;
        Location = spawnPoint;
        CurrentDirection = Direction.Idle;
        CurrentTarget = null;
        TicksSinceTargetCalculated = 0;
        CurrentPath = null;
    }

    public void SetDirection(Direction newDirection)
    {
        CurrentDirection = newDirection;
    }

    public void SetLocation(GridCoords newLocation)
    {
        Location = newLocation;
    }
}
