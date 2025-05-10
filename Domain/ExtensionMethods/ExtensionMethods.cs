using System;
using Zooscape.Domain.Enums;

namespace Zooscape.Domain.ExtensionMethods;

public static class ExtensionMethods
{
    public static bool IsTraversable(this CellContents contents)
    {
        return contents is CellContents.Empty or CellContents.Pellet or CellContents.ZookeeperSpawn;
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

    public static char ToChar(this CellContents contents)
    {
        return (char)('0' + (int)contents);
    }
}
