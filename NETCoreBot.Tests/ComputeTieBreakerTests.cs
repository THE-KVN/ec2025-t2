using System;
using System.Collections.Generic;
using Xunit;
using NETCoreBot.Models;     // Ensure that GameState, Zookeeper, etc. are defined here.
using NETCoreBot.Strategy;   // Ensure that Gorilla.ComputeTieBreaker is defined here.

namespace NETCoreBot.Tests
{
    public class ComputeTieBreakerTests
    {
        [Fact]
        public void ComputeTieBreaker_NoZookeepers_ReturnsZero()
        {
            // Arrange
            var state = new GameState { Zookeepers = null };
            int testX = 5, testY = 5;

            // Act
            int result = Gorilla.ComputeTieBreaker(testX, testY, state);

            // Assert: if there are no zookeepers, the method should return 0.
            Assert.Equal(0, result);
        }

        [Fact]
        public void ComputeTieBreaker_EmptyZookeepers_ReturnsZero()
        {
            // Arrange
            // Initialize Zookeepers to an empty list.
            var state = new GameState { Zookeepers = new List<Zookeeper>() };
            int testX = 5, testY = 5;

            // Act
            int result = Gorilla.ComputeTieBreaker(testX, testY, state);

            // Assert: if the list is empty, the method should return 0.
            Assert.Equal(0, result);
        }

        [Fact]
        public void ComputeTieBreaker_SingleZookeeper_ReturnsNegativeManhattanDistance()
        {
            // Arrange
            var state = new GameState { Zookeepers = new List<Zookeeper>() };
            // Place one zookeeper at (8,8)
            state.Zookeepers.Add(new Zookeeper { X = 8, Y = 8 });
            int testX = 5, testY = 5;
            // Manhattan distance: |5 - 8| + |5 - 8| = 3 + 3 = 6.
            // Expected tie-breaker is the negative of that: -6.
            int expected = -6;

            // Act
            int result = Gorilla.ComputeTieBreaker(testX, testY, state);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void ComputeTieBreaker_MultipleZookeepers_ReturnsNegativeMinDistance()
        {
            // Arrange
            var state = new GameState { Zookeepers = new List<Zookeeper>() };
            // Add two zookeepers:
            // One at (8,8) → distance from (5,5): |5-8|+|5-8| = 6.
            // Another at (4,6) → distance from (5,5): |5-4|+|5-6| = 1+1 = 2.
            state.Zookeepers.Add(new Zookeeper { X = 8, Y = 8 });
            state.Zookeepers.Add(new Zookeeper { X = 4, Y = 6 });
            int testX = 5, testY = 5;
            // Expected tie-breaker is the negative of the smallest distance, i.e. -2.
            int expected = -2;

            // Act
            int result = Gorilla.ComputeTieBreaker(testX, testY, state);

            // Assert
            Assert.Equal(expected, result);
        }
    }
}
