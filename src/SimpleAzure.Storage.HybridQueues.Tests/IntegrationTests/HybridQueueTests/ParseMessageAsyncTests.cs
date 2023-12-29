using Azure.Storage.Queues.Models;


namespace WorldDomination.SimpleAzure.Storage.HybridQueues.Tests.IntegrationTests.HybridQueueTests
{
    public class ParseMessageAsyncTests : CustomAzuriteTestContainer
    {
        public record FakePerson(string Name, int Age);

        [Theory]
        [InlineData("{\"Name\": \"Anabel\", \"Age\": 30}")] // Note: Capitalized keys.
        [InlineData("{\"name\": \"Anabel\", \"age\": 30}")] // Note: Lowercase keys.
        [InlineData("{\"nAmE\": \"Anabel\", \"aGe\": 30}")] // Note: Mixed case keys.
        public async Task ParseMessageAsync_LowercaseJsonKeys_CaseInsensitive(string json)
        {
            // Arrange.
            var queueMessage = QueuesModelFactory.QueueMessage(
                "1",
                "2",
                new BinaryData(json),
                0);

            // Act.
            var fakePerson = await HybridQueue.ParseMessageAsync<FakePerson>(queueMessage, CancellationToken.None);

            // Assert.
            fakePerson.ShouldNotBeNull();
            fakePerson.Content.ShouldNotBeNull();
            fakePerson.Content.Name.ShouldBe("Anabel");
            fakePerson.Content.Age.ShouldBe(30);
        }
    }
}
