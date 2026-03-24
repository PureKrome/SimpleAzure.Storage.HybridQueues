namespace WorldDomination.SimpleAzure.Storage.HybridQueues.Tests.UnitTests.HybridMessageTests;

public class HybridMessageTests
{
    [Fact]
    public void HybridMessage_GivenValidParameters_ShouldCreateSuccessfully()
    {
        // Arrange.
        var content = "test content";
        var messageId = "msg-123";
        var popReceipt = "receipt-456";
        var blobId = Guid.NewGuid();

        // Act.
        var message = new HybridMessage<string>(content, messageId, popReceipt, blobId);

        // Assert.
        message.Content.ShouldBe(content);
        message.MessageId.ShouldBe(messageId);
        message.PopReceipt.ShouldBe(popReceipt);
        message.BlobId.ShouldBe(blobId);
    }

    [Fact]
    public void HybridMessage_GivenNullBlobId_ShouldCreateSuccessfully()
    {
        // Arrange.
        var content = "test content";
        var messageId = "msg-123";
        var popReceipt = "receipt-456";

        // Act.
        var message = new HybridMessage<string>(content, messageId, popReceipt, null);

        // Assert.
        message.Content.ShouldBe(content);
        message.MessageId.ShouldBe(messageId);
        message.PopReceipt.ShouldBe(popReceipt);
        message.BlobId.ShouldBeNull();
    }

    [Fact]
    public void HybridMessage_GivenComplexContent_ShouldCreateSuccessfully()
    {
        // Arrange.
        var content = new { Name = "Test", Value = 123 };
        var messageId = "msg-123";
        var popReceipt = "receipt-456";
        var blobId = Guid.NewGuid();

        // Act.
        var message = new HybridMessage<object>(content, messageId, popReceipt, blobId);

        // Assert.
        message.Content.ShouldBe(content);
        message.MessageId.ShouldBe(messageId);
        message.PopReceipt.ShouldBe(popReceipt);
        message.BlobId.ShouldBe(blobId);
    }
}
