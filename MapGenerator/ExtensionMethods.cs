using System.Text;

namespace Zooscape.MapGenerator;

static class ExtensionMethods
{
    public static string ToMapString(this char[,] input)
    {
        var sb = new StringBuilder();

        for (int y = 0; y < input.GetLength(0); y++)
        {
            for (int x = 0; x < input.GetLength(1); x++)
            {
                sb.Append(input[x, y]);
            }
            sb.AppendLine();
        }

        return sb.ToString().TrimEnd('\r', '\n');
    }

    public static char[,] Mirror(this char[,] input, Axis axis)
    {
        int baseHeight = input.GetLength(1);
        int baseWidth = input.GetLength(0);
        char[,] output = new char[
            axis == Axis.Horizontal ? 2 * baseWidth - 1 : baseWidth,
            axis == Axis.Vertical ? 2 * baseHeight - 1 : baseHeight
        ];

        var yRange = axis == Axis.Vertical ? 2 * baseHeight - 1 : baseHeight;
        var xRange = axis == Axis.Horizontal ? 2 * baseWidth - 1 : baseWidth;
        for (int y = 0; y < yRange; y++)
        for (int x = 0; x < xRange; x++)
            output[x, y] = input[
                x < baseWidth ? x : -x + 2 * (baseWidth - 1),
                y < baseHeight ? y : -y + 2 * (baseHeight - 1)
            ];

        return output;
    }

    public static char[,] TruncateMap(this char[,] original, int newSize)
    {
        int height = original.GetLength(1);
        int width = original.GetLength(0);

        if (newSize > height || newSize > width)
            throw new ArgumentException(
                "New size must be smaller than or equal to original width and height."
            );

        char[,] result = new char[newSize, newSize];

        for (int y = 0; y < newSize; y++)
        {
            for (int x = 0; x < newSize; x++)
            {
                result[x, y] = original[x, y];
            }
        }

        return result;
    }
}
