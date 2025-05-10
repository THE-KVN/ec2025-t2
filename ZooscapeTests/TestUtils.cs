using System;
using System.Collections.Generic;
using Zooscape.Domain.Enums;
using Zooscape.Domain.Interfaces;
using Zooscape.Domain.Models;

namespace ZooscapeTests;

public class TestUtils
{
    public static List<Guid> GenerateAnimalIds(int length = 4)
    {
        var animalIds = new List<Guid>();
        for (int i = 0; i < length; i++)
        {
            animalIds.Add(Guid.NewGuid());
        }

        return animalIds;
    }

    public static void QueueAnimalAction(IAnimal animal, BotAction direction)
    {
        animal.AddCommand(new AnimalCommand(animal.Id, direction));
    }
}
