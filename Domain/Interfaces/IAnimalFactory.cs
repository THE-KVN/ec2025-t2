using System;
using Zooscape.Domain.Models;
using Zooscape.Domain.ValueObjects;

namespace Zooscape.Domain.Interfaces;

public interface IAnimalFactory
{
    public Animal CreateAnimal(Guid id, string nickname, GridCoords spawnPoint, int queueSize);
}