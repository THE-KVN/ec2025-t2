using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using NETCoreBot.Strategy;   // Contains Gorilla and its methods/fields.
using NETCoreBot.Models;     // Contains Animal, Cell, GameState, etc.
using NETCoreBot.Enums;      // Contains CellContent

namespace NETCoreBot.Tests
{
    public class ValidateOrFindTargetTests
    {
        [Fact]
        public void ValidateOrFindTarget_WithPersistentTarget_ReturnsPersistentTarget()
        {
            // Arrange
            // Set up a persistent target that is still valid.
            var persistentTarget = new Cell { X = 2, Y = 2, Content = CellContent.Pellet };
            Gorilla.PersistentTarget = persistentTarget;
            // For clarity, set a persistent path (which should be ignored in this case).
            Gorilla.PersistentPath = new List<GorillaNode> { new GorillaNode(0, 0, 0, 0, null) };

            var myAnimal = new Animal { X = 0, Y = 0 };
            // Create a GameState with cells that include the persistent target as a pellet.
            var cells = new List<Cell>
            {
                new Cell { X = 2, Y = 2, Content = CellContent.Pellet },
                new Cell { X = 1, Y = 1, Content = CellContent.Pellet }
            };
            var state = new GameState { Cells = cells };

            // Act
            var result = Gorilla.ValidateOrFindTarget(myAnimal, state);

            // Assert
            Assert.NotNull(result);
            // The returned cell should be the same as the persistent target.
            Assert.Equal(persistentTarget.X, result!.X);
            Assert.Equal(persistentTarget.Y, result.Y);
        }

        [Fact]
        public void ValidateOrFindTarget_NoPersistentTarget_ComputesClusterTarget()
        {
            // Arrange
            // Clear any existing persistent data.
            Gorilla.PersistentTarget = null;
            Gorilla.PersistentPath = null;
            // Set ALPHA to a known value (if needed for scoring in cluster computation).
            Gorilla.ALPHA = 0.7;

            var myAnimal = new Animal { X = 0, Y = 0 };

            // Create a collection of cells with a connected cluster of pellets around (1,1).
            var cells = new List<Cell>
            {
                new Cell { X = 1, Y = 1, Content = CellContent.Pellet },
                new Cell { X = 1, Y = 2, Content = CellContent.Pellet },
                new Cell { X = 2, Y = 1, Content = CellContent.Pellet },
                // Add some non-pellet cells.
                new Cell { X = 3, Y = 3, Content = CellContent.Empty }
            };
            var state = new GameState { Cells = cells };

            // Act
            var result = Gorilla.ValidateOrFindTarget(myAnimal, state);

            // Assert
            Assert.NotNull(result);
            // The returned target should be one of the pellet cells from the cluster.
            bool inCluster = cells.Any(c => c.Content == CellContent.Pellet &&
                                            c.X == result!.X && c.Y == result.Y);
            Assert.True(inCluster);
            // PersistentTarget should now be set to the computed target.
            Assert.NotNull(Gorilla.PersistentTarget);
            Assert.Equal(result!.X, Gorilla.PersistentTarget!.X);
            Assert.Equal(result.Y, Gorilla.PersistentTarget.Y);
            // PersistentPath should be cleared.
            Assert.Null(Gorilla.PersistentPath);
        }

        [Fact]
        public void ValidateOrFindTarget_NoPellets_ReturnsNull()
        {
            // Arrange
            Gorilla.PersistentTarget = null;
            Gorilla.PersistentPath = null;

            var myAnimal = new Animal { X = 0, Y = 0 };
            // Create a GameState with cells that do not have any pellets.
            var cells = new List<Cell>
            {
                new Cell { X = 1, Y = 1, Content = CellContent.Empty },
                new Cell { X = 2, Y = 2, Content = CellContent.Wall }
            };
            var state = new GameState { Cells = cells };

            // Act
            var result = Gorilla.ValidateOrFindTarget(myAnimal, state);

            // Assert
            Assert.Null(result);
            Assert.Null(Gorilla.PersistentTarget);
        }
    }
}
