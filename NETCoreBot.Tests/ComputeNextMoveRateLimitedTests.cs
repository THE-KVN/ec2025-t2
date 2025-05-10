using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using NETCoreBot.Strategy;   // Contains Gorilla and GorillaNode
using NETCoreBot.Models;     // Contains Animal, etc.
using NETCoreBot.Enums;      // Contains BotAction

namespace NETCoreBot.Tests
{
    public class ComputeNextMoveRateLimitedTests
    {
        [Fact]
        public void ComputeNextMoveRateLimited_EmptyPath_ReturnsNull()
        {
            // Arrange
            var animal = new Animal { X = 5, Y = 5 };
            var path = new List<GorillaNode>(); // empty path

            // Act
            var result = Gorilla.ComputeNextMoveRateLimited(animal, path);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void ComputeNextMoveRateLimited_AnimalNotOnFirstStep_ReturnsNull()
        {
            // Arrange
            var animal = new Animal { X = 5, Y = 5 };
            // Create a path where the first node does NOT match animal's position.
            var node1 = new GorillaNode(4, 5, 0, 0, null);
            var node2 = new GorillaNode(5, 5, 0, 0, node1);
            var path = new List<GorillaNode> { node1, node2 };

            // Act
            var result = Gorilla.ComputeNextMoveRateLimited(animal, path);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void ComputeNextMoveRateLimited_PathCountLessThanTwo_ReturnsNull()
        {
            // Arrange
            var animal = new Animal { X = 5, Y = 5 };
            // Only one node in the path that matches animal's position.
            var node1 = new GorillaNode(5, 5, 0, 0, null);
            var path = new List<GorillaNode> { node1 };

            // Act
            var result = Gorilla.ComputeNextMoveRateLimited(animal, path);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void ComputeNextMoveRateLimited_NormalMoveWithoutReversal_ReturnsAction()
        {
            // Arrange
            var animal = new Animal { X = 5, Y = 5 };
            // Create a simple two-node path:
            // Move from (5,5) to (5,6) should yield BotAction.Down.
            var node1 = new GorillaNode(5, 5, 0, 0, null);
            var node2 = new GorillaNode(5, 6, 1, 0, node1);
            var path = new List<GorillaNode> { node1, node2 };

            // Ensure no reversal logic is triggered by clearing LastMove.
            Gorilla.LastMove = null;

            // Act
            var result = Gorilla.ComputeNextMoveRateLimited(animal, path);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(BotAction.Down, result);
        }

        [Fact]
        public void ComputeNextMoveRateLimited_ReversalWithAlternative_ReturnsAlternativeActionAndRemovesSecondStep()
        {
            // Arrange
            var animal = new Animal { X = 5, Y = 5 };
            // Set LastMove so that its opposite equals the initial computed action.
            // For example, LastMove = BotAction.Left, so Opposite(Left) = Right.
            Gorilla.LastMove = BotAction.Left;

            // Build a path:
            // First node at (5,5) matches the animal.
            // Second node: set to (6,5) so GetDirection returns Right.
            // Third node: choose (6,6) so that GetDirection from second to third returns Down.
            var node1 = new GorillaNode(5, 5, 0, 0, null);
            var node2 = new GorillaNode(6, 5, 1, 0, node1); // Direction from node1 to node2 is Right.
            var node3 = new GorillaNode(6, 6, 2, 0, node2); // Direction from node2 to node3 is Down.
            var path = new List<GorillaNode> { node1, node2, node3 };

            // Set REVERSAL_RANDOM_SKIP_CHANCE to 0 so that the random skip does not occur.
            Gorilla.REVERSAL_RANDOM_SKIP_CHANCE = 0.0;

            // Act
            var result = Gorilla.ComputeNextMoveRateLimited(animal, path);

            // Assert
            // Since the initial action (Right) equals Opposite(Left), the method should check for an alternative.
            // The alternative action computed from node2 to node3 is Down, which is not Opposite(LastMove) (which is Right).
            // Therefore, the expected action is Down.
            Assert.NotNull(result);
            Assert.Equal(BotAction.Down, result);
            // Also, node2 should have been removed from the path.
            Assert.Equal(2, path.Count);
            Assert.Same(node1, path[0]);
            Assert.Same(node3, path[1]);
        }

        [Fact]
        public void ComputeNextMoveRateLimited_ReversalNoAlternative_RandomSkipTriggered_ReturnsNull()
        {
            // Arrange
            var animal = new Animal { X = 5, Y = 5 };
            // Set LastMove to BotAction.Left (Opposite = Right).
            Gorilla.LastMove = BotAction.Left;

            // Create a path with exactly 2 nodes (no third node available for an alternative).
            var node1 = new GorillaNode(5, 5, 0, 0, null);
            var node2 = new GorillaNode(6, 5, 1, 0, node1); // Direction = Right.
            var path = new List<GorillaNode> { node1, node2 };

            // Force the random skip to occur by setting REVERSAL_RANDOM_SKIP_CHANCE to 1.
            Gorilla.REVERSAL_RANDOM_SKIP_CHANCE = 1.0;

            // Act
            var result = Gorilla.ComputeNextMoveRateLimited(animal, path);

            // Assert
            // Expect null because random skip should force no move.
            Assert.Null(result);
        }

        [Fact]
        public void ComputeNextMoveRateLimited_ReversalNoAlternative_RandomSkipNotTriggered_ReturnsReversalAction()
        {
            // Arrange
            var animal = new Animal { X = 5, Y = 5 };
            // Set LastMove to BotAction.Left (Opposite = Right).
            Gorilla.LastMove = BotAction.Left;

            // Create a path with exactly 2 nodes.
            var node1 = new GorillaNode(5, 5, 0, 0, null);
            var node2 = new GorillaNode(6, 5, 1, 0, node1); // Direction = Right.
            var path = new List<GorillaNode> { node1, node2 };

            // Set REVERSAL_RANDOM_SKIP_CHANCE to 0 to avoid random skip.
            Gorilla.REVERSAL_RANDOM_SKIP_CHANCE = 0.0;

            // Act
            var result = Gorilla.ComputeNextMoveRateLimited(animal, path);

            // Assert
            // With no alternative available and random skip disabled, the method returns the initial action.
            // The initial action is Right (which is the opposite of LastMove), so it should be returned.
            Assert.NotNull(result);
            Assert.Equal(BotAction.Right, result);
        }
    }
}
