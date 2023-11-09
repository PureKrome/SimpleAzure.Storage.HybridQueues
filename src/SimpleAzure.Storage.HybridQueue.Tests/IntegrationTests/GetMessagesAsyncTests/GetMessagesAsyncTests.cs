using Shouldly;

namespace WorldDomination.SimpleAzure.Storage.HybridQueues.Tests.IntegrationTests.GetMessagesAsyncTests
{
    public class GetMessagesAsyncTests : CustomAzuriteTestContainer
    {
        [Theory]
        [InlineData(0)]
        [InlineData(33)]
        public async Task GetMessagesAsync_GivenAnInvalidMaxMessages_ShouldThrowAnException(int maxMessages)
        {
            // Arrange.
            var cancellationToken = CancellationToken.None;

            // Act.
            var exception = await Should.ThrowAsync<ArgumentOutOfRangeException>(HybridQueue.GetMessagesAsync<string>(
                maxMessages,
                null,
                cancellationToken));

            // Assert.
            exception.ShouldNotBeNull();
        }
    }
}
