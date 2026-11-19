using System.Collections.Immutable;
using Rush.Core.Compression;

namespace Rush.Core.Rom;

public sealed class Narc : IFileArchive
{
    private record struct SectionPosition(int Offset, uint Size);
    
    private const string MAGIC = "NARC";
    private const int MAGIC_OFFSET = 0x00, MAGIC_SIZE = 4;
    private const ushort BYTE_ORDER_LITTLE_ENDIAN = 0xFFFE;
    private const int BYTE_ORDER_OFFSET = 0x04, BYTE_ORDER_SIZE = 2;
    private const int VERSION_OFFSET = 0x06, VERSION_SIZE = 2;
    private const int FILE_SIZE_OFFSET = 0x08, FILE_SIZE_SIZE = 4;
    private const int HEADER_SIZE_OFFSET = 0x0C, HEADER_SIZE_SIZE = 2;
    private const int SECTION_COUNT_OFFSET = 0x0E,  SECTION_COUNT_SIZE = 2;

    private const int SECTION_MAGIC_OFFSET = 0, SECTION_MAGIC_SIZE = 4;
    private const int SECTION_SIZE_OFFSET = 4, SECTION_SIZE_SIZE = 4;
    private const int SECTION_HEADER_SIZE = SECTION_MAGIC_SIZE + SECTION_SIZE_SIZE;

    private const string FAT_MAGIC = "BTAF";
    private const string FNT_MAGIC = "BTNF";
    private const string DATA_MAGIC = "GMIF";
    private const int FAT_FILE_COUNT_OFFSET = SECTION_HEADER_SIZE, FAT_FILE_COUNT_SIZE = 2;
    private const int FAT_NONE_OFFSET = FAT_FILE_COUNT_OFFSET + FAT_FILE_COUNT_SIZE, FAT_NONE_SIZE = 2;
    private const int FAT_ENTRIES_OFFSET = FAT_NONE_OFFSET + FAT_NONE_SIZE;
    private const int FAT_ENTRY_SIZE = 8;

    private readonly ReadOnlyMemory<byte> _data;
    private readonly ImmutableDictionary<string, RomFile> _filesByPath;
    
    public ImmutableArray<RomFile> Files { get; }

    private Narc(ReadOnlyMemory<byte> data, ImmutableArray<RomFile> files)
    {
        _data = data;
        Files = files;
        _filesByPath = files.ToImmutableDictionary(file => file.Path);
    }

    public static bool IsNarc(ReadOnlySpan<byte> data)
        => data.Length >= MAGIC_SIZE && data.GetString(MAGIC_OFFSET, MAGIC_SIZE) == MAGIC;

    public static Narc Parse(ReadOnlyMemory<byte> data)
    {
        if (!IsNarc(data.Span) && Lz77.IsCompressed(data.Span))
            data = Lz77.Decompress(data.Span);

        var dataSpan = data.Span;
        if (!IsNarc(dataSpan))
            throw new InvalidDataException("Not a NARC (magic must be \"NARC\")");
        if (dataSpan.ReadUInt16(BYTE_ORDER_OFFSET) != BYTE_ORDER_LITTLE_ENDIAN)
            throw new InvalidDataException("NARC is not little-endian");
        if (dataSpan.ReadUInt32(FILE_SIZE_OFFSET) != dataSpan.Length)
            throw new InvalidDataException(
                $"NARC size mismatch: header {dataSpan.ReadUInt32(FILE_SIZE_OFFSET)}, actual {dataSpan.Length}");

        var sections = _ReadSections(
            data, 
            dataSpan.ReadUInt16(HEADER_SIZE_OFFSET),
            dataSpan.ReadUInt16(SECTION_COUNT_OFFSET));
        var fatSection = _GetSection(sections, FAT_MAGIC);
        var fntSection = _GetSection(sections, FNT_MAGIC);
        var dataSection =  _GetSection(sections, DATA_MAGIC);

        var fileCount = fatSection.Span.ReadUInt16(FAT_FILE_COUNT_OFFSET);
        var fat = fatSection.Slice(FAT_ENTRIES_OFFSET, fileCount * FAT_ENTRY_SIZE);
        var fnt = fntSection[SECTION_HEADER_SIZE..];
        var files = NitroFs.ReadFiles(fnt, fat);
        if (files.Length != fileCount)
            throw new InvalidDataException($"{FNT_MAGIC} names {files.Length} files but {FAT_MAGIC} has {fileCount} files");
        
        var fileData = dataSection[SECTION_HEADER_SIZE..];
        if (files.Any(file => (long)file.Offset + file.Size > fileData.Length))
            throw new InvalidDataException($"A {FAT_MAGIC} entry points outside {DATA_MAGIC}");

        return new Narc(fileData, files);
    }

    public ReadOnlyMemory<byte> Read(RomFile file) => _data.ReadSection(file.Offset, file.Size);
    
    public ReadOnlyMemory<byte> Read(string path)
        => _filesByPath.TryGetValue(path, out var file)
            ? Read(file)
            : throw new FileNotFoundException($"Not in NARC: {path}");
    
    private static ImmutableDictionary<string, ReadOnlyMemory<byte>> _ReadSections(
        ReadOnlyMemory<byte> data,
        int headerSize,
        int sectionCount)
    {
        return GetSections()
            .Select(sectionPosition => new KeyValuePair<string, ReadOnlyMemory<byte>>(
                data.Span.GetString(sectionPosition.Offset + SECTION_MAGIC_OFFSET, SECTION_MAGIC_SIZE),
                data.ReadSection((uint)sectionPosition.Offset, sectionPosition.Size)))
            .ToImmutableDictionary();

        IEnumerable<SectionPosition> GetSections()
        {
            var offset = headerSize;
            foreach (var _ in Enumerable.Range(0, sectionCount))
            {
                var size = data.Span.ReadUInt32(offset + SECTION_SIZE_OFFSET);
                yield return new SectionPosition(offset, size);
                offset += checked((int)size);
            }
        }
    }

    private static ReadOnlyMemory<byte> _GetSection(
        ImmutableDictionary<string, ReadOnlyMemory<byte>> sections,
        string magic)
        => sections.TryGetValue(magic, out var section)
            ? section
            : throw new InvalidDataException($"NARC has no {magic} section");
}