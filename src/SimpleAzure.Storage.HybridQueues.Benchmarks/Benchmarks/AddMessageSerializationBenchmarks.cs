using System.Text;
using System.Text.Json;
using BenchmarkDotNet.Attributes;

namespace WorldDomination.SimpleAzure.Storage.HybridQueues.Benchmarks.Benchmarks;

/// <summary>
/// Benchmarks comparing three serialization strategies in <c>HybridQueue.AddMessageAsync</c>:
/// <list type="bullet">
///   <item><term>Current</term>
///     <description>
///       <c>JsonSerializer.Serialize</c> → UTF-16 string, then <c>Encoding.UTF8.GetByteCount</c>
///       for size, then <c>Encoding.UTF8.GetBytes</c> (= <c>new BinaryData(string)</c>) for blob.
///     </description>
///   </item>
///   <item><term>Proposed</term>
///     <description>
///       <c>JsonSerializer.SerializeToUtf8Bytes</c> → UTF-8 bytes, then <c>Length</c> for size,
///       then <c>Encoding.UTF8.GetString</c> only when the item goes to the queue.
///     </description>
///   </item>
///   <item><term>Mixed (implemented)</term>
///     <description>
///       Best-of-both-worlds: <c>Serialize</c> → string for the queue path (fastest for small/medium);
///       <c>SerializeToUtf8Bytes</c> → bytes when <c>isForcedOntoBlob = true</c> (fastest for blob).
///       For non-forced large payloads that overflow to blob: string is already available so convert
///       it once with <c>Encoding.UTF8.GetBytes</c> (avoids re-serialization).
///     </description>
///   </item>
/// </list>
///
/// <para>
/// Summary of what each strategy does per code path:
/// <code>
/// Path                              | Current                          | Proposed                          | Mixed
/// ----------------------------------|----------------------------------|-----------------------------------|--------------------------------------
/// Small/Medium queue (non-forced)   | Serialize→str, GetByteCount      | SerializeToUtf8Bytes, GetString   | Serialize→str, GetByteCount  (= current)
/// Large blob, non-forced            | Serialize→str, GetByteCount, GetBytes | SerializeToUtf8Bytes, Length | Serialize→str, GetByteCount, GetBytes (= current)
/// Any payload, forced blob          | Serialize→str, GetBytes          | SerializeToUtf8Bytes              | SerializeToUtf8Bytes  (= proposed)
/// </code>
/// </para>
/// </summary>
[MemoryDiagnoser]
// SimpleJob runs one job on the host process runtime (here: .NET 9.0 in Release mode).
// It uses BenchmarkDotNet's default iteration count (15 warmup + 15 target iterations per benchmark).
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

    /// <summary>
    /// Mixed approach for the queue path: uses <c>Serialize</c> → string (same as current).
    /// Mixed = current for all non-forced queue paths.
    /// </summary>
    [Benchmark(Description = "Small: mixed — queue path (Serialize→string, same as current)")]
    public (string message, int byteCount) Small_Mixed()
    {
        // Mixed uses Serialize→string for non-forced complex types (queue-optimised path).
        // This benchmark confirms mixed == current for small queue payloads.
        var message = JsonSerializer.Serialize(_small);
        var byteCount = Encoding.UTF8.GetByteCount(message);
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

    /// <summary>Mixed approach for the queue path: uses <c>Serialize</c> → string (same as current).</summary>
    [Benchmark(Description = "Medium: mixed — queue path (Serialize→string, same as current)")]
    public (string message, int byteCount) Medium_Mixed()
    {
        var message = JsonSerializer.Serialize(_medium);
        var byteCount = Encoding.UTF8.GetByteCount(message);
        return (message, byteCount);
    }

    // =========================================================================
    // Large payload — non-forced; exceeds the queue limit; goes through the blob path.
    // =========================================================================

    /// <summary>
    /// Current approach for blob path: serialize to a UTF-16 string, measure its UTF-8 byte
    /// count, then re-encode the string to UTF-8 bytes for blob upload.
    /// <c>new BinaryData(string)</c> in production is equivalent to <c>Encoding.UTF8.GetBytes</c>.
    /// </summary>
    [Benchmark(Description = "Large (blob, non-forced): current (Serialize→string, UTF8.GetBytes for blob)")]
    public (byte[] blobBytes, int byteCount) Large_NonForced_Current()
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
    [Benchmark(Description = "Large (blob, non-forced): proposed (SerializeToUtf8Bytes, Length, bytes direct)")]
    public (byte[] blobBytes, int byteCount) Large_NonForced_Proposed()
    {
        var blobBytes = JsonSerializer.SerializeToUtf8Bytes(_large);
        var byteCount = blobBytes.Length;
        return (blobBytes, byteCount);
    }

    /// <summary>
    /// Mixed approach for non-forced large blob: serialize to string first (needed to check size),
    /// then encode to bytes for upload. This is the same work as current — mixed cannot improve
    /// the non-forced blob path without knowing the size upfront.
    /// </summary>
    [Benchmark(Description = "Large (blob, non-forced): mixed (Serialize→string, GetByteCount, UTF8.GetBytes — same as current)")]
    public (byte[] blobBytes, int byteCount) Large_NonForced_Mixed()
    {
        // Serialize to string first: we need the size check before knowing which path to take.
        var message = JsonSerializer.Serialize(_large);
        var byteCount = Encoding.UTF8.GetByteCount(message);

        // Too large → encode the string we already have to UTF-8 bytes for blob upload.
        // This avoids re-serialization but still performs one UTF-16→UTF-8 encoding pass.
        var blobBytes = Encoding.UTF8.GetBytes(message);
        return (blobBytes, byteCount);
    }

    // =========================================================================
    // Forced blob path — isForcedOntoBlob = true (any size payload).
    // Mixed wins here: serialize directly to UTF-8 bytes, skip the UTF-16 string entirely.
    // =========================================================================

    /// <summary>
    /// Current approach when <c>isForcedOntoBlob = true</c>, small payload:
    /// serialize to a UTF-16 string then re-encode to UTF-8 bytes for blob.
    /// (No <c>GetByteCount</c> since size is irrelevant when forced.)
    /// </summary>
    [Benchmark(Description = "Small (forced blob): current (Serialize→string, UTF8.GetBytes)")]
    public (byte[] blobBytes, int length) SmallForced_Current()
    {
        var message = JsonSerializer.Serialize(_small);
        var blobBytes = Encoding.UTF8.GetBytes(message);
        return (blobBytes, blobBytes.Length);
    }

    /// <summary>
    /// Mixed approach when <c>isForcedOntoBlob = true</c>, small payload:
    /// serialize directly to UTF-8 bytes — no intermediate UTF-16 string allocated.
    /// </summary>
    [Benchmark(Description = "Small (forced blob): mixed (SerializeToUtf8Bytes, no intermediate string)")]
    public (byte[] blobBytes, int length) SmallForced_Mixed()
    {
        var blobBytes = JsonSerializer.SerializeToUtf8Bytes(_small);
        return (blobBytes, blobBytes.Length);
    }

    /// <summary>
    /// Current approach when <c>isForcedOntoBlob = true</c>, large payload:
    /// serialize to a UTF-16 string (~120 KB) then re-encode to UTF-8 bytes (~60 KB).
    /// Triggers LOH / gen2 GC pressure.
    /// </summary>
    [Benchmark(Description = "Large (forced blob): current (Serialize→string, UTF8.GetBytes)")]
    public (byte[] blobBytes, int length) LargeForced_Current()
    {
        var message = JsonSerializer.Serialize(_large);
        var blobBytes = Encoding.UTF8.GetBytes(message);
        return (blobBytes, blobBytes.Length);
    }

    /// <summary>
    /// Mixed approach when <c>isForcedOntoBlob = true</c>, large payload:
    /// serialize directly to UTF-8 bytes (~60 KB) — no UTF-16 string, no LOH pressure.
    /// </summary>
    [Benchmark(Description = "Large (forced blob): mixed (SerializeToUtf8Bytes, no intermediate string)")]
    public (byte[] blobBytes, int length) LargeForced_Mixed()
    {
        var blobBytes = JsonSerializer.SerializeToUtf8Bytes(_large);
        return (blobBytes, blobBytes.Length);
    }
}
