namespace Rush.Core.Graphics;

public readonly record struct TileMapEntry(ushort Raw)
{
    public const int SIZE = 2;
    
    public int Tile => Raw.GetBits(0, 10);
    public bool FlipX => Raw.GetBit(10) != 0;
    public bool FlipY => Raw.GetBit(11) != 0;
    public int Palette => Raw.GetBits(12, 4);
}