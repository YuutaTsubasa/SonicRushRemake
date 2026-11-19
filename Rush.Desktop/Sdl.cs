using SDL;
using static SDL.SDL3;

namespace Rush.Desktop;

internal static unsafe class Sdl
{
    public static void Check(SDLBool ok, string what)
    {
        if (!ok)
            throw new InvalidOperationException($"{what} failed: {SDL_GetError()}");
    }

    public static T* Check<T>(T* pointer, string what) where T : unmanaged
        => pointer != null ? pointer : throw new InvalidOperationException($"{what} failed: {SDL_GetError()}");
}