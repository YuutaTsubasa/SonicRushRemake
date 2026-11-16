using Rush.Tools.ListFiles;

try
{
    var rom = new Rom(args[0]);
    foreach (var romFile in rom.Files)
    {
        Console.WriteLine($"{romFile.Path}\t{romFile.Size:N0} bytes");
    }

    Console.WriteLine();
    Console.WriteLine($"Total files: {rom.Files.Length}");
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Error: {ex.Message}");
}