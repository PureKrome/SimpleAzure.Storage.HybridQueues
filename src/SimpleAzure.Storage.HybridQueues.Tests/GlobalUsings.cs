global using Azure.Storage.Blobs;
global using Azure.Storage.Queues;
global using Moq;
global using Shouldly;
global using Xunit;

// xunit.v3 4.0+: run every test in parallel, including tests within the same class.
// Safe here because each test uses its own uniquely named queue and blob container;
// a single Azurite container is shared for the whole run via the AzuriteFixture assembly fixture.
[assembly: Xunit.v3.Parallelization(Mode = Xunit.Sdk.ParallelMode.All)]
[assembly: AssemblyFixture(typeof(WorldDomination.SimpleAzure.Storage.HybridQueues.Tests.IntegrationTests.AzuriteFixture))]
