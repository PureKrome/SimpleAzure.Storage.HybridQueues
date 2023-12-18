namespace WorldDomination.SimpleAzure.Storage.HybridQueues.Tests.IntegrationTests.HybridQueueTests;

public class SetupContainerStorageAsyncTests : CustomAzuriteTestContainer
{
    public SetupContainerStorageAsyncTests() : base(false)
    {
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task SetupContainerStorageAsync_GivenSomeContainerThatDoesntExist_ShouldCreateTheContainer(bool isLoggingEnabled)
    {
        // Arrange.
        var cancellationToken = CancellationToken.None;

        // Act.
        await HybridQueue.SetupContainerStorageAsync(isLoggingEnabled, cancellationToken);

        // Assert.
        await AssertContainerExists(ConnectionString, ContainerName, cancellationToken);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task SetupContainerStorageAsync_GivenSomeContainerThatAlreadyExists_ShouldNotDoAnything(bool isLoggingEnabled)
    {
        // Arrange.
        var cancellationToken = CancellationToken.None;

        // This creates the container.
        await HybridQueue.SetupContainerStorageAsync(isLoggingEnabled, cancellationToken);
        await AssertContainerExists(ConnectionString, ContainerName, cancellationToken);

        // Act.
        await HybridQueue.SetupContainerStorageAsync(isLoggingEnabled, cancellationToken); // No errors should occur.

        // Assert.
        await AssertContainerExists(ConnectionString, ContainerName, cancellationToken); // Container still exists.
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task SetupQueueStorageAsync_GivenSomeQueueThatDoesntExist_ShouldCreateTheQueue(bool isLoggingEnabled)
    {
        // Arrange.
        var cancellationToken = CancellationToken.None;

        // Act.
        await HybridQueue.SetupContainerStorageAsync(isLoggingEnabled, cancellationToken);

        // Assert.
        await AssertQueueExists(ConnectionString, QueueName, cancellationToken);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task SetupQueueStorageAsync_GivenSomeQueueThatAlreadyExists_ShouldNotDoAnything(bool isLoggingEnabled)
    {
        // Arrange.
        var cancellationToken = CancellationToken.None;

        // This creates the queue.
        await HybridQueue.SetupContainerStorageAsync(isLoggingEnabled, cancellationToken);
        await AssertQueueExists(ConnectionString, QueueName, cancellationToken);

        // Act.
        await HybridQueue.SetupContainerStorageAsync(isLoggingEnabled, cancellationToken); // No errors should occur.

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
