using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Options;
using Zooscape.Application.Config;
using Zooscape.Domain.Algorithms.DataStructures;
using Zooscape.Domain.Algorithms.Pathfinding;
using Zooscape.Domain.Enums;
using Zooscape.Domain.Interfaces;
using Zooscape.Domain.Utilities;

namespace Zooscape.Application.Services;

public class ZookeeperService(IOptions<GameSettings> gameOptions) : IZookeeperService
{
    private readonly GameSettings _gameSettings = gameOptions.Value;

    public Direction CalculateZookeeperDirection(IWorld world, Guid zookeeperId)
    {
        var zookeeper = world.Zookeepers[zookeeperId];
        var animals = world.Animals.Values.ToList();

        zookeeper.CurrentTarget ??= PickTargetAnimal(zookeeper, world, animals);
        if (zookeeper.CurrentTarget == null)
        {
            return Direction.Idle;
        }
        if (!zookeeper.CurrentTarget.IsViable)
        {
            zookeeper.CurrentTarget = null;
            return Direction.Idle;
        }

        zookeeper.CurrentPath = GetPathToAnimal(zookeeper, world, zookeeper.CurrentTarget);
        if (zookeeper.CurrentPath == null)
        {
            // Can't calculate a path to target, so we should pick another target.
            zookeeper.CurrentTarget = null;
            return Direction.Idle;
        }

        var direction = GetDirectionFromPath(zookeeper.CurrentPath);

        if (++zookeeper.TicksSinceTargetCalculated >= _gameSettings.TicksBetweenZookeeperRetarget)
        {
            zookeeper.CurrentTarget = null;
            zookeeper.CurrentPath = null;
            zookeeper.TicksSinceTargetCalculated = 0;
        }
        return direction;
    }

    private static IAnimal? PickTargetAnimal(
        IZookeeper zookeeper,
        IWorld world,
        List<IAnimal> animals
    )
    {
        var validAnimals = animals.Where(animal => animal.IsViable).ToList();
        if (validAnimals.Count <= 0)
        {
            return null;
        }

        if (validAnimals.Count == 1)
        {
            return validAnimals[0];
        }

        var animalDistances = CalculateAnimalDistances(zookeeper, validAnimals);

        // Order animals by distance.
        var orderedAnimals = validAnimals.OrderBy((animal) => animalDistances[animal]);

        var firstAnimal = orderedAnimals.First();
        // If there is a tie for first place, we don't pick one.
        if (
            animalDistances.Values.Count(distance =>
                FloatingPoint.RoughlyEqual(distance, animalDistances[firstAnimal])
            ) > 1
        )
        {
            return null;
        }

        return firstAnimal;
    }

    private static Direction GetDirectionFromPath(Path path)
    {
        if (path.Length < 2)
        {
            return Direction.Idle;
        }

        var currentNode = path.Pop();
        var nextNode = path.Pop();
        return currentNode.GetDirectionToNode(nextNode);
    }

    private static Path? GetPathToAnimal(IZookeeper zookeeper, IWorld world, IAnimal animal)
    {
        return AStar.PerformAStarSearch(world, zookeeper.Location, animal.Location);
    }

    private static Dictionary<IAnimal, double> CalculateAnimalDistances(
        IZookeeper zookeeper,
        List<IAnimal> animals
    )
    {
        var animalDistances = new Dictionary<IAnimal, double>();
        foreach (IAnimal animal in animals)
        {
            animalDistances[animal] = zookeeper.Location.ManhattanDistance(animal.Location);
        }

        return animalDistances;
    }
}
