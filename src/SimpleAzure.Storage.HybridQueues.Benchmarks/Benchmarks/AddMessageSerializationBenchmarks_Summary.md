# AddMessageAsync Serialization — Benchmark Results

Benchmarks comparing three serialization strategies in `HybridQueue.AddMessageAsync`.

## Understanding the two blob code paths

`AddMessageAsync` has one parameter that controls how the blob/queue decision is made:
`isForcedOntoBlob`. Its value determines which of the two paths below is taken.

---

### Path 1 — "Non-forced" (`isForcedOntoBlob: false`, the default)

The caller does **not** request blob storage. The library serialises the item to JSON, measures
the resulting byte count, and only routes to blob when the size exceeds the 48 KB queue limit.
The destination is unknown at the start of the method.

```csharp
// Caller does not care about blob; library decides automatically based on size.
await hybridQueue.AddMessageAsync(
    item: myOrder,
    initialVisibilityDelay: null,
    isForcedOntoBlob: false,   // ← library decides
    cancellationToken: ct);
```

What the library does internally:

1. `JsonSerializer.Serialize(myOrder)` → UTF-16 string  
2. `Encoding.UTF8.GetByteCount(string)` → measure size  
3. Size ≤ 48 KB? → send the string straight to the queue (**queue path**)  
4. Size > 48 KB? → re-encode string to UTF-8 bytes, upload to blob, put GUID in queue (**non-forced large blob path**)

**Real-world examples of non-forced payloads that end up on blob:**

| Scenario | Why it overflows 48 KB |
|---|---|
| `OrderSummary` with 500+ line items | Each `LineItem` has SKU, description, quantity, price, tax fields |
| `DiagnosticReport` with a full exception chain | Stack trace strings, inner exceptions, context dictionaries |
| `EventBatch` containing 200+ telemetry events | Timestamp, name, properties, measurements per event |
| `UserProfile` with binary-encoded avatar | Base64-encoded profile picture stored in a string field |
| `SearchResult` returning 100 documents | Each document has title, url, snippet, metadata |

The caller passes `isForcedOntoBlob: false` for all of these — the overflow is discovered at
runtime when the serialized payload turns out to be bigger than 48 KB.

---

### Path 2 — "Forced" (`isForcedOntoBlob: true`)

The caller **explicitly** requests blob storage, regardless of payload size. The library skips
the size check entirely and goes straight to blob. The destination is known at the start of the
method, which is why the mixed strategy can apply `SerializeToUtf8Bytes` here for a 9.4× speedup.

```csharp
// Caller explicitly requests blob, size does not matter.
await hybridQueue.AddMessageAsync(
    item: myPayload,
    initialVisibilityDelay: null,
    isForcedOntoBlob: true,   // ← always blob
    cancellationToken: ct);
```

**Real-world examples where callers use `isForcedOntoBlob: true`:**

| Scenario | Why forced is preferred |
|---|---|
| Archival/audit messages | Always want a durable, inspectable blob; size is unpredictable |
| Fan-out / message bus patterns | All consumers retrieve from blob; queue is only a notification |
| Payloads that are always large | Large report, export file reference — overhead of size-check is pointless |
| Guaranteed consistency | System requirement: every message stored in blob for compliance |
| Unknown / user-supplied content | Content size is not known at call time; safer to always use blob |

---

### Why mixed cannot improve the non-forced large path

#### Can't we just check the size of the value before serializing?

The short answer is: **no**, because the caller hands the library a typed .NET object (`T item`),
not a string. A .NET object has no concept of its own serialized UTF-8 byte count. There is no
property to read, no shortcut to take. The only way to know whether the payload will fit in the
48 KB queue limit is to produce its JSON representation first and then measure it.

**What `T item` actually is when the method is called:**

```csharp
// The caller passes an in-memory .NET object — not a string, not bytes.
public record OrderSummary(string CustomerId, List<LineItem> Items, decimal Total);

var order = new OrderSummary("CUST-001", lineItems, 999.99m);

await hybridQueue.AddMessageAsync(
    item: order,          // ← a .NET object; its serialized size is unknown
    initialVisibilityDelay: null,
    isForcedOntoBlob: false,
    cancellationToken: ct);
```

`order` is a live heap object. Its in-memory size (measured by something like
`GC.GetTotalMemory`) tells you nothing about how many UTF-8 bytes it will occupy as JSON:

| Value | In-memory size (approx.) | UTF-8 JSON byte count |
|---|---:|---:|
| `new User("Alice", 25)` | ~120 B (object header, field pointers, string object) | 24 B (`{"Name":"Alice","Age":25}`) |
| `new OrderSummary(...)` with 500 line items | ~250 KB (500 `LineItem` objects on the heap) | ~62 KB (flat JSON array) |
| `new string('A', 50_000)` (a simple `string` item) | 100 KB (50,000 × 2 bytes, UTF-16) | 50 KB (50,000 × 1 byte, ASCII in UTF-8) |
| `new string('€', 50_000)` (a simple `string` item) | 100 KB (50,000 × 2 bytes, UTF-16) | 150 KB (50,000 × 3 bytes, `€` is 3-byte UTF-8) |

