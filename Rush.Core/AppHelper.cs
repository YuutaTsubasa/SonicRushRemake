using Rush.Core.Viewer;

namespace Rush.Core;

public static class AppHelper
{
    public static string GetTitle(BbgGallery gallery, long frame)
        =>
            $"Sonic Rush Remake - {gallery.Current.Path} ({gallery.Position}/{gallery.Count}) - {frame / Constants.FRAMES_PER_SECOND}s";
}