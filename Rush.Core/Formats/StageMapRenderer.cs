using System.Collections.Immutable;
using Rush.Core.Graphics;

namespace Rush.Core.Formats;

public static class StageMapRenderer
{
    private const byte OPAQUE = 0xFF;

    public static RgbaImage Render(StageMap map)
    {
        var colors = Enumerable.Range(0, StageMap.COLOR_COUNT)
            .Select(i => Bgr555.ToRgb(map.Palette.Span.ReadUInt16(i * Bgr555.SIZE)))
            .ToImmutableArray();
        var pixels = new byte[map.Width * map.Height * RgbaImage.BYTES_PER_PIXEL];
        
        _DrawLayer(map, map.Back, colors, pixels);
        _DrawLayer(map, map.Front, colors, pixels);
        return new RgbaImage(map.Width, map.Height, pixels);
    }

    private static void _DrawLayer(StageMap map, ChunkLayout layout, ImmutableArray<Color> colors, byte[] pixels)
    {
        foreach (var chunkY in Enumerable.Range(0, layout.Height))
        foreach (var chunkX in Enumerable.Range(0, layout.Width))
        {
            var chunk = layout.ChunkAt(chunkX, chunkY);
            if (chunk >= map.ChunkCount)
                throw new InvalidDataException(
                    $"Chunk {chunk} at ({chunkX}, {chunkY}) is past the last chunk {map.ChunkCount - 1}");
            
            foreach(var tileY in Enumerable.Range(0, StageMap.TILES_PER_CHUNK_SIDE))
            foreach (var tileX in Enumerable.Range(0, StageMap.TILES_PER_CHUNK_SIDE))
            {
                var entryIndex = tileY * StageMap.TILES_PER_CHUNK_SIDE + tileX;
                var entryOffset = chunk * StageMap.BYTES_PER_CHUNK + entryIndex * TileMapEntry.SIZE;
                var entry = new TileMapEntry(map.Chunks.Span.ReadUInt16(entryOffset));
                _DrawTile(
                    map,
                    entry,
                    chunkX * StageMap.CHUNK_SIZE + tileX * StageMap.TILE_SIZE,
                    chunkY  * StageMap.CHUNK_SIZE + tileY * StageMap.TILE_SIZE,
                    colors, 
                    pixels);
            }
        }
    }

    private static void _DrawTile(StageMap map, TileMapEntry entry, int left, int top, ImmutableArray<Color> colors, byte[] pixels)
    {
        if (entry.Tile >= map.TileCount)
            throw new InvalidDataException($"Tile {entry.Tile} is past the last tile {map.TileCount - 1}");
        
        var tile = map.Tiles.Span.Slice(entry.Tile * StageMap.BYTES_PER_TILE, StageMap.BYTES_PER_TILE);
        foreach(var y in Enumerable.Range(0, StageMap.TILE_SIZE))
        foreach (var x in Enumerable.Range(0, StageMap.TILE_SIZE))
        {
            var sourceX = entry.FlipX ? StageMap.TILE_SIZE - 1 - x : x;
            var sourceY = entry.FlipY ? StageMap.TILE_SIZE - 1 - y : y;
            var colorIndex = tile[sourceY * StageMap.TILE_SIZE + sourceX];
            if (colorIndex == 0)
                continue;
            
            var color = colors[colorIndex];
            var offset = ((top + y) * map.Width + left + x) * RgbaImage.BYTES_PER_PIXEL;
            pixels[offset] = color.Red;
            pixels[offset + 1] = color.Green;
            pixels[offset + 2] = color.Blue;
            pixels[offset + 3] = OPAQUE;
        }
    }
}