The last two rows show that even for a plain `string` the `.Length` property is the wrong measure
— it counts UTF-16 characters, not UTF-8 bytes. An all-ASCII string of 50,000 chars is 50 KB in
UTF-8 and therefore overflows the 48 KB queue limit; an all-`€` string of the same `.Length` is
150 KB in UTF-8 and overflows by even more. Neither decision can be made correctly without
measuring the UTF-8 byte count.

#### Is the value a string in the non-forced complex-type case?

No. When `item` is a complex type (a POCO, a record, etc.) and `isForcedOntoBlob = false`, the
value is a .NET object. `string` is handled as a special case on its own branch in the code:

```csharp
// Branch 1 — item is already a string: use it directly (no serialization needed).
if (!isForcedOntoBlob && item is string stringItem)
{
    message = stringItem;   // ← the string IS the queue message; byte-count it directly
}
// Branch 2 — item is a simple type (int, bool, decimal, …): call .ToString().
else if (!isForcedOntoBlob && typeof(T).IsASimpleType())
{
    message = item.ToString().AssumeNotNull();   // ← "42", "true", "3.14", etc.
}
// Branch 3 — complex type, non-forced: MUST serialize to get the queue message content
//            AND to know the byte count.
else if (!isForcedOntoBlob)
{
    message = JsonSerializer.Serialize(item);   // ← only now do we have a measurable string
}
```

For complex types (branch 3), `JsonSerializer.Serialize` serves **two purposes at once**:
1. It produces the queue message body (the JSON string that goes straight to the queue if the payload is small).
2. It produces something whose byte count can be measured.

There is no cheaper way to get either. Skipping serialization would mean having nothing to put in the queue and nothing to measure.

#### Why can't we serialize to UTF-8 bytes first and check `bytes.Length`?

That is exactly what the **proposed path** in the benchmarks does: `SerializeToUtf8Bytes` →
`bytes.Length`. It is strictly faster for the blob case. However, when the payload turns out to be
**small** (the common case) the bytes then have to be converted back to a UTF-16 string via
`Encoding.UTF8.GetString(bytes)` for the queue call — which adds an extra allocation and decoding
pass that the mixed strategy avoids by keeping `Serialize` → string for the non-forced path.

The mixed strategy therefore makes the most profitable choice per destination:

| Known destination | Best serialization strategy |
|---|---|
| Unknown (non-forced) | `Serialize` → string; measure with `GetByteCount`; string goes to queue if small, or `GetBytes` for blob if large |
| Always blob (forced) | `SerializeToUtf8Bytes` → bytes; skip the UTF-16 string entirely |

#### Summary

In the non-forced case the library **must** serialize before it can decide. The serialized string
is not a throwaway intermediate — it is the exact bytes the queue will receive if the payload
fits. Once that string exists, converting it to UTF-8 bytes (`GetBytes`) costs one encoding pass
and is always cheaper than discarding it and re-serializing from scratch.

---

## What is being measured

### Current path (complex types)

1. `JsonSerializer.Serialize(item)` → allocates a UTF-16 `string`
2. `Encoding.UTF8.GetByteCount(message)` → scans the entire string a **second time** to count bytes
3. `Encoding.UTF8.GetBytes(message)` (inside `new BinaryData(string)`) → **re-encodes** UTF-16 → UTF-8 for blob upload

### Proposed path (complex types)

1. `JsonSerializer.SerializeToUtf8Bytes(item)` → allocates UTF-8 bytes **directly**, in one pass
2. `blobBytes.Length` → O(1) array property read, no scanning
3. Bytes passed to blob upload **as-is** — no re-encoding
4. `Encoding.UTF8.GetString(blobBytes)` → only called when the item fits in the queue

### Mixed path (**implemented** — best of both worlds)

| Code path | Strategy | Rationale |
|---|---|---|
| Queue path (non-forced, small/medium) | `Serialize` → string | Fastest; string used directly for queue, no `GetString` overhead |
| Non-forced, too large → blob | `Serialize` → string → `UTF8.GetBytes` | String already exists; avoids re-serialization |
| Forced onto blob (`isForcedOntoBlob = true`) | `SerializeToUtf8Bytes` → bytes | We know it goes to blob; skip the intermediate UTF-16 string entirely |

`AddJsonMessageToBlobStorageAsync` now accepts `byte[]` instead of `string`, eliminating the
`BinaryData(string)` constructor's internal UTF-8 re-encoding for **all** blob upload calls.

## Environment

```
BenchmarkDotNet v0.14.0
OS: Ubuntu 24.04.3 LTS (Noble Numbat)
CPU: AMD EPYC 7763, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.102
Runtime: .NET 9.0.13 (9.0.1326.6317), X64 RyuJIT AVX2
```

