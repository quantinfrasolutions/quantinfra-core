using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace QuantInfra.Common.ServiceBase;

public static class BinaryRecordReader
{
    public static IEnumerable<ReadOnlyMemory<byte>> ReadAll(string path)
    {
        using var fs = new FileStream(
            path, FileMode.Open, FileAccess.Read, FileShare.None,
            bufferSize: 4 * 1024 * 1024, options: FileOptions.SequentialScan);

        var lenBuf = new byte[4];
        var seqBuf = new byte[4];

        while (true)
        {
            if (!TryReadExactly(fs, lenBuf))
                yield break;

            uint len = BinaryPrimitives.ReadUInt32LittleEndian(lenBuf);
            int ilen = checked((int)len);

            byte[] rented = ArrayPool<byte>.Shared.Rent(ilen);
            var mem = rented.AsMemory(0, ilen);

            if (!TryReadExactly(fs, mem))
            {
                ArrayPool<byte>.Shared.Return(rented);
                yield break;
            }

            // Important: caller must return buffer to pool (or copy it).
            yield return mem;
        }
    }

    public static void ReturnToPool(ReadOnlyMemory<byte> msg)
    {
        if (MemoryMarshal.TryGetArray(msg, out ArraySegment<byte> seg) && seg.Array != null)
            ArrayPool<byte>.Shared.Return(seg.Array);
    }

    private static bool TryReadExactly(Stream s, byte[] dest)
    {
        int off = 0;
        while (off < dest.Length)
        {
            int n = s.Read(dest, off, dest.Length - off);
            if (n <= 0) return false;
            off += n;
        }
        return true;
    }

    private static bool TryReadExactly(Stream s, Memory<byte> dest)
    {
        while (dest.Length > 0)
        {
            int n = s.Read(dest.Span);
            if (n <= 0) return false;
            dest = dest.Slice(n);
        }
        return true;
    }
}