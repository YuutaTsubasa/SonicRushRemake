using Rush.Core.Graphics;

namespace Rush.Core.Formats;

public static class BbgRenderer
{
    private const int TILE_SIZE = 8;
    private const int COLORS_PER_PALETTE_4BPP = 16;
    private const int COLORS_PER_PALETTE_8BPP = 256;

    private readonly record struct MapEntry(ushort Raw)
    {
        public int Tile => Raw.GetBits(0, 10);
        public bool FlipX => Raw.GetBit(10) != 0;
        public bool FlipY => Raw.GetBit(11) != 0;
        public int Palette => Raw.GetBits(12, 4);
    }

    public static RgbaImage Render(Bbg bbg)
    {
        var entryCount = bbg.WidthInTiles * bbg.HeightInTiles;
        if (bbg.Map.Length != entryCount * sizeof(ushort))
            throw new InvalidDataException(
                $"Map has {bbg.Map.Length} bytes, expected {entryCount * sizeof(ushort)} for {bbg.WidthInTiles}x{bbg.HeightInTiles}");

        var entries = Enumerable.Range(0, entryCount)
            .Select(i => new MapEntry(bbg.Map.Span.ReadUInt16(i * sizeof(ushort))))
            .ToArray();
        var colors = Enumerable.Range(0, bbg.Palette.Length / sizeof(ushort))
            .Select(i => Bgr555.ToRgb(bbg.Palette.Span.ReadUInt16(i * sizeof(ushort))))
            .ToArray();
        var tileBase = _TileBase(entries, bbg.TileCount);

        var width = bbg.WidthInTiles * TILE_SIZE;
        var height = bbg.HeightInTiles * TILE_SIZE;
        var pixels = new byte[width * height * RgbaImage.BYTES_PER_PIXEL];
        
        foreach (var y in Enumerable.Range(0, height))
        foreach (var x in Enumerable.Range(0, width))
        {
            var entry = entries[y / TILE_SIZE * bbg.WidthInTiles + x / TILE_SIZE];
            var colorIndex = _ColorIndex(bbg, entry, entry.Tile - tileBase, x % TILE_SIZE, y % TILE_SIZE);
            if (colorIndex is not {} index || _IsTransparent(colors.Length, index))
                continue;
            
            var color = colors[index];
            var offset = (y * width + x) * RgbaImage.BYTES_PER_PIXEL;
            pixels[offset] = color.Red;
            pixels[offset + 1] = color.Green;
            pixels[offset + 2] = color.Blue;
            pixels[offset + 3] = 0xFF;
        }
        
        return new RgbaImage(width, height, pixels);
    }

    private static int _TileBase(MapEntry[] entries, int tileCount)
    {
        var maxTile = entries.Max(entry => entry.Tile);
        return maxTile < tileCount ? 0 : maxTile + 1 - tileCount;
    }

    private static int? _ColorIndex(Bbg bbg, MapEntry entry, int tile, int x, int y)
    {
        if (tile < 0 || tile >= bbg.TileCount)
            return null;

        var px = entry.FlipX ? TILE_SIZE - 1 - x : x;
        var py = entry.FlipY ? TILE_SIZE - 1 - y : y;
        var tileOffset = tile * bbg.BytesPerTile;

        var colorValue = bbg.Format switch
        {
            BbgFormat.Tiles4Bpp => _Get4BppColorPart(bbg.Tiles.Span[tileOffset + py * (TILE_SIZE / 2) + px / 2], px),
            BbgFormat.Tiles8Bpp or BbgFormat.Tiles8BppExtendedPalette => bbg.Tiles.Span[tileOffset + py * TILE_SIZE + px],
            _ => throw new ArgumentOutOfRangeException(nameof(bbg.Format))
        };
        if (colorValue == 0)
            return null;

        var hardwareIndex = bbg.Format switch
        {
            BbgFormat.Tiles4Bpp => entry.Palette * COLORS_PER_PALETTE_4BPP + colorValue,
            BbgFormat.Tiles8Bpp => colorValue,
            BbgFormat.Tiles8BppExtendedPalette => entry.Palette * COLORS_PER_PALETTE_8BPP + colorValue,
            _ => throw new ArgumentOutOfRangeException(nameof(bbg.Format))
        };
        
        var fileStart = bbg.Format == BbgFormat.Tiles8BppExtendedPalette
            ? bbg.ExtendedPaletteBase * COLORS_PER_PALETTE_8BPP
            : bbg.PaletteBase;

        return hardwareIndex - fileStart;
    }

    private static byte _Get4BppColorPart(byte value, int px)
        => px.GetBit(0) == 0
            ? value.GetBits(0, 4)
            : value.GetBits(4, 4);
    
    private static bool _IsTransparent(int colorsLength, int colorIndex)
        => colorIndex < 0 || colorIndex >= colorsLength;
}