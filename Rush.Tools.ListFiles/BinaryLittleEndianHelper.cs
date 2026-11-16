using System.Buffers.Binary;

namespace Rush.Tools.ListFiles;

public static class BinaryHelper
{
    public static uint ReadUInt32(this byte[] data, int offset) =>
        BinaryPrimitives.ReadUInt32LittleEndian(
            data.AsSpan(offset, sizeof(uint)));
}