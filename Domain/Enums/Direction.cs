using System;

namespace Zooscape.Domain.Enums;

public enum Direction
{
    Idle = 0,
    Up = 1,
    Down = 2,
    Left = 3,
    Right = 4,
}

public static class ExtensionMethods
{
    public static Direction Reverse(this Direction direction)
    {
        return direction switch
        {
            Direction.Idle => Direction.Idle,
            Direction.Up => Direction.Down,
            Direction.Down => Direction.Up,
            Direction.Left => Direction.Right,
            Direction.Right => Direction.Left,
            _ => throw new ArgumentException("Invalid direction specified"),
        };
    }
}
