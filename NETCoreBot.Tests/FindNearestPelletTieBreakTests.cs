using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using NETCoreBot.Strategy;  // Ensure this namespace contains Gorilla
using NETCoreBot.Enums;     // Ensure BotAction is accessible
using NETCoreBot.Models;    // Ensure Cell & CellContent are defined

namespace NETCoreBot.Tests
{
    public class FindNearestPelletTieBreakTests
    {
        [Fact]
        public void FindNearestPelletTieBreak_NoPellets_ReturnsNull()
        {
            // Arrange
            int startX = 5, startY = 5;
            // Create a list of cells with no pellet content
            var cells = new List<Cell>
            {
                new Cell { X = 3, Y = 3, Content = CellContent.Empty },
                new Cell { X = 6, Y = 6, Content = CellContent.Wall }
            };
            BotAction? lastMove = null;

            // Act
            var result = Gorilla.FindNearestPelletTieBreak(startX, startY, cells, lastMove);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void FindNearestPelletTieBreak_SinglePellet_ReturnsThatPellet()
        {
            // Arrange
            int startX = 5, startY = 5;
            var pelletCell = new Cell { X = 4, Y = 5, Content = CellContent.Pellet };
            var cells = new List<Cell>
            {
                pelletCell,
                new Cell { X = 6, Y = 6, Content = CellContent.Empty }
            };
            // When lastMove is null, alignment is ignored, so the nearest pellet is returned.
            BotAction? lastMove = null;

            // Act
            var result = Gorilla.FindNearestPelletTieBreak(startX, startY, cells, lastMove);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(pelletCell.X, result!.X);
            Assert.Equal(pelletCell.Y, result.Y);
        }

        [Fact]
        public void FindNearestPelletTieBreak_MultiplePellets_AlignedPreference_ReturnsAlignedPellet()
        {
            // Arrange
            int startX = 5, startY = 5;
            // Two pellets at the same Manhattan distance (distance 1)
            // Pellet A: at (4,5) and Pellet B: at (5,4)
            var pelletA = new Cell { X = 4, Y = 5, Content = CellContent.Pellet };
            var pelletB = new Cell { X = 5, Y = 4, Content = CellContent.Pellet };

            // Arrange the cells in a specific order
            var cells = new List<Cell>
            {
                pelletA,
                pelletB,
                new Cell { X = 7, Y = 7, Content = CellContent.Empty }
            };

            // For lastMove Up, IsAligned returns true when pellet.Y < myY.
            // PelletB at (5,4) satisfies this, whereas pelletA does not.
            BotAction? lastMove = BotAction.Up;

            // Act
            var result = Gorilla.FindNearestPelletTieBreak(startX, startY, cells, lastMove);

            // Assert
            Assert.NotNull(result);
            // Expect pelletB to be returned because it's aligned with the Up direction.
            Assert.Equal(pelletB.X, result!.X);
            Assert.Equal(pelletB.Y, result.Y);
        }

        [Fact]
        public void FindNearestPelletTieBreak_MultiplePellets_NoAlignment_ReturnsFirstClosest()
        {
            // Arrange
            int startX = 5, startY = 5;
            // Two pellets at the same Manhattan distance (distance 1)
            // Pellet A: at (4,5) and Pellet B: at (5,4)
            var pelletA = new Cell { X = 4, Y = 5, Content = CellContent.Pellet };
            var pelletB = new Cell { X = 5, Y = 4, Content = CellContent.Pellet };

            // Order in the list determines the fallback choice.
            var cells = new List<Cell>
            {
                pelletA,
                pelletB,
                new Cell { X = 7, Y = 7, Content = CellContent.Empty }
            };

            // Choose a lastMove that does not align with either pellet.
            // For example, BotAction.Right requires pellet.X > startX.
            BotAction? lastMove = BotAction.Right;

            // Act
            var result = Gorilla.FindNearestPelletTieBreak(startX, startY, cells, lastMove);

            // Assert
            Assert.NotNull(result);
            // Neither pellet is aligned to the Right from (5,5) so we expect the first closest (pelletA) to be returned.
            Assert.Equal(pelletA.X, result!.X);
            Assert.Equal(pelletA.Y, result.Y);
        }
    }
}
