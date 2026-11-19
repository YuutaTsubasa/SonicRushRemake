using System.Security.Cryptography;
using Rush.Core.Formats;
using Rush.Core.Graphics;
using Rush.Core.Rom;

if (args.Length is < 2 or > 3)
{
    Console.Error.WriteLine("Usage: Rush.Tools.BbgToPng <rom.nds> <path-in-rom.bbg> [out-dir]");
    return 2;
}

try
{
    var rom = NdsRom.Load(args[0]);
    var bbg = Bbg.Parse(rom.ReadNested(args[1]).Span);
    var image = BbgRenderer.Render(bbg);

    var outputDirectory = args.Length == 3 ? args[2] : "out";
    var outputPath = Path.Combine(outputDirectory, Path.ChangeExtension(args[1], ".png"));
    Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
    File.WriteAllBytes(outputPath, Png.Encode(image).Span);
    
    Console.WriteLine($"{args[1]}: {bbg.Format}, {bbg.WidthInTiles}x{bbg.HeightInTiles} tiles -> {image.Width}x{image.Height} px");
    Console.WriteLine($"Tiles:   {bbg.TileCount} ({bbg.Tiles.Length:N0} bytes)");
    Console.WriteLine($"Palette: {bbg.Palette.Length / 2} colors, base {bbg.PaletteBase}");
    Console.WriteLine($"RGBA SHA256: {Convert.ToHexStringLower(SHA256.HashData(image.Pixels.Span))}");
    Console.WriteLine($"Wrote:   {outputPath}");
    return 0;
}
catch  (Exception ex)
{
    Console.Error.WriteLine($"Error: {ex.Message}");
    return 1;
}