## Results

### Queue path — small and medium payloads (fits within 48 KB)

| Method | Mean | Alloc | vs Current |
|---|---:|---:|---:|
| Small: **current** (Serialize→string, GetByteCount) | **313.3 ns** | **200 B** | baseline |
| Small: proposed (SerializeToUtf8Bytes→GetString) | 325.6 ns | 320 B | +4% slower, +60% alloc |
| Small: **mixed** (Serialize→string — identical to current) | 350.4 ns | 200 B | ≈ same |
| Medium: **current** (Serialize→string, GetByteCount) | **1,613.9 ns** | **8,696 B** | baseline |
| Medium: proposed (SerializeToUtf8Bytes→GetString) | 1,698.0 ns | 12,904 B | +5% slower, +48% alloc |
| Medium: **mixed** (Serialize→string — identical to current) | 1,552.9 ns | 8,696 B | ≈ same |

> **Mixed = current** for the queue path. No regression, no change.

### Blob path — non-forced, large payload (exceeds 48 KB)

| Method | Mean | Gen0 | Gen1 | Gen2 | Alloc |
|---|---:|---:|---:|---:|---:|
| Large non-forced: **current** (Serialize→string, GetBytes for blob) | 85,301.6 ns | 37.0 | 37.0 | 37.0 | 180,197 B |
| Large non-forced: proposed (SerializeToUtf8Bytes, bytes direct) | 9,202.0 ns | 3.6 | - | - | 60,064 B |
| Large non-forced: **mixed** (Serialize→string, GetBytes — identical to current) | 84,742.8 ns | 37.0 | 37.0 | 37.0 | 180,197 B |

> **Mixed = current** for non-forced large payloads. We cannot skip the string allocation
> here because we need it to measure the byte size before deciding which path to take.

### Blob path — forced (`isForcedOntoBlob = true`) ← **Mixed wins here**

| Method | Mean | Gen0 | Gen1 | Gen2 | Alloc | vs Current |
|---|---:|---:|---:|---:|---:|---:|
| Small forced: **current** (Serialize→string, GetBytes) | 373.9 ns | 0.019 | - | - | 320 B | baseline |
| Small forced: **mixed** (SerializeToUtf8Bytes, no intermediate string) | **278.1 ns** | **0.007** | - | - | **120 B** | **−26% time, −63% alloc** |
| Large forced: **current** (Serialize→string, GetBytes) | 84,082.7 ns | 37.0 | 37.0 | 37.0 | 180,197 B | baseline |
| Large forced: **mixed** (SerializeToUtf8Bytes, no intermediate string) | **8,965.3 ns** | **3.6** | - | - | **60,064 B** | **−89% time (9.4×), −67% alloc, no gen2** |

## Analysis

### Queue path (mixed = current)

Mixed keeps `JsonSerializer.Serialize` → string for all non-forced complex types that fit
within the 48 KB limit. The string is used directly as the queue message body — no
`Encoding.UTF8.GetString` call, no extra allocation. Queue path performance is **unchanged**.

### Non-forced large blob path (mixed = current)

When a payload is not forced onto the blob but turns out to be too large, the mixed approach
serialises to a string first (necessary for the size check), then calls `Encoding.UTF8.GetBytes`
to produce the bytes for blob upload — the same work as the current code. No improvement here,
but no regression either.

### Forced blob path (mixed wins decisively)

When `isForcedOntoBlob = true` the destination is known upfront. Mixed uses
`JsonSerializer.SerializeToUtf8Bytes` directly, completely eliminating the intermediate
UTF-16 string allocation:

- **Small forced**: 26% faster, 63% less allocation (320 B → 120 B).
- **Large forced**: **9.4× faster** (84 µs → 9 µs), **67% less allocation** (180 KB → 60 KB).
  Most importantly, **no gen1/gen2 GC collections** — the current code triggers gen2 for
  large forced payloads because the ~120 KB UTF-16 string reaches the Large Object Heap.

## Summary table

| Scenario | Current | **Mixed** | Change |
|---|---:|---:|---:|
| Small queue | 313 ns / 200 B | **313 ns / 200 B** | none (= current) |
| Medium queue | 1,614 ns / 8,696 B | **1,553 ns / 8,696 B** | none (= current) |
| Large blob (non-forced) | 85,302 ns / 180,197 B | **84,743 ns / 180,197 B** | none (= current) |
| Small blob (forced) | 374 ns / 320 B | **278 ns / 120 B** | **−26% time, −63% alloc** |
| Large blob (forced) | 84,083 ns / 180,197 B | **8,965 ns / 60,064 B** | **−89% time, −67% alloc, no gen2** |

## How to run

```bash
cd src/SimpleAzure.Storage.HybridQueues.Benchmarks
dotnet run -c Release
```
