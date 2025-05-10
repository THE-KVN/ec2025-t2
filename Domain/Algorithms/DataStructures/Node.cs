using System.Collections.Generic;
using Zooscape.Domain.Enums;
using Zooscape.Domain.ValueObjects;

namespace Zooscape.Domain.Algorithms.DataStructures;

public class Node : ValueObject
{
    public readonly GridCoords Coords;
    public Node? Parent;
    public int GCost;
    public int HCost;

    public int X => Coords.X;
    public int Y => Coords.Y;

    public int FCost => GCost + HCost;

    public Node(GridCoords coords, Node? parent = null, int hCost = 0)
    {
        Coords = coords;
        GCost = parent != null ? parent.GCost + 1 : 0;
        Parent = parent;
        HCost = hCost;
    }

    public override int GetHashCode()
    {
        return Coords.GetHashCode();
    }

    protected override IEnumerable<object> GetAtomicValues()
    {
        yield return Coords;
    }

    public Direction GetDirectionToNode(Node? node)
    {
        return Coords.DirectionTo(node?.Coords);
    }

    public override string ToString()
    {
        return $"Node {{ Coords: {Coords}, HCost: {HCost}, GCost: {GCost}, FCost: {FCost} }}";
    }
}
