using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using NETCoreBot.Strategy;   // Contains Gorilla and GorillaNode
using NETCoreBot.Models;     // Contains Animal, GameState, Zookeeper, Cell
using NETCoreBot.Enums;      // Contains CellContent and BotAction

namespace NETCoreBot.Tests
{
    public class GetNeighborsPortalTests
    {
        [Fact]
        public void GetNeighborsPortal_ReturnsExpectedNeighbors()
        {
            // Arrange

            // Set the grid dimensions and portal flags.
            // (Assuming these fields are accessible in the test context.)
            Gorilla.MapWidth = 3;
            Gorilla.MapHeight = 3;
            Gorilla.PortalLeft = true;
            Gorilla.PortalRight = true;
            Gorilla.PortalUp = true;
            Gorilla.PortalDown = true;

            // Clear any visited counts.
            Gorilla.VisitedCounts.Clear();

            // Create a simple 3x3 grid: every cell is a Pellet.
            var grid = new Dictionary<(int, int), CellContent>();
            for (int x = 0; x < 3; x++)
            {
                for (int y = 0; y < 3; y++)
                {
                    grid[(x, y)] = CellContent.Pellet;
                }
            }

            // Create a starting node at the center of the grid: (1,1) with G=0.
            var current = new GorillaNode(1, 1, 0, 0, null);

            // Define target coordinates for the heuristic calculation.
            // Here we choose target = (1,1), so for neighbors the Manhattan distance is 1.
            int targetX = 1, targetY = 1;

            // Setup a dummy animal and game state.
            // Ensure that there are no zookeepers (so ComputeZookeeperPenalty returns 0).
            var myAnimal = new Animal { Score = 100 };
            var state = new GameState { Zookeepers = new List<Zookeeper>() };

            // Act
            // Get the neighbors via the method under test.
            var neighbors = Gorilla.GetNeighborsPortal(current, grid, targetX, targetY, myAnimal, state).ToList();

            // Assert
            // We expect four neighbors corresponding to Up, Down, Left, and Right from (1,1).
            // Based on the directions array: { {0, -1}, {0, 1}, {-1, 0}, {1, 0} },
            // the neighbors should be:
            //   Up: (1, 0)
            //   Down: (1, 2)
            //   Left: (0, 1)
            //   Right: (2, 1)
            Assert.Equal(4, neighbors.Count);

            // For simplicity, since current.G is 0 and pellet bonus is 1 (and no extra penalties apply),
            // the new G cost for each neighbor should be: 0 + 1 - 1 = 0.
            // And new H = Manhattan(neighbor, target) = 1.

            // Verify Up neighbor: (1, 0)
            var upNeighbor = neighbors[0];
            Assert.Equal(1, upNeighbor.X);
            Assert.Equal(0, upNeighbor.Y);
            Assert.Equal(0, upNeighbor.G);
            Assert.Equal(1, upNeighbor.H);
            Assert.Equal(current, upNeighbor.Parent);

            // Verify Down neighbor: (1, 2)
            var downNeighbor = neighbors[1];
            Assert.Equal(1, downNeighbor.X);
            Assert.Equal(2, downNeighbor.Y);
            Assert.Equal(0, downNeighbor.G);
            Assert.Equal(1, downNeighbor.H);
            Assert.Equal(current, downNeighbor.Parent);

            // Verify Left neighbor: (0, 1)
            var leftNeighbor = neighbors[2];
            Assert.Equal(0, leftNeighbor.X);
            Assert.Equal(1, leftNeighbor.Y);
            Assert.Equal(0, leftNeighbor.G);
            Assert.Equal(1, leftNeighbor.H);
            Assert.Equal(current, leftNeighbor.Parent);

            // Verify Right neighbor: (2, 1)
            var rightNeighbor = neighbors[3];
            Assert.Equal(2, rightNeighbor.X);
            Assert.Equal(1, rightNeighbor.Y);
            Assert.Equal(0, rightNeighbor.G);
            Assert.Equal(1, rightNeighbor.H);
            Assert.Equal(current, rightNeighbor.Parent);
        }
    }
}
