using System.Collections.Generic;

namespace Zooscape.Domain.Interfaces;

public interface IWinCondtion
{
    List<IAnimal> GetWinners(List<IAnimal> animals);
}
