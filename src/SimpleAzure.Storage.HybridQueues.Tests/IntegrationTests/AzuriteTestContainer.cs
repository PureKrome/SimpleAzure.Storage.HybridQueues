using Microsoft.Extensions.Logging.Testing;
using Testcontainers.Azurite;

namespace WorldDomination.SimpleAzure.Storage.HybridQueues.Tests.IntegrationTests;

public class CustomAzuriteTestContainer : IAsyncLifetime
{
    protected const string QueueName = "test-queue";
    protected const string ContainerName = "test-container";

    private readonly AzuriteContainer _azuriteContainer;
    private QueueClient? _queueClient;
    private BlobContainerClient? _blobContainerClient;
    private IHybridQueue? _hybridQueue;
    private readonly bool _areResourcesCreated;

    public CustomAzuriteTestContainer(bool areResourcesCreated)
    {
        _areResourcesCreated = areResourcesCreated;

        _azuriteContainer = new AzuriteBuilder()
            .WithImage("mcr.microsoft.com/azure-storage/azurite")
            .WithCommand("--skipApiVersionCheck")
            .Build();
    }

    public CustomAzuriteTestContainer() : this(true)
    {
    }

    protected string ConnectionString { get; set; } = default!;

    protected QueueClient QueueClient { get { return _queueClient!; } }

    protected BlobContainerClient BlobContainerClient { get { return _blobContainerClient!; } }

    protected IHybridQueue HybridQueue { get { return _hybridQueue!; } }

    protected FakeLogger<HybridQueue> Logger { get; } = new FakeLogger<HybridQueue>();

    public async Task DisposeAsync()
    {
        await _azuriteContainer.DisposeAsync();
    }

    public async Task InitializeAsync()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));
        await _azuriteContainer.StartAsync(cts.Token);

        var blobPort = _azuriteContainer.GetMappedPublicPort(AzuriteBuilder.BlobPort);
        var queuePort = _azuriteContainer.GetMappedPublicPort(AzuriteBuilder.QueuePort);

        // Have to use a custom string because of the random port numbers.
        // REF: https://learn.microsoft.com/en-us/azure/storage/common/storage-configure-connection-string
        ConnectionString = $@"DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;BlobEndpoint=http://127.0.0.1:{blobPort}/devstoreaccount1;QueueEndpoint=http://127.0.0.1:{queuePort}/devstoreaccount1;";

        _queueClient = new QueueClient(ConnectionString, QueueName);
        _blobContainerClient = new BlobContainerClient(ConnectionString, ContainerName);

        if (_areResourcesCreated)
        {
            await QueueClient.CreateIfNotExistsAsync(cancellationToken: cts.Token);
            await BlobContainerClient.CreateIfNotExistsAsync(cancellationToken: cts.Token);
        }

        _hybridQueue = new HybridQueue(QueueClient, BlobContainerClient, Logger);
    }
}
