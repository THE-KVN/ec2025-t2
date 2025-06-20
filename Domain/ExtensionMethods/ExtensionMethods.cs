using System;
using Zooscape.Domain.Enums;

namespace Zooscape.Domain.ExtensionMethods;

public static class ExtensionMethods
{
    public static bool IsTraversable(this CellContents contents)
    {
        return contents
            is CellContents.Empty
                or CellContents.Pellet
                or CellContents.ZookeeperSpawn
                or CellContents.Scavenger
                or CellContents.PowerPellet
                or CellContents.ChameleonCloak
                or CellContents.BigMooseJuice;
    }

    public static Direction ToDirection(this BotAction action)
    {
        return action switch
        {
            BotAction.Up => Direction.Up,
            BotAction.Down => Direction.Down,
            BotAction.Left => Direction.Left,
            BotAction.Right => Direction.Right,
            _ => throw new ArgumentException("Invalid enum value", nameof(action)),
        };
    }

    public static CellContents ToCellContents(this PowerUpType powerUpType)
    {
        return powerUpType switch
        {
            PowerUpType.PowerPellet => CellContents.PowerPellet,
            PowerUpType.Scavenger => CellContents.Scavenger,
            PowerUpType.ChameleonCloak => CellContents.ChameleonCloak,
            PowerUpType.BigMooseJuice => CellContents.BigMooseJuice,
            _ => throw new ArgumentException("Invalid enum value", nameof(powerUpType)),
        };
    }

    public static PowerUpType ToPowerUpType(this CellContents contents)
    {
        return contents switch
        {
            CellContents.PowerPellet => PowerUpType.PowerPellet,
            CellContents.Scavenger => PowerUpType.Scavenger,
            CellContents.ChameleonCloak => PowerUpType.ChameleonCloak,
            CellContents.BigMooseJuice => PowerUpType.BigMooseJuice,
            _ => throw new ArgumentException("Invalid enum value", nameof(contents)),
        };
    }

    public static char ToChar(this CellContents contents)
    {
        return (char)('0' + (int)contents);
    }
}
