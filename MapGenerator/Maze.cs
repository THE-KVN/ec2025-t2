using System.Drawing;

namespace Zooscape.MapGenerator;

public class Maze
{
    private Stack _stack;
    private Random _rng;
    private int _width;
    private int _height;
    private double _smoothness;
    private int[] _mazeBase;

    public Cell[,] Cells;

    public Maze(int width, int height, Random rng, double smoothness)
    {
        _rng = rng;
        _smoothness = smoothness * 0.99; // Ensure that smoothness is never a perfect 1.0
        _width = width;
        _height = height;
        _mazeBase = new int[_width * _height];
        Cells = new Cell[_width, _height];

        _stack = new Stack();

        MazeInit();

        var state = new MazeState(rng.Next() % _width, rng.Next() % _height, 0);
        AnalyzeCell(state);
    }

    public Maze Generate(int width, int height, Random rng, double smoothness)
    {
        _rng = rng;
        _smoothness = smoothness * 0.99; // Ensure that smoothness is never a perfect 1.0
        _width = width;
        _height = height;
        _mazeBase = new int[_width * _height];
        Cells = new Cell[_width, _height];

        _stack = new Stack();

        MazeInit();

        var state = new MazeState(rng.Next() % _width, rng.Next() % _height, 0);
        AnalyzeCell(state);

        return this;
    }

    public Maze RemoveDeadEnds(bool verbose = false)
    {
        for (int y = 0; y < _height; y++)
        for (int x = 0; x < _width; x++)
        {
            var walls = Cells[x, y].Walls().ToList();
            if (verbose)
                Console.Write($"{x},{y}:{walls.Count}");
            if (walls.Count == 3)
            {
                if (x == 0)
                    walls.Remove(Direction.W);
                if (x == _width - 1)
                    walls.Remove(Direction.E);
                if (y == 0)
                    walls.Remove(Direction.N);
                if (y == _height - 1)
                    walls.Remove(Direction.S);

                var randomWall = walls[_rng.Next(walls.Count)];
                if (verbose)
                    Console.WriteLine($"(smash -> {randomWall})");

                BreakWall(x, y, randomWall);
            }
            else
            {
                if (verbose)
                    Console.WriteLine();
            }
        }

        return this;
    }

    public Maze AddForks(double openness)
    {
        var oneWalledCells = (
            from x in Enumerable.Range(0, Cells.GetLength(0))
            from y in Enumerable.Range(0, Cells.GetLength(1))
            let item = Cells[x, y]
            where item != null && item.Walls().Count() == 1
            select new
            {
                X = x,
                Y = y,
                Cell = item,
            }
        ).ToList();

        var twoWalledCells = (
            from x in Enumerable.Range(0, Cells.GetLength(0))
            from y in Enumerable.Range(0, Cells.GetLength(1))
            let item = Cells[x, y]
            where item != null && item.Walls().Count() == 2
            select new
            {
                X = x,
                Y = y,
                Cell = item,
            }
        ).ToList();

        var targetNumber = (int)(_width * _height * openness) - oneWalledCells.Count;

        if (targetNumber <= 0)
            return this;

        var targetCells = twoWalledCells.OrderBy(_ => _rng.Next()).Take(targetNumber).ToList();

        foreach (var target in targetCells)
        {
            var walls = target.Cell.Walls().ToList();
            if (walls.Count == 2)
            {
                if (target.X == 0)
                    walls.Remove(Direction.W);
                if (target.X == _width - 1)
                    walls.Remove(Direction.E);
                if (target.Y == 0)
                    walls.Remove(Direction.N);
                if (target.Y == _height - 1)
                    walls.Remove(Direction.S);

                if (walls.Count == 0)
                    continue;

                var randomWall = walls[_rng.Next(walls.Count)];
                var srcIndex = CellIndex(target.X, target.Y);
                int dstIndex = randomWall switch
                {
                    Direction.N => CellIndex(target.X, target.Y - 1),
                    Direction.S => CellIndex(target.X, target.Y + 1),
                    Direction.E => CellIndex(target.X + 1, target.Y),
                    Direction.W => CellIndex(target.X - 1, target.Y),
                    _ => CellIndex(target.X, target.Y),
                };

                BreakWall(target.X, target.Y, randomWall);
            }
        }

        return this;
    }

