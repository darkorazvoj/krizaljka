
namespace Krizaljka.Console;

public static class IdGenerator
{
    private static long _lastId = 0;

    public static long GetNextId()
    {
        return Interlocked.Increment(ref _lastId);
    }
}
