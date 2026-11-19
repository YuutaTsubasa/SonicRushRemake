using Rush.Core.Rom;

if (args.Length is < 1 or > 2)
{
    Console.Error.WriteLine("Usage: Rush.Tools.ListFiles <rom.nds> [path-in-rom.narc]");
    return 2;
}

try
{
    var rom = NdsRom.Load(args[0]) as IFileArchive;
    var archive = args.Length == 2 ? rom.OpenNarc(args[1]) : rom;
    foreach (var romFile in archive.Files)
    {
        Console.WriteLine($"{romFile.Path}\t{romFile.Size:N0} bytes");
    }

    Console.WriteLine();
    Console.WriteLine($"Total files: {archive.Files.Length}");
    return 0;
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Error: {ex.Message}");
    return 1;
}