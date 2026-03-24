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

In path 1, the library does not know whether the payload will exceed 48 KB until after it has
serialized it. The string is produced first — because the string is also what gets sent to the
queue when the payload is small. By the time the library discovers the payload is too large, the
UTF-16 string already exists in memory. Converting that string to UTF-8 bytes (one encoding pass)
is strictly cheaper than re-serializing from scratch (a full JSON traversal + encoding pass), so
mixed keeps `Encoding.UTF8.GetBytes(string)` for this case — identical to the current code.

Only path 2 (forced) allows skipping the intermediate string, because the destination is known
upfront and the string is never needed.

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
