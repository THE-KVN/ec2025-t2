using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Zooscape.Domain.Enums;
using Zooscape.Domain.ExtensionMethods;
using Zooscape.Domain.Interfaces;
using Zooscape.Domain.Utilities;
using Zooscape.Domain.ValueObjects;

namespace Zooscape.Domain.Models;

public class World : IWorld
{
    private readonly int _animalCount;
    private readonly int _queueSize;
    private readonly GridCoords _zookeeperSpawnPoint;
    private readonly List<GridCoords> _animalSpawnPoints;
    private readonly object _animalLock = new();

    public CellContents[,] Cells { get; set; }
    public Dictionary<Guid, IZookeeper> Zookeepers { get; private set; } = [];
    public ConcurrentDictionary<Guid, IAnimal> Animals { get; private set; } = [];
    public int Width => Cells.GetLength(1);
    public int Height => Cells.GetLength(0);
    public bool IsReady => Zookeepers.Count > 0 && Animals.Count == _animalCount;

    #region Constructor

    public World(string mapInput, int animalCount, int queueSize)
    {
        _animalCount = animalCount;
        _queueSize = queueSize;

        Cells = MapFromString(mapInput);

        var zookeeperSpawns = Enumerable
            .Range(0, Cells.GetLength(0))
            .SelectMany(x =>
                Enumerable
                    .Range(0, Cells.GetLength(1))
                    .Where(y => Cells[x, y] == CellContents.ZookeeperSpawn)
                    .Select(y => new GridCoords { X = x, Y = y })
            )
            .ToList();

        _animalSpawnPoints = Enumerable
            .Range(0, Cells.GetLength(0))
            .SelectMany(x =>
                Enumerable
                    .Range(0, Cells.GetLength(1))
                    .Where(y => Cells[x, y] == CellContents.AnimalSpawn)
                    .Select(y => new GridCoords { X = x, Y = y })
            )
            .ToList();

        if (zookeeperSpawns.Count != 1)
            throw new ArgumentException("Map Error: Exactly one zookeeper spawn point required");

        if (_animalSpawnPoints.Count != 4)
            throw new ArgumentException("Map Error: Exactly four animal spawn points required");

        _zookeeperSpawnPoint = zookeeperSpawns.First();
    }

    #endregion

    #region Public Methods

    public Result<IZookeeper> AddZookeeper(Guid id)
    {
        if (Zookeepers.ContainsKey(id))
        {
            return new ResultError($"Zookeeper ({id}) already added.");
        }

        var nickname = Helpers.GenerateRandomName();

        var zookeeper = new Zookeeper(id, nickname, _zookeeperSpawnPoint);

        Zookeepers.Add(id, zookeeper);

        return zookeeper;
    }

    public Result<IAnimal> AddAnimal(Guid id, string nickname)
    {
        lock (_animalLock)
        {
            if (Animals.ContainsKey(id))
            {
                return new ResultError($"Animal already added.");
            }

            if (Animals.Count >= _animalCount)
            {
                return new ResultError($"Maximum animal count ({_animalCount}) reached.");
            }

            var animal = new Animal(id, nickname, _animalSpawnPoints[Animals.Count], _queueSize);

            Animals.TryAdd(id, animal);
            return animal;
        }
    }

    public bool IsPointInBounds(GridCoords coords)
    {
        return IsPointInBounds(coords.X, coords.Y);
    }

    public bool IsPointInBounds(int x, int y)
    {
        return x >= 0 && x < Width && y >= 0 && y < Height;
    }

    public CellContents GetCellContents(GridCoords coords)
    {
        return GetCellContents(coords.X, coords.Y);
    }

    public CellContents GetCellContents(int x, int y)
    {
        return Cells[x, y];
    }

    public void SetCellContents(GridCoords coords, CellContents cellContents)
    {
        Cells[coords.X, coords.Y] = cellContents;
    }

    public IEnumerable<GridCoords> Neighbours(GridCoords coords)
    {
        var rightX = (coords.X + 1) % Width;
        var belowY = (coords.Y + 1) % Height;
        var leftX = (coords.X - 1 + Width) % Width;
        var aboveY = (coords.Y - 1 + Height) % Height;

        // Check for northern neighbour
        if (Cells[coords.X, aboveY].IsTraversable())
        {
            yield return new GridCoords { X = coords.X, Y = aboveY };
        }

        // Check for eastern neighbour
        if (Cells[rightX, coords.Y].IsTraversable())
        {
            yield return new GridCoords { X = rightX, Y = coords.Y };
        }

        // Check for southern neighbour
        if (Cells[coords.X, belowY].IsTraversable())
        {
            yield return new GridCoords { X = coords.X, Y = belowY };
        }

        // Check for western neighbour
        if (Cells[leftX, coords.Y].IsTraversable())
        {
            yield return new GridCoords { X = leftX, Y = coords.Y };
        }
    }

    #endregion

    #region Private methods

    private static CellContents[,] MapFromString(string input)
    {
        var lines = input.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        var height = lines.Length;
        var width = lines[0].Length;

        var cells = new CellContents[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                cells[x, y] = (CellContents)(lines[y][x] - '0');
            }
        }

        return cells;
    }

    #endregion
}
