using Xunit;
using NETCoreBot.Strategy;  // Adjust namespace if needed
using NETCoreBot.Enums;     // Ensure BotAction is accessible
using NETCoreBot.Models;    // Ensure Cell is accessible

namespace NETCoreBot.Tests
{
    public class IsAlignedTests
    {
        [Fact]
        public void IsAligned_NullLastMove_ReturnsFalse()
        {
            // Arrange
            BotAction? lastMove = null;
            int myX = 5, myY = 5;
            var pellet = new Cell { X = 4, Y = 4 };

            // Act
            bool result = Gorilla.IsAligned(lastMove, myX, myY, pellet);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsAligned_UpDirectionAligned_ReturnsTrue()
        {
            // Arrange: For Up, pellet.Y must be less than myY.
            BotAction? lastMove = BotAction.Up;
            int myX = 5, myY = 5;
            var pellet = new Cell { X = 5, Y = 4 };

            // Act
            bool result = Gorilla.IsAligned(lastMove, myX, myY, pellet);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsAligned_UpDirectionNotAligned_ReturnsFalse()
        {
            // Arrange: For Up, pellet.Y must be less than myY.
            BotAction? lastMove = BotAction.Up;
            int myX = 5, myY = 5;
            var pellet = new Cell { X = 5, Y = 6 };

            // Act
            bool result = Gorilla.IsAligned(lastMove, myX, myY, pellet);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsAligned_DownDirectionAligned_ReturnsTrue()
        {
            // Arrange: For Down, pellet.Y must be greater than myY.
            BotAction? lastMove = BotAction.Down;
            int myX = 5, myY = 5;
            var pellet = new Cell { X = 5, Y = 6 };

            // Act
            bool result = Gorilla.IsAligned(lastMove, myX, myY, pellet);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsAligned_DownDirectionNotAligned_ReturnsFalse()
        {
            // Arrange: For Down, pellet.Y must be greater than myY.
            BotAction? lastMove = BotAction.Down;
            int myX = 5, myY = 5;
            var pellet = new Cell { X = 5, Y = 4 };

            // Act
            bool result = Gorilla.IsAligned(lastMove, myX, myY, pellet);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsAligned_LeftDirectionAligned_ReturnsTrue()
        {
            // Arrange: For Left, pellet.X must be less than myX.
            BotAction? lastMove = BotAction.Left;
            int myX = 5, myY = 5;
            var pellet = new Cell { X = 4, Y = 5 };

            // Act
            bool result = Gorilla.IsAligned(lastMove, myX, myY, pellet);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsAligned_LeftDirectionNotAligned_ReturnsFalse()
        {
            // Arrange: For Left, pellet.X must be less than myX.
            BotAction? lastMove = BotAction.Left;
            int myX = 5, myY = 5;
            var pellet = new Cell { X = 6, Y = 5 };

            // Act
            bool result = Gorilla.IsAligned(lastMove, myX, myY, pellet);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsAligned_RightDirectionAligned_ReturnsTrue()
        {
            // Arrange: For Right, pellet.X must be greater than myX.
            BotAction? lastMove = BotAction.Right;
            int myX = 5, myY = 5;
            var pellet = new Cell { X = 6, Y = 5 };

            // Act
            bool result = Gorilla.IsAligned(lastMove, myX, myY, pellet);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsAligned_RightDirectionNotAligned_ReturnsFalse()
        {
            // Arrange: For Right, pellet.X must be greater than myX.
            BotAction? lastMove = BotAction.Right;
            int myX = 5, myY = 5;
            var pellet = new Cell { X = 4, Y = 5 };

            // Act
            bool result = Gorilla.IsAligned(lastMove, myX, myY, pellet);

            // Assert
            Assert.False(result);
        }
    }
}
