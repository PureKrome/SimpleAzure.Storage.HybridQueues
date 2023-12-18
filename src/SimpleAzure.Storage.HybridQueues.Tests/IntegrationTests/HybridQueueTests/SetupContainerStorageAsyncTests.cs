namespace WorldDomination.SimpleAzure.Storage.HybridQueues.Tests.IntegrationTests.HybridQueueTests;

public class SetupContainerStorageAsyncTests : CustomAzuriteTestContainer
{
    public SetupContainerStorageAsyncTests() : base(false)
    {
    }

    [Fact]
    public async Task SetupContainerStorageAsync_GivenSomeContainerThatDoesntExist_ShouldCreateTheContainer()
    {
        // Arrange.
        var cancellationToken = CancellationToken.None;

        // Act.
        await HybridQueue.SetupContainerStorageAsync(false, cancellationToken);

        // Assert.
        await AssertContainerExists(ConnectionString, ContainerName, cancellationToken);
    }

    [Fact]
    public async Task SetupContainerStorageAsync_GivenSomeContainerThatAlreadyExists_ShouldNotDoAnything()
    {
        // Arrange.
        var cancellationToken = CancellationToken.None;

        // This creates the container.
        await HybridQueue.SetupContainerStorageAsync(false, cancellationToken);
        await AssertContainerExists(ConnectionString, ContainerName, cancellationToken);

        // Act.
        await HybridQueue.SetupContainerStorageAsync(false, cancellationToken); // No errors should occur.

        // Assert.
        await AssertContainerExists(ConnectionString, ContainerName, cancellationToken); // Container still exists.
    }

    [Fact]
    public async Task SetupQueueStorageAsync_GivenSomeQueueThatDoesntExist_ShouldCreateTheQueue()
    {
        // Arrange.
        var cancellationToken = CancellationToken.None;

        // Act.
        await HybridQueue.SetupContainerStorageAsync(false, cancellationToken);

        // Assert.
        await AssertQueueExists(ConnectionString, QueueName, cancellationToken);
    }

    [Fact]
    public async Task SetupQueueStorageAsync_GivenSomeQueueThatAlreadyExists_ShouldNotDoAnything()
    {
        // Arrange.
        var cancellationToken = CancellationToken.None;

        // This creates the queue.
        await HybridQueue.SetupContainerStorageAsync(false, cancellationToken);
        await AssertQueueExists(ConnectionString, QueueName, cancellationToken);

        // Act.
        await HybridQueue.SetupContainerStorageAsync(false, cancellationToken); // No errors should occur.

        // Assert.
        await AssertQueueExists(ConnectionString, QueueName, cancellationToken); // Queue still exists.
    }

    private static async Task AssertContainerExists(string connectionString, string containerName, CancellationToken cancellationToken)
    {
        var containerClient = new BlobContainerClient(connectionString, containerName);
        var containerExists = await containerClient.ExistsAsync(cancellationToken);
        containerExists.Value.ShouldBeTrue();
    }

    private static async Task AssertQueueExists(string connectionString, string queueName, CancellationToken cancellationToken)
    {
        var queueClient = new QueueClient(connectionString, queueName);
        var queueExists = await queueClient.ExistsAsync(cancellationToken);
        queueExists.Value.ShouldBeTrue();
    }
}
