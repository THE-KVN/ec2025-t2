using Xunit;
using NETCoreBot.Strategy;  // Adjust if your Gorilla and GorillaNode classes are in a different namespace
using NETCoreBot.Models;    // Contains Animal, Cell, etc.

namespace NETCoreBot.Tests
{
    public class ReconstructPathTests
    {
        [Fact]
        public void ReconstructPath_ReturnsCorrectOrder_ForChainOfNodes()
        {
            // Arrange: Create a chain of nodes.
            // The chain is: root -> child -> final.
            var root = new GorillaNode(0, 0, 0, 0, null);
            var child = new GorillaNode(1, 1, 1, 1, root);
            var final = new GorillaNode(2, 2, 2, 2, child);

            // Act: Reconstruct the path from the final node.
            var path = Gorilla.ReconstructPath(final);

            // Assert: The path should be in order [root, child, final].
            Assert.Equal(3, path.Count);
            Assert.Same(root, path[0]);
            Assert.Same(child, path[1]);
            Assert.Same(final, path[2]);
        }

        [Fact]
        public void ReconstructPath_ReturnsSingleElement_ForSingleNode()
        {
            // Arrange: Create a single node with no parent.
            var single = new GorillaNode(5, 5, 0, 0, null);

            // Act
            var path = Gorilla.ReconstructPath(single);

            // Assert: The path should contain just the single node.
            Assert.Single(path);
            Assert.Same(single, path[0]);
        }
    }
}
