namespace Rush.Core.Graphics;

public record struct RgbaImage(
    int Width,
    int Height,
    ReadOnlyMemory<byte> Pixels)
{
    public const int BYTES_PER_PIXEL = 4;
}