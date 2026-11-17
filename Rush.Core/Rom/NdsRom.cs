using System.Collections.Immutable;

namespace Rush.Core.Rom;

public class NdsRom
{
    private const int FNT_OFFSET_HEADER_OFFSET = 0x40, FNT_OFFSET_HEADER_SIZE = 4;
    private const int FNT_SIZE_HEADER_OFFSET = 0x44, FNT_SIZE_HEADER_SIZE = 4;
    private const int FAT_OFFSET_HEADER_OFFSET = 0x48, FAT_OFFSET_HEADER_SIZE = 4;
    private const int FAT_SIZE_HEADER_OFFSET = 0x4C, FAT_SIZE_HEADER_SIZE = 4;
    
    private readonly ReadOnlyMemory<byte> _data;
    private readonly ImmutableDictionary<string, RomFile> _filesByPath;
    
    public ImmutableArray<RomFile> Files { get; }

    public NdsRom(ReadOnlyMemory<byte> data)
    {
        _data = data;

        var fntOffset = _data.Span.ReadUInt32(FNT_OFFSET_HEADER_OFFSET);
        var fntSize = _data.Span.ReadUInt32(FNT_SIZE_HEADER_OFFSET);
        var fnt = _data.ReadSection(fntOffset, fntSize);
        
        var fatOffset = _data.Span.ReadUInt32(FAT_OFFSET_HEADER_OFFSET);
        var fatSize = _data.Span.ReadUInt32(FAT_SIZE_HEADER_OFFSET);
        var fat = _data.ReadSection(fatOffset, fatSize);

        Files = NitroFs.ReadFiles(fnt, fat);
        _filesByPath = Files.ToImmutableDictionary(file => file.Path);
    }

    public static NdsRom Load(string fileName) => new(File.ReadAllBytes(fileName));
};