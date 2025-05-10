using Xunit;
using System.Collections.Generic;
using NETCoreBot.Strategy;   // Contains Gorilla (and its Manhattan method, if needed)
using NETCoreBot.Models;     // Contains Animal, GameState, Zookeeper, Cell, etc.
using NETCoreBot.Enums;      // Contains CellContent, BotAction, etc.

namespace NETCoreBot.Tests
{
    public class IsSafeToLeaveSpawnTests
    {
        [Fact]
        public void IsSafeToLeaveSpawn_NoZookeepers_ReturnsTrue()
        {
            // Arrange: Animal at (5,5), no zookeepers in state.
            var animal = new Animal { X = 5, Y = 5 };
            var state = new GameState
            {
                Zookeepers = new List<Zookeeper>(),
                // Other properties (like Cells) can be empty for this test.
            };
            int safeDistance = 3;

            // Act
            bool safe = Gorilla.IsSafeToLeaveSpawn(animal, state, safeDistance);

            // Assert
            Assert.True(safe);
        }

        [Fact]
        public void IsSafeToLeaveSpawn_ZookeeperAtExactSafeDistance_ReturnsTrue()
        {
            // Arrange:
            // Animal at (5,5). We want a zookeeper exactly safeDistance away.
            // For Manhattan distance = safeDistance (3), one option is (8,5): |8-5| + |5-5| = 3.
            var animal = new Animal { X = 5, Y = 5 };
            var zookeeper = new Zookeeper { X = 8, Y = 5 };
            var state = new GameState
            {
                Zookeepers = new List<Zookeeper> { zookeeper }
            };
            int safeDistance = 3;

            // Act
            bool safe = Gorilla.IsSafeToLeaveSpawn(animal, state, safeDistance);

            // Assert: Since distance equals safeDistance, it should be considered safe.
            Assert.True(safe);
        }

        [Fact]
        public void IsSafeToLeaveSpawn_ZookeeperTooClose_ReturnsFalse()
        {
            // Arrange:
            // Animal at (5,5), safeDistance = 3. Place a zookeeper at (6,5): Manhattan = |6-5| + |5-5| = 1.
            var animal = new Animal { X = 5, Y = 5 };
            var zookeeper = new Zookeeper { X = 6, Y = 5 };
            var state = new GameState
            {
                Zookeepers = new List<Zookeeper> { zookeeper }
            };
            int safeDistance = 3;

            // Act
            bool safe = Gorilla.IsSafeToLeaveSpawn(animal, state, safeDistance);

            // Assert: The zookeeper is too close, so it should not be safe.
            Assert.False(safe);
        }

        [Fact]
        public void IsSafeToLeaveSpawn_MultipleZookeepers_AtLeastOneTooClose_ReturnsFalse()
        {
            // Arrange:
            // Animal at (5,5), safeDistance = 3.
            // One zookeeper is at (8,5) (distance = 3, safe), another at (4,4) (distance = |4-5|+|4-5|=2, unsafe).
            var animal = new Animal { X = 5, Y = 5 };
            var zookeeperSafe = new Zookeeper { X = 8, Y = 5 };   // Manhattan = 3
            var zookeeperClose = new Zookeeper { X = 4, Y = 4 };  // Manhattan = 2
            var state = new GameState
            {
                Zookeepers = new List<Zookeeper> { zookeeperSafe, zookeeperClose }
            };
            int safeDistance = 3;

            // Act
            bool safe = Gorilla.IsSafeToLeaveSpawn(animal, state, safeDistance);

            // Assert: Because one zookeeper is too close, it should return false.
            Assert.False(safe);
        }
    }
}
