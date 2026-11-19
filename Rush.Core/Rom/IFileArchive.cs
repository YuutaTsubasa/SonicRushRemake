using System.Collections.Immutable;

namespace Rush.Core.Rom;

public interface IFileArchive
{
    ImmutableArray<RomFile> Files { get; }
    ReadOnlyMemory<byte> Read(RomFile file);
    ReadOnlyMemory<byte> Read(string path);
}

public static class FileArchiveExtensions
{
    private const string NARC_SEPARATOR = ".narc/";
    
    public static ReadOnlyMemory<byte> ReadNested(this IFileArchive fileArchive, string path)
    {
        var index = path.IndexOf(NARC_SEPARATOR, StringComparison.Ordinal);
        if (index < 0)
            return fileArchive.Read(path);
        
        var narcPathLength = index + NARC_SEPARATOR.Length - 1;
        return fileArchive
            .OpenNarc(path[..narcPathLength])
            .ReadNested(path[(narcPathLength + 1)..]);
    }

    public static Narc OpenNarc(this IFileArchive fileArchive, string path)
        => Narc.Parse(fileArchive.ReadNested(path));
}