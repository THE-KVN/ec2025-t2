using System;
using System.Collections.Generic;
using Xunit;
using NETCoreBot.Enums;     // Contains CellContent, BotAction, etc.
using NETCoreBot.Strategy;   // Contains Gorilla and ComputeZookeeperPenalty
using NETCoreBot.Models;     // Contains Animal, GameState, Zookeeper, etc.

namespace NETCoreBot.Tests
{
    public class ComputeZookeeperPenaltyTests
    {
        [Fact]
        public void ComputeZookeeperPenalty_SingleZookeeper_DistanceOne_ReturnsExpectedPenalty()
        {
            // Arrange
            // Set avoidance factor to 1.0 for predictable results.
            Gorilla.ZOOKEEPER_AVOIDANCE_FACTOR = 1.0;
            // Set animal score to 100; thus 100 * 0.1 = 10.
            var myAnimal = new Animal { Score = 100 };
            // Place a zookeeper at (5,5) and choose a test point (4,5)
            // Manhattan distance = |4-5| + |5-5| = 1.
            var zookeeper = new Zookeeper { X = 5, Y = 5 };
            var state = new GameState { Zookeepers = new List<Zookeeper> { zookeeper } };
            int nx = 4, ny = 5;
            // Expected penalty: 1.0 * (100*0.1)/1 = 10.
            int expectedPenalty = 10;

            // Act
            int penalty = Gorilla.ComputeZookeeperPenalty(nx, ny, myAnimal, state);

            // Assert
            Assert.Equal(expectedPenalty, penalty);
        }

        [Fact]
        public void ComputeZookeeperPenalty_SingleZookeeper_DistanceTwo_ReturnsExpectedPenalty()
        {
            // Arrange
            Gorilla.ZOOKEEPER_AVOIDANCE_FACTOR = 1.0;
            var myAnimal = new Animal { Score = 100 };
            // Place a zookeeper so that Manhattan distance is 2.
            // For test point (4,5), set zookeeper at (6,5):
            // Distance = |4-6| + |5-5| = 2.
            var zookeeper = new Zookeeper { X = 6, Y = 5 };
            var state = new GameState { Zookeepers = new List<Zookeeper> { zookeeper } };
            int nx = 4, ny = 5;
            // Expected penalty: 1.0 * (100*0.1)/2 = 10/2 = 5.
            int expectedPenalty = 5;

            // Act
            int penalty = Gorilla.ComputeZookeeperPenalty(nx, ny, myAnimal, state);

            // Assert
            Assert.Equal(expectedPenalty, penalty);
        }

        [Fact]
        public void ComputeZookeeperPenalty_MultipleZookeepers_ReturnsSumOfPenalties()
        {
            // Arrange
            Gorilla.ZOOKEEPER_AVOIDANCE_FACTOR = 1.0;
            var myAnimal = new Animal { Score = 100 };
            // Place two zookeepers: one at distance 1 and another at distance 2 from (4,5)
            var zk1 = new Zookeeper { X = 5, Y = 5 };  // distance = 1
            var zk2 = new Zookeeper { X = 6, Y = 5 };  // distance = 2
            var state = new GameState { Zookeepers = new List<Zookeeper> { zk1, zk2 } };
            int nx = 4, ny = 5;
            // Expected penalty:
            // For zk1: (1.0 * (100*0.1))/1 = 10.
            // For zk2: (1.0 * (100*0.1))/2 = 5.
            // Total = 10 + 5 = 15.
            int expectedPenalty = 15;

            // Act
            int penalty = Gorilla.ComputeZookeeperPenalty(nx, ny, myAnimal, state);

            // Assert
            Assert.Equal(expectedPenalty, penalty);
        }
    }
}
