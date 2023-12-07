namespace WorldDomination.SimpleAzure.Storage.HybridQueues.Tests.IntegrationTests.HybridQueueTests;

public class AddMessagesAsyncTests : CustomAzuriteTestContainer
{
    [Fact]
    public async Task AddMessagesAsync_GivenSomeSimpleStrings_ShouldAddThemAllToTheQueue()
    {
        // Arrange.
        var cancellationToken = CancellationToken.None;
        var messages = new[] { "aaa", "bbb", "ccc", "ddd" };

        // Act.
        await HybridQueue.AddMessagesAsync(messages, cancellationToken);

        // Assert.
        var retrievedMessage = await HybridQueue.GetMessagesAsync<string>(cancellationToken);
        retrievedMessage.ShouldNotBeNull();

        retrievedMessage
            .Select(hm => hm.Content)
            .ToArray()
            .OrderBy(s => s)
            .ShouldBeEquivalentTo(messages.OrderBy(s => s));
    }

    [Fact]
    public async Task AddMessagesAsync_GivenSomeSimpleStringsAndForcedOntoTheBlob_ShouldAddThemAllToTheBlobAndQueue()
    {
        // Arrange.
        var cancellationToken = CancellationToken.None;
        var messages = new[] { "aaa", "bbb", "ccc", "ddd" };

        // Act.
        await HybridQueue.AddMessagesAsync(messages, null, 10, true, cancellationToken);

        // Assert.
        var retrievedMessage = await HybridQueue.GetMessagesAsync<string>(cancellationToken);
        retrievedMessage.ShouldNotBeNull();

        retrievedMessage
            .Select(hm => hm.Content)
            .ToArray()
            .OrderBy(s => s)
            .ShouldBeEquivalentTo(messages.OrderBy(s => s));
    }

    [Fact]
    public async Task AddMessagesAsync_GivenSomeComplexInstances_ShouldAddThemAllToTheQueue()
    {
        // Arrange.
        var cancellationToken = CancellationToken.None;
        var messages = new[]
        {
            new FakeMessage(20),
            new FakeMessage(20),
            new FakeMessage(20),
            new FakeMessage(20),
            new FakeMessage(20)
        };

        // Act.
        await HybridQueue.AddMessagesAsync(messages, cancellationToken);

        // Assert.
        var retrievedMessage = await HybridQueue.GetMessagesAsync<FakeMessage>(cancellationToken);
        retrievedMessage.ShouldNotBeNull();

        retrievedMessage
            .Select(hm => hm.Content!)
            .ToArray()
            .OrderBy(fm => fm.Content)
            .ShouldBeEquivalentTo(messages.OrderBy(fm => fm.Content));
    }

    [Fact]
    public async Task AddMessagesAsync_GivenSomeLargeComplexInstances_ShouldAddThemAllToTheblobAndQueue()
    {
        // Arrange.
        var cancellationToken = CancellationToken.None;
        var messages = new[]
        {
            new FakeMessage(QueueClient.MessageMaxBytes + 1),
            new FakeMessage(QueueClient.MessageMaxBytes + 1),
            new FakeMessage(QueueClient.MessageMaxBytes + 1),
            new FakeMessage(QueueClient.MessageMaxBytes + 1),
            new FakeMessage(QueueClient.MessageMaxBytes + 1)
        };

        // Act.
        await HybridQueue.AddMessagesAsync(messages, cancellationToken);

        // Assert.
        var retrievedMessage = await HybridQueue.GetMessagesAsync<FakeMessage>(cancellationToken);
        retrievedMessage.ShouldNotBeNull();

        retrievedMessage
            .Select(hm => hm.Content!)
            .ToArray()
            .OrderBy(fm => fm.Content)
            .ShouldBeEquivalentTo(messages.OrderBy(fm => fm.Content));
    }
}
