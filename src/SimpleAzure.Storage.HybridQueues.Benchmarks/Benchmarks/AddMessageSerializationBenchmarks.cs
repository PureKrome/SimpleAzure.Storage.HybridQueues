using System.Text;
using System.Text.Json;
using BenchmarkDotNet.Attributes;

namespace WorldDomination.SimpleAzure.Storage.HybridQueues.Benchmarks.Benchmarks;

/// <summary>
/// Benchmarks that compare the current serialization path in <c>AddMessageAsync</c> with the
/// proposed <c>JsonSerializer.SerializeToUtf8Bytes</c> optimization.
///
/// The current path for complex types:
///   1. <c>JsonSerializer.Serialize(item)</c>            → allocates a UTF-16 <c>string</c>
///   2. <c>Encoding.UTF8.GetByteCount(message)</c>       → scans the string a second time
///   3. <c>Encoding.UTF8.GetBytes(message)</c>           → re-encodes UTF-16 → UTF-8 for blob
///      (equivalent to what <c>new BinaryData(string)</c> does internally)
///
/// The proposed path:
///   1. <c>JsonSerializer.SerializeToUtf8Bytes(item)</c> → allocates UTF-8 bytes directly
///   2. <c>blobBytes.Length</c>                          → O(1) array property, no scan
///   3. bytes are passed directly to blob upload         → no re-encoding
///   4. <c>Encoding.UTF8.GetString(blobBytes)</c>        → only when item fits in the queue
/// </summary>
[MemoryDiagnoser]
[SimpleJob]
public class AddMessageSerializationBenchmarks
{
    // ---------------------------------------------------------------------------
    // Payload types
    // ---------------------------------------------------------------------------

    /// <summary>Represents a small complex payload (≈ 200 B serialized).</summary>
    public sealed record SmallPayload(string Name, int Age, bool IsActive, DateTimeOffset CreatedAt);

    /// <summary>Represents a medium complex payload (≈ 5 KB serialized).</summary>
    public sealed record MediumPayload(string Name, string Description, string[] Tags);

    /// <summary>Represents a large complex payload (≈ 60 KB serialized — exceeds the 48 KB queue limit).</summary>
    public sealed record LargePayload(string Name, string Content);

    // ---------------------------------------------------------------------------
    // Fields populated by [GlobalSetup]
    // ---------------------------------------------------------------------------

    private SmallPayload _small = default!;
    private MediumPayload _medium = default!;
    private LargePayload _large = default!;

    [GlobalSetup]
    public void Setup()
    {
        _small = new SmallPayload("Alice", 30, true, DateTimeOffset.UtcNow);

        _medium = new MediumPayload(
            "Medium item",
            new string('a', 4096),
            ["tag1", "tag2", "tag3", "tag4", "tag5"]);

        // ~60 KB of content so it exceeds the 48 KB queue-message limit.
        _large = new LargePayload("Large item", new string('x', 60_000));
    }

    // =========================================================================
    // Small payload — fits in the queue; no blob needed.
    // =========================================================================

    /// <summary>
    /// Current approach: serialize to a UTF-16 string, count bytes separately, then the
    /// string goes directly to the queue message.
    /// </summary>
    [Benchmark(Baseline = true, Description = "Small: current (Serialize→string, GetByteCount)")]
    public (string message, int byteCount) Small_Current()
    {
        var message = JsonSerializer.Serialize(_small);
        var byteCount = Encoding.UTF8.GetByteCount(message);
        return (message, byteCount);
    }

    /// <summary>
    /// Proposed approach: serialize directly to UTF-8 bytes, use <c>Length</c> for the size
    /// check, then convert to a string only when writing to the queue.
    /// </summary>
    [Benchmark(Description = "Small: proposed (SerializeToUtf8Bytes→GetString for queue)")]
    public (string message, int byteCount) Small_Proposed()
    {
        var blobBytes = JsonSerializer.SerializeToUtf8Bytes(_small);
        var byteCount = blobBytes.Length;
        var message = Encoding.UTF8.GetString(blobBytes);
        return (message, byteCount);
    }

    // =========================================================================
    // Medium payload — fits in the queue; no blob needed.
    // =========================================================================

    [Benchmark(Description = "Medium: current (Serialize→string, GetByteCount)")]
    public (string message, int byteCount) Medium_Current()
    {
        var message = JsonSerializer.Serialize(_medium);
        var byteCount = Encoding.UTF8.GetByteCount(message);
        return (message, byteCount);
    }

    [Benchmark(Description = "Medium: proposed (SerializeToUtf8Bytes→GetString for queue)")]
    public (string message, int byteCount) Medium_Proposed()
    {
        var blobBytes = JsonSerializer.SerializeToUtf8Bytes(_medium);
        var byteCount = blobBytes.Length;
        var message = Encoding.UTF8.GetString(blobBytes);
        return (message, byteCount);
    }

    // =========================================================================
    // Large payload — exceeds the queue limit; goes through the blob path.
    // =========================================================================

    /// <summary>
    /// Current approach for blob path: serialize to a UTF-16 string, measure its UTF-8 byte
    /// count, then re-encode the string to UTF-8 bytes for blob upload.
    /// <c>new BinaryData(string)</c> in production is equivalent to <c>Encoding.UTF8.GetBytes</c>.
    /// </summary>
    [Benchmark(Description = "Large (blob): current (Serialize→string, UTF8.GetBytes for blob)")]
    public (byte[] blobBytes, int byteCount) Large_Current()
    {
        var message = JsonSerializer.Serialize(_large);
        var byteCount = Encoding.UTF8.GetByteCount(message);
        // Simulates new BinaryData(jsonMessage) inside AddJsonMessageToBlobStorageAsync;
        // that constructor internally calls Encoding.UTF8.GetBytes(string).
        var blobBytes = Encoding.UTF8.GetBytes(message);
        return (blobBytes, byteCount);
    }

    /// <summary>
    /// Proposed approach for blob path: serialize directly to UTF-8 bytes; use the array
    /// length for the size check; pass bytes directly to blob upload — no re-encoding step.
    /// </summary>
    [Benchmark(Description = "Large (blob): proposed (SerializeToUtf8Bytes, Length, bytes direct)")]
    public (byte[] blobBytes, int byteCount) Large_Proposed()
    {
        var blobBytes = JsonSerializer.SerializeToUtf8Bytes(_large);
        var byteCount = blobBytes.Length;
        return (blobBytes, byteCount);
    }
}
