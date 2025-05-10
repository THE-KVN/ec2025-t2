using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Zooscape.Domain.Enums;
using Zooscape.Domain.Utilities;
using Zooscape.Domain.ValueObjects;

namespace Zooscape.Domain.Interfaces;

public interface IWorld
{
    public CellContents[,] Cells { get; set; }
    public Dictionary<Guid, IZookeeper> Zookeepers { get; }
    public ConcurrentDictionary<Guid, IAnimal> Animals { get; }
    public int Width { get; }
    public int Height { get; }

    /// <summary>
    /// Adds a zookeeper to the world with specified id
    /// </summary>
    /// <param name="id">Unique identifier for zookeeper</param>
    /// <returns>
    /// A <see cref="Result{T}"/> where <typeparamref name="T"/> is <see cref="IZookeeper"/>.
    /// <list type="bullet">
    ///     <item><description>If successful, <see cref="Result{T}.IsSuccess"/> is <c>true</c>, and <see cref="Result{T}.Value"/> contains the newly created zookeeper.</description></item>
    ///     <item><description>If unsuccessful, <see cref="Result{T}.IsSuccess"/> is <c>false</c>, and <see cref="Result{T}.Error"/> contains an error describing the failure.</description></item>
    /// </list>
    /// </returns>
    public Result<IZookeeper> AddZookeeper(Guid id);

    /// <summary>
    /// Adds an animal to the world with specified id
    /// </summary>
    /// <param name="id">Unique identifier for animal</param>
    /// <param name="nickname">Nickname for animal</param>
    /// <returns>
    /// A <see cref="Result{T}"/> where <typeparamref name="T"/> is <see cref="IAnimal"/>.
    /// <list type="bullet">
    ///     <item><description>If successful, <see cref="Result{T}.IsSuccess"/> is <c>true</c>, and <see cref="Result{T}.Value"/> contains the newly created animal.</description></item>
    ///     <item><description>If unsuccessful, <see cref="Result{T}.IsSuccess"/> is <c>false</c>, and <see cref="Result{T}.Error"/> contains an error describing the failure.</description></item>
    /// </list>
    /// </returns>
    public Result<IAnimal> AddAnimal(Guid id, string nickname);

    /// <summary>
    /// Calculates whether specified coordinates are within the bounds of the world
    /// </summary>
    /// <param name="coords">Coordinates to check</param>
    /// <returns><c>true</c> if the specified coordinates are within the bounds of the world, <c>false</c> otherwise</returns>
    public bool IsPointInBounds(GridCoords coords);

    /// <summary>
    /// Calculates whether specified coordinates are within the bounds of the world
    /// </summary>
    /// <param name="x">X coordinate to check</param>
    /// <param name="y">Y coordinate to check</param>
    /// <returns><c>true</c> if the specified coordinates are within the bounds of the world, <c>false</c> otherwise</returns>
    public bool IsPointInBounds(int x, int y);

    /// <summary>
    /// Retrieves the contents of a cell in the world at the specified coordinates
    /// </summary>
    /// <param name="coords">Coordinates from where to read the cell</param>
    /// <returns>A <see cref="CellContents"/> with the value of the retrieved cell</returns>
    public CellContents GetCellContents(GridCoords coords);

    /// <summary>
    /// Retrieves the contents of a cell in the world at the specified coordinates
    /// </summary>
    /// <param name="x">X coordinate from where to read the cell</param>
    /// <param name="y">Y coordinate from where to read the cell</param>
    /// <returns>A <see cref="CellContents"/> with the value of the retrieved cell</returns>
    public CellContents GetCellContents(int x, int y);

    /// <summary>
    /// Sets the contents of a cell in the world at specified coordinates
    /// </summary>
    /// <param name="coords">Coordinates of cell to update</param>
    /// <param name="cellContents">New contents for cell</param>
    public void SetCellContents(GridCoords coords, CellContents cellContents);

    /// <summary>
    /// Retrieves all the traversable neighbours of the cell at the specified coordinates
    /// </summary>
    /// <param name="coords">Coordinates of cell for which to find neighbours</param>
    /// <returns>A <see cref="IEnumerable{T}"/> where <typeparamref name="T"/> is <see cref="GridCoords"/> with all the traversable neighbours of the specified cell</returns>
    public IEnumerable<GridCoords> Neighbours(GridCoords coords);
}
