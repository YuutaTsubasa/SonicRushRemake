using Rush.Core.Compression;

namespace Rush.Core.Formats;

public enum BbgFormat : uint
{
    Tiles4Bpp = 1,
    Tiles8Bpp = 2,
    Tiles8BppExtendedPalette = 5,
}

public record struct Bbg(
    BbgFormat Format,
    int WidthInTiles,
    int HeightInTiles,
    int PaletteBase,
    int ExtendedPaletteBase,
    ReadOnlyMemory<byte> Tiles,
    ReadOnlyMemory<byte> Map,
    ReadOnlyMemory<byte> Palette)
{
    private const string MAGIC = "BBG\0";
    private const int MAGIC_OFFSET = 0x00, MAGIC_SIZE = 4;
    private const int TILES_OFFSET_OFFSET = 0x08;
    private const int MAP_OFFSET_OFFSET = 0x0C;
    private const int PALETTE_OFFSET_OFFSET = 0x10;
    private const int FORMAT_OFFSET = 0x14;
    private const int WIDTH_OFFSET = 0x18;
    private const int HEIGHT_OFFSET = 0x1A;
    private const int PALETTE_BASE_OFFSET = 0x1C;
    private const int EXTENDED_PALETTE_BASE_OFFSET = 0x1E;

    private const byte SECTION_RAW = 0x00;
    private const int SECTION_HEADER_SIZE = 4;

    public int BytesPerTile => Format switch
    {
        BbgFormat.Tiles4Bpp => 32,
        BbgFormat.Tiles8Bpp or BbgFormat.Tiles8BppExtendedPalette => 64,
        _ => throw new ArgumentOutOfRangeException(nameof(Format), Format, null)
    };
    public int TileCount => Tiles.Length / BytesPerTile;

    public static Bbg Parse(ReadOnlySpan<byte> data)
    {
        if (data.GetString(MAGIC_OFFSET, MAGIC_SIZE) != MAGIC)
            throw new InvalidDataException("Not a BBG File");

        var format = (BbgFormat)data.ReadUInt32(FORMAT_OFFSET);
        if (!Enum.IsDefined(format))
            throw new InvalidDataException($"Unknown BBG format: {(uint)format}");

        return new Bbg(
            format,
            data.ReadUInt16(WIDTH_OFFSET),
            data.ReadUInt16(HEIGHT_OFFSET),
            data.ReadUInt16(PALETTE_BASE_OFFSET),
            data.ReadUInt16(EXTENDED_PALETTE_BASE_OFFSET),
            _ReadSection(data, data.ReadUInt32(TILES_OFFSET_OFFSET)),
            _ReadSection(data, data.ReadUInt32(MAP_OFFSET_OFFSET)),
            _ReadSection(data, data.ReadUInt32(PALETTE_OFFSET_OFFSET)));
    }

    private static ReadOnlyMemory<byte> _ReadSection(ReadOnlySpan<byte> data, uint offset)
    {
        var section = data[checked((int)offset)..];
        if (Lz77.IsCompressed(section))
            return Lz77.Decompress(section);

        if (section[0] != SECTION_RAW)
            throw new InvalidDataException($"Unknown section type 0x{section[0]:X2} at 0x{offset:X}");

        var size = section.ReadInt24(1);
        return section.Slice(SECTION_HEADER_SIZE, size).ToArray();
    }
}