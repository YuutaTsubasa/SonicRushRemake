using System.Buffers.Binary;
using System.IO.Compression;
using System.Text;

namespace Rush.Core.Graphics;

public static class Png
{
    private enum SectionType
    {
        Header,
        Data,
        End
    }
    
    private static readonly byte[] SIGNATURE = [0x89, (byte)'P', (byte)'N', (byte)'G', 0x0D, 0x0A, 0x1A, 0x0A];
    
    private const int HEADER_SIZE = 13;
    private const int WIDTH_HEADER_OFFSET = 0;
    private const int HEIGHT_HEADER_OFFSET = 4;
    private const byte BIT_DEPTH = 8;
    private const int BIT_DEPTH_HEADER_OFFSET = 8;
    private const byte COLOR_TYPE_RGBA = 6;
    private const int COLOR_TYPE_RGBA_HEADER_OFFSET = 9;
    
    private const int BUFFER_SIZE = 4;
    private const byte FILTER_NONE = 0;

    public static ReadOnlyMemory<byte> Encode(RgbaImage image)
    {
        using var stream = new MemoryStream();
        stream.Write(SIGNATURE);
        _WriteChunk(stream, SectionType.Header, _Header(image).Span);
        _WriteChunk(stream, SectionType.Data, _CompressScanlines(image).Span);
        _WriteChunk(stream, SectionType.End, []);
        return stream.ToArray();
    }
    
    private static ReadOnlyMemory<byte> _Header(RgbaImage image)
    {
        var header = new byte[HEADER_SIZE];
        BinaryPrimitives.WriteUInt32BigEndian(header.AsSpan(WIDTH_HEADER_OFFSET), (uint)image.Width);
        BinaryPrimitives.WriteUInt32BigEndian(header.AsSpan(HEIGHT_HEADER_OFFSET), (uint)image.Height);
        header[BIT_DEPTH_HEADER_OFFSET] = BIT_DEPTH;
        header[COLOR_TYPE_RGBA_HEADER_OFFSET] = COLOR_TYPE_RGBA;
        return header;
    }

    private static ReadOnlyMemory<byte> _CompressScanlines(RgbaImage image)
    {
        var rowSize = image.Width * RgbaImage.BYTES_PER_PIXEL;
        var pixels = image.Pixels.ToArray();
        
        using var output = new MemoryStream();
        using (var zlib = new ZLibStream(output, CompressionLevel.Optimal))
        {
            foreach (var y in Enumerable.Range(0, image.Height))
            {
                zlib.WriteByte(FILTER_NONE);
                zlib.Write(pixels, y * rowSize, rowSize);
            }
        }
        return output.ToArray();
    }
    
    private static ReadOnlyMemory<byte> _GetTypeBytes(SectionType type)
        => Encoding.ASCII.GetBytes(
            type switch
            {
                SectionType.Header => "IHDR",
                SectionType.Data => "IDAT",
                SectionType.End => "IEND",
                _ => throw new ArgumentOutOfRangeException(nameof(type))
            });
    
    private static void _WriteChunk(Stream stream, SectionType type, ReadOnlySpan<byte> data)
    {
        var typeBytes = _GetTypeBytes(type).Span;
        var buffer = (Span<byte>)(stackalloc byte[BUFFER_SIZE]);
        
        BinaryPrimitives.WriteUInt32BigEndian(buffer, (uint)data.Length);
        stream.Write(buffer);
        stream.Write(typeBytes);
        stream.Write(data);
        BinaryPrimitives.WriteUInt32BigEndian(buffer, Crc32.Compute([.. typeBytes, .. data]));
        stream.Write(buffer);
    }
}