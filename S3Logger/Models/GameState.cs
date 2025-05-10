using Zooscape.Application.Services;
using Zooscape.Domain.Enums;
using Zooscape.Infrastructure.SignalRHub.Models;

namespace Zooscape.Infrastructure.S3Logger.Models;

public class GameState
{
    public DateTime TimeStamp { get; set; }
    public int Tick { get; set; }
    public CellContents[,] Cells { get; set; }
    public List<Animal> Animals { get; set; }
    public List<Zookeeper> Zookeepers { get; set; }

    public GameState(IGameStateService gameState)
    {
        TimeStamp = DateTime.UtcNow;
        Tick = gameState.TickCounter;
        Cells = gameState.World.Cells;

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
}
