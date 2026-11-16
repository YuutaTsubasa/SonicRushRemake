namespace Rush.Tools.ListFiles;

public record struct RomFile(
    ushort Id,
    string Path,
    uint Offset,
    uint Size);