using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using NETCoreBot.Strategy;  // Adjust namespace as needed
using NETCoreBot.Models;    // Contains Cell, Animal, etc.
using NETCoreBot.Enums;     // Contains CellContent and BotAction

namespace NETCoreBot.Tests
{
    public class FindPelletInLargestConnectedClusterWeightedTests
    {
        [Fact]
        public void FindPelletInLargestConnectedClusterWeighted_NoPellets_ReturnsNull()
        {
            // Arrange
            var animal = new Animal { X = 0, Y = 0 };
            // Grid has no pellets
            var cells = new List<Cell>
            {
                new Cell { X = 1, Y = 1, Content = CellContent.Empty },
                new Cell { X = 2, Y = 2, Content = CellContent.Wall }
            };
            double alpha = 0.5;

            // Act
            var result = Gorilla.FindPelletInLargestConnectedClusterWeighted(animal, cells, alpha);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void FindPelletInLargestConnectedClusterWeighted_SinglePellet_ReturnsThatPellet()
        {
            // Arrange
            var animal = new Animal { X = 0, Y = 0 };
            var pelletCell = new Cell { X = 3, Y = 3, Content = CellContent.Pellet };
            var cells = new List<Cell>
            {
                pelletCell,
                new Cell { X = 1, Y = 1, Content = CellContent.Empty },
                new Cell { X = 2, Y = 2, Content = CellContent.Wall }
            };
            double alpha = 0.5;

            // Act
            var result = Gorilla.FindPelletInLargestConnectedClusterWeighted(animal, cells, alpha);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(pelletCell.X, result!.X);
            Assert.Equal(pelletCell.Y, result.Y);
        }

        [Fact]
        public void FindPelletInLargestConnectedClusterWeighted_MultipleClusters_ReturnsBestClusterPellet()
        {
            // Arrange
            var animal = new Animal { X = 0, Y = 0 };

            // Cluster 1: Three pellets close to the animal
            var pelletA = new Cell { X = 1, Y = 1, Content = CellContent.Pellet };
            var pelletB = new Cell { X = 1, Y = 2, Content = CellContent.Pellet };
            var pelletC = new Cell { X = 2, Y = 1, Content = CellContent.Pellet };

            // Cluster 2: Two pellets far from the animal
            var pelletD = new Cell { X = 10, Y = 10, Content = CellContent.Pellet };
            var pelletE = new Cell { X = 10, Y = 11, Content = CellContent.Pellet };

            // Include an extra non-pellet cell for variety
            var emptyCell = new Cell { X = 5, Y = 5, Content = CellContent.Empty };

            var cells = new List<Cell>
            {
                pelletA, pelletB, pelletC, pelletD, pelletE, emptyCell
            };

            // With alpha = 0.5, the score for cluster 1 is:
            // score = 3 - 0.5 * (Manhattan distance from (0,0) to (1,1)=2) = 3 - 1 = 2.
            // For cluster 2:
            // score = 2 - 0.5 * (Manhattan distance from (0,0) to (10,10)=20) = 2 - 10 = -8.
            // Therefore, cluster 1 should be selected and among its pellets,
            // pelletA at (1,1) is the closest to the animal.
            double alpha = 0.5;

            // Act
            var result = Gorilla.FindPelletInLargestConnectedClusterWeighted(animal, cells, alpha);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(pelletA.X, result!.X);
            Assert.Equal(pelletA.Y, result.Y);
        }
    }
}
