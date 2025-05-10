using System;
using Zooscape.Domain.Enums;
using Zooscape.Domain.Interfaces;

namespace Zooscape.Application.Services;

public interface IZookeeperService
{
    /// <summary>
    /// Calculates new direction for zookeeper based on pathfinding algorithm to target animal
    /// </summary>
    /// <param name="world"></param>
    /// <param name="zookeeperId"></param>
    /// <returns>A <see cref="Direction"/> value with new direction</returns>
    public Direction CalculateZookeeperDirection(IWorld world, Guid zookeeperId);
}