    public Maze AddTeleports(int teleports)
    {
        var teleportsOnWall = teleports / 2;
        var teleportsOnCentreLine = teleports % 2;
        var centreLineTop = _rng.Next(2) == 1;

        if (teleportsOnCentreLine != 0)
        {
            if (centreLineTop)
                Cells[_width - 1, 0] |= (byte)Direction.N;
            else
                Cells[0, _height - 1] |= (byte)Direction.W;
        }

        var fewerTeleports = Enumerable
            .Range(0, centreLineTop ? _width - 1 : _height - 1)
            .OrderBy(_ => _rng.Next())
            .Take(teleportsOnWall / 2)
            .ToList();

        var moreTeleports = Enumerable
            .Range(0, centreLineTop ? _height - 1 : _width - 1)
            .OrderBy(_ => _rng.Next())
            .Take(teleportsOnWall - fewerTeleports.Count)
            .ToList();

        foreach (var pos in fewerTeleports)
        {
            Cells[centreLineTop ? pos : 0, centreLineTop ? 0 : pos] |= (byte)(
                centreLineTop ? Direction.N : Direction.W
            );
        }

        foreach (var pos in moreTeleports)
        {
            Cells[centreLineTop ? 0 : pos, centreLineTop ? pos : 0] |= (byte)(
                centreLineTop ? Direction.W : Direction.N
            );
        }

        return this;
    }

    public void AnalyzeCell(MazeState state)
    {
        bool end = false;
        bool found;
        int indexSrc;
        int indexDest;
        int prevDir;
        int dir = 0;

        while (true)
        {
            if (state!.dir == 15)
            {
                while (state.dir == 15)
                {
                    state = (MazeState)_stack.Pop()!;
                    if (state == null)
                    {
                        end = true;
                        break;
                    }
                }
                if (end == true)
                    break;
            }
            else
            {
                do
                {
                    prevDir = dir;
                    dir = (int)Math.Pow(2, _rng.Next() % 4);

                    if (_rng.NextDouble() < _smoothness)
                        if ((state.dir & prevDir) == 0)
                            dir = prevDir;

                    if ((state.dir & dir) != 0)
                        found = true;
                    else
                        found = false;
                } while (found == true && state.dir != 15);

                state.dir |= dir;

                indexSrc = CellIndex(state.x, state.y);

                // W
                if (dir == 1 && state.x > 0)
                {
                    indexDest = CellIndex(state.x - 1, state.y);
                    if (BaseCell(indexSrc) != BaseCell(indexDest))
                    {
                        Merge(indexSrc, indexDest);
                        BreakWall(state.x, state.y, Direction.W);

                        _stack.Push(new MazeState(state));
                        state.x -= 1;
                        state.dir = 0;
                    }
                }

                // E
                if (dir == 2 && state.x < _width - 1)
                {
                    indexDest = CellIndex(state.x + 1, state.y);
                    if (BaseCell(indexSrc) != BaseCell(indexDest))
                    {
                        Merge(indexSrc, indexDest);
                        BreakWall(state.x, state.y, Direction.E);

                        _stack.Push(new MazeState(state));
                        state.x += 1;
                        state.dir = 0;
                    }
                }

                // N
                if (dir == 4 && state.y > 0)
                {
                    indexDest = CellIndex(state.x, state.y - 1);
                    if (BaseCell(indexSrc) != BaseCell(indexDest))
                    {
                        Merge(indexSrc, indexDest);
                        BreakWall(state.x, state.y, Direction.N);

                        _stack.Push(new MazeState(state));
                        state.y -= 1;
                        state.dir = 0;
                    }
                }

                // S
                if (dir == 8 && state.y < _height - 1)
                {
                    indexDest = CellIndex(state.x, state.y + 1);
                    if (BaseCell(indexSrc) != BaseCell(indexDest))
                    {
                        Merge(indexSrc, indexDest);
                        BreakWall(state.x, state.y, Direction.S);

                        _stack.Push(new MazeState(state));
                        state.y += 1;
                        state.dir = 0;
                    }
                }
            }
        }
    }

    private void BreakWall(int x, int y, Direction dir)
    {
        Cells[x, y] |= (byte)dir;
        switch (dir)
        {
            case Direction.N:
                Cells[x, y - 1] |= (byte)Direction.S;
                break;
            case Direction.S:
                Cells[x, y + 1] |= (byte)Direction.N;
                break;
            case Direction.E:
                Cells[x + 1, y] |= (byte)Direction.W;
                break;
            case Direction.W:
                Cells[x - 1, y] |= (byte)Direction.E;
                break;
        }
    }

