using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using NETCoreBot.Strategy;   // Contains Gorilla and GorillaNode
using NETCoreBot.Models;     // Contains Animal, Cell, GameState, Zookeeper, etc.
using NETCoreBot.Enums;      // Contains CellContent, BotAction, etc.

namespace NETCoreBot.Tests
{
    public class ValidateOrComputePathTests
    {
        // Helper method to build a simple 3x3 grid with all cells set as Pellet.
        private Dictionary<(int, int), CellContent> BuildSimpleGrid(int width = 3, int height = 3)
        {
            var grid = new Dictionary<(int, int), CellContent>();
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    grid[(x, y)] = CellContent.Pellet;
                }
            }
            return grid;
        }

        [Fact]
        public void ValidateOrComputePath_WithPersistentPath_ReturnsPersistentPath()
        {
            // Arrange
            // Reset any static persistent path.
            Gorilla.PersistentPath = new List<GorillaNode>
            {
                new GorillaNode(0, 0, 0, 0, null),
                new GorillaNode(0, 1, 1, 0, null),
                new GorillaNode(0, 2, 2, 0, null)
            };
            var persistentPath = Gorilla.PersistentPath;

            var myAnimal = new Animal { X = 0, Y = 0 };
            var target = new Cell { X = 0, Y = 2, Content = CellContent.Pellet };
            var grid = BuildSimpleGrid();
            // Dummy game state with no zookeepers.
            var state = new GameState { Zookeepers = new List<Zookeeper>() };

            // Act
            var result = Gorilla.ValidateOrComputePath(myAnimal, target, grid, state);

            // Assert
            // Since a persistent path is already set, the same instance should be returned.
            Assert.NotNull(result);
            Assert.Same(persistentPath, result);
        }

        [Fact]
        public void ValidateOrComputePath_NoPersistentPath_ComputesNewPathAndStoresIt()
        {
            // Arrange
            // Clear any persistent path.
            Gorilla.PersistentPath = null;
            var myAnimal = new Animal { X = 0, Y = 0 };
            var target = new Cell { X = 2, Y = 2, Content = CellContent.Pellet };
            // Create a simple 3x3 grid where every cell is a pellet.
            var grid = BuildSimpleGrid();
            // Create a dummy game state with no zookeepers.
            var state = new GameState { Zookeepers = new List<Zookeeper>() };

            // Act
            var result = Gorilla.ValidateOrComputePath(myAnimal, target, grid, state);

            // Assert
            // A new path should have been computed.
            Assert.NotNull(result);
            // The returned path should be stored in PersistentPath.
            Assert.Same(result, Gorilla.PersistentPath);
            // Verify that the path starts at the animal's position.
            Assert.Equal(myAnimal.X, result![0].X);
            Assert.Equal(myAnimal.Y, result[0].Y);
            // Verify that the final node in the path reaches the target.
            var lastNode = result.Last();
            Assert.Equal(target.X, lastNode.X);
            Assert.Equal(target.Y, lastNode.Y);
        }
    }
}
