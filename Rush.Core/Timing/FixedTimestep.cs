namespace Rush.Core.Timing;

public sealed class FixedTimestep(
    int frequency, 
    int maxStepsPerAdvance = 5)
{
    private const long NANOSECONDS_PER_SECOND = 1_000_000_000;
    private long _accumulated = 0;

    public int Advance(long elapsedNanoseconds)
    {
        _accumulated += elapsedNanoseconds * frequency;
        var steps = _accumulated / NANOSECONDS_PER_SECOND;
        _accumulated -= steps * NANOSECONDS_PER_SECOND;

        return (int)Math.Min(steps, maxStepsPerAdvance);
    }
}