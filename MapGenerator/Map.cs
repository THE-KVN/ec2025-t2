using Zooscape.Domain.Enums;
using Zooscape.Domain.ExtensionMethods;

namespace Zooscape.MapGenerator;

public class Map
{
    private readonly Random _rng;
    private readonly char[,] _map;

    public Map(int size, double smoothness, double openness, int teleports, int seed)
    {
        var quadrantSize = (size + 1) / 2;
        var mazeSize = (quadrantSize + 1) / 2;

        if (seed == 0)
            seed = new Random().Next();
        _rng = new Random(seed);
        var maze = new Maze(mazeSize, mazeSize, _rng, smoothness)
            .RemoveDeadEnds()
            .AddForks(openness)
            .AddTeleports(teleports);

        var quadMap = maze.ToCharArray(CellContents.Wall.ToChar(), CellContents.Pellet.ToChar())
            .TruncateMap(quadrantSize);

        // Add animal spawn on diagonal in top left quadrant
        var cornerDist = _rng.Next(1, quadMap.GetLength(0) / 2);
        for (int y = cornerDist; y < cornerDist + 3; y++)
        for (int x = cornerDist; x < cornerDist + 3; x++)
            quadMap[x, y] = CellContents.Empty.ToChar();
        quadMap[cornerDist + 1, cornerDist + 1] = CellContents.AnimalSpawn.ToChar();

        // Add zookeeper spawn in bottom right corner
        quadMap[quadrantSize - 1, quadrantSize - 1] = CellContents.ZookeeperSpawn.ToChar();
        quadMap[quadrantSize - 1, quadrantSize - 2] = CellContents.Empty.ToChar();
        quadMap[quadrantSize - 2, quadrantSize - 1] = CellContents.Empty.ToChar();
        quadMap[quadrantSize - 2, quadrantSize - 2] = CellContents.Empty.ToChar();

        // Mirror map horizontally and vertically
        _map = quadMap.Mirror(Axis.Horizontal).Mirror(Axis.Vertical);
    }

    public override string ToString()
    {
        return _map.ToMapString();
    }
}
