using NETCoreBot.Enums;
using NETCoreBot.Models;
using NETCoreBot.Strategy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NETCoreBot.Tests
{
    //public  class AvoidContestedPelletTests
    //{
    //    [Fact]
    //    public void AvoidsContestedPellet_IfAnotherAnimalIsCloser()
    //    {
    //        // Arrange
    //        var myAnimal = new Animal { Id = Guid.NewGuid(), X = 1, Y = 1, Score = 100 };
    //        var otherAnimal = new Animal { Id = Guid.NewGuid(), X = 1, Y = 3 };
    //        var pellet = new Cell { X = 1, Y = 5, Content = CellContent.Pellet };

    //        var cells = new List<Cell>
    //        {
    //            new Cell { X = 1, Y = 1, Content = CellContent.Empty },
    //            new Cell { X = 1, Y = 3, Content = CellContent.Empty },
    //            pellet
    //        };

    //        var gameState = new GameState
    //        {
    //            Animals = new List<Animal> { myAnimal, otherAnimal },
    //            Cells = cells,
    //            Zookeepers = new List<Zookeeper>()
    //        };

    //        // Act
    //        var result = Penguin.ShouldAvoidTarget(pellet.X, pellet.Y, myAnimal, gameState);

    //        // Assert
    //        Assert.True(result); // The pellet is contested by a closer animal
    //    }

    //}
}
