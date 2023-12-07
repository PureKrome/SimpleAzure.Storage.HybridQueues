namespace WorldDomination.SimpleAzure.Storage.HybridQueues.Tests;

internal class FakeMessage
{
    public string Content { get; set; } = default!;

    public FakeMessage()
    {
    }

    public FakeMessage(int length)
    {
        Content = GenerateContent(length);
    }

    private static string GenerateContent(int length)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(length);

        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

        var random = Random.Shared;

        var content = new string(Enumerable
            .Repeat(chars, length)
            .Select(s => s[random.Next(s.Length)])
            .ToArray());

        return content;
    }
}
