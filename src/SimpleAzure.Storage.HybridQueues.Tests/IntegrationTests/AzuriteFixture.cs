using Testcontainers.Azurite;

namespace WorldDomination.SimpleAzure.Storage.HybridQueues.Tests.IntegrationTests;

/// <summary>
/// An xUnit assembly fixture: exactly one Azurite container is started for the whole test run,
/// shared by every test, and disposed when the run finishes.
/// </summary>
/// <remarks>
/// Because tests run in parallel against this single container, each test must use its own
/// uniquely named queue and blob container (see <see cref="CustomAzuriteTestContainer"/>).
/// If the process dies before disposal, the Testcontainers resource reaper (Ryuk) removes the container.
/// </remarks>
public sealed class AzuriteFixture : IAsyncLifetime
{
    private readonly AzuriteContainer _azuriteContainer = new AzuriteBuilder("mcr.microsoft.com/azure-storage/azurite")
        // Temporary workaround for Azurite compatibility with the latest Azure SDK.
        // This flag should be removed once Azurite is updated to support the latest API versions.
        .WithCommand("--skipApiVersionCheck")
        .Build();

    public string ConnectionString { get; private set; } = default!;

    public async ValueTask InitializeAsync()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));
        await _azuriteContainer.StartAsync(cts.Token);

        // Azurite listens on fixed ports *inside* the container (Blob 10000, Queue 10001, Table 10002).
        // Testcontainers publishes each of those to a random free port on the host, so that parallel
        // test runs (or other containers/Azurite instances on this machine) never fight over a port.
        // GetMappedPublicPort translates "container port" -> "the random host port", which is what
        // our test code (running on the host, not in the container) must connect to.
        // AzuriteBuilder.BlobPort / QueuePort are the *container-side* ports, used here as the lookup key.
        var blobPort = _azuriteContainer.GetMappedPublicPort(AzuriteBuilder.BlobPort);
        var queuePort = _azuriteContainer.GetMappedPublicPort(AzuriteBuilder.QueuePort);

        // Have to use a custom string because of the random port numbers.
        // REF: https://learn.microsoft.com/en-us/azure/storage/common/storage-configure-connection-string
        ConnectionString = $@"DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;BlobEndpoint=http://127.0.0.1:{blobPort}/devstoreaccount1;QueueEndpoint=http://127.0.0.1:{queuePort}/devstoreaccount1;";
    }

    public ValueTask DisposeAsync()
    {
        return _azuriteContainer.DisposeAsync();
    }
}
