using System.Collections.Generic;
using System.Linq;
using Zooscape.Domain.Algorithms.DataStructures;
using Zooscape.Domain.ExtensionMethods;
using Zooscape.Domain.Interfaces;
using Zooscape.Domain.ValueObjects;

namespace Zooscape.Domain.Algorithms.Pathfinding;

public class AStar
{
    public static Path? PerformAStarSearch(IWorld world, GridCoords start, GridCoords end)
    {
        // If the end point isn't even walkable, just skip.
        if (!IsPointWalkable(world, end))
        {
            return null;
        }

        var startNode = new Node(start, hCost: start.ManhattanDistance(end));
        var endNode = new Node(end);

        HashSet<Node> openSet = [];
        HashSet<Node> closedSet = [];

        openSet.Add(startNode);

        while (openSet.Count > 0)
        {
            var currentNode = openSet.First();
            if (currentNode.Equals(endNode))
            {
                endNode.Parent = currentNode.Parent;
                return ConstructPath(endNode);
            }
            openSet.Remove(currentNode);
            closedSet.Add(currentNode);

            foreach (var neighbour in Neighbours(world, currentNode))
            {
                if (closedSet.Contains(neighbour))
                {
                    continue;
                }
                neighbour.HCost = neighbour.Coords.ManhattanDistance(end);

                if (openSet.Add(neighbour))
                {
                    continue;
                }
                var openNeighbour = openSet.First(neighbour.Equals);
                if (neighbour.GCost < openNeighbour.GCost)
                {
                    openNeighbour.GCost = neighbour.GCost;
                    openNeighbour.Parent = neighbour.Parent;
                }
            }
            openSet = openSet.OrderBy((node) => node.FCost).ToHashSet();
        }

        return null;
    }

    private static HashSet<Node> Neighbours(IWorld world, Node node)
    {
        HashSet<GridCoords> allNeighbours =
        [
            new(node.X - 1, node.Y),
            new(node.X + 1, node.Y),
            new(node.X, node.Y - 1),
            new(node.X, node.Y + 1),
        ];
        HashSet<Node> neighbours = allNeighbours
            .Where(coords => IsPointWalkable(world, coords))
            .Select(coords => new Node(coords, node))
            .ToHashSet();
        return neighbours;
    }

    private static bool IsPointWalkable(IWorld world, GridCoords coords)
    {
        return world.IsPointInBounds(coords) && world.GetCellContents(coords).IsTraversable();
    }

    private static Path ConstructPath(Node node)
    {
        Path path = new();
        path.Add(node);

        while (node.Parent != null)
        {
            node = node.Parent;
            path.Add(node);
        }
        return path;
    }
}
