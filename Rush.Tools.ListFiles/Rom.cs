using System.Collections.Immutable;

namespace Rush.Tools.ListFiles;

public class Rom
{
    private union FntSubTableControl(FntSubTableControl.NameLengthControl, FntSubTableControl.FinishedControl)
    {
        public record struct NameLengthControl(byte Control)
        {
            private const byte DIRECTORY_FLAG = 0x80;
            private const byte NAME_LENGTH_FLAG = 0x7F;
            
            public bool IsDirectory => (Control & DIRECTORY_FLAG) > 0;
            public byte NameLength => (byte)(Control & NAME_LENGTH_FLAG);
        }
        public record struct FinishedControl;
        
        public static FntSubTableControl Parse(byte control)
            => control switch
            {
                0 => new FinishedControl(),
                _ => new NameLengthControl(control)
            };
    }

    private const int FNT_OFFSET_HEADER_OFFSET = 0x40, FNT_OFFSET_HEADER_SIZE = 4;
    private const int FNT_SIZE_HEADER_OFFSET = 0x44, FNT_SIZE_HEADER_SIZE = 4;
    private const int FAT_OFFSET_HEADER_OFFSET = 0x48, FAT_OFFSET_HEADER_SIZE = 4;
    private const int FAT_SIZE_HEADER_OFFSET = 0x4C, FAT_SIZE_HEADER_SIZE = 4;

    private const int FAT_START_OFFSET = 0, FAT_START_SIZE = 4;
    private const int FAT_END_OFFSET = 4, FAT_END_SIZE = 4;
    private const int FAT_ENTRY_SIZE = FAT_START_SIZE + FAT_END_SIZE;
    
    private const int FNT_SUB_TABLE_OFFSET_OFFSET = 0, FNT_SUB_TABLE_OFFSET_SIZE = 4;
    private const int FNT_FIRST_FILE_ID_OFFSET = 4,  FNT_FIRST_FILE_ID_SIZE = 2;
    private const int FNT_PARENT_FILE_ID_OR_DIRECTORY_COUNT_OFFSET = 6, FNT_PARENT_FILE_ID_OR_DIRECTORY_COUNT_SIZE = 2;
    private const int FNT_ENTRY_SIZE = FNT_SUB_TABLE_OFFSET_SIZE + FNT_FIRST_FILE_ID_SIZE + FNT_PARENT_FILE_ID_OR_DIRECTORY_COUNT_SIZE;
    
    private const int DIRECTORY_ID_BASE = 0xF000;
    private const int FNT_SUB_TABLE_CONTROL_OFFSET = 0, FNT_SUB_TABLE_CONTROL_SIZE = 1;
    private const int FNT_SUB_TABLE_NAME_OFFSET = 1;
    private const int FNT_SUB_TABLE_SUB_DIRECTORY_ID_SIZE = 2;
    
    public readonly string FileName;
    private readonly byte[] _data;
    
    public uint FntOffset { get; }
    public uint FntSize { get; }
    public uint FatOffset { get; }
    public uint FatSize { get; }
    
    public ImmutableArray<RomFile> Files { get; }

    public Rom(string fileName)
    {
        FileName = fileName;
        _data = File.ReadAllBytes(fileName);
        
        FntOffset = _data.ReadUInt32(FNT_OFFSET_HEADER_OFFSET);
        FntSize = _data.ReadUInt32(FNT_SIZE_HEADER_OFFSET);
        FatOffset = _data.ReadUInt32(FAT_OFFSET_HEADER_OFFSET);
        FatSize = _data.ReadUInt32(FAT_SIZE_HEADER_OFFSET);
        
        var fnt = _data.ReadSection(FntOffset, FntSize);
        var fat = _data.ReadSection(FatOffset, FatSize);
        Files = _ParseFiles(fnt, fat);
    }

    private static ImmutableArray<RomFile> _ParseFiles(
        ReadOnlySpan<byte> fnt,
        ReadOnlySpan<byte> fat)
    {
        var directoryCount = fnt.ReadUInt16(FNT_PARENT_FILE_ID_OR_DIRECTORY_COUNT_OFFSET);
        return _ParseDirectory(
            DIRECTORY_ID_BASE, 
            string.Empty,
            fat,
            fnt,
            directoryCount,
            new HashSet<ushort>())
            .OrderBy(file => file.Id)
            .ToImmutableArray();
    }

    private static ImmutableArray<RomFile> _ParseDirectory(
        ushort directoryId,
        string path,
        ReadOnlySpan<byte> fat,
        ReadOnlySpan<byte> fnt,
        int directoryCount,
        HashSet<ushort> visitedDirectoryIds)
    {
        var index = directoryId - DIRECTORY_ID_BASE;
        if (index >= directoryCount)
            throw new InvalidDataException($"Invalid directory ID: 0x{directoryId:X4}, which is larger than directoryCount({directoryCount})");

        if (!visitedDirectoryIds.Add(directoryId))
            throw new InvalidDataException($"Directory cycle with directory ID: 0x{directoryId:X4}");

        var entryOffset = index * FNT_ENTRY_SIZE;
        var fntSubTableOffset = checked((int)fnt.ReadUInt32(entryOffset));
        var fileId = fnt.ReadUInt16(entryOffset + FNT_FIRST_FILE_ID_OFFSET);

        var files = ImmutableArray.CreateBuilder<RomFile>();
        while (true)
        {
            var control = FntSubTableControl.Parse(fnt[fntSubTableOffset + FNT_SUB_TABLE_CONTROL_OFFSET]);
            if (control.Value is FntSubTableControl.FinishedControl)
                break;

            var nameOffset = fntSubTableOffset + FNT_SUB_TABLE_NAME_OFFSET;
            var nameLengthControl = (FntSubTableControl.NameLengthControl)control.Value;
            var isDirectory = nameLengthControl.IsDirectory;
            var nameLength = nameLengthControl.NameLength;
            var name = fnt.GetString(nameOffset, nameLength);
            var fullPath = $"{path}/{name}";

            if (isDirectory)
            {
                var childId = fnt.ReadUInt16(nameOffset + nameLength);
                files.AddRange(_ParseDirectory(childId, fullPath, fat, fnt, directoryCount, visitedDirectoryIds));

                fntSubTableOffset += FNT_SUB_TABLE_CONTROL_SIZE + nameLength + FNT_SUB_TABLE_SUB_DIRECTORY_ID_SIZE;
                continue;
            }

            var fatOffset = fileId * FAT_ENTRY_SIZE;
            var start = fat.ReadUInt32(fatOffset);
            var end = fat.ReadUInt32(fatOffset + FAT_END_OFFSET);

            if (end < start)
                throw new InvalidDataException(
                    $"Invalid FAT entry: {fileId}");
            
            files.Add(new RomFile(fileId, fullPath, start, end - start));
            fileId++;
            fntSubTableOffset += FNT_SUB_TABLE_CONTROL_SIZE + nameLength;
        }

        return files.ToImmutableArray();
    }
};