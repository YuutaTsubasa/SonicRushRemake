using System.Collections.Immutable;

namespace Rush.Core.Rom;

internal static class NitroFs
{
    private union FntEntry(FntEntry.FileEntry, FntEntry.DirectoryEntry)
    {
        public record struct FileEntry(string Name);
        public record struct DirectoryEntry(string Name, ushort Id);
    }
    
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
    
    public static ImmutableArray<RomFile> ReadFiles(
        ReadOnlyMemory<byte> fnt,
        ReadOnlyMemory<byte> fat)
    {
        var directoryCount = fnt.Span.ReadUInt16(FNT_PARENT_FILE_ID_OR_DIRECTORY_COUNT_OFFSET);
        return _ReadDirectory(
                fat,
                fnt,
                directoryCount,
                DIRECTORY_ID_BASE,
                string.Empty,
                ImmutableHashSet.Create<ushort>())
            .OrderBy(file => file.Id)
            .ToImmutableArray();
    }
    
    private static IEnumerable<RomFile> _ReadDirectory(
        ReadOnlyMemory<byte> fat,
        ReadOnlyMemory<byte> fnt,
        int directoryCount,
        ushort directoryId,
        string path,
        ImmutableHashSet<ushort> parentDirectories)
    {
        var index = directoryId - DIRECTORY_ID_BASE;
        if (index < 0 || index >= directoryCount)
            throw new InvalidDataException($"Invalid directory ID: 0x{directoryId:X4}. It is larger than directoryCount({directoryCount})");
        if (parentDirectories.Contains(directoryId))
            throw new InvalidDataException($"Directory cycle with directory ID: 0x{directoryId:X4}");

        var entryOffset = index * FNT_ENTRY_SIZE;
        var fntSubTableOffset = checked((int)fnt.Span.ReadUInt32(entryOffset));
        var firstFileId = fnt.Span.ReadUInt16(entryOffset + FNT_FIRST_FILE_ID_OFFSET);
        var entries = _ReadFntEntries(fnt, fntSubTableOffset).ToImmutableArray();
        var innerParentDirectories = parentDirectories.Add(directoryId);

        var files = entries
            .Select(entry => entry.Value)
            .OfType<FntEntry.FileEntry>()
            .Select((file, i) => _ReadFatEntry(fat, (ushort)(firstFileId + i), _JoinPath(path, file.Name)));

        var subDirectoryFiles = entries
            .Select(entry => entry.Value)
            .OfType<FntEntry.DirectoryEntry>()
            .SelectMany(directory => _ReadDirectory(
                fat, fnt, directoryCount, directory.Id, _JoinPath(path, directory.Name), innerParentDirectories));

        return files.Concat(subDirectoryFiles);
    }

    private static IEnumerable<FntEntry> _ReadFntEntries(
        ReadOnlyMemory<byte> fnt,
        int offset)
    {
        int GetNextOffset(int currentOffset, int nameLength, bool isDirectory)
            => currentOffset + FNT_SUB_TABLE_CONTROL_SIZE + nameLength + (
                isDirectory
                    ? FNT_SUB_TABLE_SUB_DIRECTORY_ID_SIZE
                    : 0);

        var currentFileOffset = offset;
        while (true)
        {
            var control = FntSubTableControl.Parse(fnt.Span[currentFileOffset + FNT_SUB_TABLE_CONTROL_OFFSET]);
            if (control.Value is FntSubTableControl.FinishedControl)
                yield break;

            var nameOffset = currentFileOffset + FNT_SUB_TABLE_NAME_OFFSET;
            var nameLengthControl = (FntSubTableControl.NameLengthControl)(control.Value ??
               throw new InvalidOperationException(
                   $"Can not identify FNT sub table control: offset {currentFileOffset}"));
            var nameLength = nameLengthControl.NameLength;
            var name = fnt.Span.GetString(nameOffset, nameLength);

            var isDirectory = nameLengthControl.IsDirectory;
            yield return isDirectory
                ? new FntEntry.DirectoryEntry(name,
                    fnt.Span.ReadUInt16(currentFileOffset + FNT_SUB_TABLE_CONTROL_SIZE + nameLength))
                : new FntEntry.FileEntry(name);
            currentFileOffset = GetNextOffset(currentFileOffset, nameLength, isDirectory);
        }
    }

    private static RomFile _ReadFatEntry(
        ReadOnlyMemory<byte> fat,
        ushort fileId,
        string path)
    {
        var start = fat.Span.ReadUInt32(fileId * FAT_ENTRY_SIZE);
        var end = fat.Span.ReadUInt32(fileId * FAT_ENTRY_SIZE + FAT_END_OFFSET);
        if (end < start)
            throw new InvalidDataException($"Invalid FAT entry: {fileId}");
        return new RomFile(fileId, path, start, end - start);
    }

    private static string _JoinPath(string path, string name)
        => path.Length == 0 ? name : $"{path}/{name}";
}