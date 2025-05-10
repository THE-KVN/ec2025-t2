using System.Collections.Generic;
using System.Linq;

namespace Zooscape.Domain.Algorithms.DataStructures;

public class Path
{
    public List<Node> Nodes;

    public Path()
    {
        Nodes = new();
    }

    public Node Pop()
    {
        var node = Nodes[0];
        Nodes.RemoveAt(0);
        return node;
    }

    public void Add(Node node)
    {
        Nodes.Insert(0, node);
    }

    public int Length => Nodes.Count;
}
