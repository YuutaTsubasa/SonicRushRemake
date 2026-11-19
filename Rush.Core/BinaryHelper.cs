using System.Buffers.Binary;
using System.Text;

namespace Rush.Core;

public static class BinaryHelper
{
    private const int BITS_PER_BYTE = 8;
    
    public static ushort ReadUInt16(this ReadOnlySpan<byte> data, int offset) =>
        BinaryPrimitives.ReadUInt16LittleEndian(
            data.Slice(offset, sizeof(ushort)));

    public static uint ReadUInt32(this ReadOnlySpan<byte> data, int offset) =>
        BinaryPrimitives.ReadUInt32LittleEndian(
            data.Slice(offset, sizeof(uint)));

    public static int ReadInt24(this ReadOnlySpan<byte> data, int offset) =>
       data[offset + 2] << 16 | data[offset + 1] << 8 | data[offset];
    
    public static string GetString(this ReadOnlySpan<byte> data, int offset, int size) =>
        Encoding.ASCII.GetString(
            data.Slice(offset, size));

    public static ReadOnlyMemory<byte> ReadSection(
        this ReadOnlyMemory<byte> data,
        uint offset,
        uint size)
    {
        var start = checked((int)offset);
        var length = checked((int)size);

        return data.Slice(start, length);
    }
    
    public static byte GetBit(this byte data, int index)
        => (byte)((data >> index) & 1);

    public static byte GetBits(this byte data, int index, int size)
    {
        var mask = byte.MaxValue >> (sizeof(byte) * BITS_PER_BYTE - size);
        return (byte)((data >> index) & mask);
    }
    
    public static ushort GetBit(this ushort data, int index)
        => (ushort)((data >> index) & 1);

    public static ushort GetBits(this ushort data, int index, int size)
    {
        var mask = ushort.MaxValue >> (sizeof(ushort) * BITS_PER_BYTE - size);
        return (ushort)((data >> index) & mask);
    }
    
    public static int GetBit(this int data, int index)
        => (data >> index) & 1;

    public static int GetBits(this int data, int index, int size)
    {
        var mask = uint.MaxValue >> (sizeof(uint) * BITS_PER_BYTE - size);
        return (int)((data >> index) & mask);
    }
}