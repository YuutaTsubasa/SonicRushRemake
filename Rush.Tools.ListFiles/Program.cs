using Rush.Core.Rom;

if (args.Length != 1)
{
    Console.Error.WriteLine("Usage: Rush.Tools.ListFiles <rom.nds>");
    return 2;
}

try
{
    var rom = NdsRom.Load(args[0]);
    foreach (var romFile in rom.Files)
    {
        Console.WriteLine($"{romFile.Path}\t{romFile.Size:N0} bytes");
    }

    Console.WriteLine();
    Console.WriteLine($"Total files: {rom.Files.Length}");
    return 0;
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Error: {ex.Message}");
    return 1;
}