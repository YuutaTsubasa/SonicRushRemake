namespace Rush.Core.Graphics;

public static class Crc32
{
    private const uint POLYNOMIAL = 0xEDB88320;
    
    private static readonly uint[] TABLE = Enumerable.Range(0, 256)
        .Select(n => Enumerable.Range(0, 8)
            .Aggregate((uint)n, (c, _) => (c & 1) != 0 
                ? POLYNOMIAL ^ (c >> 1)
                : c >> 1))  
        .ToArray();

    public static uint Compute(ReadOnlySpan<byte> data)
    {
        var crc = 0xFFFFFFFFu;
        foreach (var b in data)
        {
            crc = TABLE[(crc ^ b) & 0xFF] ^ (crc >> 8);
        }
        
        return crc ^ 0xFFFFFFFFu;
    }
}