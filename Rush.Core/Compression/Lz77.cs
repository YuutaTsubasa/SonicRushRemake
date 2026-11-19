namespace Rush.Core.Compression;

public static class Lz77
{
    private const byte TYPE = 0x10;
    private const int TYPE_OFFSET = 0, TYPE_SIZE = 1;
    private const int UNCOMPRESSED_SIZE_OFFSET = 1, UNCOMPRESSED_SIZE_SIZE = 3;
    private const int HEADER_SIZE = TYPE_SIZE + UNCOMPRESSED_SIZE_SIZE;
    private const int MIN_LENGTH = 3;
    
    public static bool IsCompressed(ReadOnlySpan<byte> data)
        => data.Length >= HEADER_SIZE && data[TYPE_OFFSET] == TYPE;

    public static ReadOnlyMemory<byte> Decompress(ReadOnlySpan<byte> data)
    {
        if (!IsCompressed(data))
            throw new InvalidDataException("Not LZ77 Data (first byte must be 0x10)");
        
        var sizeData = data.Slice(UNCOMPRESSED_SIZE_OFFSET, UNCOMPRESSED_SIZE_SIZE).ToArray();
        var size = Enumerable.Range(0, UNCOMPRESSED_SIZE_SIZE)
            .Select(offset => sizeData[offset] << offset * 8)
            .Aggregate((result, partialSize) => result | partialSize);
        var output = new byte[size];
        var read = HEADER_SIZE;
        var written = 0;

        while (written < size)
        {
            var flags = data[read++];
            foreach (var bit in Enumerable.Range(0, 8).Reverse())
            {
                if (written >= size)
                    break;

                if (flags.GetBit(bit) == 0)
                {
                    output[written++] = data[read++];
                    continue;
                }
                
                var b0 = data[read++];
                var b1 = data[read++];
                var length = b0.GetBits(4, 4) + MIN_LENGTH;
                var distance = (b0.GetBits(0, 4) << 8 | b1) + 1;
                if (distance > written)
                    throw new InvalidDataException($"Back-reference before start at output {written}");

                foreach (var _ in Enumerable.Range(0, length))
                {
                    if (written >= size)
                        break;
                    
                    output[written] = output[written - distance];
                    ++written;
                }
            }
        }

        return output;
    }
}