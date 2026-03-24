# AddMessageAsync Serialization — Benchmark Results

Benchmarks comparing the **current** serialization path against the **proposed**
`JsonSerializer.SerializeToUtf8Bytes` optimization in `HybridQueue.AddMessageAsync`.

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

## Environment

```
BenchmarkDotNet v0.14.0
OS: Ubuntu 24.04.3 LTS (Noble Numbat)
CPU: AMD EPYC 7763, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.102
Runtime: .NET 9.0.13 (9.0.1326.6317), X64 RyuJIT AVX2
```

## Results

| Method | Mean | Error | StdDev | Ratio | Gen0 | Gen1 | Gen2 | Allocated | Alloc Ratio |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| Small: current (Serialize→string, GetByteCount) | 318.7 ns | 0.70 ns | 0.55 ns | **1.00** | 0.0119 | - | - | **200 B** | 1.00 |
| Small: proposed (SerializeToUtf8Bytes→GetString for queue) | 322.4 ns | 1.27 ns | 1.06 ns | 1.01 | 0.0191 | - | - | 320 B | 1.60 |
| Medium: current (Serialize→string, GetByteCount) | 1,846.6 ns | 26.02 ns | 24.34 ns | 5.79 | 0.5188 | - | - | **8,696 B** | 43.48 |
| Medium: proposed (SerializeToUtf8Bytes→GetString for queue) | 2,073.2 ns | 33.59 ns | 31.42 ns | 6.50 | 0.7706 | - | - | 12,904 B | 64.52 |
| **Large (blob): current** (Serialize→string, UTF8.GetBytes for blob) | **88,789.9 ns** | 1,030.64 ns | 964.06 ns | 278.57 | 36.9873 | 36.9873 | 36.9873 | **180,197 B** | 900.99 |
| **Large (blob): proposed** (SerializeToUtf8Bytes, Length, bytes direct) | **10,839.0 ns** | 200.86 ns | 187.88 ns | 34.01 | 3.5706 | - | - | **60,064 B** | 300.32 |

> Baseline = `Small: current`. Run `dotnet run -c Release` in this project to reproduce.

## Analysis

### Queue-only path (small / medium payloads that fit within 48 KB)

The proposed approach is **marginally slower** (~1–12%) and allocates ~1.5–1.6× more memory
for the queue path. This is expected: the current approach serialises directly to a `string`
which is exactly what the queue needs, whereas the proposed approach serialises to bytes and
then calls `UTF8.GetString` to produce the string. This extra conversion is the cost of
unifying the serialisation step.

### Blob path (large payloads that exceed 48 KB)

The proposed approach is **~8.2× faster** (88.8 µs vs 10.8 µs) and allocates **~3× less
memory** (180 KB vs 60 KB). The gains come from eliminating:

- The intermediate UTF-16 string allocation (≈ 120 KB for a 60 KB payload)
- The second UTF-8 byte-count scan (`Encoding.UTF8.GetByteCount`)
- The UTF-16 → UTF-8 re-encoding pass (`Encoding.UTF8.GetBytes` / `new BinaryData(string)`)
- Gen1 and Gen2 GC pressure (the current path triggers gen2 collections; the proposed path stays in gen0)

## Conclusion

The optimization is a **clear win** for the blob path, which is the path that matters
most for large payloads. The slight regression for the queue-only path is negligible in
practice because:

1. The absolute difference is tiny (< 230 ns for small, < 230 ns for medium).
2. In any real application the I/O cost of the queue-send operation dwarfs this difference.
3. The blob path savings (eliminating large gen1/gen2 allocations) reduce GC pause times
   for high-throughput workloads.

## How to run

```bash
cd src/SimpleAzure.Storage.HybridQueues.Benchmarks
dotnet run -c Release
```
