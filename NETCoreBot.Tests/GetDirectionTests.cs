using Xunit;
using NETCoreBot.Enums;     // Ensure BotAction is accessible
using NETCoreBot.Strategy;   // Adjust namespace if GetDirection is defined here

namespace NETCoreBot.Tests
{
    public class GetDirectionTests
    {
        [Theory]
        // Right: nextX > currentX (ignores Y differences)
        [InlineData(0, 0, 1, 0, BotAction.Right)]
        // Left: nextX < currentX
        [InlineData(0, 0, -1, 0, BotAction.Left)]
        // Down: nextX equals currentX, nextY > currentY
        [InlineData(0, 0, 0, 1, BotAction.Down)]
        // Up: nextX equals currentX, nextY < currentY
        [InlineData(0, 0, 0, -1, BotAction.Up)]
        // No movement: same coordinates return null
        [InlineData(5, 5, 5, 5, null)]
        // Diagonal move: with both differences, condition for X is evaluated first (returns Right)
        [InlineData(1, 1, 2, 2, BotAction.Right)]
        public void GetDirection_ReturnsExpectedDirection(int currentX, int currentY, int nextX, int nextY, BotAction? expected)
        {
            // Act
            BotAction? result = Gorilla.GetDirection(currentX, currentY, nextX, nextY);

            // Assert
            Assert.Equal(expected, result);
        }
    }
}
