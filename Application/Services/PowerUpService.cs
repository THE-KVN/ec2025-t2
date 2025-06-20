using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Zooscape.Application.Config;
using Zooscape.Domain.Enums;
using Zooscape.Domain.Models;
using Zooscape.Domain.Utilities;

namespace Zooscape.Application.Services;

public class PowerUpService : IPowerUpService
{
    private readonly ILogger<PowerUpService> _logger;
    private readonly GameSettings _gameSettings;
    private readonly PowerUps _powerUpConfig;
    private readonly GlobalSeededRandomizer _globalRandomizer;

    private readonly Dictionary<PowerUpType, (int value, int duration)> _powerUpValues;

    public int DistanceFromPlayers => _powerUpConfig.DistanceFromPlayers;
    public int DistanceFromOtherPowerUps => _powerUpConfig.DistanceFromOtherPowerUps;

    public PowerUpService(
        ILogger<PowerUpService> logger,
        IOptions<GameSettings> gameOptions,
        GlobalSeededRandomizer globalRandomizer
    )
    {
        _logger = logger;
        _gameSettings = gameOptions.Value;
        _powerUpConfig = gameOptions.Value.PowerUps;
        _powerUpValues = new Dictionary<PowerUpType, (int value, int duration)>();
        foreach (var kv in _powerUpConfig.Types)
        {
            var key = PowerUpTypeExtensions.FromName(kv.Key);
            var value = (kv.Value.Value, kv.Value.Duration);
            _powerUpValues.Add(key, value);
        }
        _globalRandomizer = globalRandomizer;
    }

    public int GetTimeToNextPowerUp()
    {
        var spawnIntervals = _powerUpConfig.SpawnInterval;
        var timeToNextPowerUpDouble = _globalRandomizer.NormalNextDouble(
            spawnIntervals.Mean,
            spawnIntervals.StdDev,
            spawnIntervals.Min,
            spawnIntervals.Max
        );
        var ticksUntilNextPowerUp = (int)Math.Round(timeToNextPowerUpDouble);
        _logger.LogInformation(
            "The next power up will spawn in {TicksUntilNextPowerUp} ticks.",
            ticksUntilNextPowerUp
        );
        return ticksUntilNextPowerUp;
    }

    public PowerUpType SpawnPowerUp()
    {
        Dictionary<string, int> weightedValues = new();
        foreach (var type in _powerUpConfig.Types)
        {
            weightedValues.Add(type.Key, type.Value.RarityWeight);
        }

        var typeName = _globalRandomizer.NextWeightedValue(weightedValues);
        var typeConfig = _powerUpConfig.Types[typeName];
        _logger.LogDebug(
            "Spawning power up: {TypeName}(Value={Value}, Duration={Duration})",
            typeName,
            typeConfig.Value,
            typeConfig.Duration
        );
        return PowerUpTypeExtensions.FromName(typeName);
    }

    public ActivePowerUp GetActivePowerUp(PowerUpType type)
    {
        if (!_powerUpValues.TryGetValue(type, out var powerUpValues))
        {
            throw new ArgumentException($"Unknown power up type: {nameof(type)}");
        }
        return new ActivePowerUp()
        {
            Type = type,
            Value = powerUpValues.value,
            TicksRemaining = powerUpValues.duration,
        };
    }

    public int GetPowerUpValue(PowerUpType type)
    {
        return _powerUpValues[type].value;
    }
}