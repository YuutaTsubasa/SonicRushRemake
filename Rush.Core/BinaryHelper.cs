using System.Buffers.Binary;
using System.Text;

namespace Rush.Core;

public static class BinaryHelper
{
    
    public static ushort ReadUInt16(this ReadOnlySpan<byte> data, int offset) =>
        BinaryPrimitives.ReadUInt16LittleEndian(
            data.Slice(offset, sizeof(ushort)));

    public static uint ReadUInt32(this ReadOnlySpan<byte> data, int offset) =>
        BinaryPrimitives.ReadUInt32LittleEndian(
            data.Slice(offset, sizeof(uint)));
    
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
        var mask = 0xFF >> (8 - size);
        return (byte)((data >> index) & mask);
    }
}