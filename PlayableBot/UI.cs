using PlayableBot.ExtensionMethods;
using PlayableBot.Models;

namespace PlayableBot;

class UI
{
    private char[,] _grid = null;

    public void DrawGrid(List<Cell> cells, List<Animal> animals, List<Zookeeper> zookeepers)
    {
        _grid ??= new char[cells.Max(c => c.X) + 1, cells.Max(c => c.Y) + 1];

        var width = _grid.GetLength(0);
        var height = _grid.GetLength(1);

        foreach (var cell in cells)
        {
            _grid[cell.X, cell.Y] = cell.Content.ToChar();
        }

        for (int i = 0; i < animals.Count; i++)
        {
            _grid[animals[i].X, animals[i].Y] = (char)(i + 49);
        }

        foreach (var zookeeper in zookeepers)
        {
            _grid[zookeeper.X, zookeeper.Y] = 'Z';
        }

        for (int y = 0; y < height; y++)
        {
            Console.SetCursorPosition(0, y);
            for (int x = 0; x < width; x++)
            {
                Console.Write(_grid[x, y]);
            }
        }
    }
}
