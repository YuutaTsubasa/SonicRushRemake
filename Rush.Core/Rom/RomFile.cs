namespace Rush.Core.Rom;

public record struct RomFile(
    ushort Id,
    string Path,
    uint Offset,
    uint Size);