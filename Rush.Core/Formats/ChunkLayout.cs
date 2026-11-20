namespace Rush.Core.Formats;

public readonly record struct ChunkLayout(
    int Width,
    int Height,
    ReadOnlyMemory<byte> Entries)
{
    private const int WIDTH_OFFSET = 0x00, WIDTH_SIZE = 2;
    private const int HEIGHT_OFFSET = 0x02, HEIGHT_SIZE = 2;
    private const int ENTRIES_OFFSET = 0x04;
    private const int ENTRY_SIZE = 2;

    public static ChunkLayout Parse(ReadOnlyMemory<byte> data)
    {
        var width = data.Span.ReadUInt16(WIDTH_OFFSET);
        var height = data.Span.ReadUInt16(HEIGHT_OFFSET);
        var expectedSize = ENTRIES_OFFSET + width * height * ENTRY_SIZE;
        if (data.Length != expectedSize)
            throw new InvalidDataException(
                $"Layout {width}x{height} need {expectedSize} bytes, but got {data.Length} bytes");

        return new ChunkLayout(width, height, data[ENTRIES_OFFSET..]);
    }

    public int ChunkAt(int x, int y)
        => Entries.Span.ReadUInt16((y * Width + x) * ENTRY_SIZE);
}