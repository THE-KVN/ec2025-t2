using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;
using Zooscape.Application.Config;
using Zooscape.Application.Services;
using Zooscape.Domain.Enums;
using Zooscape.Domain.Models;
using Zooscape.Domain.Utilities;
using Zooscape.Domain.ValueObjects;

namespace ZooscapeTests;

public class WorldTests
{
    [Fact]
    public void Create_ValidParameters_ReturnsWorldInstance()
    {
        // Arrange
        var inputMap =
            "110111111111111111011"
            + Environment.NewLine
            + "142212222121222212241"
            + Environment.NewLine
            + "021222112121211222120"
            + Environment.NewLine
            + "121112212222212211121"
            + Environment.NewLine
            + "122222212121212222221"
            + Environment.NewLine
            + "112111212121212111211"
            + Environment.NewLine
            + "122222222222222222221"
            + Environment.NewLine
            + "121212111222111212121"
            + Environment.NewLine
            + "121212222212222212121"
            + Environment.NewLine
            + "122212122222221212221"
            + Environment.NewLine
            + "111222111232111222111"
            + Environment.NewLine
            + "122212122222221212221"
            + Environment.NewLine
            + "121212222212222212121"
            + Environment.NewLine
            + "121212111222111212121"
            + Environment.NewLine
            + "122222222222222222221"
            + Environment.NewLine
            + "112111212121212111211"
            + Environment.NewLine
            + "122222212121212222221"
            + Environment.NewLine
            + "121112212222212211121"
            + Environment.NewLine
            + "021222112121211222120"
            + Environment.NewLine
            + "142212222121222212241"
            + Environment.NewLine
            + "110111111111111111011";
        var zookeeperIds = new List<Guid> { Guid.NewGuid() };

        var animalIds = TestUtils.GenerateAnimalIds();

        // Act
        var world = new World(inputMap, 4, 10);

        foreach (var zookeeperId in zookeeperIds)
            world.AddZookeeper(zookeeperId);

        foreach (var animalId in animalIds)
            world.AddAnimal(animalId, "");

        // Assert
        var spawnPoints = world.Animals.Select(a => a.Value.SpawnPoint);

        Assert.Multiple(
            () => Assert.Contains(spawnPoints, v => v == new GridCoords(1, 1)),
            () => Assert.Contains(spawnPoints, v => v == new GridCoords(1, 1)),
            () => Assert.Contains(spawnPoints, v => v == new GridCoords(19, 1)),
            () => Assert.Contains(spawnPoints, v => v == new GridCoords(19, 1)),
            () => Assert.Equal(21, world.Width),
            () => Assert.Equal(21, world.Height),
            () => Assert.Equal(new GridCoords(10, 10), world.Zookeepers.First().Value.SpawnPoint),
            () => Assert.Equal(CellContents.Wall, world.Cells[0, 0]),
            () => Assert.Equal(CellContents.Empty, world.Cells[2, 0]),
            () => Assert.Equal(CellContents.Pellet, world.Cells[6, 15])
        );
    }

    [Fact]
    public void Create_ThreeZookeeperSpawnPoints_ThrowsArgumentException()
    {
        // Arrange
        var inputMap =
            "1111111"
            + Environment.NewLine
            + "1422241"
            + Environment.NewLine
            + "1233321"
            + Environment.NewLine
            + "1422241"
            + Environment.NewLine
            + "1111111";

        var zookeeperIds = new List<Guid>() { Guid.NewGuid() };

        var animalIds = TestUtils.GenerateAnimalIds();

        // Act
        var exception = Assert.Throws<ArgumentException>(() => new World(inputMap, 4, 10));

        // Assert
        Assert.Equal("Map Error: Exactly one zookeeper spawn point required", exception.Message);
    }

    [Fact]
    public void Create_ThreeAnimalSpawnPoints_ThrowsArgumentException()
    {
        // Arrange
        var inputMap =
            "1111111"
            + Environment.NewLine
            + "1422241"
            + Environment.NewLine
            + "1223221"
            + Environment.NewLine
            + "1422201"
            + Environment.NewLine
            + "1111111";

        var zookeeperIds = new List<Guid>() { Guid.NewGuid() };

        var animalIds = TestUtils.GenerateAnimalIds();

        // Act
        var exception = Assert.Throws<ArgumentException>(() => new World(inputMap, 4, 10));

        // Assert
        Assert.Equal("Map Error: Exactly four animal spawn points required", exception.Message);
    }

