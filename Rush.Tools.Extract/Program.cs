using System.Security.Cryptography;
using System.Text;
using Rush.Core.Compression;
using Rush.Core.Rom;

const string DEFAULT_OUTPUT_DIRECTORY = "out";

if (args.Length is < 2 or > 3)
{
    Console.Error.WriteLine("Usage: Rush.Tools.Extract <rom.nds> <path-in-rom> [out-dir]");
    return;
}

try
{
    var rom = NdsRom.Load(args[0]);
    var raw = rom.Read(args[1]).Span;
    var data = Lz77.IsCompressed(raw) ? Lz77.Decompress(raw) : raw.ToArray();

    var outDirectory = args.Length == 3 ? args[2] : DEFAULT_OUTPUT_DIRECTORY;
    var outPath = Path.Combine(outDirectory, args[1]);
    Directory.CreateDirectory(Path.GetDirectoryName(outPath)!);
    File.WriteAllBytes(outPath, data);

    Console.WriteLine($"{args[1]}: {raw.Length:N0} -> {data.Length:N0} bytes");
    Console.WriteLine($"Magic: {Encoding.ASCII.GetString(data, 0, 4)}");
    Console.WriteLine($"SHA256: {Convert.ToHexStringLower(SHA256.HashData(data))}");
    Console.WriteLine($"Wrote: {outPath}");
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Error: {ex.Message}");
}
