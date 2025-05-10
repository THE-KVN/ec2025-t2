using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using NETCoreBot.Strategy;   // Contains Gorilla and GorillaNode
using NETCoreBot.Models;     // Contains Animal, GameState, Cell, BotCommand, Zookeeper, etc.
using NETCoreBot.Enums;      // Contains CellContent, BotAction

namespace NETCoreBot.Tests
{
    public class CollectPelletsTests
    {
        // Helper method to create a complete grid for a 2x3 world.
        private List<Cell> BuildGrid()
        {
            var cells = new List<Cell>();
            // Create grid for x = 0 to 1, y = 0 to 2.
            for (int x = 0; x < 2; x++)
            {
                for (int y = 0; y < 3; y++)
                {
                    // Mark cell (1,2) as Pellet (target), all others as Empty.
                    cells.Add(new Cell
                    {
                        X = x,
                        Y = y,
                        Content = (x == 1 && y == 2) ? CellContent.Pellet : CellContent.Empty
                    });
                }
            }
            return cells;
        }

        [Fact]
        public void CollectPellets_ValidScenario_ReturnsBotCommandWithDownAction()
        {
            // Arrange

            // Reset static fields to ensure clean state.
            Gorilla.lastTickCommandIssued = 0;
            Gorilla.PersistentPath = null;
            Gorilla.PersistentTarget = null;
            // Clear visited counts and recent positions.
            Gorilla.VisitedCounts.Clear();
            while (Gorilla.RecentPositions.Count > 0)
                Gorilla.RecentPositions.Dequeue();
            Gorilla.stuckCounter = 0;
            Gorilla.LastMove = null;

            // Create a unique animal ID.
            Guid animalId = Guid.NewGuid();
            // Animal is positioned at (1,1) and its spawn is set elsewhere (e.g. (0,0)) so it's not in spawn.
            var myAnimal = new Animal
            {
                Id = animalId,
                X = 1,
                Y = 1,
                SpawnX = 0,
                SpawnY = 0,
                Score = 0
            };

            // Create a complete grid. Our grid covers coordinates (0,0) to (1,2).
            List<Cell> cells = BuildGrid();

            // Create a GameState.
            var gameState = new GameState
            {
                Tick = 1,  // Current tick is 1 (different from lastTickCommandIssued which is 0)
                Cells = cells,
                // Animals: ensure our animal is in the state.
                Animals = new List<Animal> { myAnimal },
                // No zookeepers for this test.
                Zookeepers = new List<Zookeeper>()
            };

            // Act
            BotCommand? command = Gorilla.CollectPellets(gameState, animalId);

            // Assert
            Assert.NotNull(command);
            // Expected behavior: with myAnimal at (1,1) and target determined as the pellet at (1,2),
            // the computed move from (1,1) to (1,2) is Down.
            Assert.Equal(BotAction.Down, command!.Action);
            // Additionally, lastTickCommandIssued should now be updated to the current tick.
            Assert.Equal(gameState.Tick, Gorilla.lastTickCommandIssued);
            // And LastMove should be updated to Down.
            Assert.Equal(BotAction.Down, Gorilla.LastMove);
        }
    }
}
