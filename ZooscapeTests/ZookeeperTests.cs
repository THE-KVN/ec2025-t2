using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;
using Xunit.Abstractions;
using Zooscape.Application.Config;
using Zooscape.Application.Services;
using Zooscape.Domain.Enums;
using Zooscape.Domain.ExtensionMethods;
using Zooscape.Domain.Interfaces;
using Zooscape.Domain.Models;

namespace ZooscapeTests;

public class ZookeeperTests
{
    private const string inputMap = "file:StarterWorlds/World1.txt";
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly IZookeeperService _zookeeperService;
    private readonly IGameStateService _gameStateService;
    private readonly IZookeeper _zookeeper;
    private readonly IAnimal _animal;

    public ZookeeperTests(ITestOutputHelper testOutputHelper)
    {
        var gameSettings = new GameSettings() { WorldMap = inputMap };
        var options = Options.Create(gameSettings);

        _testOutputHelper = testOutputHelper;
        _zookeeperService = new ZookeeperService(options);
        _gameStateService = new GameStateService(options, NullLogger<GameStateService>.Instance);

        _gameStateService.AddZookeeper();

        _animal = AddAnimals().First();
        _zookeeper = _gameStateService.Zookeepers.Values.First();
    }

    private List<IAnimal> AddAnimals(int amount = 4)
    {
        var animalIds = TestUtils.GenerateAnimalIds(amount);
        return animalIds.Select((id) => _gameStateService.AddAnimal(id, "").Value!).ToList();
    }

    [Fact]
    public void Pathfinding_NotTooExpensive()
    {
        const int numberOfTicks = 50;
        _animal.IsViable = true;
        TestUtils.QueueAnimalAction(_animal, BotAction.Right);
        TestUtils.QueueAnimalAction(_animal, BotAction.Down);
        TimeSpan totalTime = TimeSpan.Zero;
        TimeSpan longestTick = TimeSpan.Zero;
        for (var tick = 0; tick < numberOfTicks; tick++)
        {
            var command = _animal.GetNextCommand();
            if (command != null)
                _animal.SetDirection(command.Action.ToDirection());
            _gameStateService.MoveAnimal(_animal);
            var time1 = DateTime.UtcNow;
            var newDirection = _zookeeperService.CalculateZookeeperDirection(
                _gameStateService.World,
                _zookeeper.Id
            );
            _zookeeper.SetDirection(newDirection);
            _gameStateService.MoveZookeeper(_zookeeper);
            var time2 = DateTime.UtcNow;
            var elapsedTime = time2 - time1;
            totalTime += elapsedTime;
            if (elapsedTime > longestTick)
                longestTick = elapsedTime;
            Assert.True(elapsedTime < TimeSpan.FromMilliseconds(50));
        }
        var averageTime = totalTime / numberOfTicks;
        _testOutputHelper.WriteLine(
            $"Total time elapsed over {numberOfTicks} ticks: {totalTime.TotalMilliseconds}ms"
        );
        _testOutputHelper.WriteLine($"Average time per tick: {averageTime.TotalMilliseconds}ms");
        _testOutputHelper.WriteLine($"Longest running tick: {longestTick.TotalMilliseconds}ms");
    }

    [Fact]
    public void Zookeeper_Selects_Target()
    {
        const int numberOfTicks = 9;
        _animal.IsViable = true;

        TestUtils.QueueAnimalAction(_animal, BotAction.Right);
        TestUtils.QueueAnimalAction(_animal, BotAction.Down);

        for (var tick = 0; tick < numberOfTicks; tick++)
        {
            var command = _animal.GetNextCommand();
            if (command != null)
                _animal.SetDirection(command.Action.ToDirection());
            _gameStateService.MoveAnimal(_animal);
            var newDirection = _zookeeperService.CalculateZookeeperDirection(
                _gameStateService.World,
                _zookeeper.Id
            );
            _zookeeper.SetDirection(newDirection);
            _gameStateService.MoveZookeeper(_zookeeper);
        }
        Assert.NotEqual(_animal.Location, _animal.SpawnPoint);
        Assert.Equal(_zookeeper.CurrentTarget, _animal);
    }

    [Fact]
    public void Zookeeper_Moves_Closer_To_Target()
    {
        const int numberOfTicks = 9;

        _animal.IsViable = true;

        TestUtils.QueueAnimalAction(_animal, BotAction.Right);

        var zookeeperLocation = _zookeeper.Location;
        var distanceToTarget = Math.Round(_animal.Location.EuclideanDistance(zookeeperLocation), 2);
        _testOutputHelper.WriteLine($"Initial animal location: {_animal.Location}");
        _testOutputHelper.WriteLine($"Initial zookeeper location: {zookeeperLocation}");
        _testOutputHelper.WriteLine($"Initial distance to target: {distanceToTarget}");
        for (var tick = 0; tick < numberOfTicks; tick++)
        {
            var command = _animal.GetNextCommand();
            if (command != null)
                _animal.SetDirection(command.Action.ToDirection());
            _gameStateService.MoveAnimal(_animal);

            _zookeeper.SetDirection(
                _zookeeperService.CalculateZookeeperDirection(
                    _gameStateService.World,
                    _zookeeper.Id
                )
            );
            _gameStateService.MoveZookeeper(_zookeeper);

            _testOutputHelper.WriteLine(
                $"Path Length: {((Zookeeper)_zookeeper).CurrentPath?.Length}"
            );

            if (tick < 1)
                continue;

            Assert.NotEqual(zookeeperLocation, _zookeeper.Location);
            zookeeperLocation = _zookeeper.Location;
        }
        var finalDistanceToTarget = Math.Round(
            _animal.Location.EuclideanDistance(_zookeeper.Location),
            2
        );
        _testOutputHelper.WriteLine($"Final animal location: {_animal.Location}");
        _testOutputHelper.WriteLine($"Final zookeeper location: {_zookeeper.Location}");
        _testOutputHelper.WriteLine($"Final distance to target: {finalDistanceToTarget}");
        Assert.True(finalDistanceToTarget < distanceToTarget);
    }

    [Fact]
    public void Zookeeper_Does_Not_Select_NonViable_Target()
    {
        const int numberOfTicks = 20;

        _animal.IsViable = true;
        _animal.SetDirection(Direction.Right);
        _gameStateService.MoveAnimal(_animal);
        _zookeeperService.CalculateZookeeperDirection(_gameStateService.World, _zookeeper.Id);
        Assert.Equal(_zookeeper.CurrentTarget, _animal);
        _animal.IsViable = false;
        _zookeeperService.CalculateZookeeperDirection(_gameStateService.World, _zookeeper.Id);
        Assert.Null(_zookeeper.CurrentTarget);

        for (var tick = 0; tick < numberOfTicks; tick++)
        {
            _zookeeperService.CalculateZookeeperDirection(_gameStateService.World, _zookeeper.Id);
            Assert.Null(_zookeeper.CurrentTarget);
        }
    }
}
