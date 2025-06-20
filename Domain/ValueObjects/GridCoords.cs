using System;
using System.Collections.Generic;
using Zooscape.Domain.Enums;

namespace Zooscape.Domain.ValueObjects;

public class GridCoords : ValueObject
{
    public int X { get; set; }
    public int Y { get; set; }

    public GridCoords() { }

    public GridCoords(int x, int y)
    {
        X = x;
        Y = y;
    }

    protected override IEnumerable<object> GetAtomicValues()
    {
        yield return X;
        yield return Y;
    }

    public int ManhattanDistance(GridCoords coords)
    {
        return Math.Abs(X - coords.X) + Math.Abs(Y - coords.Y);
    }

    public double EuclideanDistance(GridCoords coords)
    {
        int deltaX = X - coords.X;
        int deltaY = Y - coords.Y;
        return Math.Sqrt((deltaX * deltaX) + (deltaY * deltaY));
    }

    public int EuclideanDistanceSquared(GridCoords coords)
    {
        int deltaX = X - coords.X;
        int deltaY = Y - coords.Y;
        return (deltaX * deltaX) + (deltaY * deltaY);
    }

    public static GridCoords operator -(GridCoords coords, GridCoords otherCoords)
    {
        return new GridCoords(coords.X - otherCoords.X, coords.Y - otherCoords.Y);
    }

    public static GridCoords operator +(GridCoords coords, Direction direction)
    {
        switch (direction)
        {
            case Direction.Up:
                return new GridCoords(coords.X, coords.Y - 1);
            case Direction.Down:
                return new GridCoords(coords.X, coords.Y + 1);
            case Direction.Left:
                return new GridCoords(coords.X - 1, coords.Y);
            case Direction.Right:
                return new GridCoords(coords.X + 1, coords.Y);
            default:
                return coords;
        }
    }

    public Direction DirectionTo(GridCoords? otherCoords)
    {
        if (otherCoords == null)
        {
            return Direction.Idle;
        }
        GridCoords delta = otherCoords - this;
        if (delta.X == 0 && delta.Y == 0)
        {
            return Direction.Idle;
        }
        if (delta.X == 0)
        {
            return delta.Y > 0 ? Direction.Down : Direction.Up;
        }
        return delta.X > 0 ? Direction.Right : Direction.Left;
    }

    public override string ToString()
    {
        return $"({X}, {Y})";
    }
}
