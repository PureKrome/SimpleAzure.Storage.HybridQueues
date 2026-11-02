using Microsoft.Extensions.Logging.Testing;

namespace WorldDomination.SimpleAzure.Storage.HybridQueues.Tests.IntegrationTests;

/// <summary>
/// Base class for integration tests. The Azurite container is shared (see <see cref="AzuriteFixture"/>),
/// so each test instance gets its own uniquely named queue and blob container to stay isolated
/// when tests run in parallel.
/// </summary>
public class CustomAzuriteTestContainer : IAsyncLifetime
{
    private readonly bool _areResourcesCreated;
    private QueueClient? _queueClient;
    private BlobContainerClient? _blobContainerClient;
    private IHybridQueue? _hybridQueue;

    public CustomAzuriteTestContainer(AzuriteFixture azuriteFixture, bool areResourcesCreated = true)
    {
        ConnectionString = azuriteFixture.ConnectionString;
        _areResourcesCreated = areResourcesCreated;
    }

    protected string QueueName { get; } = $"test-queue-{Guid.NewGuid():N}";

    protected string ContainerName { get; } = $"test-container-{Guid.NewGuid():N}";

    protected string ConnectionString { get; }

    protected QueueClient QueueClient { get { return _queueClient!; } }

    protected BlobContainerClient BlobContainerClient { get { return _blobContainerClient!; } }

    protected IHybridQueue HybridQueue { get { return _hybridQueue!; } }

    protected FakeLogger<HybridQueue> Logger { get; } = new FakeLogger<HybridQueue>();

    public async ValueTask InitializeAsync()
    {
        _queueClient = new QueueClient(ConnectionString, QueueName);
        _blobContainerClient = new BlobContainerClient(ConnectionString, ContainerName);

        if (_areResourcesCreated)
        {
            await QueueClient.CreateIfNotExistsAsync();
            await BlobContainerClient.CreateIfNotExistsAsync();
        }

        _hybridQueue = new HybridQueue(QueueClient, BlobContainerClient, Logger);
    }

    public async ValueTask DisposeAsync()
    {
        // The container outlives the test, so remove this test's resources.
        await QueueClient.DeleteIfExistsAsync();
        await BlobContainerClient.DeleteIfExistsAsync();
    }
}
