using Xunit;
using NETCoreBot.Enums;     // Ensure BotAction is accessible
using NETCoreBot.Strategy;   // Adjust namespace if Opposite is defined here

namespace NETCoreBot.Tests
{
    public class OppositeTests
    {
        [Theory]
        [InlineData(BotAction.Up, BotAction.Down)]
        [InlineData(BotAction.Down, BotAction.Up)]
        [InlineData(BotAction.Left, BotAction.Right)]
        [InlineData(BotAction.Right, BotAction.Left)]
        public void Opposite_ReturnsExpectedOpposite(BotAction input, BotAction expected)
        {
            // Act
            BotAction result = Gorilla.Opposite(input);

            // Assert
            Assert.Equal(expected, result);
        }
    }
}
