namespace Zooscape.MapGenerator;

[Flags]
public enum Direction : byte
{
    N = 0b0001,
    S = 0b0010,
    E = 0b0100,
    W = 0b1000,
}

public enum Axis
{
    Vertical,
    Horizontal,
}
