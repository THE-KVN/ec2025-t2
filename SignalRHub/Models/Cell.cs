using Zooscape.Domain.Enums;

namespace Zooscape.Infrastructure.SignalRHub.Models;

public class Cell
{
    public int X { get; set; }
    public int Y { get; set; }
    public CellContents Content { get; set; }
}
