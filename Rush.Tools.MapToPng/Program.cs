using System.Diagnostics;
using System.Security.Cryptography;
using Rush.Core.Formats;
using Rush.Core.Graphics;
using Rush.Core.Rom;

const string DEFAULT_OUTPUT_DIRECTORY = "out";

if (args.Length is < 2 or > 3)
{
    Console.Error.WriteLine("Usage: Rush.Tools.MapToPng <rom.nds> <zone, e.g. z11> [out-dir]");
    return 2;
}

try
{
    var rom = NdsRom.Load(args[0]);
    var zone = args[1];
    var stopwatch = Stopwatch.StartNew();
    var map = StageMap.Load(rom, zone);
    var image = StageMapRenderer.Render(map);
    var renderTime = stopwatch.Elapsed;

    var outputDirectory = args.Length == 3 ? args[2] : DEFAULT_OUTPUT_DIRECTORY;
    var outputPath = Path.Combine(outputDirectory, $"{zone}.png");
    Directory.CreateDirectory(outputDirectory);
    File.WriteAllBytes(outputPath, Png.Encode(image).Span);

    Console.WriteLine($"{zone}: {map.Front.Width}x{map.Front.Height} chunks -> {image.Width}x{image.Height} px");
    Console.WriteLine($"Chunks: {map.ChunkCount}, tiles: {map.TileCount}");
    Console.WriteLine(
        $"Load + render: {renderTime.TotalMilliseconds:N0} ms, with PNG: {stopwatch.Elapsed.TotalMilliseconds:N0} ms");
    Console.WriteLine($"RGBA SHA256: {Convert.ToHexStringLower(SHA256.HashData(image.Pixels.Span))}");
    Console.WriteLine($"Wrote: {outputPath}");
    return 0;
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Error: {ex.Message}");
    return 1;
}