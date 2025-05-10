using ReferenceBot.Algorithms.Pathfinding;
using ReferenceBot.Enums;
using ReferenceBot.Models;
using ReferenceBot.ValueObjects;
using Path = ReferenceBot.Algorithms.DataStructures.Path;

namespace ReferenceBot.Services;

public class BotService
{
    private Guid _botId;
    private int _ticksSinceRetarget = 0;
    private const int _retargetInterval = 10;
    private Cell? _currentDestination = null;
    private static Random _random = new(21);

    public void SetBotId(Guid botId)
    {
        _botId = botId;
    }

    public Guid GetBotId()
    {
        return _botId;
    }

    public BotCommand ProcessState(GameState gameStateDTO)
    {
        var animal = gameStateDTO.Animals.First(bot => bot.Id == _botId);

        //Console.Clear();
        Console.WriteLine($"Score: {animal.Score}, Captured {animal.CapturedCounter} times");

        var newDirection = CalculateBotDirection(gameStateDTO, animal);
        var command = new BotCommand { Action = (BotAction)newDirection };
        return command;
    }

    private Direction CalculateBotDirection(GameState gameState, Animal animal)
    {
        if (
            _currentDestination == null
            || _ticksSinceRetarget >= _retargetInterval
            || HasReachedTarget(animal, _currentDestination)
        )
        {
            _currentDestination = PickRandomTargetCell(gameState.Cells);
            _ticksSinceRetarget = 0;
        }

        _ticksSinceRetarget++;

        if (_currentDestination == null)
        {
            return Direction.Up;
        }

        var currentPath = GetPathToDestination(animal, gameState.Cells, _currentDestination);
        if (currentPath == null)
        {
            return Direction.Up;
        }

        return GetDirectionFromPath(currentPath);
    }

    private static Direction GetDirectionFromPath(Path path)
    {
        if (path.Length < 2)
        {
            return Direction.Up;
        }

        var currentNode = path.Pop();
        var nextNode = path.Pop();
        return currentNode.GetDirectionToNode(nextNode);
    }

    private static Path? GetPathToDestination(Animal animal, List<Cell> cells, Cell destination)
    {
        var grid = ConvertTo2DArray(cells);
        var animalCoords = new GridCoords(animal.X, animal.Y);
        var destinationCoords = new GridCoords(destination.X, destination.Y);
        return AStar.PerformAStarSearch(grid, animalCoords, destinationCoords);
    }

    private static Cell? PickRandomTargetCell(List<Cell> world)
    {
        var cellsWithPellet = world.Where(cell => cell.Content == CellContent.Pellet).ToList();
        if (cellsWithPellet.Count <= 0)
        {
            return null;
        }

        int randomIndex = _random.Next(cellsWithPellet.Count);

        return cellsWithPellet[randomIndex];
    }

    private static Cell[,] ConvertTo2DArray(List<Cell> cells)
    {
        int maxX = cells.Max(cell => cell.X);
        int maxY = cells.Max(cell => cell.Y);

        Cell[,] world = new Cell[maxX + 1, maxY + 1];
        foreach (var cell in cells)
        {
            world[cell.X, cell.Y] = cell;
        }
        return world;
    }

    private static bool HasReachedTarget(Animal animal, Cell target)
    {
        return animal.X == target.X && animal.Y == target.Y;
    }
}
