using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using NETCoreBot.Strategy;   // Contains Gorilla and GorillaNode
using NETCoreBot.Models;     // Contains Animal, Cell, GameState, Zookeeper, etc.
using NETCoreBot.Enums;      // Contains CellContent

namespace NETCoreBot.Tests
{
    public class IsPathStillValidTests
    {
        [Fact]
        public void IsPathStillValid_NullPath_ReturnsFalse()
        {
            // Arrange
            Animal myAnimal = new Animal { X = 0, Y = 0 };
            List<GorillaNode>? path = null;
            List<Cell> allCells = new List<Cell>(); // empty grid
            GameState state = new GameState { Cells = allCells, Zookeepers = new List<Zookeeper>() };

            // Act
            bool result = Gorilla.IsPathStillValid(path, myAnimal, allCells, state);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsPathStillValid_EmptyPath_ReturnsFalse()
        {
            // Arrange
            Animal myAnimal = new Animal { X = 0, Y = 0 };
            List<GorillaNode> path = new List<GorillaNode>(); // empty list
            List<Cell> allCells = new List<Cell>();
            GameState state = new GameState { Cells = allCells, Zookeepers = new List<Zookeeper>() };

            // Act
            bool result = Gorilla.IsPathStillValid(path, myAnimal, allCells, state);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsPathStillValid_AnimalNotAtPathStart_ReturnsFalse()
        {
            // Arrange
            // Animal is at (1,1) but the path starts at (0,0).
            Animal myAnimal = new Animal { X = 1, Y = 1 };
            var node1 = new GorillaNode(0, 0, 0, 0, null);
            var node2 = new GorillaNode(0, 1, 1, 0, node1);
            List<GorillaNode> path = new List<GorillaNode> { node1, node2 };

            // Build grid containing these nodes.
            List<Cell> allCells = new List<Cell>
            {
                new Cell { X = 0, Y = 0, Content = CellContent.Pellet },
                new Cell { X = 0, Y = 1, Content = CellContent.Pellet },
            };

            GameState state = new GameState { Cells = allCells, Zookeepers = new List<Zookeeper>() };

            // Act
            bool result = Gorilla.IsPathStillValid(path, myAnimal, allCells, state);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsPathStillValid_ValidPath_ReturnsTrue()
        {
            // Arrange
            // Animal starts at (0,0); path is (0,0) -> (0,1) -> (0,2)
            Animal myAnimal = new Animal { X = 0, Y = 0 };
            var node1 = new GorillaNode(0, 0, 0, 0, null);
            var node2 = new GorillaNode(0, 1, 1, 0, node1);
            var node3 = new GorillaNode(0, 2, 2, 0, node2);
            List<GorillaNode> path = new List<GorillaNode> { node1, node2, node3 };

            // Build grid: all these cells are pellets.
            List<Cell> allCells = new List<Cell>
            {
                new Cell { X = 0, Y = 0, Content = CellContent.Pellet },
                new Cell { X = 0, Y = 1, Content = CellContent.Pellet },
                new Cell { X = 0, Y = 2, Content = CellContent.Pellet },
            };

            // Game state with no zookeepers; penalty will be zero.
            GameState state = new GameState { Cells = allCells, Zookeepers = new List<Zookeeper>() };

            // Set a high danger threshold so that the penalty check passes.
            Gorilla.DANGER_THRESHOLD = 100;

            // Act
            bool result = Gorilla.IsPathStillValid(path, myAnimal, allCells, state);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsPathStillValid_InvalidCellInPath_ReturnsFalse()
        {
            // Arrange
            // Animal starts at (0,0); path is (0,0) -> (0,1)
            Animal myAnimal = new Animal { X = 0, Y = 0 };
            var node1 = new GorillaNode(0, 0, 0, 0, null);
            var node2 = new GorillaNode(0, 1, 1, 0, node1);
            List<GorillaNode> path = new List<GorillaNode> { node1, node2 };

            // Build grid: (0,1) is invalid (e.g. a wall).
            List<Cell> allCells = new List<Cell>
            {
                new Cell { X = 0, Y = 0, Content = CellContent.Pellet },
                new Cell { X = 0, Y = 1, Content = CellContent.Wall },
            };

            GameState state = new GameState { Cells = allCells, Zookeepers = new List<Zookeeper>() };

            // Act
            bool result = Gorilla.IsPathStillValid(path, myAnimal, allCells, state);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsPathStillValid_DangerExceedsThreshold_ReturnsFalse()
        {
            // Arrange
            // Animal with a score set to 100 (for predictable penalty calculation).
            Animal myAnimal = new Animal { X = 0, Y = 0, Score = 100 };
            // Path: (0,0) -> (0,1)
            var node1 = new GorillaNode(0, 0, 0, 0, null);
            var node2 = new GorillaNode(0, 1, 1, 0, node1);
            List<GorillaNode> path = new List<GorillaNode> { node1, node2 };

            // Build grid: both (0,0) and (0,1) are pellets.
            List<Cell> allCells = new List<Cell>
            {
                new Cell { X = 0, Y = 0, Content = CellContent.Pellet },
                new Cell { X = 0, Y = 1, Content = CellContent.Pellet },
            };

            // Set up a game state with a zookeeper placed at (0,0), causing a high penalty.
            var zookeeper = new Zookeeper { X = 0, Y = 0 };
            GameState state = new GameState { Cells = allCells, Zookeepers = new List<Zookeeper> { zookeeper } };

            // Set the danger threshold low so that the computed penalty exceeds it.
            // With myAnimal.Score = 100 and assuming ZOOKEEPER_AVOIDANCE_FACTOR = 1.0,
            // penalty at (0,0) = (1.0 * (100 * 0.1)) = 10, which is > threshold 5.
            Gorilla.DANGER_THRESHOLD = 5;
            Gorilla.ZOOKEEPER_AVOIDANCE_FACTOR = 1.0;

            // Act
            bool result = Gorilla.IsPathStillValid(path, myAnimal, allCells, state);

            // Assert
            Assert.False(result);
        }
    }
}
