using System;
using System.Collections.Generic;
using System.Linq;
using Zooscape.Application.Models;
using Zooscape.Domain.Interfaces;

namespace Zooscape.Application.Services;

public class ScoreService
{
    private readonly List<IWinCondtion> _winCondtions;

    public ScoreService()
    {
        _winCondtions =
        [
            new HighestScore(),
            new LeastDeaths(),
            new LeastTimeInCage(),
            new FarthestTraveled(),
            new FirstCommand(),
        ];
    }

    private IAnimal DetermineWinner(List<IAnimal> animals)
    {
        List<IAnimal> winners = animals;
        foreach (var condition in _winCondtions)
        {
            winners = condition.GetWinners(winners);
            if (winners.Count == 1)
            {
                return winners.First();
            }
        }
        if (winners.Count > 1)
        {
            throw new InvalidOperationException(
                "There is no single winner after all win conditons have been applied"
            );
        }
        return winners.First();
    }

    public List<IAnimal> OrderedWinners(List<IAnimal> animals)
    {
        var orderedWinners = new List<IAnimal>();
        var animalList = animals.ToList();
        while (animalList.Count >= 2)
        {
            var winner = DetermineWinner(animalList);
            orderedWinners.Add(winner);
            animalList.Remove(winner);
        }
        orderedWinners.Add(animalList.First());
        return orderedWinners;
    }
}
