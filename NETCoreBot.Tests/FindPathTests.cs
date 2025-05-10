using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using NETCoreBot.Strategy;   // Contains Gorilla and GorillaNode
using NETCoreBot.Models;     // Contains Animal, GameState, Zookeeper, Cell
using NETCoreBot.Enums;      // Contains CellContent and BotAction

namespace NETCoreBot.Tests
{
    public class FindPathTests
    {
        [Fact]
        public void FindPath_ReturnsValidPath_InSimple3x3Grid()
        {
            // Arrange
            // Set up map dimensions and portal settings.
            Gorilla.MapWidth = 3;
            Gorilla.MapHeight = 3;
            Gorilla.PortalLeft = true;
            Gorilla.PortalRight = true;
            Gorilla.PortalUp = true;
            Gorilla.PortalDown = true;
            // Clear visited counts.
            Gorilla.VisitedCounts.Clear();

            // Create a 3x3 grid where every cell is a Pellet.
            var grid = new Dictionary<(int, int), CellContent>();
            for (int x = 0; x < 3; x++)
            {
                for (int y = 0; y < 3; y++)
                {
                    grid[(x, y)] = CellContent.Pellet;
                }
            }

            // Create a dummy animal.
            var myAnimal = new Animal { Score = 100 };

            // Create a dummy game state with no zookeepers.
            var state = new GameState
            {
                Zookeepers = new List<Zookeeper>()
            };

            // Define start and target coordinates.
            int startX = 0, startY = 0;
            int targetX = 2, targetY = 2;

            // Act
            var path = Gorilla.FindPath(startX, startY, targetX, targetY, grid, myAnimal, state);

            // Assert
            Assert.NotNull(path);
            // The first node must be at the start.
            Assert.Equal(startX, path![0].X);
            Assert.Equal(startY, path[0].Y);
            // The last node must be at the target.
            var lastNode = path.Last();
            Assert.Equal(targetX, lastNode.X);
            Assert.Equal(targetY, lastNode.Y);

            // Additionally, verify that each consecutive node is adjacent according to the grid with portal wrapping.
            // That is, for each pair of consecutive nodes, either:
            // - They are one cell apart (dx == 1, dy == 0 or dx == 0, dy == 1), OR
            // - They wrap around: dx == MapWidth - 1 with dy == 0, or dy == MapHeight - 1 with dx == 0.
            for (int i = 1; i < path.Count; i++)
            {
                int dx = Math.Abs(path[i].X - path[i - 1].X);
                int dy = Math.Abs(path[i].Y - path[i - 1].Y);

                bool normalMove = (dx == 1 && dy == 0) || (dx == 0 && dy == 1);
                bool wrappedHorizontal = (dx == Gorilla.MapWidth - 1 && dy == 0);
                bool wrappedVertical = (dy == Gorilla.MapHeight - 1 && dx == 0);

                Assert.True(normalMove || wrappedHorizontal || wrappedVertical,
                    $"Nodes at index {i - 1} and {i} are not adjacent: ({path[i - 1].X},{path[i - 1].Y}) -> ({path[i].X},{path[i].Y})");
            }
        }
    }
}
