using NETCoreBot.Enums;

namespace NETCoreBot.Models;

public class ActivePowerUp
{
    public double Value { get; set; }
    public int TicksRemaining { get; set; }
    public PowerUpType Type { get; set; }
}