    public void SetAccess(Point entrance, Point exit)
    {
        for (int i = 0; i < _width; i++)
        {
            Cells[i, 0] &= (byte)~Direction.N;
            Cells[i, _height - 1] &= (byte)~Direction.S;
        }
        for (int j = 0; j < _height; j++)
        {
            Cells[0, j] &= (byte)~Direction.W;
            Cells[_width - 1, j] &= (byte)~Direction.E;
        }

        Direction entranceDir = 0,
            exitDir = 0;
        if (entrance.X == 0)
            entranceDir |= Direction.W;
        if (entrance.X == _width + 1)
            entranceDir |= Direction.E;
        if (entrance.Y == 0)
            entranceDir |= Direction.N;
        if (entrance.Y == _height + 1)
            entranceDir |= Direction.S;

        if (exit.X == 0)
            exitDir |= Direction.W;
        if (exit.X == _width + 1)
            exitDir |= Direction.E;
        if (exit.Y == 0)
            exitDir |= Direction.N;
        if (exit.Y == _height + 1)
            exitDir |= Direction.S;

        Cells[
            Math.Min(Math.Max(entrance.X - 1, 0), _width - 1),
            Math.Min(Math.Max(entrance.Y - 1, 0), _height - 1)
        ] |= (byte)entranceDir;
        Cells[
            Math.Min(Math.Max(exit.X - 1, 0), _width - 1),
            Math.Min(Math.Max(exit.Y - 1, 0), _height - 1)
        ] |= (byte)exitDir;
    }

    public int CellIndex(int x, int y)
    {
        return _width * y + x;
    }

    public int BaseCell(int Index)
    {
        int index = Index;
        while (_mazeBase[index] >= 0)
        {
            index = _mazeBase[index];
        }
        return index;
    }

    void Merge(int index1, int index2)
    {
        // merge both lists
        int base1 = BaseCell(index1);
        int base2 = BaseCell(index2);
        _mazeBase[base2] = base1;
    }

    void MazeInit()
    {
        int i,
            j;

        for (i = 0; i < _width; i++)
        for (j = 0; j < _height; j++)
        {
            _mazeBase[CellIndex(i, j)] = -1;
            Cells[i, j] = 0;
        }
    }

    public char[,] ToCharArray(char wallChar, char emptyChar)
    {
        var retval = new char[_width * 2, _height * 2];

        for (int y = 0; y < _height; y++)
        {
            // Upper margin
            for (int x = 0; x < _width; x++)
            {
                // Above and left of cell
                retval[x * 2, y * 2] = wallChar;

                // Above cell
                if (Cells[x, y].CanTraverse(Direction.N))
                    retval[x * 2 + 1, y * 2] = emptyChar;
                else
                    retval[x * 2 + 1, y * 2] = wallChar;
            }
            // Above and right of right most cell
            retval[_width * 2 - 1, y * 2] = emptyChar;

            // Actual row
            for (int x = 0; x < _width; x++)
            {
                // Left of cell
                if (Cells[x, y].CanTraverse(Direction.W))
                    retval[x * 2, y * 2 + 1] = emptyChar;
                else
                    retval[x * 2, y * 2 + 1] = wallChar;

                // Actual cell
                retval[x * 2 + 1, y * 2 + 1] = emptyChar;
            }
            // Right of right most cell
            retval[_width * 2 - 1, y * 2 + 1] = emptyChar;
        }

        // Bottom margin
        for (int x = 0; x < _width; x++)
        {
            // Below and left of cell
            retval[x * 2, _height * 2 - 1] = emptyChar;

            // Below cell
            retval[x * 2 + 1, _height * 2 - 1] = emptyChar;
        }

        // Top right and bottom left corners
        retval[_width * 2 - 1, 0] = wallChar;
        retval[0, _height * 2 - 1] = wallChar;

        return retval;
    }
}

public class Cell(byte value)
{
    public byte Value { get; } = value;

    public bool CanTraverse(Direction direction)
    {
        return (Value & (byte)direction) > 0;
    }

    public IEnumerable<Direction> Walls()
    {
        if (!CanTraverse(Direction.N))
            yield return Direction.N;

        if (!CanTraverse(Direction.S))
            yield return Direction.S;

        if (!CanTraverse(Direction.E))
            yield return Direction.E;

        if (!CanTraverse(Direction.W))
            yield return Direction.W;
    }

    public static implicit operator byte(Cell b) => b.Value;

    public static implicit operator Cell(byte b) => new(b);
}

public class MazeState
{
    public int x,
        y,
        dir;

    public MazeState(int tx, int ty, int td)
    {
        x = tx;
        y = ty;
        dir = td;
    }

    public MazeState(MazeState s)
    {
        x = s.x;
        y = s.y;
        dir = s.dir;
    }
}

public class CellPosition
{
    public int x,
        y;

    public CellPosition() { }

    public CellPosition(int xp, int yp)
    {
        x = xp;
        y = yp;
    }
}
