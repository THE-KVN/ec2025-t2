using System;
using System.Collections.Generic;
using System.Linq;
using NSubstitute;
using Xunit;
using Zooscape.Application.Models;
using Zooscape.Application.Services;
using Zooscape.Domain.Interfaces;

namespace ZooscapeTests;

public class WinConditionsTests()
{
    [Fact]
    public void HighestScore_ShouldReturnAnimalWithHighestScore()
    {
        var animal1 = Substitute.For<IAnimal>();
        animal1.Score.Returns(10);

        var animal2 = Substitute.For<IAnimal>();
        animal2.Score.Returns(20);

        var animal3 = Substitute.For<IAnimal>();
        animal3.Score.Returns(15);

        var animals = new List<IAnimal> { animal1, animal2, animal3 };
        var highestScoreCondition = new HighestScore();
        var winners = highestScoreCondition.GetWinners(animals);

        Assert.Single(winners);
        Assert.Equal(20, winners.First().Score);
    }

    [Fact]
    public void LeastDeaths_ShouldReturnAnimalWithLeastDeaths()
    {
        var animal1 = Substitute.For<IAnimal>();
        animal1.CapturedCounter.Returns(5);

        var animal2 = Substitute.For<IAnimal>();
        animal2.CapturedCounter.Returns(2);

        var animal3 = Substitute.For<IAnimal>();
        animal3.CapturedCounter.Returns(3);

        var animals = new List<IAnimal> { animal1, animal2, animal3 };
        var leastDeathsCondition = new LeastDeaths();
        var winners = leastDeathsCondition.GetWinners(animals);

        Assert.Single(winners);
        Assert.Equal(2, winners.First().CapturedCounter);
    }

    [Fact]
    public void LeastTimeInCage_ShouldReturnAnimalWithFewestTicksOnSpawn()
    {
        var animal1 = Substitute.For<IAnimal>();
        animal1.TicksOnSpawn.Returns(100);

        var animal2 = Substitute.For<IAnimal>();
        animal2.TicksOnSpawn.Returns(50);

        var animal3 = Substitute.For<IAnimal>();
        animal3.TicksOnSpawn.Returns(75);

        var animals = new List<IAnimal> { animal1, animal2, animal3 };
        var leastTimeInCageCondition = new LeastTimeInCage();
        var winners = leastTimeInCageCondition.GetWinners(animals);

        Assert.Single(winners);
        Assert.Equal(50, winners.First().TicksOnSpawn);
    }

    [Fact]
    public void FarthestTraveled_ShouldReturnAnimalWithFarthestDistanceCovered()
    {
        var animal1 = Substitute.For<IAnimal>();
        animal1.DistanceCovered.Returns(100);

        var animal2 = Substitute.For<IAnimal>();
        animal2.DistanceCovered.Returns(250);

        var animal3 = Substitute.For<IAnimal>();
        animal3.DistanceCovered.Returns(150);

        var animals = new List<IAnimal> { animal1, animal2, animal3 };
        var farthestTraveledCondition = new FarthestTraveled();
        var winners = farthestTraveledCondition.GetWinners(animals);

        Assert.Single(winners);
        Assert.Equal(250, winners.First().DistanceCovered);
    }

    [Fact]
    public void FirstCommand_ShouldReturnAnimalWithEarliestCommand()
    {
        var animal1 = Substitute.For<IAnimal>();
        animal1.FirstCommandTimeStamp.Returns(new DateTime(1000));

        var animal2 = Substitute.For<IAnimal>();
        animal2.FirstCommandTimeStamp.Returns(new DateTime(2000));

        var animal3 = Substitute.For<IAnimal>();
        animal3.FirstCommandTimeStamp.Returns(new DateTime(3000));

        var animals = new List<IAnimal> { animal1, animal2, animal3 };
        var firstCommandCondition = new FirstCommand();
        var winners = firstCommandCondition.GetWinners(animals);

        Assert.Single(winners);
        Assert.Equal(new DateTime(1000), winners.First().FirstCommandTimeStamp);
    }

    [Fact]
    public void OrderedWinners_ShouldReturnAnimalsInCorrectOrderBasedOnAllWinConditions()
    {
        var animal1 = Substitute.For<IAnimal>();
        animal1.Score.Returns(100);
        animal1.CapturedCounter.Returns(5);
        animal1.TicksOnSpawn.Returns(100);
        animal1.DistanceCovered.Returns(200);
        animal1.FirstCommandTimeStamp.Returns(new DateTime(1000));

        var animal2 = Substitute.For<IAnimal>();
        animal2.Score.Returns(200);
        animal2.CapturedCounter.Returns(3);
        animal2.TicksOnSpawn.Returns(100);
        animal2.DistanceCovered.Returns(250);
        animal2.FirstCommandTimeStamp.Returns(new DateTime(2000));

        var animal3 = Substitute.For<IAnimal>();
        animal3.Score.Returns(200);
        animal3.CapturedCounter.Returns(3);
        animal3.TicksOnSpawn.Returns(75);
        animal3.DistanceCovered.Returns(150);
        animal3.FirstCommandTimeStamp.Returns(new DateTime(4000));

        var animal4 = Substitute.For<IAnimal>();
        animal4.Score.Returns(200);
        animal4.CapturedCounter.Returns(3);
        animal4.TicksOnSpawn.Returns(75);
        animal4.DistanceCovered.Returns(150);
        animal4.FirstCommandTimeStamp.Returns(new DateTime(3000));

        var animals = new List<IAnimal> { animal1, animal2, animal3, animal4 };

        var scoreService = new ScoreService();

        var orderedWinners = scoreService.OrderedWinners(animals);

        Assert.Equal(animal1, orderedWinners[3]);
        Assert.Equal(animal2, orderedWinners[2]);
        Assert.Equal(animal3, orderedWinners[1]);
        Assert.Equal(animal4, orderedWinners[0]);
    }

    [Fact]
    public void DetermineWinner_ShouldThrowInvalidOperationException_WhenNoSingleWinnerIsFound()
    {
        var animal1 = Substitute.For<IAnimal>();
        animal1.Score.Returns(10);
        animal1.CapturedCounter.Returns(3);
        animal1.TicksOnSpawn.Returns(100);
        animal1.DistanceCovered.Returns(200);
        animal1.FirstCommandTimeStamp.Returns(new DateTime(1000));

        var animal2 = Substitute.For<IAnimal>();
        animal2.Score.Returns(10);
        animal2.CapturedCounter.Returns(3);
        animal2.TicksOnSpawn.Returns(100);
        animal2.DistanceCovered.Returns(200);
        animal2.FirstCommandTimeStamp.Returns(new DateTime(1000));

        var animals = new List<IAnimal> { animal1, animal2 };
        var scoreService = new ScoreService();

        var exception = Assert.Throws<InvalidOperationException>(
            () => scoreService.OrderedWinners(animals)
        );
        Assert.Equal(
            "There is no single winner after all win conditons have been applied",
            exception.Message
        );
    }
}
