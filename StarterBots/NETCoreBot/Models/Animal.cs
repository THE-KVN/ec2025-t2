
using NETCoreBot.Enums;
namespace NETCoreBot.Models;

public class Animal
{
    public Guid Id { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
    public int SpawnX { get; set; }
    public int SpawnY { get; set; }
    public int Score { get; set; }
    public int CapturedCounter { get; set; }
    public int DistanceCovered { get; set; }
    public bool IsViable { get; set; }

    public PowerUpType? HeldPowerUp { get; set; }
    public ActivePowerUp? ActivePowerUp { get; set; }


    public float CurrentMultiplier { get; set; } = 1.0f;
    public int TicksSinceLastScore { get; set; } = 0;

}
