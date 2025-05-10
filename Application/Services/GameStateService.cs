using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Zooscape.Application.Config;
using Zooscape.Domain.Enums;
using Zooscape.Domain.ExtensionMethods;
using Zooscape.Domain.Interfaces;
using Zooscape.Domain.Models;
using Zooscape.Domain.Utilities;
using Zooscape.Domain.ValueObjects;
using Zooscape.MapGenerator;

namespace Zooscape.Application.Services;

public class GameStateService : IGameStateService
{
    private readonly GameSettings _gameSettings;
    private readonly ILogger<GameStateService> _logger;
    private readonly World _world;

    public ConcurrentDictionary<string, (Guid BotId, string Nickname)> BotIds { get; set; } = [];
    public List<string> Visualisers { get; set; } = [];
    public IWorld World => _world;
    public Dictionary<Guid, IZookeeper> Zookeepers => _world.Zookeepers;
    public Dictionary<Guid, IAnimal> Animals => _world.Animals.ToDictionary();
    public bool IsReady => _world.IsReady;
    public int TickCounter { get; set; }

    public GameStateService(
        IOptions<GameSettings> options,
        ILogger<GameStateService> logger,
        GlobalSeededRandomizer randomizer
    )
    {
        _logger = logger;
        _gameSettings = options.Value;

        var mapConfig = _gameSettings.WorldMap.Split(':');
        var mapString = mapConfig[0] switch
        {
            "file" => File.ReadAllText(mapConfig[1]),
            "string" => mapConfig[1],
            "generate" => GenerateMap(mapConfig[1], randomizer.Next()),
            _ => throw new ArgumentException(
                "Error reading world map",
                nameof(options.Value.WorldMap)
            ),
        };

        _world = new World(mapString, _gameSettings.NumberOfBots, _gameSettings.CommandQueueSize);
    }

    private static String GenerateMap(string values, int seed)
    {
        var parts = values.Split('|');
        if (parts.Length < 4)
        {
            throw new ArgumentException($"Invalid generator format: {values}", nameof(values));
        }
        if (seed == 0)
        {
            throw new ArgumentException($"Invalid seed: {seed}", nameof(seed));
        }

        int size;
        int teleports;
        double smoothness;
        double openness;

        try
        {
            size = int.Parse(parts[0]);
            teleports = int.Parse(parts[1]);
            smoothness = double.Parse(parts[2]);
            openness = double.Parse(parts[3]);
        }
        catch (FormatException exception)
        {
            throw new ArgumentException(
                $"Invalid generator format, non number value given: {values}",
                nameof(values),
                exception
            );
        }
        if (int.IsEvenInteger(size))
        {
            throw new ArgumentException(
                $"Invalid generator format, given size ({size}) is not odd.",
                nameof(values)
            );
        }

        var map = new Map(
            size: size,
            smoothness: smoothness,
            openness: openness,
            teleports: teleports,
            seed: seed
            );

        return map.ToString();
    }

    public Result<IAnimal> AddAnimal(Guid botId, string nickname)
    {
        var newAnimal = _world.AddAnimal(botId, nickname);

        if (newAnimal.IsSuccess)
        {
            _logger.LogInformation(
            "Animal ({nickname}) added to game world with ID {botId}.",
            newAnimal.Value?.Nickname,
            botId
        );
        }
        else
        {
            return new ResultError("Error adding animal.", newAnimal.Error);
        }

        return newAnimal;
    }

    public Result<IZookeeper> AddZookeeper()
    {
        var zookeeper = _world.AddZookeeper(Guid.NewGuid());

        if (zookeeper.IsSuccess)
        {
            _logger.LogInformation(
                "Zookeeper ({nickname}) added to game world.",
                zookeeper.Value?.Nickname
            );
        }
        else
        {
            _logger.LogError("Error adding zookeeper. {error}", zookeeper.Error?.ToString());
        }

        return zookeeper;
    }

    public Result<int> EnqueueCommand(Guid botId, BotCommand command)
    {
        if (!_world.Animals.TryGetValue(botId, out var animal))
            return new ResultError("Bot not found.");
        if (command != null)
        {
            var queueSize = animal.AddCommand(new AnimalCommand(botId, command.Action));

            if (!queueSize.IsSuccess)
                return new ResultError("Error enqueueing command for bot.", queueSize.Error);

            return queueSize.Value;
        }

        return animal._commandQueue.Count;
    }

    public GridCoords MoveAnimal(IAnimal animal)
    {
        var oldLocation = animal.Location;
        var newLocation = animal.Location + animal.CurrentDirection;

        // Wrap around if the map allows it
        newLocation.X = (newLocation.X + World.Width) % World.Width;
        newLocation.Y = (newLocation.Y + World.Height) % World.Height;

        if (!World.GetCellContents(newLocation).IsTraversable())
        {
            newLocation = animal.Location;
        }

        animal.SetLocation(newLocation);

        return newLocation;
    }

    public GridCoords MoveZookeeper(IZookeeper zookeeper)
    {
        var newLocation = zookeeper.Location + zookeeper.CurrentDirection;

        if (
            !World.IsPointInBounds(newLocation)
            || !World.GetCellContents(newLocation).IsTraversable()
        )
        {
            newLocation = zookeeper.Location;
        }

        zookeeper.SetLocation(newLocation);

        return newLocation;
    }
}
