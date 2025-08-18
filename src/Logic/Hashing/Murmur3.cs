using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using static System.Numerics.BitOperations;

namespace Logic.Hashing;

/// <summary>
/// Provides hashing functionality using the MurmurHash3 x64 128-bit variant, simplified to return a 64-bit hash.
/// </summary>
public static class Murmur3
{
    private static readonly ulong C1 = 0x87c37b91114253d5;
    private static readonly ulong C2 = 0x4cf5ad432745937f;

    /// <summary>
    /// Hashes the <paramref name="data"/> using MurmurHash3 x64 128-bit variant, simplified to return 64-bit hash.
    /// </summary>
    /// <param name="data">The data to hash.</param>
    /// <param name="seed">The seed value for the hash.</param>
    /// <returns>A 64-bit hash result.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Meziantou.Analyzer", "MA0051:Method is too long", Justification = "MurmurHash3 implementation requires this length for performance")]
    public static ulong Hash64(ref ReadOnlySpan<byte> data, ulong seed)
    {
        var h1 = seed;
        var h2 = seed;
        int len = data.Length;
        int pos = 0;

        ref var dataRef = ref MemoryMarshal.GetReference(data);
        int nBlocks = len / 16;

        for (int i = 0; i < nBlocks; i++)
        {
            ulong k1 = ReadUInt64(ref dataRef, pos);
            ulong k2 = ReadUInt64(ref dataRef, pos + 8);

            k1 *= C1;
            k1 = RotateLeft(k1, 31);
            k1 *= C2;
            h1 ^= k1;
            h1 = RotateLeft(h1, 27);
            h1 += h2;
            h1 = (h1 * 5) + 0x52dce729;

            k2 *= C2;
            k2 = RotateLeft(k2, 33);
            k2 *= C1;
            h2 ^= k2;
            h2 = RotateLeft(h2, 31);
            h2 += h1;
            h2 = (h2 * 5) + 0x38495ab5;

            pos += 16;
        }

        // Tail
        ulong k1Tail = 0, k2Tail = 0;
        switch (len & 15)
        {
            case 15: k2Tail ^= (ulong)data[pos + 14] << 48; goto case 14;
            case 14: k2Tail ^= (ulong)data[pos + 13] << 40; goto case 13;
            case 13: k2Tail ^= (ulong)data[pos + 12] << 32; goto case 12;
            case 12: k2Tail ^= (ulong)data[pos + 11] << 24; goto case 11;
            case 11: k2Tail ^= (ulong)data[pos + 10] << 16; goto case 10;
            case 10: k2Tail ^= (ulong)data[pos + 9] << 8; goto case 9;
            case 9: k2Tail ^= (ulong)data[pos + 8]; goto case 8;
            case 8: k1Tail ^= (ulong)data[pos + 7] << 56; goto case 7;
            case 7: k1Tail ^= (ulong)data[pos + 6] << 48; goto case 6;
            case 6: k1Tail ^= (ulong)data[pos + 5] << 40; goto case 5;
            case 5: k1Tail ^= (ulong)data[pos + 4] << 32; goto case 4;
            case 4: k1Tail ^= (ulong)data[pos + 3] << 24; goto case 3;
            case 3: k1Tail ^= (ulong)data[pos + 2] << 16; goto case 2;
            case 2: k1Tail ^= (ulong)data[pos + 1] << 8; goto case 1;
            case 1:
                k1Tail ^= (ulong)data[pos];
                break;
        }

        h1 ^= RotateLeft(k1Tail * C1, 31) * C2;
        h2 ^= RotateLeft(k2Tail * C2, 33) * C1;

        // Finalization
        h1 ^= (ulong)len;
        h2 ^= (ulong)len;

        h1 += h2;
        h2 += h1;

        h1 = FMix(h1);
        h2 = FMix(h2);

        h1 += h2;

        return h1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong ReadUInt64(ref byte dataRef, int position)
    {
        return Unsafe.ReadUnaligned<ulong>(ref Unsafe.Add(ref dataRef, position));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong FMix(ulong k)
    {
        k ^= k >> 33;
        k *= 0xff51afd7ed558ccd;
        k ^= k >> 33;
        k *= 0xc4ceb9fe1a85ec53;
        k ^= k >> 33;
        return k;
    }
}
