using ReferenceBot.Algorithms.DataStructures;
using ReferenceBot.Enums;
using ReferenceBot.Models;
using ReferenceBot.ValueObjects;
using Path = ReferenceBot.Algorithms.DataStructures.Path;

namespace ReferenceBot.Algorithms.Pathfinding;

public class AStar
{
    public static Path? PerformAStarSearch(Cell[,] world, GridCoords start, GridCoords end)
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
                neighbour.HCost = CalculateHCost(world, neighbour);

                if (openSet.Add(neighbour))
                {
                    continue;
                }
                var openNeighbour = openSet.Where(neighbour.Equals).First();
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

    private static int CalculateHCost(Cell[,] world, Node neighbour)
    {
        var contents = world[neighbour.X, neighbour.Y];
        return contents.Content switch
        {
            CellContent.Pellet => 5,
            CellContent.Scavenger => 10,
            CellContent.ChameleonCloak => 10,
            CellContent.BigMooseJuice => 10,
            CellContent.PowerPellet => 20,
            _ => 0,
        };
    }

    private static HashSet<Node> Neighbours(Cell[,] world, Node node)
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

    private static bool IsPointWalkable(Cell[,] world, GridCoords coords)
    {
        return IsPointInBounds(world, coords) && IsTraversable(world, coords);
    }

    private static bool IsTraversable(Cell[,] world, GridCoords coords)
    {
        return world[coords.X, coords.Y].Content
            is CellContent.Empty
                or CellContent.Pellet
                or CellContent.ZookeeperSpawn
                or CellContent.ZookeeperSpawn
                or CellContent.PowerPellet
                or CellContent.ChameleonCloak
                or CellContent.BigMooseJuice
                or CellContent.Scavenger;
    }

    private static bool IsPointInBounds(Cell[,] world, GridCoords coords)
    {
        return coords.X >= 0
            && coords.X < world.GetLength(1)
            && coords.Y >= 0
            && coords.Y < world.GetLength(0);
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
