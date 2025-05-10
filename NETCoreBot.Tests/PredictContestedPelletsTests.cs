using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using NETCoreBot.Models;     // Assumes your models are here
using NETCoreBot.Enums;      // Assumes your enums (e.g., CellContent) are here
using NETCoreBot.Strategy;   // For Gorilla class

namespace NETCoreBot.Tests
{
    public class PredictContestedPelletsTests
    {
        // Helper: creates a grid of cells with the given default content.
        private List<Cell> CreateGrid(int width, int height, CellContent defaultContent)
        {
            var cells = new List<Cell>();
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    cells.Add(new Cell { X = x, Y = y, Content = defaultContent });
                }
            }
            return cells;
        }

        [Fact]
        public void PredictContestedPellets_NoOpponents_ReturnsEmptySet()
        {
            // Arrange: only myAnimal in the game state.
            var myAnimal = new Animal { Id = Guid.NewGuid(), X = 1, Y = 1, Score = 0 };
            var cells = CreateGrid(3, 3, CellContent.Empty);
            var state = new GameState
            {
                Animals = new List<Animal> { myAnimal },
                Cells = cells,
                Tick = 1
            };



            // Act
            var contested = Gorilla.PredictContestedPellets(myAnimal, state);

            // Assert: no opponents => empty contested set.
            Assert.Empty(contested);
        }

        [Fact]
        public void PredictContestedPellets_OneOpponent_WithValidPath_ReturnsContestedSet()
        {
            // Arrange:
            var myAnimal = new Animal { Id = Guid.NewGuid(), X = 1, Y = 1, Score = 0 };
            var opponent = new Animal { Id = Guid.NewGuid(), X = 0, Y = 0, Score = 0 };

            // Create a simple 3x3 grid.
            // Set all cells to Empty except one pellet cell at (2,2) which is the target.
            var cells = CreateGrid(3, 3, CellContent.Empty);
            // Ensure the pellet cell exists at (2,2)
            var pelletCell = cells.First(c => c.X == 2 && c.Y == 2);
            pelletCell.Content = CellContent.Pellet;

            // Assemble game state with myAnimal and one opponent.
            var state = new GameState
            {
                Animals = new List<Animal> { myAnimal, opponent },
                Cells = cells,
                Tick = 1
            };

            Gorilla.MapWidth = 3;
            Gorilla.MapHeight = 3;


            // Act
            var contested = Gorilla.PredictContestedPellets(myAnimal, state);

            // Assert:
            // With one opponent and a valid pellet/path, the contested set should not be empty.
            Assert.NotEmpty(contested);
            // Expect that the pellet cell (target) is included in the contested path.
            Assert.Contains((2, 2), contested);
        }

        [Fact]
        public void PredictContestedPellets_MultipleOpponents_AggregatesContestedPaths()
        {
            // Arrange:
            var myAnimal = new Animal { Id = Guid.NewGuid(), X = 1, Y = 1, Score = 0 };
            var opponent1 = new Animal { Id = Guid.NewGuid(), X = 0, Y = 0, Score = 0 };
            var opponent2 = new Animal { Id = Guid.NewGuid(), X = 2, Y = 0, Score = 0 };

            // Create a 3x3 grid.
            var cells = CreateGrid(3, 3, CellContent.Empty);
            // Set two pellet cells:
            // Opponent1 should select pellet at (2,2) and opponent2 should select pellet at (0,2)
            cells.First(c => c.X == 2 && c.Y == 2).Content = CellContent.Pellet;
            cells.First(c => c.X == 0 && c.Y == 2).Content = CellContent.Pellet;

            var state = new GameState
            {
                Animals = new List<Animal> { myAnimal, opponent1, opponent2 },
                Cells = cells,
                Tick = 1
            };

            Gorilla.MapWidth = 3;
            Gorilla.MapHeight = 3;


            // Act
            var contested = Gorilla.PredictContestedPellets(myAnimal, state);

            // Assert:
            Assert.NotEmpty(contested);
            // We expect contested cells from both opponents’ predicted paths.
            // For example, one path should lead to (2,2) and the other to (0,2).
            Assert.Contains((2, 2), contested);
            Assert.Contains((0, 2), contested);
        }
    }
}
