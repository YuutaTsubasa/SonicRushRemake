namespace Rush.Core.Graphics;

public static class Bgr555
{
    public const int SIZE = 2;
    
    private const int CHANNEL_MASK = 0x1F;
    private const int CHANNEL_SIZE = 5;

    public static Color ToRgb(ushort color)
        => new(
            _Expand(color), 
            _Expand(color >> CHANNEL_SIZE), 
            _Expand(color >> (2 * CHANNEL_SIZE)));

    private static byte _Expand(int value)
    {
        var channel = value & CHANNEL_MASK;
        return (byte)(channel << 3 | channel >> 2);
    }
}