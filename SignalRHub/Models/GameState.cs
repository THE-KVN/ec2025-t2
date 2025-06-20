using Zooscape.Application.Services;
using Zooscape.Domain.Enums;

namespace Zooscape.Infrastructure.SignalRHub.Models;

public class GameState
{
    public DateTime TimeStamp { get; set; }
    public int Tick { get; set; }
    public List<Cell> Cells { get; set; }
    public List<Animal> Animals { get; set; }
    public List<Zookeeper> Zookeepers { get; set; }



    public GameState(IGameStateService gameState)
    {
        TimeStamp = DateTime.UtcNow;
        Tick = gameState.TickCounter;
        Cells = ConvertCellsToSerializable(gameState.World.Cells);

        Animals = gameState
            .Animals.Values.Select(a => new Animal()
            {
                Id = a.Id,
                Nickname = a.Nickname,
                X = a.Location.X,
                Y = a.Location.Y,
                SpawnX = a.SpawnPoint.X,
                SpawnY = a.SpawnPoint.Y,
                Score = a.Score,
                CapturedCounter = a.CapturedCounter,
                DistanceCovered = a.DistanceCovered,
                IsViable = a.IsViable,
                HeldPowerUp = a.HeldPowerUp,
                ActivePowerUp = a.ActivePowerUp,
            })
            .ToList();

        Zookeepers = gameState
            .Zookeepers.Values.Select(z => new Zookeeper()
            {
                Id = z.Id,
                Nickname = z.Nickname,
                X = z.Location.X,
                Y = z.Location.Y,
                SpawnX = z.SpawnPoint.X,
                SpawnY = z.SpawnPoint.Y,
            })
            .ToList();
    }

    private List<Cell> ConvertCellsToSerializable(CellContents[,] cells)
    {
        var cellList = new List<Cell>();

        for (int x = 0; x < cells.GetLength(0); x++)
        {
            for (int y = 0; y < cells.GetLength(1); y++)
            {
                cellList.Add(new Cell
                {
                    X = x,
                    Y = y,
                    Content = (CellContents)cells[x, y]
                });
            }
        }

        return cellList;
    }
}
