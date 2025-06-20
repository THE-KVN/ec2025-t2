using System;
using Microsoft.Extensions.Options;
using Zooscape.Application.Config;
using Zooscape.Domain.Interfaces;
using Zooscape.Domain.Models;
using Zooscape.Domain.ValueObjects;

namespace Zooscape.Application.Services;

public class AnimalFactory : IAnimalFactory
{
    private readonly GameSettings _gameSettings;

    public AnimalFactory(IOptions<GameSettings> options)
    {
        _gameSettings = options.Value;
    }

    public Animal CreateAnimal(Guid id, string nickname, GridCoords spawnPoint, int queueSize)
    {
        return new Animal(
            id,
            nickname,
            spawnPoint,
            queueSize,
            _gameSettings.ScoreStreak.MultiplierGrowthFactor,
            _gameSettings.ScoreStreak.Max,
            _gameSettings.ScoreStreak.ResetGrace
        );
    }
}
