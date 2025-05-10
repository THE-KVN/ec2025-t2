using System.Collections.Generic;
using System.Linq;
using Zooscape.Domain.Interfaces;

namespace Zooscape.Application.Models;

public class HighestScore : IWinCondtion
{
    public List<IAnimal> GetWinners(List<IAnimal> animals)
    {
        var highestScore = animals.Max(animal => animal.Score);
        return animals.Where(animal => animal.Score == highestScore).ToList();
    }
}

public class LeastDeaths : IWinCondtion
{
    public List<IAnimal> GetWinners(List<IAnimal> animals)
    {
        var leastDeaths = animals.Min(animal => animal.CapturedCounter);
        return animals.Where(animal => animal.CapturedCounter == leastDeaths).ToList();
    }
}

public class FarthestTraveled : IWinCondtion
{
    public List<IAnimal> GetWinners(List<IAnimal> animals)
    {
        var farthestTraveled = animals.Max(animal => animal.DistanceCovered);
        return animals.Where(animal => animal.DistanceCovered == farthestTraveled).ToList();
    }
}

public class LeastTimeInCage : IWinCondtion
{
    public List<IAnimal> GetWinners(List<IAnimal> animals)
    {
        var leastTimeInCage = animals.Min(animal => animal.TicksOnSpawn);
        return animals.Where(animal => animal.TicksOnSpawn == leastTimeInCage).ToList();
    }
}

public class FirstCommand : IWinCondtion
{
    public List<IAnimal> GetWinners(List<IAnimal> animals)
    {
        var firstCommand = animals.Min(animal => animal.FirstCommandTimeStamp);
        return animals.Where(animal => animal.FirstCommandTimeStamp == firstCommand).ToList();
    }
}
