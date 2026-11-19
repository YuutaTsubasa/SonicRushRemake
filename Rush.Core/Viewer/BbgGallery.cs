using System.Collections.Immutable;
using Rush.Core.Formats;
using Rush.Core.Graphics;
using Rush.Core.Rom;

namespace Rush.Core.Viewer;

public sealed class BbgGallery
{
    public enum MoveDirection
    {
        Left = -1,
        Right = 1,
    }
    
    private readonly NdsRom _rom;
    private readonly ImmutableArray<RomFile> _romFiles;
    private int _index;
    
    public RomFile Current => _romFiles[_index];
    public RgbaImage Image { get; private set; }
    public int Position => _index + 1;
    public int Count => _romFiles.Length;
    
    private BbgGallery(NdsRom rom, ImmutableArray<RomFile> romFiles, int index, RgbaImage image)
    {
        _rom = rom;
        _romFiles = romFiles;
        _index = index;
        Image = image;
    }

    public static BbgGallery Create(NdsRom rom, string startPath)
    {
        var romFiles = rom.Files.Where(file => file.Path.EndsWith(".bbg")).ToImmutableArray();
        var index = romFiles.Select(file => file.Path).ToList().IndexOf(startPath);
        if (index < 0)
            throw new FileNotFoundException($"Not a .bbg in NitroFS: {startPath}");

        return new BbgGallery(
            rom, 
            romFiles, 
            index, 
            _Render(rom, romFiles[index]) ?? throw new InvalidDataException($"Not a background: {startPath}"));
    }

    public void Move(MoveDirection direction)
    {
        foreach (var step in Enumerable.Range(1, _romFiles.Length - 1))
        {
            var index = ((_index + (int)direction * step) % _romFiles.Length + _romFiles.Length) % _romFiles.Length;
            if (_Render(_rom, _romFiles[index]) is not { } image)
                continue;

            _index = index;
            Image = image;
            return;
        }
    }

    private static RgbaImage? _Render(NdsRom rom, RomFile romFile)
    {
        try
        {
            return BbgRenderer.Render(Bbg.Parse(rom.Read(romFile).Span));
        }
        catch (InvalidDataException)
        {
            return null;
        }
    }
}