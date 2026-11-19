using System;
using System.Linq;
using System.Runtime.InteropServices.JavaScript;
using Rush.Core;
using Rush.Core.Rom;
using Rush.Core.Timing;
using Rush.Core.Viewer;

namespace Rush.Web;

public static partial class WebApp
{
    private const string DEFAULT_BBG = "bg/sp_fix_base.bbg";
    private const double NANOSECONDS_PER_MILLISECOND = 1_000_000;

    private const string JS_KEY_ARROW_LEFT = "ArrowLeft";
    private const string JS_KEY_ARROW_RIGHT = "ArrowRight";

    [JSImport("setText", "app.host")]
    private static extern void SetText(string selector, string text);
    
    [JSImport("putPixels", "app.host")]
    private static extern void PutPixels(ReadOnlySpan<byte> rgba, int width, int height);
    
    [JSImport("onFrame", "app.host")]
    private static extern JSSubscription OnFrame(Action<double> frame);

    [JSImport("onKey", "app.host")]
    private static extern JSSubscription OnKey(Action<string> key);

    private static BbgGallery? _gallery;
    private static FixedTimestep _timestep = new(Constants.FRAMES_PER_SECOND);
    private static JSSubscription? _frames;
    private static JSSubscription? _keys;
    private static double? _lastTime;
    private static long _frame;
    private static bool _isImageChanged;

    [JSExport("start")]
    public static string Start(byte[] rom)
    {
        Stop();
        try
        {
            _gallery = BbgGallery.Create(new NdsRom(rom), DEFAULT_BBG);
        }
        catch (Exception ex)
        {
            return ex.Message;
        }

        _timestep = new FixedTimestep(Constants.FRAMES_PER_SECOND);
        _lastTime = null;
        _frame = 0;
        _isImageChanged = true;
        _UpdateTitle();
        _keys = OnKey(_OnKey);
        _frames = OnFrame(_OnFrame);
        return string.Empty;
    }

    [JSExport("stop")]
    public static void Stop()
    {
        _frames?.Dispose();
        _frames = null;
        
        _keys?.Dispose();
        _keys = null;
    }

    private static void _UpdateTitle()
    {
        if (_gallery is not null)
            SetText(
                "#title",
                AppHelper.GetTitle(_gallery, _frame));
    }

    private static void _OnKey(string key)
    {
        var direction = key switch
        {
            JS_KEY_ARROW_LEFT => BbgGallery.MoveDirection.Left,
            JS_KEY_ARROW_RIGHT => BbgGallery.MoveDirection.Right,
            _ => (BbgGallery.MoveDirection?)null
        };

        if (direction is not { } move || _gallery is null)
            return;

        _gallery.Move(move);
        _isImageChanged = true;
        _UpdateTitle();
    }

    private static void _OnFrame(double time)
    {
        if (_gallery is null)
            return;

        var elapsed = _lastTime is { } last
            ? (long)((time - last) * NANOSECONDS_PER_MILLISECOND)
            : 0;
        _lastTime = time;
        foreach (var _ in Enumerable.Range(0, _timestep.Advance(elapsed)))
        {
            _frame++;
            if (_frame % Constants.FRAMES_PER_SECOND == 0)
                _UpdateTitle();
        }

        if (!_isImageChanged)
            return;

        var image = _gallery.Image;
        PutPixels(image.Pixels.Span, image.Width, image.Height);
        _isImageChanged = false;
    }
}