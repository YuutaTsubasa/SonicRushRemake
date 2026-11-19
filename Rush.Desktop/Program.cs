using Rush.Core;
using Rush.Core.Graphics;
using Rush.Core.Rom;
using Rush.Core.Timing;
using Rush.Core.Viewer;
using Rush.Desktop;
using SDL;
using static SDL.SDL3;

const int SCREEN_WIDTH = 256, SCREEN_HEIGHT = 192, WINDOW_SCALE = 3;

if (args.Length is < 1 or > 2)
{
    Console.Error.WriteLine("Usage: Rush.Desktop <rom.nds> [path-in-rom.bbg]");
    return 2;
}

try
{
    var gallery = BbgGallery.Create(NdsRom.Load(args[0]), args.Length == 2 ? args[1] : Constants.DEFAULT_BBG);
    _Run(gallery);
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Error: {ex.Message}");
    return 1;
}

static unsafe void _Run(BbgGallery gallery)
{
    Sdl.Check(SDL_Init(SDL_InitFlags.SDL_INIT_VIDEO), "SDL_Init");
    var window = (SDL_Window*)null;
    var renderer = (SDL_Renderer*)null;
    var texture = (SDL_Texture*)null;

    try
    {
        Sdl.Check(
            SDL_CreateWindowAndRenderer(
                "Sonic Rush Remake", 
                SCREEN_WIDTH * WINDOW_SCALE, 
                SCREEN_HEIGHT * WINDOW_SCALE, 
                SDL_WindowFlags.SDL_WINDOW_RESIZABLE, 
                &window, 
                &renderer),
            "SDL_CreateWindowAndRenderer");
        SDL_SetRenderVSync(renderer, 1);

        texture = _UploadImage(renderer, null, gallery.Image);
        var timestep = new FixedTimestep(Constants.FRAMES_PER_SECOND);
        var frame = 0L;
        var lastTicks = SDL_GetTicksNS();
        var isRunning = true;
        
        while (isRunning)
        {
            var sdlEvent = new SDL_Event();
            while (SDL_PollEvent(&sdlEvent))
            {
                switch ((SDL_EventType)sdlEvent.type)
                {
                    case SDL_EventType.SDL_EVENT_QUIT:
                        StopRunning();
                        break;
                    
                    case SDL_EventType.SDL_EVENT_KEY_DOWN:
                        switch (sdlEvent.key.key)
                        {
                            case SDL_Keycode.SDLK_ESCAPE:
                                StopRunning();
                                break;
                            
                            case SDL_Keycode.SDLK_LEFT:
                                gallery.Move(BbgGallery.MoveDirection.Left);
                                UpdateGalleryImage(renderer);
                                break;
                                
                            case SDL_Keycode.SDLK_RIGHT:
                                gallery.Move(BbgGallery.MoveDirection.Right);
                                UpdateGalleryImage(renderer);
                                break;
                        }
                        break;
                }
            }
            
            var now = SDL_GetTicksNS();
            var steps = timestep.Advance((long)(now - lastTicks));
            lastTicks = now;
            foreach (var _ in Enumerable.Range(0, steps))
            {
                ++frame;
                if (frame % Constants.FRAMES_PER_SECOND == 0)
                    SDL_SetWindowTitle(
                        window,
                        AppHelper.GetTitle(gallery, frame));
            }

            SDL_SetRenderDrawColor(renderer, 0, 0, 0, 0xFF);
            SDL_RenderClear(renderer);
            SDL_RenderTexture(renderer, texture, null, null);
            SDL_RenderPresent(renderer);
            if (steps == 0)
                SDL_DelayNS(1_000_000);
            
            void StopRunning()
            {
                isRunning = false;
            }

            void UpdateGalleryImage(SDL_Renderer* renderer)
            {
                texture = _UploadImage(renderer, texture, gallery.Image);
            }
        }
    }
    finally
    {
        if (texture != null)
            SDL_DestroyTexture(texture);
        if (renderer != null)
            SDL_DestroyRenderer(renderer);
        if (window != null)
            SDL_DestroyWindow(window);
        SDL_Quit();
    }
}

return 0;

static unsafe SDL_Texture* _UploadImage(SDL_Renderer* renderer, SDL_Texture* previousTexture, RgbaImage image)
{
    if (previousTexture != null)
        SDL_DestroyTexture(previousTexture);

    var texture = Sdl.Check(
        SDL_CreateTexture(
            renderer,
            SDL_PixelFormat.SDL_PIXELFORMAT_ABGR8888,
            SDL_TextureAccess.SDL_TEXTUREACCESS_STATIC,
            image.Width,
            image.Height),
        "SDL_CreateTexture");
    SDL_SetTextureScaleMode(texture, SDL_ScaleMode.SDL_SCALEMODE_NEAREST);
    fixed (byte* pixels = image.Pixels.Span)
        Sdl.Check(
            SDL_UpdateTexture(texture, null, (IntPtr)pixels, image.Width * RgbaImage.BYTES_PER_PIXEL),
            "SDL_UpdateTexture");

    Sdl.Check(
        SDL_SetRenderLogicalPresentation(
            renderer,
            image.Width,
            image.Height,
            SDL_RendererLogicalPresentation.SDL_LOGICAL_PRESENTATION_INTEGER_SCALE),
        "SDL_SetRenderLogicalPresentation");
    return texture;
}