    [Theory]
    [InlineData(Direction.Right, 1, 2)] // Move to traversable cell
    [InlineData(Direction.Up, 0, 2)] // Move to non-traversable cell
    [InlineData(Direction.Left, 0, 2)] // Attempt to move through portal
    public void Move_Zookeeper(Direction direction, int expectedX, int expectedY)
    {
        // Arrange
        var inputMap =
            "string:11111"
            + Environment.NewLine
            + "14241"
            + Environment.NewLine
            + "32220"
            + Environment.NewLine
            + "14241"
            + Environment.NewLine
            + "11111";

        var settings = new GameSettings { WorldMap = inputMap };
        var options = Options.Create(settings);
        var gameState = new GameStateService(options, NullLogger<GameStateService>.Instance);

        foreach (var animalId in TestUtils.GenerateAnimalIds())
            gameState.AddAnimal(animalId, "");

        gameState.AddZookeeper();

        // Act
        var zookeeper = gameState.Zookeepers.Values.First();
        zookeeper.SetDirection(direction);
        var newLocation = gameState.MoveZookeeper(zookeeper);

        // Assert
        Assert.Equal(new GridCoords(expectedX, expectedY), newLocation);
    }

    [Theory]
    [InlineData(Direction.Down, 1, 1)] // Move to traversable cell
    [InlineData(Direction.Left, 1, 0)] // Move to non-traversable cell
    [InlineData(Direction.Up, 1, 4)] // Move through portal
    public void Move_Animal(Direction direction, int expectedX, int expectedY)
    {
        // Arrange
        var inputMap =
            "string:14101"
            + Environment.NewLine
            + "02240"
            + Environment.NewLine
            + "12311"
            + Environment.NewLine
            + "04140"
            + Environment.NewLine
            + "10101";

        var settings = new GameSettings { WorldMap = inputMap };
        var options = Options.Create(settings);
        var gameState = new GameStateService(options, NullLogger<GameStateService>.Instance);

        var animal = gameState.AddAnimal(Guid.NewGuid(), "").Value!;
        gameState.AddAnimal(Guid.NewGuid(), "");
        gameState.AddAnimal(Guid.NewGuid(), "");
        gameState.AddAnimal(Guid.NewGuid(), "");

        // Act
        animal.SetDirection(direction);
        gameState.MoveAnimal(animal);

        // Assert
        Assert.Equal(new GridCoords(expectedX, expectedY), animal.Location);
    }

    [Fact]
    public void Create_FromSampleFile_ReturnsWorldInstance()
    {
        var inputMap = WorldUtilities.ReadWorld("StarterWorlds/World1.txt");
        var zookeeperIds = new List<Guid> { Guid.NewGuid() };

        var animalIds = TestUtils.GenerateAnimalIds();

        var world = new World(inputMap, 4, 10);

        foreach (var zookeeperId in zookeeperIds)
            world.AddZookeeper(zookeeperId);

        foreach (var animalId in animalIds)
            world.AddAnimal(animalId, "");

        // Assert
        var spawnPoints = world.Animals.Select(a => a.Value.SpawnPoint);

        Assert.Multiple(
            () => Assert.Contains(spawnPoints, v => v == new GridCoords() { X = 12, Y = 12 }),
            () => Assert.Contains(spawnPoints, v => v == new GridCoords() { X = 12, Y = 36 }),
            () => Assert.Contains(spawnPoints, v => v == new GridCoords() { X = 36, Y = 12 }),
            () => Assert.Contains(spawnPoints, v => v == new GridCoords() { X = 36, Y = 36 }),
            () => Assert.Equal(49, world.Width),
            () => Assert.Equal(49, world.Height),
            () =>
                Assert.Equal(
                    new GridCoords() { X = 24, Y = 24 },
                    world.Zookeepers.First().Value.SpawnPoint
                )
        );
    }
}
