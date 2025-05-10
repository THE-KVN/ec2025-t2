using ReferenceBot.Enums;

namespace ReferenceBot.Models;

public class Cell
{
    public int X { get; set; }
    public int Y { get; set; }
    public CellContent Content { get; set; }
}
