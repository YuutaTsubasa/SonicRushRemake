using Rush.Core.Compression;
using Rush.Core.Graphics;
using Rush.Core.Rom;

namespace Rush.Core.Formats;

public readonly record struct StageMap(
    string Zone,
    ReadOnlyMemory<byte> Tiles,
    ReadOnlyMemory<byte> Palette,
    ReadOnlyMemory<byte> Chunks,
    ChunkLayout Front,
    ChunkLayout Back)
{
    public const int COLOR_COUNT = 256;
    public const int TILE_SIZE = 8;
    public const int BYTES_PER_TILE = TILE_SIZE * TILE_SIZE;
    public const int TILES_PER_CHUNK_SIDE = 8;
    public const int CHUNK_SIZE = TILE_SIZE * TILES_PER_CHUNK_SIDE;
    public const int BYTES_PER_CHUNK = TILES_PER_CHUNK_SIDE * TILES_PER_CHUNK_SIDE * TileMapEntry.SIZE;
    
    public int TileCount => Tiles.Length / BYTES_PER_TILE;
    public int ChunkCount => Chunks.Length / BYTES_PER_CHUNK;
    public int Width => Front.Width * CHUNK_SIZE;
    public int Height => Front.Height * CHUNK_SIZE;

    public static StageMap Load(IFileArchive rom, string zone)
    {
        var narc = rom.OpenNarc($"narc/{zone}_map.narc");

        ReadOnlyMemory<byte> Read(string name)
        {
            var raw = narc.Read(name);
            return Lz77.IsCompressed(raw.Span) ? Lz77.Decompress(raw.Span) : raw;
        }

        var map = new StageMap(
            zone,
            Read($"{zone}.ch"),
            Read($"{zone}.pl"),
            Read($"{zone}.bk"),
            ChunkLayout.Parse(Read($"{zone}_a.mp")),
            ChunkLayout.Parse(Read($"{zone}_b.mp")));
        map._Validate();
        return map;
    }

    private void _Validate()
    {
        if (Tiles.Length % BYTES_PER_TILE != 0)
            throw new InvalidDataException($"{Zone}.ch is {Tiles.Length} bytes, not whole 8bpp tiles");
        if (Palette.Length != COLOR_COUNT * Bgr555.SIZE)
            throw new InvalidDataException(
                $"{Zone}.pl is {Palette.Length} bytes, expected {COLOR_COUNT * Bgr555.SIZE}");
        if (Chunks.Length % BYTES_PER_CHUNK != 0)
            throw new InvalidDataException($"{Zone}.bk is {Chunks.Length} bytes, not whole chunks");
        if (Front.Width != Back.Width || Front.Height != Back.Height)
            throw new InvalidDataException(
                $"Layers differ in size: front {Front.Width}x{Front.Height}, back {Back.Width}x{Back.Height}");
    }
}