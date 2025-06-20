using System;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Zooscape.Application.Config;
using Zooscape.Domain.Utilities;

namespace Zooscape.Application.Services;

public class ObstacleService : IObstacleService
{
    private readonly ILogger<ObstacleService> _logger;
    private readonly Obstacles _obstacleConfig;
    private readonly GlobalSeededRandomizer _globalRandomizer;

    public int DistanceFromPlayers => _obstacleConfig.DistanceFromPlayers;
    public int DistanceFromPlayerSpawnPoints => _obstacleConfig.DistanceFromPlayerSpawnPoints;
    public int DistanceFromOtherObstacles => _obstacleConfig.DistanceFromOtherObstacles;

    public ObstacleService(
        ILogger<ObstacleService> logger,
        IOptions<GameSettings> gameOptions,
        GlobalSeededRandomizer globalRandomizer
    )
    {
        _logger = logger;
        _obstacleConfig = gameOptions.Value.Obstacles;
        _globalRandomizer = globalRandomizer;
    }

    public int GetTimeToNextObstacle()
    {
        var spawnIntervals = _obstacleConfig.SpawnInterval;
        var timeToNextObstacleDouble = _globalRandomizer.NormalNextDouble(
            spawnIntervals.Mean,
            spawnIntervals.StdDev,
            spawnIntervals.Min,
            spawnIntervals.Max
        );
        var ticksUntilNextObstacle = (int)Math.Round(timeToNextObstacleDouble);
        _logger.LogInformation(
            "The next obstacle will spawn in {TicksUntilNextObstacle} ticks.",
            ticksUntilNextObstacle
        );
        return ticksUntilNextObstacle;
    }
}