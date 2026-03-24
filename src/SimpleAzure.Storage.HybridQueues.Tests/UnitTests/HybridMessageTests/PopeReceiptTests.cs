namespace WorldDomination.SimpleAzure.Storage.HybridQueues.Tests.UnitTests.HybridMessageTests;

public class PopeReceiptTests
{
    [Fact]
    public void PopeReceipt_WhenGettingValue_ShouldReturnPopReceiptValue()
    {
        // Arrange.
        var content = "test content";
        var messageId = "msg-123";
        var popReceipt = "receipt-456";
        var blobId = Guid.NewGuid();
        var message = new HybridMessage<string>(content, messageId, popReceipt, blobId);

        // Act.
        var retrievedValue = message.PopeReceipt;

        // Assert.
        retrievedValue.ShouldBe(popReceipt);
        retrievedValue.ShouldBe(message.PopReceipt);
    }

    [Theory]
    [InlineData("receipt-456", "receipt-789")]
    [InlineData("", "new-receipt")]
    [InlineData("original", "")]
    public void PopeReceipt_WhenSettingValueViaInit_ShouldUpdatePopReceiptValue(string originalPopReceipt, string newPopReceipt)
    {
        // Arrange.
        var content = "test content";
        var messageId = "msg-123";
        var blobId = Guid.NewGuid();

        // Act.
        var message = new HybridMessage<string>(content, messageId, originalPopReceipt, blobId) with { PopeReceipt = newPopReceipt };

        // Assert.
        message.PopReceipt.ShouldBe(newPopReceipt);
        message.PopeReceipt.ShouldBe(newPopReceipt);
    }

    [Theory]
    [InlineData("receipt-456", "receipt-789")]
    [InlineData("", "new-receipt")]
    [InlineData("original-value", "modified-value")]
    public void PopeReceipt_WhenModifyingWithProperty_ShouldCreateNewInstanceWithUpdatedValue(string originalPopReceipt, string newPopReceipt)
    {
        // Arrange.
        var content = "test content";
        var messageId = "msg-123";
        var blobId = Guid.NewGuid();
        var originalMessage = new HybridMessage<string>(content, messageId, originalPopReceipt, blobId);

        // Act.
        var modifiedMessage = originalMessage with { PopeReceipt = newPopReceipt };

        // Assert.
        // Original message should remain unchanged.
        originalMessage.PopReceipt.ShouldBe(originalPopReceipt);
        originalMessage.PopeReceipt.ShouldBe(originalPopReceipt);

        // New message should have updated value.
        modifiedMessage.PopReceipt.ShouldBe(newPopReceipt);
        modifiedMessage.PopeReceipt.ShouldBe(newPopReceipt);
    }

    [Theory]
    [InlineData("receipt-456")]
    [InlineData("")]
    [InlineData("very-long-receipt-value-with-many-characters")]
    public void PopeReceipt_WhenBothPropertiesUsed_ShouldMaintainConsistency(string popReceipt)
    {
        // Arrange.
        var content = "test content";
        var messageId = "msg-123";
        var blobId = Guid.NewGuid();

        // Act.
        var message = new HybridMessage<string>(content, messageId, popReceipt, blobId);

        // Assert.
        // Both PopReceipt and PopeReceipt should return the same value.
        message.PopReceipt.ShouldBe(message.PopeReceipt);
        message.PopReceipt.ShouldBe(popReceipt);
        message.PopeReceipt.ShouldBe(popReceipt);
    }

    [Theory]
    [InlineData("receipt-123")]
    [InlineData("")]
    [InlineData("very-long-receipt-value-with-many-characters")]
    public void PopeReceipt_GivenVariousReceiptValues_ShouldMaintainValueConsistency(string receiptValue)
    {
        // Arrange.
        var content = "test content";
        var messageId = "msg-123";
        var blobId = Guid.NewGuid();

        // Act.
        var message = new HybridMessage<string>(content, messageId, receiptValue, blobId);

        // Assert.
        message.PopeReceipt.ShouldBe(receiptValue);
        message.PopReceipt.ShouldBe(receiptValue);
    }
}
