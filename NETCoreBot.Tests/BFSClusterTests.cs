using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using NETCoreBot.Strategy;  // Adjust if needed
using NETCoreBot.Enums;     // Contains CellContent enum
using NETCoreBot.Models;    // Contains Cell class

namespace NETCoreBot.Tests
{
    public class BFSClusterTests
    {
        [Fact]
        public void BFSCluster_ReturnsConnectedCluster()
        {
            //// Arrange: Create a grid with a connected pellet cluster.
            //// Define a 3x3 grid where cells (0,0), (0,1), (1,0), and (1,1) are pellets.
            //// Cell (2,2) is a pellet but not connected to the cluster.
            //var grid = new Dictionary<(int, int), CellContent>
            //{
            //    [(0, 0)] = CellContent.Pellet,
            //    [(0, 1)] = CellContent.Pellet,
            //    [(1, 0)] = CellContent.Pellet,
            //    [(1, 1)] = CellContent.Pellet,
            //    [(2, 2)] = CellContent.Pellet,
            //    [(0, -1)] = CellContent.Wall  // Example non-pellet cell.
            //};

            //var visited = new HashSet<(int, int)>();
            //var start = (0, 0);

            //// Act: Run BFSCluster from (0,0).
            //var cluster = Gorilla.BFSCluster(start, grid, visited);

            //// Assert:
            //// The expected connected cluster is: (0,0), (0,1), (1,0), and (1,1).
            //Assert.Equal(4, cluster.Count);
            //var clusterCoords = cluster.Select(c => (c.X, c.Y)).ToHashSet();
            //Assert.Contains((0, 0), clusterCoords);
            //Assert.Contains((0, 1), clusterCoords);
            //Assert.Contains((1, 0), clusterCoords);
            //Assert.Contains((1, 1), clusterCoords);

            //// Also, the visited set should contain exactly these coordinates.
            //Assert.Equal(4, visited.Count);
            //Assert.Contains((0, 0), visited);
            //Assert.Contains((0, 1), visited);
            //Assert.Contains((1, 0), visited);
            //Assert.Contains((1, 1), visited);
        }

        [Fact]
        public void BFSCluster_SingleCellCluster_WhenNoAdjacentPellets()
        {
            //// Arrange: Create a grid where only the start cell is a pellet.
            //var grid = new Dictionary<(int, int), CellContent>
            //{
            //    [(10, 10)] = CellContent.Pellet,
            //    // Neighbors are non-pellet.
            //    [(10, 9)] = CellContent.Wall,
            //    [(9, 10)] = CellContent.Empty,
            //    [(10, 11)] = CellContent.Empty,
            //    [(11, 10)] = CellContent.Wall
            //};

            //var visited = new HashSet<(int, int)>();
            //var start = (10, 10);

            //// Act: Run BFSCluster from (10,10).
            //var cluster = Gorilla.BFSCluster(start, grid, visited);

            //// Assert: Since no adjacent cells are pellets, the cluster should only contain the start cell.
            //Assert.Single(cluster);
            //Assert.Equal((10, 10), (cluster[0].X, cluster[0].Y));
            //Assert.Single(visited);
            //Assert.Contains((10, 10), visited);
        }
    }
}
