using System.Buffers.Binary;
using System.Collections.Immutable;
using System.Text;

namespace Rush.Tools.ListFiles;

public static class BinaryLittleEndianHelper
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

    public static ReadOnlySpan<byte> ReadSection(
        this byte[] data,
        uint offset,
        uint size)
    {
        var start = checked((int)offset);
        var length = checked((int)size);

        return data.AsSpan(start, length);
    }
}